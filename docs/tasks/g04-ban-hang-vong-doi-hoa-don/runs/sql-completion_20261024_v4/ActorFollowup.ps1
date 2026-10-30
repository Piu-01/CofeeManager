$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=$PSScriptRoot;$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$db="CoffeeManager_Test_G04_${runId}_demo"
$m=[ordered]@{RunId=$runId;Database=$db;Case='T14-K02 actor observation after captured transaction state';Status='Running';Created=@();Cleaned=@();Retained=@();Observations=@()}
function Save {$m|ConvertTo-Json -Depth 10|Set-Content "$out/actor-followup.json" -Encoding utf8}
$c=$null;Save
try{
 $c=Open-TestConnection 'master';if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'") -ne 0){throw 'Exists'};Run-TestSql $c "CREATE DATABASE [$db]";$m.Created+=$db;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G04TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v -le 16;$v++){$file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v));& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $file[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/actor-migrations.jsonl"}
 Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 Run-TestSql $c ([IO.File]::ReadAllText("$out/actor-validation-observed.sql"))
 $cmd=$c.CreateCommand();$cmd.CommandText='SELECT Mode,Rejected,SnapshotEqual,HeaderCount,ReceiptCount,TranCount,XactStateCaptured,XactStateInline FROM dbo.G04ActorObservation ORDER BY Mode';$reader=$cmd.ExecuteReader()
 try{while($reader.Read()){$m.Observations+=@{Mode=$reader.GetInt32(0);Rejected=$reader.GetBoolean(1);SnapshotEqual=$reader.GetBoolean(2);Headers=$reader.GetInt32(3);Receipts=$reader.GetInt32(4);TranCount=$reader.GetInt32(5);CapturedState=$reader.GetInt32(6);InlineState=$reader.GetInt32(7)}}}finally{$reader.Dispose();$cmd.Dispose()}
 if($m.Observations.Count -ne 4 -or @($m.Observations|Where-Object {-not $_.Rejected -or -not $_.SnapshotEqual -or $_.Headers -ne 0 -or $_.Receipts -ne 0 -or $_.TranCount -ne 0 -or $_.CapturedState -ne 0}).Count -gt 0){throw 'Actor observations did not meet expected'}
 $m.Status='PASS'
}catch{$m.Status='FAIL';$m['Error']=$_.Exception.ToString()}
finally{if($c){$c.Dispose()};if($m.Created.Count){$cleanup=$null;try{$cleanup=Open-TestConnection $db;if($db -notmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G04TestMarker') -cne $runId){throw 'Marker mismatch'};$cleanup.ChangeDatabase('master');Run-TestSql $cleanup "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$db]";$m.Cleaned+=$db}catch{$m.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($cleanup){$cleanup.Dispose()}}};Save;Write-Output "Actor followup: $($m.Status), observations=$($m.Observations.Count),cleaned=$($m.Cleaned.Count)"}
