[CmdletBinding()]param([switch]$Technical,[switch]$RunAcceptance,[ValidateRange(25,27)][int]$ThroughVersion=26,[switch]$TypedSmoke,[switch]$Regression)
$ErrorActionPreference='Stop'
if(($Technical -and $RunAcceptance) -or (-not $Technical -and -not $RunAcceptance)){throw 'Chọn Technical hoặc RunAcceptance. Chỉ dùng RunAcceptance sau khi đợt test G07 được duyệt.'}
if($RunAcceptance){$TypedSmoke=$true}
if($Regression -and (-not $RunAcceptance -or $ThroughVersion -ne 27)){throw 'Regression G07-B01 cần RunAcceptance và schema027.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$phase=if($Regression){"regression"}elseif($RunAcceptance){"acceptance"}else{"technical"}
$output=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/${phase}_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='Technical migration/dependency smoke, not acceptance';Server='localhost';ThroughVersion=$ThroughVersion;Created=@();Cleaned=@();Retained=@();Status='Running'}
if($RunAcceptance){$manifest.Kind='Acceptance SQL/unit subset; native/concurrency/performance recorded separately';$manifest['Fixtures']=@()}
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests'),(Join-Path $root 'tools') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj','.ps1' } | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}
}
$sourceBefore=@(Source-Snapshot);$sourceBefore|Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
function Save-Manifest {$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $output 'manifest.json') -Encoding utf8}
function Business-Snapshot($connection) {
 return [string](Read-TestScalar $connection "SELECT (SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Inventory,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,(SELECT MaTC,SoTien FROM dbo.PHIEUTHUCHI ORDER BY MaTC FOR JSON PATH) Finance,(SELECT MaCN,TienNo FROM dbo.CONGNO_NCC ORDER BY MaCN FOR JSON PATH) Debts,(SELECT MaBL,ThucLinh FROM dbo.BANGLUONG ORDER BY MaBL FOR JSON PATH) Payroll FOR JSON PATH)")
}
Save-Manifest
try {
 if($TypedSmoke){
  & dotnet build (Join-Path $root 'tools/CafeManager.G07Probe') -c Debug -v minimal *> (Join-Path $output 'build.log')
  if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
 }
 if($RunAcceptance){
  & dotnet test (Join-Path $root 'tests/CafeManager.Tests') -c Debug --filter 'FullyQualifiedName~CafeManager.Tests.Assets.|FullyQualifiedName~CafeManager.Tests.Reports.' --logger 'trx;LogFileName=g07-unit.trx' --results-directory $output *> (Join-Path $output 'unit.log')
  $manifest['UnitExitCode']=$LASTEXITCODE
 }
 foreach($mode in @('clean','demo')) {
  $name="CoffeeManager_Test_G07_${runId}_$mode"; $connection=Open-TestConnection 'master'
  try {
   if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target exists'}
   Run-TestSql $connection "CREATE DATABASE [$name]"
   $manifest.Created+=@{Database=$name;Marker=$runId};Save-Manifest
   $connection.ChangeDatabase($name)
   Run-TestSql $connection "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$runId')"
   Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
   if($mode -eq 'demo'){Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))}
   if($RunAcceptance -and $mode -eq 'demo'){Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root 'database/tests/g07/historical-seed.sql')))}
   $before=Business-Snapshot $connection
   for($version=1;$version -le $ThroughVersion;$version++) {
    $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
    if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
    & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   }
   if($before -cne (Business-Snapshot $connection)){throw 'Migration changed business balances'}
   $manifest["${mode}BalancesUnchanged"]=$true
   $last=Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $ThroughVersion)
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $ThroughVersion -File $last.FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   if($TypedSmoke -and $mode -eq 'demo'){
    $probeArguments=@($name,$runId,(Join-Path $output 'typed-smoke.json'),[string]$ThroughVersion)
    if($RunAcceptance){$probeArguments+='acceptance'}
    & (Join-Path $root 'tools/CafeManager.G07Probe/bin/Debug/net10.0-windows/CafeManager.G07Probe.exe') @probeArguments *> (Join-Path $output 'typed-smoke.log')
    if($LASTEXITCODE -ne 0){throw 'Typed dependency smoke failed'}
    if($RunAcceptance){$typed=Get-Content (Join-Path $output 'typed-smoke.json') -Raw|ConvertFrom-Json;$manifest['Extended']=$typed.AcceptanceSummary;if($ThroughVersion -ge 27){$manifest['AssetLockRegression']=$typed.AssetLockRegression}}
    if($RunAcceptance){foreach($fixture in Get-ChildItem (Join-Path $root 'database/tests/g07') -Filter 't*.sql'|Sort-Object Name){
     try{Run-TestSql $connection ([IO.File]::ReadAllText($fixture.FullName));$manifest.Fixtures+=@{Name=$fixture.Name;Status='Succeeded'}}
     catch{$manifest.Fixtures+=@{Name=$fixture.Name;Status='Failed';Error=$_.Exception.Message}}
    }}
   }
  } finally {$connection.Dispose()}
 }
 $manifest.Status='Succeeded'
 if($RunAcceptance -and ($manifest.UnitExitCode -ne 0 -or $manifest.Extended.Failed -gt 0 -or $manifest.Extended.Blocked -gt 0 -or @($manifest.Fixtures|Where-Object Status -eq 'Failed').Count -gt 0)){$manifest.Status='Failed'}
 if($RunAcceptance -and $ThroughVersion -ge 27 -and ($null -eq $manifest.AssetLockRegression -or $manifest.AssetLockRegression.Failed -gt 0 -or $manifest.AssetLockRegression.Blocked -gt 0)){$manifest.Status='Failed'}
} catch {$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString()}
finally {
 $after=@(Source-Snapshot);$after|Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $sourceBefore $after -Property Path,Hash).Count -eq 0)
 if(-not $manifest.SourceUnchanged){$manifest.Status='Failed'}
 foreach($item in $manifest.Created) {
  $cleanup=$null
  try {
   $name=$item.Database
   if($name -notmatch '^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$'){throw 'Invalid cleanup name'}
   $cleanup=Open-TestConnection $name
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G07TestMarker') -cne $runId){throw 'Marker mismatch'}
   $cleanup.ChangeDatabase('master');Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]"
   $manifest.Cleaned+=$name
  } catch {$manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message}}
  finally {if($null -ne $cleanup){$cleanup.Dispose()}}
 }
 if($manifest.Retained.Count){$manifest.Status='Failed'}
 Save-Manifest;Write-Output "G07 technical: $output; $($manifest.Status); created=$($manifest.Created.Count)/cleaned=$($manifest.Cleaned.Count)/retained=$($manifest.Retained.Count)"
}
if($manifest.Status -ne 'Succeeded'){throw "Technical check failed: $output/manifest.json"}


