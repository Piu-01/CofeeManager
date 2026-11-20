. "$PSScriptRoot/boundary-helpers.ps1"
Case T12-K04-boundaries '299/300/301/799/800/801 choose thresholds 0/300/300/300/800/800' @'
DECLARE @values TABLE(Points int,Threshold int);INSERT @values VALUES(299,0),(300,300),(301,300),(799,300),(800,800),(801,800);
WHILE EXISTS(SELECT 1 FROM @values)
BEGIN
 DECLARE @points int,@threshold int,@code char(5),@r binary(8);
 SELECT TOP(1) @points=Points,@threshold=Threshold FROM @values ORDER BY Points;
 SET @id=NEWID();EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Boundary customer',@Phone=NULL;
 SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;SELECT @r=Revision FROM dbo.KHACHHANG WHERE MaKH=@code;
 SET @id=NEWID();EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code=@code,@ExpectedRevision=@r,@Points=@points,@Source=N'Boundary acceptance';
 IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.DiemTichLuy=@points AND h.DiemToiThieu=@threshold) THROW 52999,'Boundary tier mismatch',1;
 DELETE @values WHERE Points=@points;
END;
'@
Case T13-K01-all-sources 'None0, tier10, KM20, tier+KM20; ship excluded' @'
DECLARE @d date='2026-10-18';
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,NULL,NULL) WHERE Source='None' AND Discount=0 AND Payable=115001) THROW 52999,'None formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,'KH001',NULL) WHERE Source='Tier' AND PercentDiscount=10 AND Discount=10000 AND Payable=105001) THROW 52999,'Tier formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,NULL,'KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Promotion formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,'KH001','KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Stacked formula',1;
'@
Case T13-K02-endpoints 'Both endpoints and same-day valid' @'
EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100003',@RequestId=@id,@Name=N'Boundary date',@StartDate='2026-10-17',@EndDate='2026-10-19',@Discount=5;
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-17',NULL,'KM99100003';
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-19',NULL,'KM99100003';
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-18',NULL,'KM99100002';
'@
Case T13-K02-before 'Before start rejected' "EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-16',NULL,'KM99100003';" 52001
Case T13-K02-after 'After end rejected' "EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-20',NULL,'KM99100003';" 52001
Case T13-K03-zero-full '0/100 discounts, half-VND and ship' @'
EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100004',@RequestId=@id,@Name=N'Zero',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=0;
SET @id=NEWID();EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100005',@RequestId=@id,@Name=N'Full',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=100;
SET @id=NEWID();EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100006',@RequestId=@id,@Name=N'Half',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=50;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(1,15000,'2026-10-18',NULL,'KM99100006') WHERE Discount=1 AND Payable=15000) THROW 52999,'Half rounding',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,'2026-10-18',NULL,'KM99100005') WHERE Discount=100001 AND Payable=15000) THROW 52999,'Ship discounted',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,'2026-10-18',NULL,'KM99100004') WHERE Discount=0 AND Payable=115001) THROW 52999,'Zero wrong',1;
'@
Case T13-K04-duplicate 'Duplicate thresholds rejected' "DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers VALUES('DONG',0,0),('BAC',0,3);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;" 52001
Case T13-K04-negative 'Negative threshold rejected' "DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers VALUES('DONG',0,0),('BAC',-1,3);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;" 52001
Case T13-K04-percent 'Percent101 rejected' "DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers VALUES('DONG',0,101);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;" 52001
Case T13-K06-overdraw 'Subtract more than balance rejected' "EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Tru',2147483647;" 52002
Case T13-K06-valid 'Add/subtract history updates once and replay stable' @'
DECLARE @old int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001');
DECLARE @preview TABLE(Balance bigint,CustomerRevision binary(8),PolicyRevision binary(8),Tier char(5));
INSERT @preview EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Cong',1;
DECLARE @cr binary(8),@pr binary(8);SELECT @cr=CustomerRevision,@pr=PolicyRevision FROM @preview;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Boundary add',@cr,@pr,@id;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Boundary add',@cr,@pr,@id;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>@old+1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001' AND LyDo=N'Boundary add')<>1 THROW 52999,'Double points',1;
'@
Case T13-K07-history-delete 'DELETE history rejected by trigger' "DELETE dbo.LICHSU_DIEM WHERE MaGD=(SELECT MAX(MaGD) FROM dbo.LICHSU_DIEM);" 51011
Case T13-K07-unpaid 'Unpaid order points rejected' "INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD20250410',N'Cong',1,N'Invalid unpaid');" 51012
Case T13-K07-duplicate 'Purchase points duplicated rejected' "INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD20250401',N'Cong',1,N'Duplicate purchase');" 2601
Case T13-K09-snapshot 'Changing referenced legacy KM preserves paid finance' @'
DECLARE @before nvarchar(max)=(SELECT MaHD,TongTien,GiamGia,ThanhTien,PhiShip,PhiDoiTac,PhanTramPhiDoiTac,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH),@code varchar(10),@sd date,@ed date;
SELECT TOP(1) @code=k.MaKM,@rev=k.Revision,@sd=k.NgayBD,@ed=k.NgayKT FROM dbo.KHUYENMAI k JOIN dbo.HOADON h ON h.MaKM=k.MaKM WHERE h.TrangThai=N'Da thanh toan';
EXEC dbo.sp_PromotionUpdate @a,@s,@code,@id,@rev,N'Acceptance snapshot change',@sd,@ed,25;
IF @before<>(SELECT MaHD,TongTien,GiamGia,ThanhTien,PhiShip,PhiDoiTac,PhanTramPhiDoiTac,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) THROW 52999,'Paid snapshot altered',1;
'@
Case T13-K05-reclassify 'Replace policy reclassifies all customers atomically and preserves points' @'
DECLARE @before nvarchar(max)=(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH),@tiers dbo.LoyaltyTierInput;
INSERT @tiers VALUES('DONG',0,0),('RICH',100,5),('VIP',1000,10);
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @r binary(8),@hash binary(32),@affected int;SELECT @r=Revision,@hash=PayloadHash,@affected=AffectedCustomers FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@r,@hash,@id;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaHang<>CASE WHEN DiemTichLuy>=1000 THEN 'VIP' WHEN DiemTichLuy>=100 THEN 'RICH' ELSE 'DONG' END) THROW 52999,'Reclassification mismatch',1;
IF @before<>(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) THROW 52999,'Policy changed points',1;
IF (SELECT ResultCount FROM dbo.G03OperationReceipt WHERE RequestId=@id)<>@affected THROW 52999,'Receipt count mismatch',1;
'@
