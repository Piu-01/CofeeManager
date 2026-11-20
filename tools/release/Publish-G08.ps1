#requires -Version 7.0
[CmdletBinding()]param([string]$RunId=((Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)))
$ErrorActionPreference='Stop'
if($RunId -cnotmatch '^\d{8}_\d{6}_[a-f0-9]{8}$'){throw 'Invalid release runID.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$releaseRoot=[IO.Path]::GetFullPath((Join-Path $root 'artifacts/g08'))
$output=[IO.Path]::GetFullPath((Join-Path $releaseRoot $RunId))
if(-not $output.StartsWith($releaseRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Output ngoài release root.'}
if(Test-Path -LiteralPath $output){throw 'Không ghi đè release cũ.'}
$package=Join-Path $output 'package';$app=Join-Path $package 'app'
New-Item -ItemType Directory -Path $app -Force|Out-Null
$manifest=[ordered]@{RunId=$RunId;Status='BuildingCandidate';RID='win-x64';SelfContained=$false;Framework='net10.0-windows';SchemaVersion=27;SDK=(& dotnet --version);OS=[Environment]::OSVersion.ToString();CreatedAt=[DateTime]::UtcNow.ToString('o');Acceptance='NOT RUN; candidate chỉ là đầu ra kỹ thuật';Files=@()}
function Save-ReleaseManifest {$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'release-manifest.json') -Encoding utf8}
function Copy-ReleaseFile([string]$Relative,[string]$Target) {
 $source=Join-Path $root $Relative
 if(-not (Test-Path -LiteralPath $source -PathType Leaf)){throw "Thiếu file allowlist:$Relative"}
 $destination=Join-Path $package $Target
 New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force|Out-Null
 Copy-Item -LiteralPath $source -Destination $destination
}
try{
 Save-ReleaseManifest
 foreach($configuration in @('Debug','Release')){
  & dotnet build (Join-Path $root 'CafeManager.slnx') -c $configuration -m:1 -nr:false -v minimal *> (Join-Path $output "build-$configuration.log")
  if($LASTEXITCODE -ne 0){throw "Build $configuration failed."}
 }
 & dotnet publish (Join-Path $root 'src/CafeManager.WinForms/CafeManager.WinForms.csproj') -c Release -r win-x64 --self-contained false -m:1 -nr:false "-p:PathMap=$root=/_/CoffeeManager" -o $app -v minimal *> (Join-Path $output 'publish.log')
 if($LASTEXITCODE -ne 0){throw 'Publish failed.'}
 foreach($relative in @('DB_Schema.sql','Insert_Data.sql')){Copy-ReleaseFile "database/$relative" "database/$relative"}
 for($v=1;$v -le 27;$v++){
  $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v))
  if($migration.Count -ne 1){throw "Migration thiếu/trùng:$v"}
  Copy-ReleaseFile "database/migrations/$($migration[0].Name)" "database/migrations/$($migration[0].Name)"
 }
 foreach($name in @('provision-app-user.sql','provision-setup-user.sql')){Copy-ReleaseFile "database/security/$name" "database/security/$name"}
 foreach($name in @('OperationsTools.ps1','Install-Database.ps1','Apply-Migration.ps1','Backup-Database.ps1','Restore-TestDatabase.ps1')){Copy-ReleaseFile "tools/database/$name" "tools/database/$name"}
 foreach($name in @('install.md','backup-restore.md','security.md','demo.md','release.md')){Copy-ReleaseFile "docs/operations/$name" "docs/operations/$name"}
 Copy-ReleaseFile 'docs/operations/release.md' 'README.md'
 Copy-ReleaseFile 'docs/tasks/g08-van-hanh-nghiem-thu-ban-giao/report.md' 'verification/G08-report.md'
 # Báo cáo trong package độc lập: đường link hồ sơ raw chỉ có trong source, không hứa có file không đóng gói.
 $summaryPath=Join-Path $package 'verification/G08-report.md'
 $summary=[IO.File]::ReadAllText($summaryPath)
 [IO.File]::WriteAllText($summaryPath,[regex]::Replace($summary,'\[([^\]]+)\]\(([^)]+)\)','$1 (xem hồ sơ tương ứng trong source)'))
 foreach($file in Get-ChildItem -LiteralPath $app -Recurse -File -Filter '*.pdb'){Remove-Item -LiteralPath $file.FullName} # Không phát hành symbol chứa đường dẫn source máy tác giả.
 & (Join-Path $PSScriptRoot 'Test-ReleaseInventory.ps1') -Package $package -Output (Join-Path $output 'inventory-audit.json')|Out-Null
 $runtime=Get-Content -LiteralPath (Join-Path $app 'CafeManager.WinForms.runtimeconfig.json') -Raw|ConvertFrom-Json
 $manifest['RuntimeFrameworks']=$runtime.runtimeOptions.frameworks
 $manifest['InstalledRuntimes']=@(& dotnet --list-runtimes | ForEach-Object {$_ -replace '\s+\[.*\]$',''})
 $manifest['SourceHashes']=@(Get-ChildItem (Join-Path $root 'src') -Recurse -File | Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.csproj','.resx'}|Sort-Object FullName|ForEach-Object {@{Path=$_.FullName.Substring($root.Length+1).Replace('\','/');SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
 $manifest.Files=@(Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName | ForEach-Object {@{Path=$_.FullName.Substring($package.Length+1).Replace('\','/');SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;Bytes=$_.Length}})
 $manifest.Status='CandidateBuilt';Save-ReleaseManifest
 Copy-Item -LiteralPath (Join-Path $output 'release-manifest.json') -Destination (Join-Path $package 'package-manifest.json')
 Compress-Archive -LiteralPath $package -DestinationPath (Join-Path $output 'candidate.zip')
 $manifest['ArchiveHash']=(Get-FileHash -LiteralPath (Join-Path $output 'candidate.zip') -Algorithm SHA256).Hash
 Save-ReleaseManifest
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;Save-ReleaseManifest;throw}
[pscustomobject]@{Status=$manifest.Status;Output=$output;Archive='candidate.zip';Acceptance='NOT RUN'}
