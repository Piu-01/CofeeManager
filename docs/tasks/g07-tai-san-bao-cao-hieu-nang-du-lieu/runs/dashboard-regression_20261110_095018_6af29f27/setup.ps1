$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$id=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$database="CoffeeManager_Test_G07_${id}_demo"
$manifest=[ordered]@{Database=$database;Marker=$id;Created=$false;Status='Starting';Retained=$true}
$manifest|ConvertTo-Json|Set-Content "$PSScriptRoot/manifest.json"
$c=Open-TestConnection 'master'
try {
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$database'") -ne 0){throw 'Exists'}
 Run-TestSql $c "CREATE DATABASE [$database]";$manifest.Created=$true;$manifest|ConvertTo-Json|Set-Content "$PSScriptRoot/manifest.json"
 $c.ChangeDatabase($database);Run-TestSql $c "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$id')"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v -le 28;$v++){
  $file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw 'Migration missing'}
  & "$root/tools/database/Apply-Migration.ps1" -Database $database -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content "$PSScriptRoot/migrations.jsonl"
 }
 $manifest.Status='Ready'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString();throw}
finally{$c.Dispose();$manifest|ConvertTo-Json|Set-Content "$PSScriptRoot/manifest.json"}
