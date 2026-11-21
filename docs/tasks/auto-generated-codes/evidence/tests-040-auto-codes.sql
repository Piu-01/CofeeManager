-- Đợt test auto-generated-codes: chạy trên CoffeeManager_Test_040_20261121_214133
SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_040_%' THROW 52999,'Test DB 040 required',1;
IF (SELECT MAX(Version) FROM dbo.SchemaMigrations)<>40 THROW 52999,'Schema 40 required',1;

DECLARE @log TABLE(Seq INT IDENTITY, CaseCode VARCHAR(8), Result VARCHAR(10), Detail NVARCHAR(400));
DECLARE @a INT,@s BINARY(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN';
IF @a IS NULL THROW 52999,'Admin account missing',1;
DECLARE @r UNIQUEIDENTIFIER,@rev BINARY(8),@code VARCHAR(10),@next VARCHAR(10),@before INT,@after INT;
DECLARE @nm NVARCHAR(100),@tag VARCHAR(6)=RIGHT('000000'+CONVERT(VARCHAR(6),ABS(CHECKSUM(NEWID()))%1000000),6);
DECLARE @phone2 VARCHAR(15)='09'+RIGHT('00000000'+CONVERT(VARCHAR(8),ABS(CHECKSUM(NEWID()))%100000000),8);

-- ===== A: cấp mã + tạo bản ghi cho 11 nguồn =====
-- A01 Nhà cung cấp
DECLARE @c1 TABLE(Loai VARCHAR(20),Ma CHAR(10)); INSERT @c1 EXEC dbo.sp_CapMa 'NHACUNGCAP';
SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
DECLARE @phone VARCHAR(15)='09'+RIGHT('00000000'+CONVERT(VARCHAR(8),ABS(CHECKSUM(NEWID()))%100000000),8);
IF @code NOT LIKE 'NCC[0-9][0-9]' INSERT @log VALUES('A01','FAIL','Mã NCC sai định dạng: '+@code);
ELSE BEGIN
 SET @nm=N'G01 NCC kiểm thử '+@tag;
 EXEC dbo.sp_CatalogSupplierCreate @a,@s,@code,@nm,NULL,@r,@phone;
 IF EXISTS(SELECT 1 FROM dbo.NHACUNGCAP WHERE MaNCC=@code) INSERT @log VALUES('A01','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A01','FAIL','Không thấy '+@code);
END;
DECLARE @ncc VARCHAR(10)=@code;

-- A02 Phương thức thanh toán
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'PHUONGTHUCTT'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'PT[0-9][0-9][0-9]' INSERT @log VALUES('A02','FAIL','Mã PT sai: '+@code);
ELSE BEGIN
 SET @nm=N'PT kiểm thử '+@tag;
 EXEC dbo.sp_CatalogPaymentMethodCreate @a,@s,@code,@nm,NULL,@r,0;
 IF EXISTS(SELECT 1 FROM dbo.PHUONGTHUCTT WHERE MaPT=@code) INSERT @log VALUES('A02','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A02','FAIL','Không thấy '+@code);
END;

-- A03 Đối tác giao hàng
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'DOITACGIAOHANG'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'DT[0-9][0-9][0-9]' INSERT @log VALUES('A03','FAIL','Mã DT sai: '+@code);
ELSE BEGIN
 SET @nm=N'DT kiểm thử '+@tag;
 EXEC dbo.sp_CatalogDeliveryPartnerCreate @a,@s,@code,@nm,NULL,@r,10;
 IF EXISTS(SELECT 1 FROM dbo.DOITACGIAOHANG WHERE MaDT=@code) INSERT @log VALUES('A03','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A03','FAIL','Không thấy '+@code);
END;

-- A04 Loại thu/chi
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'DANHMUC_THUCHI'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'TC[0-9][0-9][0-9]' INSERT @log VALUES('A04','FAIL','Mã TC sai: '+@code);
ELSE BEGIN
 SET @nm=N'TC kiểm thử '+@tag;
 EXEC dbo.sp_CatalogExpenseCategoryCreate @a,@s,@code,@nm,NULL,@r,N'Chi';
 IF EXISTS(SELECT 1 FROM dbo.DANHMUC_THUCHI WHERE MaDMTC=@code) INSERT @log VALUES('A04','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A04','FAIL','Không thấy '+@code);
END;

-- A05 Chức vụ
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'CHUCVU'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'CV[0-9][0-9][0-9]' INSERT @log VALUES('A05','FAIL','Mã CV sai: '+@code);
ELSE BEGIN
 SET @nm=N'CV kiểm thử '+@tag;
 EXEC dbo.sp_CatalogPositionCreate @a,@s,@code,@nm,NULL,@r,500000,1.5;
 IF EXISTS(SELECT 1 FROM dbo.CHUCVU WHERE MaCV=@code) INSERT @log VALUES('A05','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A05','FAIL','Không thấy '+@code);
END;

-- A06 Ca làm việc
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'CALAMVIEC'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'CA[0-9][0-9][0-9]' INSERT @log VALUES('A06','FAIL','Mã CA sai: '+@code);
ELSE BEGIN
 SET @nm=N'CA kiểm thử '+@tag;
 EXEC dbo.sp_CatalogShiftCreate @a,@s,@code,@nm,NULL,@r,'08:00','12:00';
 IF EXISTS(SELECT 1 FROM dbo.CALAMVIEC WHERE MaCa=@code) INSERT @log VALUES('A06','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A06','FAIL','Không thấy '+@code);
END;

-- A07 Sản phẩm
DECLARE @recipe dbo.RecipeLineInput;
INSERT @recipe VALUES('NVL01',0.0500);
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'SANPHAM'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'SP[0-9][0-9][0-9]' INSERT @log VALUES('A07','FAIL','Mã SP sai: '+@code);
ELSE BEGIN
 SET @nm=N'SP kiểm thử '+@tag;
 EXEC dbo.sp_ProductCreate @a,@s,@code,@r,@recipe,NULL,@nm,N'DM001',10000,N'Dang ban';
 IF EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP=@code) INSERT @log VALUES('A07','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A07','FAIL','Không thấy '+@code);
END;
DECLARE @sp VARCHAR(10)=@code;

-- A08 Nguyên liệu
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'NGUYENVATLIEU'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'NVL[0-9][0-9]' INSERT @log VALUES('A08','FAIL','Mã NVL sai: '+@code);
ELSE BEGIN
 SET @nm=N'NVL kiểm thử '+@tag;
 EXEC dbo.sp_MaterialCreate @a,@s,@code,@r,NULL,@nm,N'Kg';
 IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL=@code) INSERT @log VALUES('A08','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A08','FAIL','Không thấy '+@code);
END;

-- A09 Bàn
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'BAN'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'B[0-9][0-9][0-9][0-9]' INSERT @log VALUES('A09','FAIL','Mã B sai: '+@code);
ELSE BEGIN
 SET @nm=N'Bàn kiểm thử '+@tag;
 EXEC dbo.sp_TableCreate @a,@s,@code,@r,NULL,@nm,N'KV001';
 IF EXISTS(SELECT 1 FROM dbo.BAN WHERE MaBan=@code) INSERT @log VALUES('A09','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A09','FAIL','Không thấy '+@code);
END;

-- A10 Khu vực
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'KHUVUC'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'KV[0-9][0-9][0-9]' INSERT @log VALUES('A10','FAIL','Mã KV sai: '+@code);
ELSE BEGIN
 SET @nm=N'Khu vực kiểm thử '+@tag;
 EXEC dbo.sp_AreaCreate @a,@s,@code,@r,NULL,@nm;
 IF EXISTS(SELECT 1 FROM dbo.KHUVUC WHERE MaKV=@code) INSERT @log VALUES('A10','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A10','FAIL','Không thấy '+@code);
END;

-- A11 Khuyến mãi
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'KHUYENMAI'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
IF @code NOT LIKE 'KM[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' INSERT @log VALUES('A11','FAIL','Mã KM sai: '+@code);
ELSE BEGIN
 SET @nm=N'KM kiểm thử '+@tag;
 EXEC dbo.sp_PromotionCreate @a,@s,@code,@r,NULL,@nm,'2027-01-01','2027-01-31',10;
 IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM=@code) INSERT @log VALUES('A11','PASS','Tạo được '+@code);
 ELSE INSERT @log VALUES('A11','FAIL','Không thấy '+@code);
END;

-- ===== B: mã tăng dần, không trùng =====
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'SANPHAM'; SET @next=(SELECT Ma FROM @c1);
IF TRY_CONVERT(INT,SUBSTRING(@next,3,3))>TRY_CONVERT(INT,SUBSTRING(@sp,3,3)) INSERT @log VALUES('B01','PASS',@sp+' -> '+@next);
ELSE INSERT @log VALUES('B01','FAIL',@sp+' -> '+@next);
-- B02: mã vừa cấp luôn lớn hơn mọi mã cũ trong bảng
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'NHACUNGCAP'; SET @next=(SELECT Ma FROM @c1);
IF TRY_CONVERT(INT,SUBSTRING(@next,4,2))>(SELECT MAX(TRY_CONVERT(INT,SUBSTRING(MaNCC,4,2))) FROM dbo.NHACUNGCAP WHERE MaNCC COLLATE Latin1_General_100_BIN2 LIKE 'NCC[0-9][0-9]')
 INSERT @log VALUES('B02','PASS',@next);
ELSE INSERT @log VALUES('B02','FAIL',@next);

-- ===== C: bỏ qua mã dành riêng =====
DECLARE @many TABLE(Loai VARCHAR(20),Ma CHAR(10));
DECLARE @i INT=0; WHILE @i<25 BEGIN DELETE @many; INSERT @many EXEC dbo.sp_CapMa 'PHUONGTHUCTT';
 IF EXISTS(SELECT 1 FROM @many WHERE Ma='PT010') INSERT @log VALUES('C01','FAIL','Cấp PT010'); SET @i+=1; END;
IF NOT EXISTS(SELECT 1 FROM @log WHERE CaseCode='C01') INSERT @log VALUES('C01','PASS','25 lần cấp PT không có PT010');
SET @i=0; WHILE @i<20 BEGIN DELETE @many; INSERT @many EXEC dbo.sp_CapMa 'DANHMUC_THUCHI';
 IF EXISTS(SELECT 1 FROM @many WHERE Ma IN('TC005','TC011','TC012')) INSERT @log VALUES('C02','FAIL','Cấp mã hệ thống'); SET @i+=1; END;
IF NOT EXISTS(SELECT 1 FROM @log WHERE CaseCode='C02') INSERT @log VALUES('C02','PASS','20 lần cấp TC không có TC005/011/012');

-- ===== D: mã legacy và mã hệ thống giữ nguyên =====
SELECT @before=COUNT(*) FROM dbo.KHUYENMAI;
IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KM24BF') INSERT @log VALUES('D01','FAIL','Thiếu KM24BF');
ELSE INSERT @log VALUES('D01','PASS','KM24BF còn nguyên');
IF NOT EXISTS(SELECT 1 FROM dbo.PHUONGTHUCTT WHERE MaPT='PT010') INSERT @log VALUES('D02','FAIL','Thiếu PT010');
ELSE INSERT @log VALUES('D02','PASS','PT010 còn nguyên');
IF (SELECT COUNT(*) FROM dbo.DANHMUC_THUCHI WHERE MaDMTC IN('TC005','TC011','TC012'))<>3 INSERT @log VALUES('D03','FAIL','Thiếu TC hệ thống');
ELSE INSERT @log VALUES('D03','PASS','TC005/011/012 còn nguyên');

-- ===== F: cấp mã rồi lưu thất bại, không tạo rác =====
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'SANPHAM'; SET @code=(SELECT Ma FROM @c1); SET @r=NEWID();
DECLARE @badRecipe dbo.RecipeLineInput; INSERT @badRecipe VALUES('NVL01',0.0500);
BEGIN TRY
 SET @nm=N'SP lỗi giá '+@tag;
 EXEC dbo.sp_ProductCreate @a,@s,@code,@r,@badRecipe,NULL,@nm,N'DM001',-1,N'Dang ban';
 INSERT @log VALUES('F01','FAIL','SQL nhận giá âm '+@code);
END TRY BEGIN CATCH
 IF EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP=@code) INSERT @log VALUES('F01','FAIL','Có dòng rác '+@code);
 ELSE INSERT @log VALUES('F01','PASS','Lưu lỗi, không có '+@code);
END CATCH;
DELETE @c1; INSERT @c1 EXEC dbo.sp_CapMa 'SANPHAM'; SET @next=(SELECT Ma FROM @c1);
IF @next<>@code INSERT @log VALUES('F02','PASS','Lần sau cấp '+@next);
ELSE INSERT @log VALUES('F02','FAIL','Cấp lại '+@next);

-- ===== G: sửa/xóa theo đúng mã đã cấp =====
SELECT @rev=CatalogRevision FROM dbo.NHACUNGCAP WHERE MaNCC=@ncc; SET @r=NEWID();
SET @nm=N'G01 NCC đã sửa '+@tag;
EXEC dbo.sp_CatalogSupplierUpdate @a,@s,@ncc,@nm,@rev,@r,@phone2;
IF EXISTS(SELECT 1 FROM dbo.NHACUNGCAP WHERE MaNCC=@ncc AND TenNCC=N'G01 NCC đã sửa '+@tag AND SDT=@phone2) INSERT @log VALUES('G01','PASS','Sửa theo '+@ncc);
ELSE INSERT @log VALUES('G01','FAIL','Sửa thất bại '+@ncc);
SELECT @rev=CatalogRevision FROM dbo.NHACUNGCAP WHERE MaNCC=@ncc; SET @r=NEWID();
EXEC dbo.sp_CatalogSupplierDelete @a,@s,@ncc,NULL,@rev,@r;
IF NOT EXISTS(SELECT 1 FROM dbo.NHACUNGCAP WHERE MaNCC=@ncc) INSERT @log VALUES('G02','PASS','Xóa theo '+@ncc);
ELSE INSERT @log VALUES('G02','FAIL','Chưa xóa '+@ncc);

-- ===== I: nguồn cũ HOADON vẫn hoạt động =====
DECLARE @c2 TABLE(Loai VARCHAR(20),Ma CHAR(10)); INSERT @c2 EXEC dbo.sp_CapMa 'HOADON';
IF (SELECT Ma FROM @c2) LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' INSERT @log VALUES('I01','PASS',(SELECT Ma FROM @c2));
ELSE INSERT @log VALUES('I01','FAIL','HOADON: '+(SELECT Ma FROM @c2));

SELECT Seq,CaseCode,Result,Detail FROM @log ORDER BY Seq;
SELECT (SELECT COUNT(*) FROM @log WHERE Result='FAIL') Fails,(SELECT COUNT(*) FROM @log) Total;
