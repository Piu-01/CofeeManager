$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_G06_${runId}_demo"
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database=$name;Kind='Extended acceptance; fixed product source';Status='Preparing';Created=@();Cleaned=@();Retained=@()}
function Save { $manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'manifest.json') -Encoding utf8 }
function Snapshot {Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests'),(Join-Path $root 'tools') -Recurse -File | Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj','.ps1'}|Sort-Object FullName|ForEach-Object{[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}}
$before=@(Snapshot);$before|Export-Csv (Join-Path $PSScriptRoot 'source-before.csv') -NoTypeInformation
Save
try {
 $c=Open-TestConnection 'master'
 try {
  if(Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'"){throw 'DB already exists'}
  Run-TestSql $c "CREATE DATABASE [$name];";$manifest.Created+=@{Name=$name;Marker=$runId};Save
  $c.ChangeDatabase($name);Run-TestSql $c "CREATE TABLE dbo.G06TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G06TestMarker VALUES('$runId');"
  Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
  Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 }finally{$c.Dispose()}
 for($v=1;$v -le 24;$v++){
  $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw 'Missing migration'}
  & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version $v -File $file[0].FullName|ConvertTo-Json -Compress|Add-Content (Join-Path $PSScriptRoot 'migrations.jsonl')
 }
 $manifest.Status='Running';Save
 & dotnet run --no-build --project (Join-Path $PSScriptRoot 'Acceptance.csproj') -- $name $runId (Join-Path $PSScriptRoot 'results.json') *> (Join-Path $PSScriptRoot 'run.log')
 if($LASTEXITCODE -ne 0){throw 'Extended harness failed; see run.log'}
 $r=Get-Content (Join-Path $PSScriptRoot 'results.json') -Raw|ConvertFrom-Json
 $manifest['PassSubcases']=@($r.Cases|Where-Object Status -eq PASS).Count;$manifest['FailSubcases']=@($r.Cases|Where-Object Status -eq FAIL).Count
 $manifest.Status='Completed'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString()}
finally {
 $after=@(Snapshot);$after|Export-Csv (Join-Path $PSScriptRoot 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 foreach($entry in $manifest.Created){$cleanup=$null;try{
  if($entry.Name -notmatch '^CoffeeManager_Test_G06_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Cleanup allowlist mismatch'}
  $cleanup=Open-TestConnection $entry.Name;if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G06TestMarker') -cne $runId){throw 'Marker mismatch'}
  $cleanup.ChangeDatabase('master');$target=$entry.Name;Run-TestSql $cleanup "ALTER DATABASE [$target] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$target];";$manifest.Cleaned+=$target
 }catch{$manifest.Retained+=@{Name=$entry.Name;Reason=$_.Exception.Message}}finally{if($null -ne $cleanup){$cleanup.Dispose()}}}
 Save;$manifest|ConvertTo-Json -Depth 4
}
