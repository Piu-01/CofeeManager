$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$setup=Get-Content (Join-Path $PSScriptRoot 'setup.json') -Raw | ConvertFrom-Json
$demo=$setup.Demo
if($demo -notmatch '^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Invalid demo name'}
$c=Open-TestConnection 'master'
try{
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$demo'") -ne 0){
  Run-TestSql $c "ALTER DATABASE [$demo] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$demo]"
 }
 Run-TestSql $c "CREATE DATABASE [$demo]"
 $c.ChangeDatabase($demo)
 Run-TestSql $c "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$($setup.RunId)')"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([IO.File]::ReadAllText("$root/database/tests/g07/dashboard-analytics-fixture.sql"))
 for($v=1;$v -le 28;$v++){
  $file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw "Migration $v missing"}
  & "$root/tools/database/Apply-Migration.ps1" -Database $demo -Version $v -File $file[0].FullName | Out-Null
 }
 $setup=$setup|Add-Member -NotePropertyName DemoBills -NotePropertyValue (Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON") -Force -PassThru
 $setup=$setup|Add-Member -NotePropertyName DemoVersion -NotePropertyValue (Read-TestScalar $c "SELECT MAX(Version) FROM dbo.SchemaMigrations") -Force -PassThru
 $setup|ConvertTo-Json -Depth 6|Set-Content (Join-Path $PSScriptRoot 'setup.json') -Encoding utf8
 Write-Output "demo reset: $demo version=$($setup.DemoVersion) bills=$($setup.DemoBills)"
}finally{$c.Dispose()}

