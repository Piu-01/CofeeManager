param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/sql-final-results.json" -Encoding utf8}
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
$pn=[string](Scalar (Runtime "DECLARE @l dbo.PurchaseLinesInput;INSERT @l VALUES('NVL84',1,10000);EXEC dbo.sp_LapPhieuNhap @actor,@stamp,'$([guid]::NewGuid())','NCC01','20261025','20261026',0,10000,@l;"))
$debt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$pn'")
$due=Table "SELECT MaCN,ConNo,TrangThai FROM dbo.vw_CongNoNCC WHERE MaCN='$debt'"
Check 'T23-K09.OverdueNow' ($due.Rows.Count -eq 1 -and $due.Rows[0].ConNo -eq 10000 -and $due.Rows[0].TrangThai -eq 'Qua han') @{Code=$debt;Remaining=$due.Rows[0].ConNo;Status=$due.Rows[0].TrangThai}
$nulls=Table "SELECT Code,Original,Paid,Remaining,CASE WHEN HistoryJson IS NULL THEN 1 ELSE 0 END NullHistory FROM dbo.vw_DebtRead WHERE Code='$debt'"
$nulls.Rows|ForEach-Object{[pscustomobject]@{Code=$_.Code;Original=$_.Original;Paid=$_.Paid;Remaining=$_.Remaining;NullHistory=$_.NullHistory}}|ConvertTo-Json|Set-Content "$PSScriptRoot/debt-null-evidence.json" -Encoding utf8
Check 'T23.NativeUnpaidHistoryNullEvidence' ($nulls.Rows.Count -eq 1 -and $nulls.Rows[0].NullHistory -eq 1 -and $nulls.Rows[0].Paid -eq 0) $debt
$dev=Open-TestConnection CoffeeManager_Development
try{$legacy=[int](Read-TestScalar $dev "SELECT COUNT(*) FROM dbo.PHIEUNHAP WHERE CutoverKind='Legacy'");$open=[int](Read-TestScalar $dev "SELECT COUNT(*) FROM dbo.PHIEUNHAP WHERE CutoverKind='Legacy' AND DaChot<>1");Check 'T20-K09.LegacyClosed' ($legacy -gt 0 -and $open -eq 0) @{Legacy=$legacy;NotClosed=$open;Balances='Full eight-part backup hash compare in development-backup-verification.json'}}finally{$dev.Dispose()}
Save;Write-Output "Final SQL records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)";$c.Dispose()
