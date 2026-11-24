-- 05 (PP08): đo thời gian khi số KM chồng chéo vượt ngưỡng duyệt chính xác (>15).
-- Không thanh toán; chỉ tạo đơn mở sau khi 04 đã chốt regression.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53806,N'Chỉ DB test PromoOnePerItem có marker.',1;
IF OBJECT_ID('dbo.PromoOnePerItemTiming') IS NULL
 CREATE TABLE dbo.PromoOnePerItemTiming(RunLabel varchar(40) NOT NULL PRIMARY KEY,Ms decimal(18,3) NOT NULL,RecordedAt datetime2(0) NOT NULL);
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME()),@id uniqueidentifier,@code varchar(10);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
DECLARE @lines dbo.SalesDraftLines;
INSERT @lines VALUES(1,'SP952',1,NULL,100000),(2,'SP952',1,NULL,100000),(3,'SP952',1,NULL,100000);
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SET @code=(SELECT Ma FROM @codes); SET @id=NEWID();
EXEC dbo.sp_TaoHoaDonTuNhap @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',
 @Customer='KH931',@Lines=@lines;
DECLARE @rev binary(8); SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @t0 datetime2(7)=SYSDATETIME();
DECLARE @preview TABLE(Source varchar(20),PercentDiscount int,Discount decimal(18,0),Payable decimal(38,0),PromotionCount int);
INSERT @preview(Source,PercentDiscount,Discount,Payable,PromotionCount)
 SELECT Source,PercentDiscount,Discount,Payable,PromotionCount FROM dbo.fn_CheckoutPreview(@code);
DECLARE @ms decimal(18,3)=CONVERT(decimal(18,3),DATEDIFF(MICROSECOND,@t0,SYSDATETIME())/1000.0);
DECLARE @label varchar(40)='Preview17Candidates';
DELETE dbo.PromoOnePerItemTiming WHERE RunLabel=@label;
INSERT dbo.PromoOnePerItemTiming VALUES(@label,@ms,SYSDATETIME());
-- 16 KM riêng + ALL 5% cùng phủ 3 dòng: nhóm 17 ứng viên, dùng nhánh tham lam; chọn 16% = 48 000.
IF EXISTS(SELECT 1 FROM @preview WHERE Discount<>48000 OR PercentDiscount<>16
 OR PromotionCount<>1 OR Payable<>252000)
 THROW 53806,N'PP08: nhánh tham lam phải chọn đúng KM 16%.',1;
EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev;

-- Đo hàm thuần với 20 dòng cùng món: 16% của 2 000 000 = 320 000.
DECLARE @json nvarchar(max)=(SELECT TOP(20) CONVERT(char(5),'SP952') Product,CONVERT(decimal(18,0),100000) Amount
 FROM sys.all_objects FOR JSON PATH,INCLUDE_NULL_VALUES);
SET @t0=SYSDATETIME();
DECLARE @t TABLE(TierPercent int,PromotionPercent int,TierDiscount decimal(18,0),PromotionDiscount decimal(18,0),Discount decimal(18,0));
INSERT @t SELECT * FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@json);
SET @ms=CONVERT(decimal(18,3),DATEDIFF(MICROSECOND,@t0,SYSDATETIME())/1000.0);
SET @label='Function20Lines17Candidates';
DELETE dbo.PromoOnePerItemTiming WHERE RunLabel=@label;
INSERT dbo.PromoOnePerItemTiming VALUES(@label,@ms,SYSDATETIME());
IF EXISTS(SELECT 1 FROM @t WHERE PromotionDiscount<>320000 OR Discount<>320000)
 THROW 53806,N'PP08: 20 dòng SP952 phải giảm 320 000.',1;
IF EXISTS(SELECT 1 FROM dbo.PromoOnePerItemTiming WHERE Ms<0) THROW 53806,N'PP08: số đo không hợp lệ.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53806,N'Giao dịch không sạch.',1;
SELECT RunLabel,Ms,RecordedAt FROM dbo.PromoOnePerItemTiming ORDER BY RunLabel;
