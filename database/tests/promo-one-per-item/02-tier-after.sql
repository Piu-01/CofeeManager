-- 02 (PP04): hạng thành viên tính trên hóa đơn sau khuyến mãi; % hiển thị là mức giảm thực tế.
-- Đọc fn_UuDaiTong/fn_UuDaiChia (không dùng INSERT..EXEC vì proc preview có applock + ROLLBACK
-- trong CATCH, SQL Server trả 3915 khi capture); gọi sp_DiscountPreview không capture làm smoke.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53803,N'Chỉ DB test PromoOnePerItem có marker.',1;
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @t TABLE(TierPercent int,PromotionPercent int,TierDiscount decimal(18,0),PromotionDiscount decimal(18,0),Discount decimal(18,0));
DECLARE @ap TABLE(Code char(10),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0));
DECLARE @lines dbo.PreviewLines;

-- PP04a: KH932 hạng VÀNG 5%, A 100k giảm KM 10% = 10k; hạng 5% của 90k = 4 500; tổng 14 500 (không phải 15% của 100k).
DECLARE @json nvarchar(max)=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH932',NULL,@json);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>5 OR PromotionPercent<>10 OR TierDiscount<>4500
 OR PromotionDiscount<>10000 OR Discount<>14500)
 THROW 53803,N'PP04a: hạng phải tính trên phần còn lại (4 500 + 10 000 = 14 500).',1;
DELETE @lines; INSERT @lines VALUES('SP940',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=0,@BusinessDate=@today,
 @Customer='KH932',@Lines=@lines;

-- PP04a-2 (ví dụ requirements): H 100 000 giảm KM 20 000, hạng 5% của 80 000 = 4 000; tổng 24 000 (không phải 25 000).
DECLARE @jsonH nvarchar(max)=(SELECT * FROM(VALUES('SP951',CONVERT(decimal(18,0),100000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH932',NULL,@jsonH);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>5 OR PromotionPercent<>20 OR TierDiscount<>4000
 OR PromotionDiscount<>20000 OR Discount<>24000)
 THROW 53803,N'PP04a-2: ví dụ 100k/20k/5% phải giảm 24 000.',1;
DELETE @lines; INSERT @lines VALUES('SP951',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=0,@BusinessDate=@today,
 @Customer='KH932',@Lines=@lines;

-- PP04b: A+B+C 200k, KM 50k; hạng 5% của 150k = 7 500; tổng 57 500; % thực tế 29 (28,75 làm tròn);
-- phải trả 157 500 khi ship 15 000.
DECLARE @jsonAbc nvarchar(max)=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)),
 ('SP941',CONVERT(decimal(18,0),60000)),('SP942',CONVERT(decimal(18,0),40000)))v(Product,Amount)
 FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH932',NULL,@jsonAbc);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>5 OR PromotionPercent<>25 OR TierDiscount<>7500
 OR PromotionDiscount<>50000 OR Discount<>57500) OR 200000+15000-(SELECT Discount FROM @t)<>157500
 THROW 53803,N'PP04b: hạng/KM A+B+C phải là 50 000 + 7 500.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH932',NULL,@jsonAbc);
IF (SELECT COUNT(*) FROM @ap)<>2 OR (SELECT SUM(Discount) FROM @ap)<>50000
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE Code='KM93000001' AND BaseAmount=100000 AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE Code='KM93000003' AND BaseAmount=100000 AND Discount=40000)
 THROW 53803,N'PP04b: danh sách KM preview sai.',1;
DELETE @lines; INSERT @lines VALUES('SP940',1),('SP941',1),('SP942',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=15000,@BusinessDate=@today,
 @Customer='KH932',@Lines=@lines;

-- PP04c: KH931 hạng ĐỒNG 0% chỉ giảm KM, không có dòng hạng.
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@json);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>0 OR TierDiscount<>0 OR PromotionDiscount<>10000 OR Discount<>10000)
 THROW 53803,N'PP04c: hạng ĐỒNG không được giảm thêm.',1;
DELETE @lines; INSERT @lines VALUES('SP940',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=0,@BusinessDate=@today,
 @Customer='KH931',@Lines=@lines;

-- PP04d: KM 100% (20k) làm phần còn lại bằng 0, hạng không âm và % thực tế 100.
DECLARE @jsonG nvarchar(max)=(SELECT * FROM(VALUES('SP950',CONVERT(decimal(18,0),20000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH932',NULL,@jsonG);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>20000 OR TierDiscount<>0 OR Discount<>20000
 OR PromotionPercent<>100)
 THROW 53803,N'PP04d: KM 100% phải chặn hạng ở 0.',1;
DELETE @lines; INSERT @lines VALUES('SP950',1);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=0,@BusinessDate=@today,
 @Customer='KH932',@Lines=@lines;

-- PP04e: sp_DiscountPreview nhận tiền tạm tính không dòng món (@Money) theo cùng luật.
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=100000,@Ship=0,@BusinessDate=@today,@Customer='KH932';

IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53803,N'Giao dịch không sạch.',1;
