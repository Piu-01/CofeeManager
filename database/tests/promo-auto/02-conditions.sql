-- 02: điều kiện đối tượng/món, trạng thái, biên ngày, làm tròn, chặn 100%/dòng, legacy.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoAuto[_]%' OR OBJECT_ID('dbo.PromoAutoTestMarker') IS NULL
 THROW 53703,N'Chỉ DB test PromoAuto có marker.',1;
DECLARE @today date=CONVERT(date,SYSDATETIME()),@a int,@s binary(8);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @t TABLE(TierPercent int,PromotionPercent int,TierDiscount decimal(18,0),PromotionDiscount decimal(18,0),Discount decimal(18,0));
DECLARE @ap TABLE(MaKM char(10),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0));

-- FIRST: khách chưa có hóa đơn đã trả được 20% SP001; khách đã có hóa đơn thì không.
DECLARE @json1 nvarchar(max)=(SELECT 'SP001' Product,CONVERT(decimal(18,0),25000) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH901',NULL,@json1);
IF NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000001' AND Discount=5000)
 THROW 53703,N'Khách lần đầu phải được FIRST 20% SP001.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH001',NULL,@json1);
IF EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000001') THROW 53703,N'Khách đã từng trả tiền không được FIRST.',1;
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH001',NULL,@json1);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>3 OR TierDiscount<>750 OR Discount<>2000)
 THROW 53703,N'KH001 hạng BAC 3% + ALL 5% phải giảm 2000.',1;

-- MEMBER: có hồ sơ thì 10% SP004, khách vãng lai thì không.
DECLARE @json4 nvarchar(max)=(SELECT 'SP004' Product,CONVERT(decimal(18,0),55000) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH901',NULL,@json4);
IF NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000004' AND Discount=5500)
 THROW 53703,N'Khách có hồ sơ phải được MEMBER 10% SP004.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@json4);
IF EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000004') THROW 53703,N'Khách vãng lai không được MEMBER.',1;

-- TIER: VANG 10% SP002; DONG thì không.
DECLARE @json2 nvarchar(max)=(SELECT 'SP002' Product,CONVERT(decimal(18,0),30000) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH902',NULL,@json2);
IF NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000002' AND Discount=3000)
 THROW 53703,N'Hạng VANG phải được TIER 10% SP002.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH901',NULL,@json2);
IF EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000002') THROW 53703,N'Hạng DONG không được ưu đãi TIER VANG.',1;

-- Làm tròn theo dòng: SP901 giá 105, 5% + 7% gộp 12% = 13 (không phải 5 + 7 = 12).
DECLARE @json9 nvarchar(max)=(SELECT 'SP901' Product,CONVERT(decimal(18,0),105) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@json9);
IF EXISTS(SELECT 1 FROM @t WHERE Discount<>13) THROW 53703,N'Làm tròn gộp 12% trên 105 phải là 13.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@json9);
IF (SELECT SUM(Discount) FROM @ap)<>13 THROW 53703,N'Phân bổ làm tròn phải giữ tổng 13.',1;
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH902',NULL,@json9);
IF EXISTS(SELECT 1 FROM @t WHERE TierDiscount<>5 OR PromotionDiscount<>13 OR Discount<>18)
 THROW 53703,N'Hạng VANG + 12% trên 105 phải là 18 (tier 5, promo 13).',1;

-- Chặn 100%/dòng: hạng 5% + 60% + 60% + 5% = 130% -> 100%.
DECLARE @json3 nvarchar(max)=(SELECT 'SP003' Product,CONVERT(decimal(18,0),32000) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH902',NULL,@json3);
IF EXISTS(SELECT 1 FROM @t WHERE TierDiscount<>1600 OR Discount<>32000)
 THROW 53703,N'Chặn 100%/dòng trên 32000 phải giảm đủ 32000, hạng 1600.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH902',NULL,@json3);
IF (SELECT COUNT(*) FROM @ap)<>3 OR (SELECT SUM(Discount) FROM @ap)<>30400
 THROW 53703,N'Phân bổ ưu đãi khi chặn 100% phải còn 30400.',1;

-- Ngày kết thúc NULL = vô hạn: SP902 30% vẫn áp ở ngày rất xa; ưu đãi có hạn thì không.
DECLARE @json10 nvarchar(max)=(SELECT 'SP902' Product,CONVERT(decimal(18,0),33333) Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(DATEADD(year,3,@today),NULL,NULL,@json10);
IF EXISTS(SELECT 1 FROM @t WHERE Discount<>10000) THROW 53703,N'Ưu đãi không giới hạn phải áp 30% của 33333 = 10000.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(DATEADD(year,3,@today),NULL,NULL,@json10);
IF (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM90000006')
 THROW 53703,N'Ngày xa chỉ còn ưu đãi không giới hạn.',1;

-- Trạng thái Nháp/Tạm dừng/Hết hạn/Tương lai không áp; biên ngày cuối vẫn áp.
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH901',NULL,@json1);
IF EXISTS(SELECT 1 FROM @t WHERE Discount<>6250) THROW 53703,N'Ưu đãi tạm dừng/nháp/hết hạn/tương lai đã lọt vào.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH901',NULL,@json1);
IF (SELECT COUNT(*) FROM @ap)<>2 OR EXISTS(SELECT 1 FROM @ap WHERE MaKM IN('KM90000007','KM90000008','KM90000009','KM90000010'))
 THROW 53703,N'Danh sách áp dụng chứa ưu đãi không hoạt động.',1;
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(DATEADD(day,30,@today),'KH901',NULL,@json1);
IF EXISTS(SELECT 1 FROM @t WHERE Discount<>6250) THROW 53703,N'Ngày kết thúc vẫn phải áp.',1;
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(DATEADD(day,31,@today),'KH901',NULL,@json1);
IF EXISTS(SELECT 1 FROM @t WHERE Discount<>0) THROW 53703,N'Sau ngày kết thúc phải hết hiệu lực.',1;

-- Legacy: đơn mở cũ có MaKM được áp đúng một lần, cộng ALL 5%, không nhân đôi.
DECLARE @legacy char(10)=(SELECT MaHD FROM dbo.HOADON WHERE MaKM='KM90000013');
IF @legacy IS NULL THROW 53703,N'Thiếu đơn legacy của fixture.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_HoaDonGiamGia(@legacy) WHERE Discount=6250 AND TierDiscount=0 AND PromotionDiscount=6250)
 THROW 53703,N'Đơn legacy phải giảm 6250 (20% KM + 5% ALL).',1;
DECLARE @legacyAp TABLE(MaKM char(10),Name nvarchar(200),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0),
 PromotionRevision binary(8),Audience varchar(10),Scope varchar(4));
INSERT @legacyAp SELECT * FROM dbo.fn_HoaDonUuDaiApDung(@legacy);
IF (SELECT COUNT(*) FROM @legacyAp)<>2 OR (SELECT SUM(Discount) FROM @legacyAp)<>6250
 OR NOT EXISTS(SELECT 1 FROM @legacyAp WHERE MaKM='KM90000013' AND Discount=5000)
 OR NOT EXISTS(SELECT 1 FROM @legacyAp WHERE MaKM='KM90000003' AND Discount=1250)
 THROW 53703,N'Danh sách legacy sai hoặc nhân đôi ưu đãi.',1;

-- Đọc/ghi vẫn chạy sau các kiểm tra.
DECLARE @legacyRev binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@legacy);
EXEC dbo.sp_SalesCheckoutPreview @a,@s,@legacy,@legacyRev;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53703,N'Giao dịch không sạch.',1;
