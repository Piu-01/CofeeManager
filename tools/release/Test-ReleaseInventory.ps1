#requires -Version 7.0
[CmdletBinding()]param([Parameter(Mandatory=$true)][string]$Package,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packagePath=(Resolve-Path -LiteralPath $Package).Path
if(Test-Path -LiteralPath $Output){throw 'Audit output phải là file mới.'}
$issues=@();$bypass='TemporaryTestLogin|TemporaryTestTarget|Start-TestLogin|Grant-TestLoginMarker|CoffeeManager\.Test|Bỏ qua đăng nhập'
# Quét source sản phẩm cùng toàn bộ launcher hiện hành, không loại cả tools để tránh bỏ sót.
$sourceFiles=@(Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'tools') -Recurse -File | Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.ps1','.json','.cmd','.bat'})
foreach($file in $sourceFiles){
 if($file.FullName -ceq $PSCommandPath){continue} # Danh sách cấm của chính audit không phải đường bypass.
 if([IO.File]::ReadAllText($file.FullName) -match $bypass){$issues+=@{Kind='SourceBypassToken';File=$file.FullName.Substring($root.Length+1)}}
}
$files=@(Get-ChildItem -LiteralPath $packagePath -Recurse -File)
foreach($file in $files){
 $relative=$file.FullName.Substring($packagePath.Length+1).Replace('\','/')
 if($relative -match '(^|/)(bin|obj|\.git|\.vs|config/local)/' -or $file.Extension -in '.bak','.trn','.mdf','.ldf','.dpapi','.pfx','.p12' -or $file.Name -match '(appsettings.*\.local\.json|\.env.*|launchSettings\.json)$'){$issues+=@{Kind='ForbiddenFile';File=$relative}}
 if($file.Extension -in '.json','.md','.ps1','.sql','.config','.cmd','.bat'){
  $text=[IO.File]::ReadAllText($file.FullName)
  if($text -match '(?i)[A-Z]:[\\/](Users|Workspace)[\\/]' -or $text -match '(?i)(Password|Pwd)\s*=\s*[^;\r\n<>]+;'){$issues+=@{Kind='PrivatePathOrCredential';File=$relative}}
 }
}
$types=@{}
foreach($assembly in @('CafeManager.WinForms','CafeManager.Business','CafeManager.DataAccess')){
 foreach($configuration in @('Debug','Release','publish')){
  $path=if($configuration -eq 'publish'){Join-Path $packagePath "app/$assembly.dll"}else{Join-Path $root "src/$assembly/bin/$configuration/net10.0$(if($assembly -eq 'CafeManager.WinForms'){'-windows'})/$assembly.dll"}
  if(-not (Test-Path -LiteralPath $path)){$issues+=@{Kind='MissingAssembly';File="$configuration/$assembly"};continue}
  $stream=[IO.File]::OpenRead($path)
  try{
   $pe=[System.Reflection.PortableExecutable.PEReader]::new($stream)
   try{
    $reader=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    if($configuration -eq 'publish'){
     foreach($entry in $pe.ReadDebugDirectory()){
      if($entry.Type -eq [System.Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView -and $pe.ReadCodeViewDebugDirectoryData($entry).Path -match '(?i)[A-Z]:[\\/](Users|Workspace)[\\/]'){$issues+=@{Kind='PrivatePdbPath';File="$configuration/$assembly"}}
     }
    }
    $names=@(foreach($handle in $reader.TypeDefinitions){$definition=$reader.GetTypeDefinition($handle);$reader.GetString($definition.Name)})
    $types["$configuration/$assembly"]=@{Hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;TypeCount=$names.Count}
    if(@($names|Where-Object {$_ -match 'TemporaryTest(Login|Target)'}).Count){$issues+=@{Kind='BypassType';File="$configuration/$assembly"}}
    $positive=if($assembly -eq 'CafeManager.WinForms'){'LoginForm'}elseif($assembly -eq 'CafeManager.Business'){'AuthService'}else{'AuthRepository'}
    if($positive -cnotin $names){$issues+=@{Kind='MissingPositiveControl';File="$configuration/$assembly"}}
   }finally{$pe.Dispose()}
  }finally{$stream.Dispose()}
 }
}
# Không xuất nội dung match để tránh lộ bí mật; audit tự động chỉ là một phần kiểm bản cuối.
$status=if($issues.Count){'Failed'}else{'Succeeded'}
[ordered]@{Status=$status;SourceFiles=$sourceFiles.Count;PackageFiles=$files.Count;Assemblies=$types;Issues=$issues;Native='NOT RUN: không thay kiểm login/logout/phiên/quyền với biến cũ'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $Output -Encoding utf8
if($issues.Count){throw "Release inventory có$($issues.Count) mục cần xem; xem audit JSON (không chứa match secret)."}
[pscustomobject]@{Status=$status;Output=$Output}
