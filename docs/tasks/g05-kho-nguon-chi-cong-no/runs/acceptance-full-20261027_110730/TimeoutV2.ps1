param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/timeout-v2-results.json" -Encoding utf8}
function Sql([string]$s){Run-TestSql $c $s}
function Scalar([string]$s){Read-TestScalar $c $s}
function Table([string]$s){$cmd=$c.CreateCommand();$cmd.CommandText=$s;$cmd.CommandTimeout=30;try{$a=[Data.SqlClient.SqlDataAdapter]::new($cmd);$t=[Data.DataTable]::new();[void]$a.Fill($t);return ,$t}finally{$cmd.Dispose()}}
function Header([string]$role='ADMIN'){$row=Table "SELECT TOP(1) MaTK,CONVERT(varchar(18),CONVERT(binary(8),SecurityStamp),1) Stamp FROM dbo.TAIKHOAN WHERE MaVT='$role' ORDER BY MaTK";return "DECLARE @actor int=$($row.Rows[0].MaTK),@stamp binary(8)=$($row.Rows[0].Stamp);"}
function Runtime([string]$s,[string]$role='ADMIN'){return (Header $role)+"EXECUTE AS USER='G05Runtime';BEGIN TRY "+$s+";REVERT;END TRY BEGIN CATCH REVERT;THROW;END CATCH;"}
function Hash {
 $chunks=foreach($pair in @('PHIEUTHUCHI:MaTC','PHIEUNHAP:MaPN','CHITIET_PN:MaPN,MaNVL','PHIEUXUAT:MaPX','CHITIET_PX:MaPX,MaNVL','CONGNO_NCC:MaCN','THANHTOAN_CONGNO:MaTT','PHIEUKIEMKE:MaKK','CHITIET_KK:MaKK,MaNVL','NGUYENVATLIEU:MaNVL','G03OperationReceipt:RequestId','HOADON:MaHD','CHITIET_THANHTOAN:MaTT','TIEUHAO_HOADON:MaHD,MaNVL','KHACHHANG:MaKH')){$p=$pair.Split(':');[string](Scalar "SELECT (SELECT * FROM dbo.$($p[0]) ORDER BY $($p[1]) FOR JSON PATH,INCLUDE_NULL_VALUES)")}
 return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($chunks -join '|'))))
}
function ErrorNumber($e){while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if($e){return @($e.Errors|ForEach-Object Number)};return @()}
function Expect([string]$part,[string]$s,[int[]]$expected,[bool]$runtime=$true,[string]$role='ADMIN'){
 $before=Hash;$numbers=@();$message='No error';$pass=$false
 try{if($runtime){Sql (Runtime $s $role)}else{Sql $s}}catch{$numbers=@(ErrorNumber $_.Exception);$message=$_.Exception.Message;$pass=@($numbers|Where-Object{$_ -in $expected}).Count -gt 0}
 $state=Scalar "DECLARE @tr int=@@TRANCOUNT,@xs int=XACT_STATE();SELECT CONCAT(@tr,':',@xs)";$after=Hash
 $results.Add([pscustomobject]@{Part=$part;Result=if($pass -and $before -ceq $after -and $state -eq '0:0'){'PASS'}else{'FAIL'};Expected=$expected;Observed=$numbers;Before=$before;After=$after;Transaction=$state;Message=$message});Save
}
function Check([string]$part,[bool]$pass,[object]$actual){$results.Add([pscustomobject]@{Part=$part;Result=if($pass){'PASS'}else{'FAIL'};Actual=$actual});Save}
function PurchaseSql([string]$rows="('NVL80',1,10000)",[string]$immediate='0',[string]$total='10000',[string]$supplier='NCC01',[string]$date='20261027',[guid]$id=[guid]::NewGuid()) {return "DECLARE @l dbo.PurchaseLinesInput;INSERT @l VALUES$rows;EXEC dbo.sp_LapPhieuNhap @actor,@stamp,'$id','$supplier','$date',NULL,$immediate,$total,@l;"}
function NewPurchase([string]$rows="('NVL80',1,10000)",[string]$immediate='0',[string]$total='10000',[string]$date='20261027'){return [string](Scalar (Runtime (PurchaseSql -rows $rows -immediate $immediate -total $total -date $date)))}
function IssueSql([string]$rows="('NVL80',0.1)",[string]$kind="N'Huy do hong'",[string]$reason="N'G05 fixture'",[guid]$id=[guid]::NewGuid()){return "DECLARE @l dbo.StockIssueLinesInput;INSERT @l VALUES$rows;EXEC dbo.sp_LapPhieuXuat @actor,@stamp,'$id','20261027',$kind,$reason,@l;"}
function NewKk([string]$rows="('NVL80')"){return [string](Scalar (Runtime "DECLARE @s dbo.StocktakeScope;INSERT @s VALUES$rows;EXEC dbo.sp_StocktakeCreate @actor,@stamp,'$([guid]::NewGuid())',@s;"))}
function KkRevision([string]$code){return [string](Scalar "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.PHIEUKIEMKE WHERE MaKK='$code'")}
function SaveKk([string]$code,[string]$counts){Sql (Runtime "DECLARE @a dbo.StocktakeCounts;INSERT @a VALUES$counts;EXEC dbo.sp_StocktakeSaveCounts @actor,@stamp,'$([guid]::NewGuid())','$code',$(KkRevision $code),@a;")}
function CommitSql([string]$code,[guid]$id=[guid]::NewGuid()){return "EXEC dbo.sp_ChotKiemKe @actor,@stamp,'$id','$code',$(KkRevision $code);"}
Sql "IF USER_ID('G05Runtime') IS NULL CREATE USER G05Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G05Runtime;"
foreach($n in 80..91){Sql "IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL$n') INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES('NVL$n',N'Fixture G05 $n',N'Kg',5);"}

function TimeoutCase([string]$part,[string]$body,[guid]$request,[string]$lockQuery,[bool]$afterCommit){
 $before=Hash;$conn=Open-TestConnection $Database;$sessionId=[int](Read-TestScalar $conn 'SELECT @@SPID');$bar=$null;$cmd=$null;$number=@();$state='not measured';$released=$false
 try{
  if(-not $afterCommit){$bar=Open-TestConnection $Database;Run-TestSql $bar ('BEGIN TRAN;'+$lockQuery)}
  $cmd=$conn.CreateCommand();$cmd.CommandTimeout=1;$cmd.CommandText=(Runtime ($body+$(if($afterCommit){"WAITFOR DELAY '00:00:03';"}else{''})))
  try{[void]$cmd.ExecuteNonQuery()}catch{$number=@(ErrorNumber $_.Exception)}
  if($bar){Run-TestSql $bar 'COMMIT';$released=$true}
  $clientState=[string]$conn.State
  if($conn.State -eq [Data.ConnectionState]::Open){
   $state=Read-TestScalar $conn "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)"
  }else{
   $watch=[Diagnostics.Stopwatch]::StartNew()
   do{$activeTx=[int](Scalar "SELECT COUNT(*) FROM sys.dm_tran_session_transactions WHERE session_id=$sessionId");if($activeTx -gt 0){Start-Sleep -Milliseconds 50}}while($activeTx -gt 0 -and $watch.Elapsed.TotalSeconds -lt 5)
   $state=if($activeTx -eq 0){'SessionClosed:NoTransaction'}else{'SessionClosed:TransactionPresent'}
  }
  $receipt=Scalar "SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId='$request'";$after=Hash
  $sameReplay=$null
  if($afterCommit){Sql (Runtime $body);$sameReplay=(Hash) -ceq $after}
  $pass=(-2 -in $number) -and $state -in @('0:0','SessionClosed:NoTransaction') -and $(if($afterCommit){$receipt -eq 1 -and $sameReplay}else{$receipt -eq 0 -and $before -ceq $after})
  $results.Add([pscustomobject]@{Part=$part;Result=if($pass){'PASS'}else{'FAIL'};AfterCommit=$afterCommit;Session=$sessionId;ClientState=$clientState;SqlErrors=$number;Transaction=$state;ReceiptCount=$receipt;SnapshotUnchanged=($before -ceq $after);ReplayUnchanged=$sameReplay});Save
 }finally{if($bar){if(-not $released){try{Run-TestSql $bar 'IF @@TRANCOUNT>0 ROLLBACK'}catch{}};$bar.Dispose()};if($cmd){$cmd.Dispose()};$conn.Dispose()}
}
foreach($afterCommit in @($false,$true)){
 $id=[guid]::NewGuid();TimeoutCase "T19.Timeout.AfterCommit=$afterCommit" "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$id','20261027','TC001',1234,NULL,NULL;" $id "SELECT MaDMTC FROM dbo.DANHMUC_THUCHI WITH(XLOCK,HOLDLOCK) WHERE MaDMTC='TC001';" $afterCommit
 $id=[guid]::NewGuid();TimeoutCase "T20-K07.Timeout.AfterCommit=$afterCommit" (PurchaseSql -id $id) $id "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(XLOCK,HOLDLOCK) WHERE MaNVL='NVL80';" $afterCommit
 $id=[guid]::NewGuid();TimeoutCase "T21-K07.Timeout.AfterCommit=$afterCommit" (IssueSql -id $id) $id "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(XLOCK,HOLDLOCK) WHERE MaNVL='NVL80';" $afterCommit
 $pn=NewPurchase;$debt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$pn'");$id=[guid]::NewGuid();TimeoutCase "T23-K11.Timeout.AfterCommit=$afterCommit" "EXEC dbo.sp_TraCongNo @actor,@stamp,'$id','$debt','PT001',1000,'20261027';" $id "SELECT * FROM dbo.CONGNO_NCC WITH(XLOCK,HOLDLOCK) WHERE MaCN='$debt';" $afterCommit
}
Save;Write-Output "Timeout records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)";$c.Dispose()

