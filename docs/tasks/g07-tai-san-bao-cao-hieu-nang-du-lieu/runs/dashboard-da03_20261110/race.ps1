$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$setup=Get-Content (Join-Path $PSScriptRoot 'setup.json') -Raw | ConvertFrom-Json
$race=$setup.Race
$backup=Join-Path $PSScriptRoot "$race.bak"
$migration=(Resolve-Path "$root/database/migrations/029_dashboard_source_lock_order.sql").Path
$duration=60
$report=[ordered]@{Database=$race;DurationSeconds=$duration;Phases=@()}
function Save{$report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'race.json') -Encoding utf8}
function Prepare-Tables($c){
 Run-TestSql $c "IF OBJECT_ID('dbo.G07RaceLog','U') IS NOT NULL DROP TABLE dbo.G07RaceLog;IF OBJECT_ID('dbo.G07RaceGate','U') IS NOT NULL DROP TABLE dbo.G07RaceGate;CREATE TABLE dbo.G07RaceGate(Id int PRIMARY KEY,StartedAt datetime2(7) NULL,StopAt datetime2(7) NULL);INSERT dbo.G07RaceGate VALUES(1,NULL,NULL);CREATE TABLE dbo.G07RaceLog(Id int IDENTITY PRIMARY KEY,Session varchar(10),Iteration int,ErrorNumber int,Message nvarchar(400));"
}
function Invoke-Phase([string]$label){
 $c=Open-TestConnection $race
 try{
  Prepare-Tables $c
  $writerLog=Join-Path $PSScriptRoot "$label-writer.log";$readerLog=Join-Path $PSScriptRoot "$label-reader.log"
  $writer=Start-Process sqlcmd -ArgumentList @('-S','localhost','-E','-C','-d',$race,'-i',(Join-Path $PSScriptRoot 'writer.sql'),'-v','Iterations=200000','-W','-h','-1','-o',$writerLog) -PassThru -NoNewWindow
  $reader=Start-Process sqlcmd -ArgumentList @('-S','localhost','-E','-C','-d',$race,'-i',(Join-Path $PSScriptRoot 'reader.sql'),'-v','Iterations=200000','-W','-h','-1','-o',$readerLog) -PassThru -NoNewWindow
  Start-Sleep -Seconds 2
  Run-TestSql $c "UPDATE dbo.G07RaceGate SET StartedAt=SYSDATETIME() WHERE Id=1"
  Start-Sleep -Seconds $duration
  Run-TestSql $c "UPDATE dbo.G07RaceGate SET StopAt=SYSDATETIME() WHERE Id=1"
  if(-not $writer.WaitForExit(300000)){$writer.Kill();throw 'Writer did not stop'}
  if(-not $reader.WaitForExit(300000)){$reader.Kill();throw 'Reader did not stop'}
  $rows=@()
  $cmd=$c.CreateCommand();$cmd.CommandText="SELECT Session,COUNT(*) N,SUM(CASE WHEN ErrorNumber=1205 THEN 1 ELSE 0 END) Deadlocks,SUM(CASE WHEN ErrorNumber=1222 THEN 1 ELSE 0 END) Timeouts,SUM(CASE WHEN ErrorNumber NOT IN(0,1205,1222) THEN 1 ELSE 0 END) Other FROM dbo.G07RaceLog WHERE Iteration>=0 GROUP BY Session;SELECT Session,Message FROM dbo.G07RaceLog WHERE Iteration=-1;SELECT TOP 5 Session,Iteration,ErrorNumber,LEFT(Message,300) Message FROM dbo.G07RaceLog WHERE Iteration>=0 ORDER BY Id;"
  $r=$cmd.ExecuteReader()
  do{while($r.Read()){$vals=@();for($k=0;$k -lt $r.FieldCount;$k++){$vals+=[string]$r.GetValue($k)};$rows+=($vals -join '|')}}while($r.NextResult());$r.Close();$cmd.Dispose()
  $deadlocks=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G07RaceLog WHERE ErrorNumber=1205")
  $timeouts=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G07RaceLog WHERE ErrorNumber=1222")
  $other=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G07RaceLog WHERE Iteration>=0 AND ErrorNumber NOT IN(0,1205,1222)")
  $iterations=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G07RaceLog WHERE Iteration>=0")
  $version=Read-TestScalar $c "SELECT MAX(Version) FROM dbo.SchemaMigrations"
  # Ring buffer cua system_health chi giu su kien gan day; luu lai neu co.
  $xe=Read-TestScalar $c "SELECT CONVERT(nvarchar(max),(SELECT CAST(t.target_data AS xml) FROM sys.dm_xe_session_targets t JOIN sys.dm_xe_sessions s ON s.address=t.event_session_address WHERE s.name='system_health' AND t.target_name='ring_buffer'))"
  $deadlockNodes=[regex]::Matches($xe,'(?s)<event name="xml_deadlock_report".*?</event>')
  $relevant=@()
  foreach($n in $deadlockNodes){if($n.Value -match [regex]::Escape($race)){$relevant+=$n.Value}}
  if($relevant.Count){$relevant -join "`n" | Set-Content (Join-Path $PSScriptRoot "$label-deadlock.xml") -Encoding utf8}
  $report.Phases+=@{Label=$label;Version=$version;Deadlocks=$deadlocks;Timeouts=$timeouts;OtherErrors=$other;LogRows=$iterations;DeadlockReportsInRingBuffer=$relevant.Count;Rows=$rows}
  Save
 }finally{$c.Dispose()}
}
function Restore-Dataset{
 $m=Open-TestConnection 'master'
 try{Run-TestSql $m "ALTER DATABASE [$race] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;RESTORE DATABASE [$race] FROM DISK=N'$backup' WITH REPLACE,RECOVERY;ALTER DATABASE [$race] SET MULTI_USER"}
 finally{$m.Dispose()}
 $c=Open-TestConnection $race
 try{Run-TestSql $c "SET NOCOUNT ON;DELETE dbo.G07TestMarker;INSERT dbo.G07TestMarker VALUES('$($setup.RunId)')"}
 finally{$c.Dispose()}
}
# Moi phase bat dau tu dung dataset 4.899 bill o 028 trong tep .bak.
Restore-Dataset
Invoke-Phase 'control-028'
Restore-Dataset
$applied=& "$root/tools/database/Apply-Migration.ps1" -Database $race -Version 29 -File $migration
$report['Apply029']=$applied
Invoke-Phase 'fix-029'
$c=Open-TestConnection $race
try{
 $checks=[ordered]@{
  BillsWithoutLines=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON h WHERE NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD)")
  OrphanLines=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.CHITIET_HD l WHERE NOT EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=l.MaHD)")
  NegativeStock=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0")
  PaidWithoutPayment=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' AND NOT EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN tt WHERE tt.MaHD=h.MaHD)")
  PaymentMismatch=[int](Read-TestScalar $c "SELECT COUNT(*) FROM (SELECT h.MaHD,h.ThanhTien,COALESCE(SUM(tt.SoTien),0) Paid FROM dbo.HOADON h LEFT JOIN dbo.CHITIET_THANHTOAN tt ON tt.MaHD=h.MaHD WHERE h.TrangThai=N'Da thanh toan' GROUP BY h.MaHD,h.ThanhTien) x WHERE Paid<>ThanhTien")
  DuplicateCheckoutReceipts=[int](Read-TestScalar $c "SELECT COUNT(*) FROM (SELECT EntityCode FROM dbo.G03OperationReceipt WHERE Operation='SalesCheckout' GROUP BY EntityCode HAVING COUNT(*)>1) x")
 }
 $report['Integrity']=$checks
 $report['Bills']=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON")
 $report['Paid']=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan'")
 $report['Status']=if(@($checks.Values|Where-Object{$_ -ne 0}).Count){'Failed'}else{'Passed'}
}finally{$c.Dispose()}
Save
Write-Output "race: $($report.Status)"
