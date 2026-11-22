-- 01: nhiều ưu đãi cùng hóa đơn, tách hạng/khuyến mãi, ship không giảm, danh sách áp dụng.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoAuto[_]%' OR OBJECT_ID('dbo.PromoAutoTestMarker') IS NULL
 THROW 53702,N'Chỉ DB test PromoAuto có marker.',1;
DECLARE @a int,@s binary(8),@cash int,@cashStamp binary(8),@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
SELECT @cash=MaTK,@cashStamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV004';
DECLARE @json nvarchar(max)=(SELECT * FROM(VALUES('SP001',CONVERT(decimal(18,0),50000)),('SP002',CONVERT(decimal(18,0),30000)))v(Product,Amount)
 FOR JSON PATH,INCLUDE_NULL_VALUES);

-- KH902 hạng VANG 5%, lần đầu: FIRST 20% SP001 + TIER 10% SP002 + ALL 5% toàn bộ.
DECLARE @t TABLE(TierPercent int,PromotionPercent int,TierDiscount decimal(18,0),PromotionDiscount decimal(18,0),Discount decimal(18,0));
INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH902',NULL,@json);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>5 OR TierDiscount<>4000 OR PromotionDiscount<>17000 OR Discount<>21000)
 THROW 53702,N'Tổng giảm 3 ưu đãi sai (tier 4000, promo 17000, discount 21000).',1;
DECLARE @ap TABLE(MaKM char(10),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0));
INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH902',NULL,@json);
IF (SELECT COUNT(*) FROM @ap)<>3 OR (SELECT SUM(Discount) FROM @ap)<>17000
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000001' AND [Percent]=20 AND BaseAmount=50000 AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000002' AND [Percent]=10 AND BaseAmount=30000 AND Discount=3000)
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000003' AND [Percent]=5 AND BaseAmount=80000 AND Discount=4000)
 THROW 53702,N'Phân bổ 3 ưu đãi áp dụng sai.',1;
IF EXISTS(SELECT 1 FROM @ap WHERE MaKM NOT IN('KM90000001','KM90000002','KM90000003'))
 THROW 53702,N'Ưu đãi không thỏa điều kiện vẫn được áp.',1;

-- Khách vãng lai: không FIRST/TIER, chỉ ALL 5%.
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@json);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>0 OR TierDiscount<>0 OR Discount<>4000)
 THROW 53702,N'Khách vãng lai chỉ được ALL 5% (4000).',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@json);
IF (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000003' AND Discount=4000)
 THROW 53702,N'Danh sách khách vãng lai phải chỉ có ALL 5%.',1;

-- KH901 hạng DONG: FIRST 20% + ALL 5%, không TIER.
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH901',NULL,@json);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>0 OR Discount<>14000)
 THROW 53702,N'KH901 phải giảm 14000 (FIRST 20% + ALL 5%).',1;

-- TVP rỗng dùng @Money như một dòng giả: chỉ ưu đãi ALL, không áp ưu đãi theo món.
DECLARE @empty dbo.PreviewLines;
DECLARE @moneyJson nvarchar(max)=(SELECT CONVERT(char(5),NULL) Product,CONVERT(decimal(18,0),80000) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH902',NULL,@moneyJson);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>5 OR TierDiscount<>4000 OR Discount<>8000)
 THROW 53702,N'TVP rỗng: chỉ hạng 5% + ALL 5% trên 80000 (8000).',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH902',NULL,@moneyJson);
IF (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000003' AND BaseAmount=80000 AND Discount=4000)
 THROW 53702,N'TVP rỗng phải chỉ áp ALL 5%.',1;

-- Gọi thật sp_DiscountPreview (hai shape result set) và sp_PromotionList cho khách/ADMIN.
DECLARE @line dbo.PreviewLines; INSERT @line VALUES('SP001',2),('SP002',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=15000,@BusinessDate=@today,@Customer='KH902',@Lines=@line;
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=80000,@Ship=15000,@BusinessDate=@today,@Customer='KH902',@Lines=@empty;
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=80000,@Ship=0,@BusinessDate=@today,@Customer=NULL,@Lines=@empty;

DECLARE @lst TABLE(Code char(10),Name nvarchar(100),StartDate date,EndDate date,Discount int,TrangThai varchar(10),
 Audience varchar(10),Scope varchar(4),ItemCount int,Revision binary(8));
INSERT @lst EXEC dbo.sp_PromotionList @a,@s,N'KM900000',0,NULL;
IF (SELECT COUNT(*) FROM @lst)<>13 THROW 53702,N'ADMIN phải thấy 13 ưu đãi KM9.',1;
IF NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000009' AND TrangThai='HetHan')
 OR NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000007' AND TrangThai='TamDung')
 OR NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000008' AND TrangThai='Nhap')
 OR NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000006' AND EndDate IS NULL AND TrangThai='DangChay')
 OR NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000010' AND TrangThai='DangChay')
 OR NOT EXISTS(SELECT 1 FROM @lst WHERE Code='KM90000002' AND Audience='TIER' AND Scope='ITEM' AND ItemCount=1)
 THROW 53702,N'Trạng thái/đối tượng/phạm vi danh sách ưu đãi sai.',1;
DECLARE @cashList TABLE(Code char(10),Name nvarchar(100),StartDate date,EndDate date,Discount int,TrangThai varchar(10),
 Audience varchar(10),Scope varchar(4),ItemCount int,Revision binary(8));
INSERT @cashList EXEC dbo.sp_PromotionList @cash,@cashStamp,N'KM900000',0,@today;
IF (SELECT COUNT(*) FROM @cashList)<>8 THROW 53702,N'CASH chỉ thấy 8 ưu đãi đang chạy còn hiệu lực.',1;
IF EXISTS(SELECT 1 FROM @cashList WHERE Code IN('KM90000007','KM90000008','KM90000009','KM90000010','KM90000013'))
 THROW 53702,N'CASH thấy ưu đãi tạm dừng/nháp/hết hạn/tương lai.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53702,N'Giao dịch không sạch.',1;
