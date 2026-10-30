param([string]$Database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo',[string]$Marker='20261027_110753_fb67dcbb')
$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection $Database
if($Database -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $Marker){throw 'Wrong test target'}
$results=[Collections.Generic.List[object]]::new()
function Save {$results|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/sql-extended-v2-results.json" -Encoding utf8}
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
foreach($v in @('0','-1','0.5','1000000000000000000')){Expect "T19-K02.Amount.$v" "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','TC001',$v,NULL,NULL;" @(52601)}
Expect 'T19-K02.Note201' "DECLARE @n nvarchar(max)=REPLICATE(N'x',201);EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','TC001',1,@n,NULL;" @(52601)
Expect 'T19-K02.AssetFK' "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','TC001',1,NULL,'TS999';" @(52602)
Expect 'T19-K02.CategoryFK' "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','ZZ999',1,NULL,NULL;" @(52602)
foreach($cat in @('TC005','TC011','TC012')){Expect "T19-K05.System.$cat" "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','$cat',1,NULL,NULL;" @(52602)}
foreach($role in @('CASH','STORE')){Expect "T19-K05.Role.$role" 'EXEC dbo.sp_FinanceCategories @actor,@stamp;' @(51605) $true $role}
Expect 'T19-K05.InternalHelper' "DECLARE @tc char(10);EXEC dbo.sp_FinanceNextCode @tc OUTPUT;" @(229)
$manual=[string](Scalar (Runtime "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','TC001',1234,NULL,NULL;"))
Expect 'T19-K07.Update' "UPDATE dbo.PHIEUTHUCHI SET SoTien=1 WHERE MaTC='$manual';" @(52602) $false
Expect 'T19-K07.Delete' "DELETE dbo.PHIEUTHUCHI WHERE MaTC='$manual';" @(52602) $false
Expect 'T19-K07.CategoryType' "UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Thu' WHERE MaDMTC='TC001';" @(52602) $false
Expect 'T19-K05.RawDml' "UPDATE dbo.PHIEUTHUCHI SET SoTien=1 WHERE MaTC='$manual';" @(229)
foreach($row in @("('NVL80',0,10000)","('NVL80',-1,10000)","('NVL80',0.00001,10000)","('NVL80',1,0)","('NVL80',1,-1)","('NVL80',1,0.5)","('NVL80',1,1000000000000000000)","('ZZ999',1,10000)","('NVL80',1,5000),('NVL80',1,5000)")){Expect 'T20-K02.Input' (PurchaseSql -rows $row) @(52701,52705)}
Expect 'T20-K02.Supplier' (PurchaseSql -supplier ZZ999) @(52701)
foreach($cash in @('-1','10001','0.5')){Expect "T20-K06.Immediate.$cash" (PurchaseSql -immediate $cash) @(52701,52703)}
Expect 'T20-K11.CASH' (PurchaseSql) @(51605) $true CASH
Expect 'T20-K11.STORE-money' (PurchaseSql -immediate 1) @(51605) $true STORE
$storeCode=Scalar (Runtime (PurchaseSql) STORE);Check 'T20-K11.STORE-zero' ([bool]$storeCode) $storeCode
$pnZero=NewPurchase;$pnFull=NewPurchase -immediate 10000;$pnPart=NewPurchase -immediate 4000
Check 'T20-K04.Zero' ((Scalar "SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaPN='$pnZero' AND TienNo=10000") -eq 1 -and (Scalar "SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN='$pnZero'") -eq 0) $pnZero
Check 'T20-K04.Full' ((Scalar "SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaPN='$pnFull'") -eq 0 -and (Scalar "SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN='$pnFull' AND SoTien=10000") -eq 1) $pnFull
Check 'T20-K05.Partial' ((Scalar "SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaPN='$pnPart' AND TienNo=6000") -eq 1 -and (Scalar "SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN='$pnPart' AND SoTien=4000") -eq 1) $pnPart
$round=NewPurchase -rows "('NVL80',0.0001,5000),('NVL81',0.0001,5000)" -total 2
Check 'T20-K01.RoundEachHalf' ((Scalar "SELECT TongTien FROM dbo.PHIEUNHAP WHERE MaPN='$round'") -eq 2) $round
foreach($statement in @("UPDATE dbo.PHIEUNHAP SET DaChot=0 WHERE MaPN='$pnPart'","UPDATE dbo.PHIEUNHAP SET MaNCC='NCC02' WHERE MaPN='$pnPart'","DELETE dbo.CHITIET_PN WHERE MaPN='$pnPart'","UPDATE dbo.CHITIET_PN SET SoLuong=2 WHERE MaPN='$pnPart'")){Expect 'T20-K08.Closed' $statement @(52702,51015,51016) $false}
foreach($row in @("('NVL80',0)","('NVL80',-1)","('NVL80',0.00001)","('ZZ999',1)","('NVL80',1),('NVL80',1)")){Expect 'T21-K03.Input' (IssueSql -rows $row) @(52801,52803)}
Expect 'T21-K02.Kind' (IssueSql -kind "N'Xuat ra quay'") @(52801)
Expect 'T21-K05.PartialRollback' (IssueSql -rows "('NVL80',0.1),('NVL81',999999)") @(52803)
Expect 'T21-K10.CASH' (IssueSql) @(51605) $true CASH
$px=Scalar (Runtime (IssueSql) STORE);Check 'T21-K10.STORE' ([bool]$px) $px
foreach($s in @("UPDATE dbo.PHIEUXUAT SET DaChot=0 WHERE MaPX='$px'","UPDATE dbo.CHITIET_PX SET SoLuongXuat=1 WHERE MaPX='$px'","DELETE dbo.CHITIET_PX WHERE MaPX='$px'")){Expect 'T21-K07.Closed' $s @(52802) $false}
$qty=[decimal](Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL82'");$q=$qty.ToString([Globalization.CultureInfo]::InvariantCulture);Sql (Runtime (IssueSql -rows "('NVL82',$q)"));Check 'T21-K04.ExactStockZero' ((Scalar "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL82'") -eq 0) 'NVL82=0'
# T22: số đếm, scope, ABA, ngày và khóa sau chốt.
$kk=NewKk;Check 'T22-K01.UncountedNull' ((Scalar "SELECT COUNT(*) FROM dbo.CHITIET_KK WHERE MaKK='$kk' AND TonThucTe IS NULL AND MaterialRevision IS NOT NULL") -eq 1) $kk
foreach($counts in @("('NVL80',-1,NULL)","('NVL80',0.00001,NULL)","('NVL80',1,NULL),('NVL80',1,NULL)","('NVL81',1,NULL)")){Expect 'T22-K02.InputScope' "DECLARE @a dbo.StocktakeCounts;INSERT @a VALUES$counts;EXEC dbo.sp_StocktakeSaveCounts @actor,@stamp,'$([guid]::NewGuid())','$kk',$(KkRevision $kk),@a;" @(52901)}
SaveKk $kk "('NVL80',0,NULL)";Expect 'T22-K03.ReasonRequired' (CommitSql $kk) @(52901)
$eq=[decimal](Scalar "SELECT TonHeThong FROM dbo.CHITIET_KK WHERE MaKK='$kk'");$eqText=$eq.ToString([Globalization.CultureInfo]::InvariantCulture)
SaveKk $kk "('NVL80',$eqText,NULL)";Sql (Runtime (CommitSql $kk));Check 'T22-K04.EqualValid' ((Scalar "SELECT DaDieuChinh FROM dbo.PHIEUKIEMKE WHERE MaKK='$kk'") -eq 1) $kk
foreach($s in @("UPDATE dbo.PHIEUKIEMKE SET DaDieuChinh=0 WHERE MaKK='$kk'","UPDATE dbo.PHIEUKIEMKE SET NgayKK=DATEADD(day,1,NgayKK) WHERE MaKK='$kk'","UPDATE dbo.CHITIET_KK SET TonThucTe=0 WHERE MaKK='$kk'","DELETE dbo.CHITIET_KK WHERE MaKK='$kk'")){Expect 'T22-K05.Immutable' $s @(52902,51115) $false}
Expect 'T22-K05.ChotAgain' (CommitSql $kk) @(52902)
$aba=NewKk "('NVL83')";SaveKk $aba "('NVL83',5,NULL)";$ignore=NewPurchase -rows "('NVL83',1,10000)";Sql (Runtime (IssueSql -rows "('NVL83',1)"));Expect 'T22-K07.ABA' (CommitSql $aba) @(52903)
$multi=NewKk "('NVL84'),('NVL85')";SaveKk $multi "('NVL84',5,NULL),('NVL85',5,NULL)";$ignore=NewPurchase -rows "('NVL85',1,10000)";Expect 'T22-K08.MultiRollback' (CommitSql $multi) @(52903)
foreach($offset in @(-1,1)){$fixture=if($offset -lt 0){'KK99000001'}else{'KK99000002'};Sql "INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV,DaDieuChinh,SnapshotAt,CutoverKind) SELECT '$fixture',DATEADD(day,$offset,CONVERT(date,SYSDATETIME())),MaNV,0,SYSDATETIME(),'New' FROM dbo.TAIKHOAN WHERE MaTK=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN');";Expect 'T22-K06.Date' (CommitSql $fixture) @(52902)}
Sql "INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV,DaDieuChinh,SnapshotAt,CutoverKind) SELECT 'KK99000003',CONVERT(date,SYSDATETIME()),MaNV,0,SYSDATETIME(),'New' FROM dbo.TAIKHOAN WHERE MaTK=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN');";Expect 'T22-K06.Empty' (CommitSql 'KK99000003') @(52901)
Expect 'T22-K11.CASH' "DECLARE @s dbo.StocktakeScope;INSERT @s VALUES('NVL80');EXEC dbo.sp_StocktakeCreate @actor,@stamp,'$([guid]::NewGuid())',@s;" @(51605) $true CASH
$storeKk=Scalar (Runtime "DECLARE @s dbo.StocktakeScope;INSERT @s VALUES('NVL86');EXEC dbo.sp_StocktakeCreate @actor,@stamp,'$([guid]::NewGuid())',@s;" STORE);Check 'T22-K11.STORE-create' ([bool]$storeKk) $storeKk
# T23: biên tiền/ngày, nguồn và cutoff.
$debt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$pnPart'")
function DebtSql([string]$debt,[string]$amount='1',[string]$method='PT001',[string]$date='20261027',[guid]$id=[guid]::NewGuid()){return "EXEC dbo.sp_TraCongNo @actor,@stamp,'$id','$debt','$method',$amount,'$date';"}
foreach($amount in @('0','-1','0.5','6001','1000000000000000000')){Expect 'T23-K04.Amount' (DebtSql $debt $amount) @(53001,53003)}
foreach($method in @('PT010','ZZ999')){Expect 'T23-K04.Method' (DebtSql $debt -method $method) @(53001)}
Expect 'T23-K05.SourceDate' (DebtSql $debt -date '20261026') @(53001)
foreach($role in @('CASH','STORE')){Expect "T23-K11.Pay.$role" (DebtSql $debt) @(51605) $true $role}
Expect 'T23-K11.CASH-read' "EXEC dbo.sp_DebtGet @actor,@stamp,'$debt';" @(51605) $true CASH
$storeDebt=Table (Runtime "EXEC dbo.sp_DebtGet @actor,@stamp,'$debt';" STORE);Check 'T23-K11.STORE-read' ($storeDebt.Rows.Count -eq 1) $storeDebt.Rows.Count
$paidCode=[string](Scalar (Runtime (DebtSql $debt -amount 1000)));$afterDebt=Scalar "SELECT ConNo FROM dbo.vw_CongNoNCC WHERE MaCN='$debt'";Check 'T23-K03.Partial' ($afterDebt -eq 5000) $afterDebt
foreach($s in @("UPDATE dbo.CONGNO_NCC SET TienNo=5999 WHERE MaCN='$debt'","DELETE dbo.THANHTOAN_CONGNO WHERE MaCN='$debt'","UPDATE dbo.PHIEUTHUCHI SET SoTien=999 WHERE MaTC='$paidCode'","UPDATE dbo.PHIEUNHAP SET MaNCC='NCC02' WHERE MaPN='$pnPart'")){Expect 'T23-K08.Immutable' $s @(52702,51017,52602) $false}
$historic=NewPurchase -rows "('NVL87',1,10000)" -immediate 2000 -date '20261025';$historyDebt=[string](Scalar "SELECT MaCN FROM dbo.CONGNO_NCC WHERE MaPN='$historic'")
$ignore=Scalar (Runtime (DebtSql $historyDebt -amount 1000 -date '20261026'));$ignore=Scalar (Runtime (DebtSql $historyDebt -amount 2000 -date '20261027'))
$asof=Table (Runtime "EXEC dbo.sp_DebtList @actor,@stamp,@Search='$historyDebt',@AsOf='20261026';");Check 'T23-K09.AsOf' ($asof.Rows.Count -eq 1 -and $asof.Rows[0].Paid -eq 1000 -and $asof.Rows[0].Remaining -eq 7000) @($asof.Rows|ForEach-Object{[ordered]@{Paid=$_.Paid;Remaining=$_.Remaining}})
$pre=Table (Runtime "EXEC dbo.sp_DebtList @actor,@stamp,@Search='$historyDebt',@AsOf='20261024';");Check 'T23-K09.BeforeSource' ($pre.Rows.Count -eq 0) $pre.Rows.Count
$max=Table (Runtime "EXEC dbo.sp_DebtList @actor,@stamp,@Search='$historyDebt',@AsOf='99991231';");Check 'T23-K09.MaxDate' ($max.Rows.Count -eq 1 -and $max.Rows[0].Remaining -eq 5000) $max.Rows.Count
Check 'T23-K10.ImmediateNotHistory' ((Scalar "SELECT COUNT(*) FROM dbo.THANHTOAN_CONGNO WHERE MaCN='$historyDebt'") -eq 2 -and (Scalar "SELECT DaTra FROM dbo.vw_CongNoNCC WHERE MaCN='$historyDebt'") -eq 3000) $historyDebt
$ignore=Scalar (Runtime (DebtSql $historyDebt -amount 5000));Check 'T23-K03.PaidOff' ((Scalar "SELECT ConNo FROM dbo.vw_CongNoNCC WHERE MaCN='$historyDebt'") -eq 0) $historyDebt
# MaPN/MaBL FK/unique, nguồn sai loại và rollback sau chi.
Expect 'T19-K03.PN-FK' "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaPN) VALUES('TC99000001',SYSDATETIME(),'TC012','NV001',1,'PurchaseImmediate','PN99999999');" @(547) $false
Expect 'T19-K03.BL-FK' "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaBL) VALUES('TC99000002',SYSDATETIME(),'TC005','NV001',1,'Payroll',2147483647);" @(547) $false
Expect 'T19-K03.PN-unique' "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaPN) VALUES('TC99000003',SYSDATETIME(),'TC012','NV001',1,'PurchaseImmediate','$pnPart');" @(2601,2627) $false
Sql "INSERT dbo.BANGLUONG(MaNV,Thang,Nam,LuongCoBan) VALUES('NV001',12,2098,1000);";$bl=[int](Scalar "SELECT MaBL FROM dbo.BANGLUONG WHERE MaNV='NV001' AND Thang=12 AND Nam=2098")
Sql "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaBL) VALUES('TC99000004',SYSDATETIME(),'TC005','NV001',1000,'Payroll',$bl);"
Expect 'T19-K03.BL-unique' "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaBL) VALUES('TC99000005',SYSDATETIME(),'TC005','NV001',1000,'Payroll',$bl);" @(2601,2627) $false
Expect 'T19-K04.SourceType' "INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,OriginKind,MaPN) VALUES('TC99000006',SYSDATETIME(),'TC009','NV001',1,'PurchaseImmediate','$pnZero');" @(52602) $false
# Trigger chỉ thuộc DBtest, được gỡ trong finally; nguồn/migration repo không đổi.
try{Sql "CREATE TRIGGER dbo.G05TestFaultFinance ON dbo.PHIEUTHUCHI AFTER INSERT AS BEGIN DECLARE @m nvarchar(2048)=N'Fault after finance insert, rows='+CONVERT(nvarchar(20),(SELECT COUNT(*) FROM inserted));THROW 53901,@m,1;END;";Expect 'T19-K06.FaultAfterWrite' "EXEC dbo.sp_FinanceCreate @actor,@stamp,'$([guid]::NewGuid())','20261027','TC001',1,NULL,NULL;" @(53901);Expect 'T20-K03.FaultAfterStockExpense' (PurchaseSql -immediate 1) @(53901)}finally{Sql 'DROP TRIGGER IF EXISTS dbo.G05TestFaultFinance;'}
try{Sql "CREATE TRIGGER dbo.G05TestFaultDebt ON dbo.THANHTOAN_CONGNO AFTER INSERT AS BEGIN THROW 53901,N'Fault after expense and debt link',1;END;";Expect 'T23-K04.FaultAfterExpenseLink' (DebtSql $debt) @(53901)}finally{Sql 'DROP TRIGGER IF EXISTS dbo.G05TestFaultDebt;'}
$idReplay=[guid]::NewGuid();Sql (Runtime (PurchaseSql -id $idReplay));Expect 'T20-K07.ReplayMismatch' (PurchaseSql -id $idReplay -rows "('NVL80',2,5000)") @(51608)
Save
Write-Output "Extended SQL records=$($results.Count), FAIL=$(@($results|Where-Object Result -eq 'FAIL').Count)"
$c.Dispose()

