$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$name="CoffeeManager_Test_G06_${runId}_demo"
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database=$name;Status='Preparing';Created=@();Cleaned=@();Retained=@()}
function Save{$manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $PSScriptRoot 'native-manifest.json') -Encoding utf8}
Save
$c=Open-TestConnection 'master'
try {
 if(Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'"){throw 'DB exists'}
 Run-TestSql $c "CREATE DATABASE [$name];";$manifest.Created+=@{Database=$name;Marker=$runId};Save;$c.ChangeDatabase($name)
 Run-TestSql $c "CREATE TABLE dbo.G06TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G06TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v -le 24;$v++){$file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));& (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version $v -File $file[0].FullName|ConvertTo-Json -Compress|Add-Content (Join-Path $PSScriptRoot 'native-migrations.jsonl')}
 $exe=Join-Path $PSScriptRoot 'bin/Debug/net10.0-windows/G06NativeAcceptance.exe'
 $process=Start-Process -FilePath $exe -ArgumentList @($name,$runId,(Join-Path $PSScriptRoot 'native-state.json')) -PassThru -WindowStyle Hidden
 $manifest['ProcessId']=$process.Id;$manifest['Executable']=$exe;$manifest.Status='ReadyForNative';$manifest.Retained=@{Database=$name;Reason='Active native acceptance; cleanup after app closes'};Save
}finally{$c.Dispose()}
$manifest|ConvertTo-Json -Depth 6
