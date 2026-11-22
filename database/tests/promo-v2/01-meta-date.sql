-- 01: lưu ngày/tháng, lặp hằng năm, vắt năm, KT trống; trạng thái và đối tượng mới.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoV2[_]%' OR OBJECT_ID('dbo.PromoV2TestMarker') IS NULL
 THROW 53702,N'Chỉ DB test PromoV2 có marker.',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- 1) Ngày lưu theo quy ước năm tham chiếu (fixture đã tạo 15 KM).
IF (SELECT CONCAT(NgayBD,'|',NgayKT) FROM dbo.KHUYENMAI WHERE MaKM='KM92000001')<>'2000-11-01|2001-02-28'
 THROW 53702,N'KM vắt năm phải lưu 2000-11-01 đến 2001-02-28.',1;
IF (SELECT CONCAT(NgayBD,'|',NgayKT) FROM dbo.KHUYENMAI WHERE MaKM='KM92000002')<>'2000-01-01|2000-12-31'
 THROW 53702,N'KM trong năm phải lưu cả hai năm 2000.',1;
IF (SELECT NgayKT FROM dbo.KHUYENMAI WHERE MaKM='KM92000013') IS NOT NULL
 OR MONTH((SELECT NgayBD FROM dbo.KHUYENMAI WHERE MaKM='KM92000013'))<>MONTH(@today)
 OR DAY((SELECT NgayBD FROM dbo.KHUYENMAI WHERE MaKM='KM92000013'))<>DAY(@today)
 THROW 53702,N'KM bắt đầu hôm nay và không có ngày kết thúc lưu sai.',1;

-- 2) Lặp hằng năm: so tháng/ngày, hai đầu inclusive, hỗ trợ vắt năm và KT trống.
IF dbo.fn_NgayTrongKy('2027-01-15',DATEFROMPARTS(2000,11,1),DATEFROMPARTS(2001,2,28))<>1
 OR dbo.fn_NgayTrongKy('2027-03-15',DATEFROMPARTS(2000,11,1),DATEFROMPARTS(2001,2,28))<>0
 OR dbo.fn_NgayTrongKy('2027-11-01',DATEFROMPARTS(2000,11,1),DATEFROMPARTS(2001,2,28))<>1
 OR dbo.fn_NgayTrongKy('2027-02-28',DATEFROMPARTS(2000,11,1),DATEFROMPARTS(2001,2,28))<>1
 THROW 53702,N'KM vắt năm 01/11-28/02 tính sai.',1;
IF dbo.fn_NgayTrongKy('2027-01-01',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,12,31))<>1
 OR dbo.fn_NgayTrongKy('2027-12-31',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,12,31))<>1
 OR dbo.fn_NgayTrongKy('2027-12-01',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,11,22))<>0
 OR dbo.fn_NgayTrongKy('2027-11-22',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,11,22))<>1
 THROW 53702,N'KM trong năm tính sai hai đầu.',1;
IF dbo.fn_NgayTrongKy(@today,DATEFROMPARTS(2000,MONTH(@today),DAY(@today)),NULL)<>1
 OR dbo.fn_NgayTrongKy(DATEADD(day,-1,@today),DATEFROMPARTS(2000,MONTH(@today),DAY(@today)),NULL)<>0
 OR dbo.fn_NgayTrongKy('2027-12-31',DATEFROMPARTS(2000,MONTH(@today),DAY(@today)),NULL)<>1
 THROW 53702,N'KM không ngày kết thúc phải áp đến hết năm.',1;
IF dbo.fn_NgayTrongKy(@today,DATEFROMPARTS(2000,12,1),NULL)<>0
 THROW 53702,N'KM chưa tới ngày bắt đầu không được áp.',1;
IF dbo.fn_NgaySauKy('2027-11-23',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,11,22))<>1
 OR dbo.fn_NgaySauKy('2027-11-22',DATEFROMPARTS(2000,1,1),DATEFROMPARTS(2000,11,22))<>0
 OR dbo.fn_NgaySauKy('2027-01-05',DATEFROMPARTS(2000,12,20),DATEFROMPARTS(2001,1,10))<>0
 OR dbo.fn_NgaySauKy('2027-01-11',DATEFROMPARTS(2000,12,20),DATEFROMPARTS(2001,1,10))<>1
 THROW 53702,N'fn_NgaySauKy tính sai trạng thái hết hạn.',1;

-- 3) Trạng thái chỉ DangChay/TamDung; Nhap cũ đã bị chặn.
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE TrangThai NOT IN('DangChay','TamDung'))
 THROW 53702,N'Còn trạng thái KM ngoài DangChay/TamDung.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE DoiTuongKhach NOT IN('ALL','TIER','WALKIN'))
 THROW 53702,N'Còn đối tượng KM FIRST/MEMBER cũ.',1;
BEGIN TRY
 UPDATE dbo.KHUYENMAI SET TrangThai='Nhap' WHERE MaKM='KM92000002';
 THROW 53702,N'Phải chặn trạng thái Nhap.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>547 THROW;
END CATCH;
IF (SELECT TrangThai FROM dbo.KHUYENMAI WHERE MaKM='KM92000002')<>'DangChay'
 THROW 53702,N'UPDATE lỗi vẫn đổi trạng thái KM.',1;

-- 4) Danh sách đọc đúng loại ưu đãi, trạng thái suy ra và số quà.
CREATE TABLE #list(Code char(10) NOT NULL,Name nvarchar(100) NOT NULL,StartDate date NOT NULL,EndDate date NULL,
 Discount int NOT NULL,TrangThai varchar(10) NOT NULL,Audience varchar(10) NOT NULL,Scope varchar(4) NOT NULL,
 ItemCount int NOT NULL,Revision binary(8) NOT NULL,Kind varchar(10) NOT NULL,GiftItemCount int NOT NULL,
 GiftQuantity int NOT NULL);
INSERT #list EXEC dbo.sp_PromotionList @Actor=@a,@Stamp=@s,@Search=N'KM9200',@Skip=0,@OnDate=NULL;
IF (SELECT COUNT(*) FROM #list)<>15 THROW 53702,N'Danh sách KM test thiếu.',1;
IF (SELECT TrangThai FROM #list WHERE Code='KM92000011')<>'HetHan'
 OR (SELECT TrangThai FROM #list WHERE Code='KM92000009')<>'TamDung'
 OR (SELECT TrangThai FROM #list WHERE Code='KM92000010')<>'TamDung'
 OR (SELECT TrangThai FROM #list WHERE Code='KM92000001')<>'DangChay'
 OR (SELECT TrangThai FROM #list WHERE Code='KM92000012')<>'DangChay'
 THROW 53702,N'Trạng thái danh sách KM sai.',1;
IF (SELECT Kind FROM #list WHERE Code='KM92000005')<>'Tang'
 OR (SELECT GiftItemCount FROM #list WHERE Code='KM92000005')<>1
 OR (SELECT GiftQuantity FROM #list WHERE Code='KM92000005')<>2
 OR (SELECT GiftItemCount FROM #list WHERE Code='KM92000006')<>2
 OR (SELECT GiftQuantity FROM #list WHERE Code='KM92000006')<>2
 OR (SELECT Kind FROM #list WHERE Code='KM92000002')<>'Giam'
 OR (SELECT GiftItemCount FROM #list WHERE Code='KM92000002')<>0
 THROW 53702,N'Loại ưu đãi/số quà trên danh sách sai.',1;
IF (SELECT Scope FROM #list WHERE Code='KM92000002')<>'ITEM'
 OR (SELECT ItemCount FROM #list WHERE Code='KM92000002')<>1
 OR (SELECT Scope FROM #list WHERE Code='KM92000001')<>'ALL'
 OR (SELECT ItemCount FROM #list WHERE Code='KM92000001')<>0
 THROW 53702,N'Phạm vi món trên danh sách sai.',1;

-- 5) Đối tượng ALL/TIER/WALKIN, chọn nhiều hạng, 0 hạng = mọi hạng.
IF dbo.fn_KhachThoaDoiTuong('ALL',NULL,'KM92000002')<>1
 OR dbo.fn_KhachThoaDoiTuong('ALL','KH921','KM92000002')<>1
 OR dbo.fn_KhachThoaDoiTuong('WALKIN',NULL,'KM92000004')<>1
 OR dbo.fn_KhachThoaDoiTuong('WALKIN','KH921','KM92000004')<>0
 OR dbo.fn_KhachThoaDoiTuong('TIER','KH922','KM92000003')<>1
 OR dbo.fn_KhachThoaDoiTuong('TIER','KH921','KM92000003')<>0
 OR dbo.fn_KhachThoaDoiTuong('TIER','KH923','KM92000003')<>0
 OR dbo.fn_KhachThoaDoiTuong('TIER',NULL,'KM92000003')<>0
 OR dbo.fn_KhachThoaDoiTuong('TIER','KH923','KM92000015')<>1
 OR dbo.fn_KhachThoaDoiTuong('TIER','KH001','KM92000015')<>1
 OR dbo.fn_KhachThoaDoiTuong('TIER',NULL,'KM92000015')<>0
 THROW 53702,N'Đối tượng khách tính sai.',1;

-- 6) FIRST/MEMBER không còn hợp lệ khi ghi mới.
BEGIN TRY
 SET @id=NEWID();
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code='KM92000090',@RequestId=@id,
  @Name=N'Promo v2 khach lan dau',@StartDate=@today,@EndDate=NULL,@Discount=5,@Audience='FIRST',
  @Status='DangChay',@Kind='Giam';
 THROW 53702,N'Phải chặn đối tượng FIRST.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52001 THROW;
END CATCH;
BEGIN TRY
 SET @id=NEWID();
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code='KM92000091',@RequestId=@id,
  @Name=N'Promo v2 khach co ho so',@StartDate=@today,@EndDate=NULL,@Discount=5,@Audience='MEMBER',
  @Status='DangChay',@Kind='Giam';
 THROW 53702,N'Phải chặn đối tượng MEMBER.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52001 THROW;
END CATCH;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM IN('KM92000090','KM92000091'))
 THROW 53702,N'KM đối tượng cũ vẫn được ghi.',1;

-- 7) Chi tiết KM tặng: hạng và món tặng lưu đúng.
IF (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG WHERE MaKM='KM92000006')<>2
 OR (SELECT SoLuong FROM dbo.KHUYENMAI_MON_TANG WHERE MaKM='KM92000005' AND MaSP='SP921')<>2
 OR NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI_HANG WHERE MaKM='KM92000006' AND MaHang='VANG')
 OR (SELECT PhanTramGiam FROM dbo.KHUYENMAI WHERE MaKM='KM92000006')<>0
 THROW 53702,N'Chi tiết KM tặng lưu sai.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI_MON WHERE MaKM LIKE 'KM92%' AND MaKM IN('KM92000005','KM92000006'))
 THROW 53702,N'KM tặng không được có phạm vi món giảm.',1;

-- 8) Legacy 039: dữ liệu seed được chuẩn hóa ngày nhưng không đổi mã/giảm giá.
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM2%'
 AND (YEAR(NgayBD)<>2000 OR (NgayKT IS NOT NULL AND YEAR(NgayKT) NOT IN(2000,2001))))
 THROW 53702,N'KM seed chưa chuẩn hóa ngày.',1;
IF (SELECT NgayKT FROM dbo.KHUYENMAI WHERE MaKM='KM24BF')<>'2000-12-01'
 OR (SELECT NgayKT FROM dbo.KHUYENMAI WHERE MaKM='KM24NEL')<>'2000-12-31'
 OR (SELECT NgayBD FROM dbo.KHUYENMAI WHERE MaKM='KM25TET')<>'2000-01-20'
 THROW 53702,N'KM seed chuẩn hóa ngày sai.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53702,N'Giao dịch không sạch.',1;
