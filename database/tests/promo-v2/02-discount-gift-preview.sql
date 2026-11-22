-- 02: giảm theo món/hạng/vãng lai, chặn 100%/dòng, ship không giảm, quà dự kiến, token đổi.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoV2[_]%' OR OBJECT_ID('dbo.PromoV2TestMarker') IS NULL
 THROW 53703,N'Chỉ DB test PromoV2 có marker.',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @tier int,@pp int,@td decimal(18,0),@pd decimal(18,0),@discount decimal(18,0);
DECLARE @sd date=DATEFROMPARTS(2000,1,1),@ed date=DATEFROMPARTS(2000,12,31);
DECLARE @split TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,BaseAmount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL);

-- 1) Vãng lai, 1 dòng SP920 x2: 10% + 20% món + 5% vãng lai = 35%.
DECLARE @j nvarchar(max)=N'[{"Product":"SP920","Amount":100000}]';
SELECT @tier=TierPercent,@pp=PromotionPercent,@td=TierDiscount,@pd=PromotionDiscount,@discount=Discount
 FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@j);
DELETE @split; INSERT @split SELECT * FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@j);
IF @discount<>35000 OR @tier<>0 OR @pp<>35 OR @td<>0 OR @pd<>35000 OR (SELECT COUNT(*) FROM @split)<>3
 THROW 53703,N'Giảm vãng lai SP920 x2 sai.',1;
IF (SELECT Discount FROM @split WHERE MaKM='KM92000001')<>10000
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000002')<>20000
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000004')<>5000
 OR (SELECT [Percent] FROM @split WHERE MaKM='KM92000002')<>20
 THROW 53703,N'Chia giảm vãng lai sai.',1;

-- 2) Theo hạng: KH922 hạng VANG 5%, TIER BAC/VANG 15% món + 20% món + 10% = 50%.
SET @j=N'[{"Product":"SP920","Amount":50000}]';
SELECT @tier=TierPercent,@pp=PromotionPercent,@td=TierDiscount,@pd=PromotionDiscount,@discount=Discount
 FROM dbo.fn_UuDaiTong(@today,'KH922',NULL,@j);
DELETE @split; INSERT @split SELECT * FROM dbo.fn_UuDaiChia(@today,'KH922',NULL,@j);
IF @discount<>25000 OR @tier<>5 OR @pp<>45 OR @td<>2500 OR @pd<>22500 OR (SELECT COUNT(*) FROM @split)<>3
 THROW 53703,N'Giảm hạng VANG SP920 sai.',1;
IF (SELECT Discount FROM @split WHERE MaKM='KM92000001')<>5000
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000002')<>10000
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000003')<>7500
 THROW 53703,N'Chia giảm hạng VANG sai.',1;
-- Khách hạng thấp DONG không nhận TIER BAC/VANG; khách vãng lai không nhận TIER.
SET @j=N'[{"Product":"SP920","Amount":50000}]';
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,'KH921',NULL,@j))<>15000
 THROW 53703,N'Khách DONG không được nhận KM hạng BAC/VANG.',1;
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@j))<>17500
 THROW 53703,N'Khách vãng lai không được nhận KM theo hạng.',1;

-- 3) Hai dòng: 20% món chỉ áp dòng SP920, bỏ trống món = mọi món.
SET @j=N'[{"Product":"SP002","Amount":60000}]';
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@j))<>9000
 OR (SELECT COUNT(*) FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@j))<>2
 THROW 53703,N'KM theo món vẫn áp món ngoài danh sách.',1;
SET @j=N'[{"Product":"SP920","Amount":50000},{"Product":"SP002","Amount":30000}]';
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,NULL,NULL,@j))<>22000
 OR (SELECT COUNT(*) FROM dbo.fn_UuDaiChia(@today,NULL,NULL,@j))<>3
 THROW 53703,N'Tính giảm nhiều dòng sai.',1;

-- 4) TIER không chọn hạng = mọi hạng (KH923 DONG nhận KM15 4% + KM01 10%).
SET @j=N'[{"Product":"SP923","Amount":20000}]';
DELETE @split; INSERT @split SELECT * FROM dbo.fn_UuDaiChia(@today,'KH923',NULL,@j);
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,'KH923',NULL,@j))<>2800
 OR (SELECT COUNT(*) FROM @split)<>2
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000001')<>2000
 OR (SELECT Discount FROM @split WHERE MaKM='KM92000015')<>800
 THROW 53703,N'KM TIER 0 hạng phải áp mọi hạng.',1;

-- 5) Chặn 100%/dòng: tạo KM 60% rồi xóa ngay trong script.
DECLARE @rev17 binary(8);
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code='KM92000017',@RequestId=@id,
 @Name=N'Promo v2 tran 100',@StartDate=@sd,@EndDate=@ed,
 @Discount=60,@Audience='ALL',@Status='DangChay',@Kind='Giam';
SET @j=N'[{"Product":"SP920","Amount":50000}]';
IF (SELECT Discount FROM dbo.fn_UuDaiTong(@today,'KH922',NULL,@j))<>50000
 OR (SELECT [Percent] FROM dbo.fn_UuDaiDong(@today,'KH922',NULL,@j))<>100
 OR (SELECT COUNT(*) FROM dbo.fn_UuDaiChia(@today,'KH922',NULL,@j))<>4
 THROW 53703,N'Chưa chặn 100% mỗi dòng.',1;
SELECT @rev17=Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000017';
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Delete',@Code='KM92000017',@RequestId=@id,
 @ExpectedRevision=@rev17;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KM92000017') THROW 53703,N'KM tạm chưa xóa được.',1;

-- 6) Đơn giao hàng mở: ship 15000 không bị giảm, giữ để kiểm quà dự kiến và token.
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
INSERT @codes EXEC dbo.sp_CapMa 'HOADON';
DECLARE @hdE char(10); SELECT @hdE=Ma FROM @codes;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hdE,@RequestId=@id,@Channel=N'Giao hang',
 @Customer='KH922',@Recipient=N'Promo v2 ship',@Phone='0900000922',@Address=N'1 Duong Test',@Ship=15000,
 @Product='SP920',@Quantity=1;
IF (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@hdE)<>50000
 OR (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hdE)<>25000
 OR (SELECT ThanhTien FROM dbo.HOADON WHERE MaHD=@hdE)<>40000
 THROW 53703,N'Đơn ship tính tiền sai.',1;
IF (SELECT PhiShip FROM dbo.HOADON WHERE MaHD=@hdE)<>15000
 OR (SELECT Payable FROM dbo.fn_CheckoutPreview(@hdE))<>40000
 OR (SELECT Discount FROM dbo.fn_CheckoutPreview(@hdE))<>25000
 THROW 53703,N'Preview ship phải giữ ship, chỉ giảm tiền món.',1;

-- 7) Quà dự kiến: vãng lai chỉ KM05; KH922 nhận KM05 + KM06 + KM07; KM tắt/tương lai không áp.
IF (SELECT COUNT(*) FROM dbo.fn_QuaTangApDung(@today,NULL))<>1
 OR NOT EXISTS(SELECT 1 FROM dbo.fn_QuaTangApDung(@today,NULL)
  WHERE MaKM='KM92000005' AND MaSP='SP921' AND SoLuong=2)
 THROW 53703,N'Quà dự kiến cho vãng lai sai.',1;
IF (SELECT COUNT(*) FROM dbo.fn_QuaTangApDung(@today,'KH922'))<>4
 OR (SELECT SUM(SoLuong) FROM dbo.fn_QuaTangApDung(@today,'KH922') WHERE MaSP='SP921')<>3
 OR (SELECT SUM(SoLuong) FROM dbo.fn_QuaTangApDung(@today,'KH922') WHERE MaSP='SP922')<>2
 OR EXISTS(SELECT 1 FROM dbo.fn_QuaTangApDung(@today,'KH922') WHERE MaKM='KM92000009')
 THROW 53703,N'Quà dự kiến cho KH922 sai.',1;
IF EXISTS(SELECT 1 FROM dbo.fn_QuaTangApDung(@today,'KH921') WHERE MaKM IN('KM92000006','KM92000007'))
 THROW 53703,N'Quà theo hạng VANG vẫn áp khách DONG.',1;
IF (SELECT COALESCE(SUM(q.SoLuong*c.DinhMuc),0) FROM dbo.fn_QuaTangApDung(@today,'KH922') q
 JOIN dbo.CONGTHUC c ON c.MaSP=q.MaSP WHERE c.MaNVL='NVL92')<>7
 THROW 53703,N'Tiêu hao nguyên liệu quà dự kiến phải là 7.',1;
IF (SELECT Discount FROM dbo.fn_CheckoutPreview(@hdE))<>25000
 OR (SELECT GiftCount FROM dbo.fn_CheckoutPreview(@hdE))<>4
 THROW 53703,N'Quà không được cộng vào tiền giảm.',1;

-- 8) Token đổi khi sửa danh sách món tặng và đổi lại như cũ.
DECLARE @token1 binary(32),@token2 binary(32),@token3 binary(32),@rev5 binary(8),@rev5b binary(8);
SELECT @token1=PreviewToken FROM dbo.fn_CheckoutPreview(@hdE);
SELECT @rev5=Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000005';
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Update',@Code='KM92000005',@RequestId=@id,
 @ExpectedRevision=@rev5,@Name=N'Promo v2 tang SP921 x3',@StartDate=@sd,
 @EndDate=@ed,@Audience='ALL',@Status='DangChay',@Kind='Tang',
 @GiftJson=N'[{"Product":"SP921","Quantity":3}]';
SELECT @token2=PreviewToken FROM dbo.fn_CheckoutPreview(@hdE);
IF @token2=@token1
 OR (SELECT SUM(SoLuong) FROM dbo.fn_QuaTangApDung(@today,'KH922') WHERE MaSP='SP921')<>4
 THROW 53703,N'Đổi số lượng quà phải đổi token và danh sách quà.',1;
SELECT @rev5b=Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000005';
IF @rev5b=@rev5 THROW 53703,N'Revision KM không tăng sau sửa quà.',1;
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Update',@Code='KM92000005',@RequestId=@id,
 @ExpectedRevision=@rev5b,@Name=N'Promo v2 tang SP921 x2',@StartDate=@sd,
 @EndDate=@ed,@Audience='ALL',@Status='DangChay',@Kind='Tang',
 @GiftJson=N'[{"Product":"SP921","Quantity":2}]';
SELECT @token3=PreviewToken FROM dbo.fn_CheckoutPreview(@hdE);
IF @token3=@token1 OR @token3=@token2
 OR (SELECT SUM(SoLuong) FROM dbo.fn_QuaTangApDung(@today,'KH922') WHERE MaSP='SP921')<>3
 THROW 53703,N'Đổi lại quà phải đổi token theo digest mới.',1;

-- 9) Procedure preview chạy được và từ chối input xấu.
DECLARE @lines dbo.PreviewLines;
INSERT @lines(MaSP,SoLuong) VALUES('SP920',2);
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=15000,@BusinessDate=@today,
 @Customer='KH922',@Lines=@lines;
EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=100000,@Ship=0,@BusinessDate=@today,
 @Customer=NULL,@Lines=@lines;
DELETE @lines; INSERT @lines(MaSP,SoLuong) VALUES('SP999',1);
BEGIN TRY
 EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=0,@Ship=0,@BusinessDate=@today,@Customer=NULL,@Lines=@lines;
 THROW 53703,N'Phải chặn món không tồn tại.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52005 THROW;
END CATCH;
DELETE @lines;
BEGIN TRY
 EXEC dbo.sp_DiscountPreview @Actor=@a,@Stamp=@s,@Money=100000,@Ship=100.5,@BusinessDate=@today,@Customer=NULL,@Lines=@lines;
 THROW 53703,N'Phải chặn ship lẻ.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52001 THROW;
END CATCH;

-- 10) Không phát sinh chứng từ/điểm/kho do preview.
IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today)
 THROW 53703,N'Preview không được chốt đơn.',1;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan')
 <> (SELECT Value FROM dbo.PromoV2Baseline WHERE Name='OpenOrderCount')+1
 THROW 53703,N'Số đơn mở sau preview sai.',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaKMQuaTang IS NOT NULL)
 OR EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaKH='KH922')
 OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@hdE)
 THROW 53703,N'Preview sinh dữ liệu quà/điểm/kho.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53703,N'Giao dịch không sạch.',1;
