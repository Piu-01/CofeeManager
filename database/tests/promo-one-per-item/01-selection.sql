-- 01 (PP01/PP02/PP03/PP05): mỗi dòng một KM, chọn tổ hợp tổng giảm lớn nhất, hòa tất định.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53802,N'Chỉ DB test PromoOnePerItem có marker.',1;
DECLARE @today date=CONVERT(date,SYSDATETIME());
DECLARE @t TABLE(TierPercent int,PromotionPercent int,TierDiscount decimal(18,0),PromotionDiscount decimal(18,0),Discount decimal(18,0));
DECLARE @ap TABLE(MaKM char(10),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0));
DECLARE @d TABLE(No int,Amount decimal(18,0),[Percent] int,Discount decimal(18,0));

-- PP01a: A 100k + B 60k + C 40k. KM1(A 10%) + KM3(B+C 40%) = 50 000; KM2 xung đột B nên không chọn cả KM2+KM3.
DECLARE @abc nvarchar(max)=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)),
 ('SP941',CONVERT(decimal(18,0),60000)),('SP942',CONVERT(decimal(18,0),40000)))v(Product,Amount)
 FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@abc);
IF EXISTS(SELECT 1 FROM @t WHERE TierPercent<>0 OR PromotionPercent<>25 OR PromotionDiscount<>50000 OR Discount<>50000)
 THROW 53802,N'PP01a: A+B+C phải giảm KM 50 000 (25%).',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@abc);
IF (SELECT COUNT(*) FROM @ap)<>2 OR (SELECT SUM(Discount) FROM @ap)<>50000
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000001' AND [Percent]=10 AND BaseAmount=100000 AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000003' AND [Percent]=40 AND BaseAmount=100000 AND Discount=40000)
 OR EXISTS(SELECT 1 FROM @ap WHERE MaKM IN('KM93000002','KM93000004'))
 THROW 53802,N'PP01a: phân bổ phải là KM1(A)+KM3(B+C).',1;

-- PP01b: A2 100k + B2 60k + C2 40k. KM11(A2 10%) + KM12(B2 50%) = 40 000; KM13 xung đột B2 nên không chọn.
DECLARE @abc2 nvarchar(max)=(SELECT * FROM(VALUES('SP945',CONVERT(decimal(18,0),100000)),
 ('SP946',CONVERT(decimal(18,0),60000)),('SP947',CONVERT(decimal(18,0),40000)))v(Product,Amount)
 FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@abc2);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>40000 OR Discount<>40000)
 THROW 53802,N'PP01b: A2+B2+C2 phải giảm 40 000.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@abc2);
IF (SELECT COUNT(*) FROM @ap)<>2 OR (SELECT SUM(Discount) FROM @ap)<>40000
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000011' AND BaseAmount=100000 AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000012' AND BaseAmount=60000 AND Discount=30000)
 OR EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000013')
 THROW 53802,N'PP01b: phân bổ phải là KM11(A2)+KM12(B2).',1;

-- PP02: chọn tổng giảm lớn hơn giữa KM theo món và KM ALL, không ưu tiên đè.
DECLARE @one nvarchar(max);
SET @one=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@one);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>10000 OR Discount<>10000) THROW 53802,N'PP02a: A phải chọn KM1 10%.',1;
SET @one=(SELECT * FROM(VALUES('SP943',CONVERT(decimal(18,0),20000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@one);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@one);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>1000)
 OR (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000004' AND Discount=1000)
 THROW 53802,N'PP02b: D 20k phải chọn ALL 5% (1 000) thay KM7 1% (200).',1;
SET @one=(SELECT * FROM(VALUES('SP948',CONVERT(decimal(18,0),50000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@one);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>2500) THROW 53802,N'PP02c: E chỉ có ALL 5% = 2 500.',1;

-- PP03: hai dòng cùng món A (100k + 50k) đều thuộc KM1, KM1 chỉ xuất hiện một lần với Base 150 000.
DECLARE @two nvarchar(max)=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)),
 ('SP940',CONVERT(decimal(18,0),50000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@two);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>15000) THROW 53802,N'PP03: hai dòng A phải giảm 15 000.',1;
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@two);
IF (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000001' AND BaseAmount=150000 AND Discount=15000)
 THROW 53802,N'PP03: KM1 phải gộp hai dòng, Base 150 000.',1;
DELETE @d; INSERT @d SELECT * FROM dbo.fn_UuDaiDong(@today,'KH931',NULL,@two);
IF (SELECT COUNT(*) FROM @d)<>2 OR EXISTS(SELECT 1 FROM @d WHERE [Percent]<>10)
 OR NOT EXISTS(SELECT 1 FROM @d WHERE No=1 AND Amount=100000 AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM @d WHERE No=2 AND Amount=50000 AND Discount=5000)
 THROW 53802,N'PP03: fn_UuDaiDong phải trả phần KM từng dòng, không cộng hạng.',1;

-- PP02d/PP05: hai KM cùng 10% trên F hòa 3 000; chọn ít KM hơn rồi mã nhỏ hơn -> KM93000008.
SET @one=(SELECT * FROM(VALUES('SP949',CONVERT(decimal(18,0),30000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@one);
IF (SELECT COUNT(*) FROM @ap)<>1 OR NOT EXISTS(SELECT 1 FROM @ap WHERE MaKM='KM93000008' AND Discount=3000)
 THROW 53802,N'PP05: hòa 10% phải chọn KM93000008.',1;
DECLARE @first char(10)=(SELECT MaKM FROM @ap);
DELETE @ap; INSERT @ap SELECT * FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@one);
IF (SELECT MaKM FROM @ap)<>@first THROW 53802,N'PP05: kết quả phải tất định.',1;

-- PP02e: KM 100% chặn ở 100% dòng, G 20k giảm 20 000.
SET @one=(SELECT * FROM(VALUES('SP950',CONVERT(decimal(18,0),20000)))v(Product,Amount) FOR JSON PATH,INCLUDE_NULL_VALUES);
DELETE @t; INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@one);
IF EXISTS(SELECT 1 FROM @t WHERE PromotionPercent<>100 OR PromotionDiscount<>20000 OR Discount<>20000)
 THROW 53802,N'PP02e: G 100% phải giảm 20 000.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53802,N'Giao dịch không sạch.',1;
