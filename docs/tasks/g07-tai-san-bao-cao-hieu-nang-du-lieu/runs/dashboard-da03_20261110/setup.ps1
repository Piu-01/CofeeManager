$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$id=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$demo="CoffeeManager_Test_G07_${id}_demo"
$race="CoffeeManager_Test_G07_${id}_race"
$source='CoffeeManager_Test_G07_20261110_102810_0b67b365_demo'
$backup=Join-Path $PSScriptRoot "$race.bak"
$manifest=[ordered]@{RunId=$id;Demo=$demo;Race=$race;RaceSource=$source;SourceVersion=28;Status='Starting';Created=@();Retained=@()}
function Save{$manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $PSScriptRoot 'setup.json') -Encoding utf8}
Save
function Apply-UpTo($database,$last){
 for($v=1;$v -le $last;$v++){
  $file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw "Migration $v missing"}
  & "$root/tools/database/Apply-Migration.ps1" -Database $database -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $PSScriptRoot "$database.migrations.jsonl")
 }
}
$c=Open-TestConnection 'master'
try{
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name IN(N'$demo',N'$race')") -ne 0){throw 'Target exists'}
 # Demo 028: schema + seed + fixture, giống runner DashboardAnalytics.
 Run-TestSql $c "CREATE DATABASE [$demo]";$manifest.Created+=$demo;Save
 $c.ChangeDatabase($demo)
 Run-TestSql $c "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$id')"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([IO.File]::ReadAllText("$root/database/tests/g07/dashboard-analytics-fixture.sql"))
 Apply-UpTo $demo 28
 $c.ChangeDatabase('master')
 # Race: bản sao dataset 4.899 hóa đơn SP999 đang ở 028.
 $c.ChangeDatabase('master')
 Run-TestSql $c "BACKUP DATABASE [$source] TO DISK=N'$backup' WITH COPY_ONLY,CHECKSUM,INIT"
 $paths=Read-TestScalar $c "SELECT CONCAT(CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')),'$race.mdf','|',CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath')),'$race_log.ldf')"
 $mdf,$ldf=$paths.Split('|')
 Run-TestSql $c @"
DECLARE @d sysname,@l sysname;
SELECT @d=name FROM (SELECT name,type FROM sys.master_files WHERE database_id=DB_ID(N'$source')) f WHERE type=0;
SELECT @l=name FROM (SELECT name,type FROM sys.master_files WHERE database_id=DB_ID(N'$source')) f WHERE type=1;
EXEC(N'RESTORE DATABASE [$race] FROM DISK=N''$backup'' WITH MOVE N'''+@d+N''' TO N''$mdf'',MOVE N'''+@l+N''' TO N''$ldf'',RECOVERY');
"@
 $manifest.Retained+=$race;Save
 $c.ChangeDatabase($race)
 Run-TestSql $c "SET NOCOUNT ON; DELETE dbo.G07TestMarker;INSERT dbo.G07TestMarker VALUES('$id')"
 $manifest.RaceVersion=Read-TestScalar $c "SELECT MAX(Version) FROM dbo.SchemaMigrations"
 $manifest.RaceBills=Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON"
 $c.ChangeDatabase('master')
 $probe=Open-TestConnection $demo;try{$manifest.DemoBills=Read-TestScalar $probe "SELECT COUNT(*) FROM dbo.HOADON"}finally{$probe.Dispose()}
 $manifest.Status='Ready'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString();throw}
finally{$c.Dispose();Save}
Write-Output "setup: $($manifest.Status); demo=$demo; race=$race; raceBills=$($manifest.RaceBills)"
