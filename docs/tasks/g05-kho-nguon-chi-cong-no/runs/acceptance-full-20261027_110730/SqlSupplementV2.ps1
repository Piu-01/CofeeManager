param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/sql-supplement-v2-results.json" -Encoding utf8}
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

# Instrumentation bổ sung; không thay source/migration/expected.
$native=Table "SELECT MaTC,MaNV,SoTien,OriginKind FROM dbo.PHIEUTHUCHI WHERE MaTC='TC20250521'"
Check 'T19-K01.NativeAdmin' ($native.Rows.Count -eq 1 -and $native.Rows[0].MaNV -eq 'NV991' -and $native.Rows[0].SoTien -eq 1234 -and $native.Rows[0].OriginKind -eq 'Manual') @($native.Rows|ForEach-Object{[ordered]@{Code=$_.MaTC;Employee=$_.MaNV;Amount=$_.SoTien;Origin=$_.OriginKind}})
$cat=[string](Scalar "SELECT TOP(1) MaDMTC FROM dbo.DANHMUC_THUCHI WHERE Loai=N'Thu'");$income=[string](Scalar (Runtime "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','$cat',3456,NULL,NULL;"))
Check 'T19-K01.Income' ((Scalar "SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTC='$income' AND SoTien=3456") -eq 1) $income
$list=Table (Runtime "EXEC dbo.sp_FinanceList @actor,@stamp,'20261027','20261027';")
$sum=Table "SELECT SUM(CASE WHEN d.Loai=N'Thu' THEN p.SoTien ELSE 0 END) Income,SUM(CASE WHEN d.Loai=N'Chi' THEN p.SoTien ELSE 0 END) Expense FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE CONVERT(date,p.NgayLap)='20261027'"
Check 'T19-K08.TotalWithoutDoubleDebt' ($list.Rows.Count -gt 0 -and $list.Rows[0].TotalIncome -eq $sum.Rows[0].Income -and $list.Rows[0].TotalExpense -eq $sum.Rows[0].Expense) @{Income=$sum.Rows[0].Income;Expense=$sum.Rows[0].Expense;ListIncome=$list.Rows[0].TotalIncome;ListExpense=$list.Rows[0].TotalExpense}
$pn=NewPurchase -rows "('NVL86',1,10000)";$debt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$pn'")
Check 'T23-K01.SourceSupplierAndUnique' ((Scalar "SELECT COUNT(*) FROM dbo.CONGNO_NCC c JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN WHERE c.MaCN='$debt' AND p.MaNCC='NCC01'") -eq 1) $debt
Expect 'T23-K01.DuplicateSource' "INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES('CN99000001','$pn',1000,NULL);" @(2601,2627,52702) $false
Expect 'T23-K02.Zero' "INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES('CN99000002','$pn',0,NULL);" @(547,52702) $false
Expect 'T23-K02.AboveTotal' "INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES('CN99000003','$pn',10001,NULL);" @(52702,547) $false
Check 'T23-K02.NullDue' ((Scalar "SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaCN='$debt' AND HanTra IS NULL") -eq 1) $debt
$mapping=Table "SELECT UseCase,MaDMTC FROM dbo.SystemExpenseCategory WHERE UseCase='SupplierDebt'"
if($mapping.Rows.Count -ne 1 -or $mapping.Rows[0].MaDMTC -ne 'TC011'){throw 'Unexpected mapping; no fault injection'}
try{
 Sql "DELETE dbo.SystemExpenseCategory WHERE UseCase='SupplierDebt'"
 Expect 'T23-K06.MissingMapping' "EXEC dbo.sp_TraCongNo @actor,@stamp,'$([guid]::NewGuid())','$debt','PT001',1,'20261027';" @(52602)
}finally{Sql "IF NOT EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory WHERE UseCase='SupplierDebt') INSERT dbo.SystemExpenseCategory(UseCase,MaDMTC) VALUES('SupplierDebt','TC011');"}
$before=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL86'")
$kk=NewKk "('NVL86')";$scope=Table "SELECT k.TonHeThong,k.TonThucTe,k.MaterialRevision,n.Revision FROM dbo.CHITIET_KK k JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=k.MaNVL WHERE k.MaKK='$kk'"
Check 'T22-K01.SnapshotNoStockChange' ($scope.Rows[0].TonHeThong -eq $before -and $scope.Rows[0].TonThucTe -is [DBNull] -and (Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL86'") -eq $before) @{Code=$kk;Stock=$before}
SaveKk $kk "('NVL86',0,N'G05 fixture zero count')";Sql (Runtime (CommitSql $kk))
Check 'T22-K02.ZeroCountAndDelta' ((Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL86'") -eq 0 -and (Scalar "SELECT DaDieuChinh FROM dbo.PHIEUKIEMKE WHERE MaKK='$kk'") -eq 1) $kk
$row=Table "SELECT Code,CASE WHEN LinesJson IS NULL THEN 1 ELSE 0 END IsNullJson FROM dbo.vw_StocktakeRead WHERE Code='KK99000002'"
Check 'T22.NativeNullEvidence' ($row.Rows.Count -eq 1 -and $row.Rows[0].IsNullJson -eq 1) @{Code='KK99000002';LinesJson='NULL';Fixture='deliberately empty negative-case header'}
Save;Write-Output "Supplement records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)";$c.Dispose()


