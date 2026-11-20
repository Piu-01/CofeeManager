$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$db=$m.Database
$results=[Collections.Generic.List[object]]::new()
function Case([string]$Id,[string]$Expected,[string]$Sql,[int]$ErrorNumber=0,[string]$Role='ADMIN',[bool]$Runtime=$false){
 $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new("Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();$r=[ordered]@{Id=$Id;Expected=$Expected;Actual=$null;Status='NOT RUN';Role=$Role;RuntimePrincipal=$Runtime;SqlFile="v2-$Id.sql"}
 try{
  if((Read-TestScalar $c 'SELECT RunId FROM dbo.G03TestMarker') -cne $m.RunId){throw 'Marker mismatch'}
  $prefix="DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='$Role' AND HoatDong=1 ORDER BY MaTK;"
  if($Runtime){$prefix+="EXECUTE AS USER='G03Runtime';"}
  $command=$prefix+"`n"+$Sql;$command|Set-Content "$out/v2-$Id.sql" -Encoding utf8
  if($ErrorNumber){Expect-TestSqlError $c $command $ErrorNumber;$r.Actual="SQL error $ErrorNumber; transaction 0:0"}else{Run-TestSql $c $command;if((Read-TestScalar $c 'DECLARE @t int=@@TRANCOUNT,@x int=XACT_STATE();SELECT CONCAT(@t,'':'',@x);') -ne '0:0'){throw 'Transaction leaked'};$r.Actual='Assertions completed; transaction 0:0'}
  $r.Status='PASS'
 }catch{$r.Status='FAIL';$r.Actual=$_.Exception.Message}
 finally{$c.Dispose();$results.Add($r);$results|ConvertTo-Json -Depth 5|Set-Content "$out/sql-results-v2.json" -Encoding utf8}
}
Case T09-K01-a 'Create zero, exact opening, replay once' @'
EXEC dbo.sp_MaterialCreate @a,@s,'NVL95',@id,NULL,N'Kiểm thử cà phê',N'Kg';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')<>0 THROW 52999,'Initial not zero',1;
SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';SET @id=NEWID();
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=0.0180,@Source=N'Acceptance';
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=0.0180,@Source=N'Acceptance';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')<>0.0180 OR (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL95')<>1 THROW 52999,'Opening mismatch',1;
'@
Case T09-K03-a 'Second opening rejected' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=1,@Source=N'Second';" 51602
Case T09-K04-a 'Scale5 rejected' "EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@Quantity=0.00001,@Source=N'Scale';" 51601
Case T09-K04-b 'Negative rejected' "EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@Quantity=-0.0001,@Source=N'Negative';" 51601
Case T09-K04-c 'Blank unit rejected' "EXEC dbo.sp_MaterialCreate @Actor=@a,@Stamp=@s,@Code='NVL93',@RequestId=@id,@Name=N'Test',@Unit=N' ';" 51601
Case T09-K06-a 'Rename keeps stock' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';EXEC dbo.sp_MaterialUpdate @a,@s,'NVL95',@id,@rev,N'Đổi tên',N'Kg';IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')<>0.0180 THROW 52999,'Stock changed',1;"
Case T09-K06-b 'Unit change after opening rejected' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';EXEC dbo.sp_MaterialUpdate @a,@s,'NVL95',@id,@rev,N'Test',N'g';" 51602
Case T09-K07-a 'CASH runtime read' "EXEC dbo.sp_MaterialList @a,@s,N'',0;" 0 CASH $true
Case T09-K07-b 'CASH create denied' "EXEC dbo.sp_MaterialCreate @a,@s,'NVL94',@id,NULL,N'Test',N'Kg';" 51605 CASH $true
Case T09-K07-c 'STORE opening denied' "EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@Quantity=0,@Source=N'Store';" 51605 STORE $true
Case T09-K07-d 'Direct stock DML denied' "UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=0 WHERE MaNVL='NVL95';" 229 ADMIN $true
Case T09-K08-a 'Delete referenced material rejected' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01';EXEC dbo.sp_MaterialDelete @a,@s,'NVL01',@id,@rev;" 51602
Case T10-K01-a 'Multi-line recipe saved' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',0.0180),('NVL02',0.0200);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP995',@RequestId=@id,@Recipe=@r,@Name=N'Món kiểm thử',@Category='DM001',@Price=10000,@Status=N'Dang ban';IF (SELECT COUNT(*) FROM dbo.CONGTHUC WHERE MaSP='SP995')<>2 THROW 52999,'Missing lines',1;"
Case T10-K02-a 'Invalid recipe rejected' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',-1);SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP995';EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP995',@id,@r,@rev;" 51701
Case T10-K03-a 'Old recipe remains after error' "IF (SELECT COUNT(*) FROM dbo.CONGTHUC WHERE MaSP='SP995')<>2 OR (SELECT DinhMuc FROM dbo.CONGTHUC WHERE MaSP='SP995' AND MaNVL='NVL01')<>0.0180 THROW 52999,'Recipe mutated',1;"
Case T10-K04-a 'Cannot sell without recipe' "DECLARE @r dbo.RecipeLineInput;EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP992',@RequestId=@id,@Recipe=@r,@Name=N'Missing',@Category='DM001',@Price=10000,@Status=N'Dang ban';" 51702
Case T10-K09-a 'STORE runtime recipe read' "EXEC dbo.sp_RecipeList @a,@s,'SP995';" 0 STORE $true
Case T10-K09-b 'CASH recipe mutation denied' "DECLARE @r dbo.RecipeLineInput;EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP995',@id,@r;" 51605 CASH $true
Case T11-K03-a 'Seed view B0003 serving' "IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0003' AND TrangThai=N'Dang phuc vu' AND MaHD='HD20250410') THROW 52999,'Seed mismatch',1;EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';"
Case T11-K06-a 'Area with tables cannot delete' "SELECT @rev=Revision FROM dbo.KHUVUC WHERE MaKV='KV001';EXEC dbo.sp_AreaDelete @a,@s,'KV001',@id,@rev;" 547
Case T11-K08-a 'STORE map read denied' "EXEC dbo.sp_TableList @a,@s,N'',0;" 51605 STORE $true
Case T11-K08-b 'CASH map read succeeds' "EXEC dbo.sp_TableList @a,@s,N'',0;" 0 CASH $true
Case T12-K01-a 'Customer zero gets threshold zero' @'
EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'GUI khách kiểm thử',@Phone=NULL;
DECLARE @code char(5)=(SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id);
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.DiemTichLuy=0 AND h.DiemToiThieu=0) THROW 52999,'Wrong initial tier',1;
'@
Case T12-K02-a 'Multiple NULL phones accepted' "EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'NULL phone2',@Phone=N' ';"
Case T12-K02-b 'Duplicate phone denied' "EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Duplicate',@Phone='0911111111';" 2601
Case T12-K03-a 'Overlength name rejected' "DECLARE @name nvarchar(max)=REPLICATE(N'X',51);EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=@name;" 51901
Case T12-K08-a 'CASH opening denied' "EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='KH001',@Points=100,@Source=N'CASH';" 51605 CASH $true
Case T12-K08-b 'STORE customer read denied' "EXEC dbo.sp_CustomerList @a,@s,N'',0;" 51605 STORE $true
Case T13-K01-a 'KM replaces tier; ship not discounted' @'
DECLARE @date date='2026-10-18';EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100002',@RequestId=@id,@Name=N'Acceptance20%',@StartDate=@date,@EndDate=@date,@Discount=20;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@date,'KH001','KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Formula mismatch',1;
'@
Case T13-K02-a 'Outside KM date rejected' "EXEC dbo.sp_DiscountPreview @a,@s,100001,15000,'2026-10-19','KH001','KM99100002';" 52001
Case T13-K03-a 'No discount adds whole ship' "IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(1,15000,'2026-10-18',NULL,NULL) WHERE Discount=0 AND Payable=15001) THROW 52999,'No discount wrong',1;"
Case T13-K04-a 'Missing zero threshold rejected' "DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers VALUES('TEST',1,3);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;" 52001
Case T13-K06-a 'Blank reason rejected' "EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N' ',NULL,NULL,@id;" 52001
Case T13-K07-a 'Runtime history DML denied' "UPDATE dbo.LICHSU_DIEM SET LyDo=N'Forbidden';" 229 ADMIN $true
Case T13-K08-a 'Runtime points DML denied' "UPDATE dbo.KHACHHANG SET DiemTichLuy=0 WHERE MaKH='KH001';" 229 ADMIN $true
Case T13-K10-a 'CASH policy operation denied' "DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers VALUES('DONG',0,0);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;" 51605 CASH $true
Case T13-K10-b 'STORE KM read denied' "EXEC dbo.sp_PromotionList @a,@s,N'',0;" 51605 STORE $true
$results|Group-Object Status|Select-Object Name,Count|Format-Table

