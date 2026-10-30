$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $PSScriptRoot 'Empty';New-Item -ItemType Directory -Path $out -Force|Out-Null
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$db="CoffeeManager_Test_G04_${runId}_clean"
$m=[ordered]@{RunId=$runId;Database=$db;Created=@();Cleaned=@();Retained=@();Status='Running';Cases=@()}
function Save {$m|ConvertTo-Json -Depth 10|Set-Content "$out/manifest.json" -Encoding utf8}
$c=$null;Save
try{
 $c=Open-TestConnection 'master';if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'")-ne0){throw 'Exists'};Run-TestSql $c "CREATE DATABASE [$db]";$m.Created+=$db;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v-le16;$v++){$f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql'-f$v));& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $f[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/migrations.jsonl"}
 if((Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.HOADON')-ne0-or(Read-TestScalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations')-ne16){throw 'Empty upgrade invariant'}
 $m.Cases+=@{Id='Clean baseline001-016 no seed/no invoice mutation';Status='PASS'};Save
 $rerun=& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version 16 -File "$root/database/migrations/016_payment_guard_point_locks.sql"
 if($rerun.Status-ne'AlreadyApplied'){throw 'Rerun not recognized'};$m.Cases+=@{Id='016 exact checksum rerun';Status='PASS';Actual=$rerun.Status}
 $definitionBefore=[string](Read-TestScalar $c "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TR_CTTT_KiemTra'))")
 $observed=$false;try{& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version 16 -File "$PSScriptRoot/checksum-mismatch.sql"|Out-Null}catch{if($_.Exception.Message-notlike'*different checksum*'){throw};$observed=$true;$m['ChecksumRejectMessage']=$_.Exception.Message}
 if(-not$observed-or$definitionBefore-cne[string](Read-TestScalar $c "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TR_CTTT_KiemTra'))")){throw 'Checksum mismatch not protected'}
 $m.Cases+=@{Id='016 changed checksum rejected with trigger unchanged';Status='PASS'};$m.Status='Succeeded'
}catch{$m.Status='Failed';$m['Error']=$_.Exception.ToString()}
finally{
 if($c){$c.Dispose()};if($m.Created.Count){$d=$null;try{$d=Open-TestConnection $db;if((Read-TestScalar $d 'SELECT RunId FROM dbo.G04TestMarker')-cne$runId-or$db-notmatch'^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_clean$'){throw 'Ownership mismatch'};$d.ChangeDatabase('master');Run-TestSql $d "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]";$m.Cleaned+=$db}catch{$m.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($d){$d.Dispose()}}};Save;Write-Output "Empty/rerun/checksum: $($m.Status),cleaned=$($m.Cleaned.Count),retained=$($m.Retained.Count)"
}
