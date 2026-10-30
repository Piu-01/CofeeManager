param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/sql-supplement-v4-results.json" -Encoding utf8}
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
$full=NewPurchase -rows "('NVL85',1,10000)" -immediate 10000
Expect 'T23-K02.ZeroIndependent' "INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES('CN99000002','$full',0,NULL);" @(547,52702) $false
Expect 'T23-K02.AboveTotalIndependent' "INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES('CN99000003','$full',10001,NULL);" @(52702,547) $false
function NewOrder {
 $codeRow=Table (Runtime "EXEC dbo.sp_CapMa 'HOADON';");$hd=[string]$codeRow.Rows[0].Ma
 Sql (Runtime "EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code='$hd',@RequestId='$([guid]::NewGuid())',@Channel=N'Mang di',@Product='SP991',@Quantity=1;")
 return $hd
}
function Revision([string]$hd){return [string](Scalar "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.HOADON WHERE MaHD='$hd'")}
$stockBefore=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91'");$hd=NewOrder
Sql (Runtime "EXEC dbo.sp_HuyHoaDon @actor,@stamp,'$hd',$(Revision $hd),'$([guid]::NewGuid())';")
$px=[string](Scalar (Runtime (IssueSql -rows "('NVL91',1)")))
$stockAfter=Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91'"
Check 'T21-K08.CancelPreparedNoSalesConsumption' ($stockAfter -eq $stockBefore-1 -and (Scalar "SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='$hd'") -eq 0 -and (Scalar "SELECT COUNT(*) FROM dbo.CHITIET_PX WHERE MaPX='$px' AND MaNVL='NVL91' AND SoLuongXuat=1") -eq 1) @{Order=$hd;Issue=$px;Before=$stockBefore;After=$stockAfter;Note='SQL only; native draft flow remains blocked'}
$financeBefore=Scalar 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI';$hd=NewOrder
$token=[string](Scalar "SELECT CONVERT(varchar(66),CONVERT(binary(32),PreviewToken),1) FROM dbo.fn_CheckoutPreview('$hd')")
Sql (Runtime "DECLARE @p dbo.DanhSachThanhToan;INSERT @p VALUES('PT001',1000,NULL);EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,'$hd',$(Revision $hd),'$([guid]::NewGuid())',$token,@p;")
Check 'T19-K09.CheckoutNoFinanceIncome' ((Scalar 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI') -eq $financeBefore -and (Scalar "SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$hd'") -eq 1000) @{Order=$hd;FinanceBefore=$financeBefore;FinanceAfter=(Scalar 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI')}
Save;Write-Output "Supplement v3 records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)";$c.Dispose()

