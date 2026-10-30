param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/concurrency-results.json" -Encoding utf8}
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

function DebtSql([string]$debt,[string]$amount='1',[guid]$id=[guid]::NewGuid()){return "EXEC dbo.sp_TraCongNo @actor,@stamp,'$id','$debt','PT001',$amount,'20261027';"}
function Parallel([string]$part,[string]$lockQuery,[string]$first,[string]$second){
 $bar=Open-TestConnection $Database;$a=Open-TestConnection $Database;$b=Open-TestConnection $Database;$cmdA=$null;$cmdB=$null;$release=$false
 try{
  $barId=[int](Read-TestScalar $bar 'SELECT @@SPID');$aId=[int](Read-TestScalar $a 'SELECT @@SPID');$bId=[int](Read-TestScalar $b 'SELECT @@SPID')
  Run-TestSql $bar ('BEGIN TRAN;'+$lockQuery)
  $cmdA=$a.CreateCommand();$cmdA.CommandText=(Runtime $first);$cmdA.CommandTimeout=35;$ta=$cmdA.ExecuteNonQueryAsync()
  $watch=[Diagnostics.Stopwatch]::StartNew();do{Start-Sleep -Milliseconds 50;$firstWait=[int](Scalar "SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id=$aId AND wait_type LIKE 'LCK%'")}while($firstWait -eq 0 -and $watch.Elapsed.TotalSeconds -lt 8)
  $cmdB=$b.CreateCommand();$cmdB.CommandText=(Runtime $second);$cmdB.CommandTimeout=35;$tb=$cmdB.ExecuteNonQueryAsync()
  $watch.Restart();do{Start-Sleep -Milliseconds 50;$waits=Table "SELECT session_id,wait_type,blocking_session_id FROM sys.dm_exec_requests WHERE session_id IN($aId,$bId) AND wait_type LIKE 'LCK%'"}while($waits.Rows.Count -lt 2 -and $watch.Elapsed.TotalSeconds -lt 8)
  $observed=@($waits.Rows|ForEach-Object{[ordered]@{Session=$_.session_id;Wait=$_.wait_type;BlockedBy=$_.blocking_session_id}})
  Run-TestSql $bar 'COMMIT';$release=$true;$codes=@();$messages=@()
  foreach($task in @($ta,$tb)){try{[void]$task.GetAwaiter().GetResult();$codes+=0;$messages+='Committed'}catch{$numbers=@(ErrorNumber $_.Exception);$codes+=if($numbers.Count){$numbers[0]}else{-1};$messages+=$_.Exception.Message}}
  $sa=Read-TestScalar $a "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)";$sb=Read-TestScalar $b "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)"
  return [pscustomobject]@{Part=$part;Codes=$codes;Messages=$messages;Sessions=@($aId,$bId);Barrier=$barId;Waits=$observed;BothBlocked=($waits.Rows.Count -eq 2);Transaction=@($sa,$sb)}
 }finally{if(-not $release){try{Run-TestSql $bar 'IF @@TRANCOUNT>0 ROLLBACK'}catch{}};if($cmdA){$cmdA.Dispose()};if($cmdB){$cmdB.Dispose()};$a.Dispose();$b.Dispose();$bar.Dispose()}
}
function Record([object]$entry,[bool]$ok,[object]$state){$entry|Add-Member -NotePropertyName Actual -NotePropertyValue $state;$entry|Add-Member -NotePropertyName Result -NotePropertyValue $(if($ok -and $entry.BothBlocked -and @($entry.Transaction|Where-Object{$_ -ne '0:0'}).Count -eq 0){'PASS'}else{'FAIL'});$results.Add($entry);Save}
foreach($reverse in @($false,$true)){
 $before=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88'");$one=PurchaseSql -rows "('NVL88',1,10000)";$two=PurchaseSql -rows "('NVL88',2,10000)" -total 20000
 $pair=if($reverse){@($two,$one)}else{@($one,$two)}
 $entry=Parallel "T20-K10.TwoOrders.reverse=$reverse" "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(XLOCK,HOLDLOCK) WHERE MaNVL='NVL88';" $pair[0] $pair[1]
 $after=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88'");Record $entry (($entry.Codes|Where-Object{$_ -ne 0}).Count -eq 0 -and $after -eq $before+3) @{Before=$before;After=$after}
}
foreach($reverse in @($false,$true)){
 $before=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89'");if($before -lt 5){$need=(5-$before).ToString([Globalization.CultureInfo]::InvariantCulture);$ignore=NewPurchase -rows "('NVL89',$need,1)" -total $need}
 $one=IssueSql -rows "('NVL89',5)";$two=IssueSql -rows "('NVL89',5)";$pair=if($reverse){@($two,$one)}else{@($one,$two)}
 $entry=Parallel "T21-K06.TwoIssues.reverse=$reverse" "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(XLOCK,HOLDLOCK) WHERE MaNVL='NVL89';" $pair[0] $pair[1]
 $after=Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89'";Record $entry (@($entry.Codes|Where-Object{$_ -eq 0}).Count -eq 1 -and @($entry.Codes|Where-Object{$_ -eq 52803}).Count -eq 1 -and $after -eq 0) $after
}
$kk=NewKk "('NVL90')";SaveKk $kk "('NVL90',5,NULL)";$first=CommitSql $kk;$second=CommitSql $kk
$entry=Parallel 'T22-K09.SameKK' "SELECT * FROM dbo.PHIEUKIEMKE WITH(XLOCK,HOLDLOCK) WHERE MaKK='$kk';" $first $second
Record $entry (@($entry.Codes|Where-Object{$_ -eq 0}).Count -eq 1 -and @($entry.Codes|Where-Object{$_ -eq 52902}).Count -eq 1) $kk
foreach($reverse in @($false,$true)){
 $pn=NewPurchase -rows "('NVL88',1,10000)";$debt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$pn'");$a=DebtSql $debt -amount 10000;$b=DebtSql $debt -amount 10000;$pair=if($reverse){@($b,$a)}else{@($a,$b)}
 $entry=Parallel "T23-K07.LastDebt.reverse=$reverse" "SELECT * FROM dbo.CONGNO_NCC WITH(XLOCK,HOLDLOCK) WHERE MaCN='$debt';" $pair[0] $pair[1]
 $remaining=Scalar "SELECT ConNo FROM dbo.vw_CongNoNCC WHERE MaCN='$debt'";$payments=Scalar "SELECT COUNT(*) FROM dbo.THANHTOAN_CONGNO WHERE MaCN='$debt'";Record $entry (@($entry.Codes|Where-Object{$_ -eq 0}).Count -eq 1 -and @($entry.Codes|Where-Object{$_ -eq 53003}).Count -eq 1 -and $remaining -eq 0 -and $payments -eq 1) @{Debt=$debt;Remaining=$remaining;Payments=$payments}
}
# Fixture món chỉ dùng một NVL, tạo theo thứ tự trạng thái/công thức hợp lệ.
Sql "IF NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP991') BEGIN INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES('SP991',N'G05 race fixture',(SELECT TOP(1) MaDM FROM dbo.DANHMUC_SP ORDER BY MaDM),1000,N'Ngung ban');INSERT dbo.CONGTHUC VALUES('SP991','NVL91',1);UPDATE dbo.SANPHAM SET TrangThai=N'Dang ban' WHERE MaSP='SP991';END;"
function CheckoutSql([string]$material='NVL91'){
 $codeRow=Table (Runtime "EXEC dbo.sp_CapMa 'HOADON';");$hd=[string]$codeRow.Rows[0].Ma
 Sql (Runtime "EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code='$hd',@RequestId='$([guid]::NewGuid())',@Channel=N'Mang di',@Product='SP991',@Quantity=1;")
 $rev=[string](Scalar "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.HOADON WHERE MaHD='$hd'");$token=[string](Scalar "SELECT CONVERT(varchar(66),CONVERT(binary(32),PreviewToken),1) FROM dbo.fn_CheckoutPreview('$hd')")
 return "DECLARE @p dbo.DanhSachThanhToan;INSERT @p VALUES('PT001',1000,NULL);EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,'$hd',$rev,'$([guid]::NewGuid())',$token,@p;"
}
foreach($reverse in @($false,$true)){
 $qty=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91'");if($qty -lt 5){$need=(5-$qty).ToString([Globalization.CultureInfo]::InvariantCulture);$ignore=NewPurchase -rows "('NVL91',$need,1)" -total $need}
 $checkout=CheckoutSql;$issue=IssueSql -rows "('NVL91',5)";$pair=if($reverse){@($issue,$checkout)}else{@($checkout,$issue)}
 $entry=Parallel "T21-K06.CheckoutIssue.reverse=$reverse" "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(XLOCK,HOLDLOCK) WHERE MaNVL='NVL91';" $pair[0] $pair[1]
 $after=Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91'";Record $entry (@($entry.Codes|Where-Object{$_ -eq 0}).Count -eq 1 -and @($entry.Codes|Where-Object{$_ -in @(52203,52803)}).Count -eq 1 -and $after -in @(0,4)) $after
}
Save;Write-Output "Concurrency records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)";$c.Dispose()
