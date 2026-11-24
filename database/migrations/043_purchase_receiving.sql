-- 043: Nhận hàng 2 bước cho phiếu nhập.
-- Lập phiếu Chờ nhận (không đụng tồn) -> STORE kiểm thực nhận -> ADMIN chốt mới cộng tồn/chi/nợ.
-- Chạy sau 042 bằng tools/database/Apply-Migration.ps1 -Version 43.
-- Phiếu cũ giữ nguyên tồn/tổng/nợ; không đổi dữ liệu lịch sử đã chốt.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=42)
    THROW 54301,N'Migration 042 bắt buộc.',1;
IF OBJECT_ID(N'dbo.sp_LapPhieuNhap',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_PurchaseValidate',N'P') IS NULL
 OR OBJECT_ID(N'dbo.vw_PurchaseRead',N'V') IS NULL
    THROW 54301,N'Thiếu contract nhập hàng của T20; cần đối chiếu trước nâng cấp.',1;
IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.TongTien<>COALESCE((SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) FROM dbo.CHITIET_PN d WHERE d.MaPN=p.MaPN),0))
    THROW 54301,N'Phiếu nhập cũ có tổng sai; đối chiếu trước khi nâng cấp.',1;
GO
/* ===== 1. Cột trạng thái/kiểm/huỷ trên phiếu ===== */
ALTER TABLE dbo.PHIEUNHAP ADD
    TraNgayDuKien decimal(18,0) NOT NULL CONSTRAINT DF_PN_TraNgayDuKien DEFAULT 0,
    HanTraDuKien date NULL,
    NgayKiem datetime2(0) NULL,
    MaNVKiem char(5) NULL,
    DaHuy bit NOT NULL CONSTRAINT DF_PN_DaHuy DEFAULT 0,
    LyDoHuy nvarchar(200) NULL,
    NgayHuy datetime2(0) NULL;
GO
ALTER TABLE dbo.PHIEUNHAP ADD CONSTRAINT FK_PN_NVKIEM FOREIGN KEY(MaNVKiem) REFERENCES dbo.NHANVIEN(MaNV);
GO
ALTER TABLE dbo.PHIEUNHAP ADD TrangThai AS (CASE WHEN DaHuy=1 THEN 'DaHuy' WHEN DaChot=1 THEN 'DaChot'
    WHEN NgayKiem IS NULL THEN 'ChoNhan' ELSE 'DaKiem' END);
GO
-- Không backfill TraNgayDuKien: phiếu cũ đã chốt đọc trả ngay từ phiếu chi nguồn ở view.
/* ===== 2. Dòng đặt/thực nhận ===== */
ALTER TABLE dbo.CHITIET_PN ADD
    SoLuongNhan decimal(18,4) NULL,
    LyDoLech nvarchar(200) NULL,
    -- Mặc định 1 để script cũ/demo chèn trực tiếp vẫn cộng tồn; phiếu nháp mới ghi rõ 0.
    DaGhiKho bit NOT NULL CONSTRAINT DF_CTPN_DaGhiKho DEFAULT 1;
GO
ALTER TABLE dbo.CHITIET_PN ADD CONSTRAINT CK_CTPN_SoLuongNhan
    CHECK(SoLuongNhan IS NULL OR (SoLuongNhan>=0 AND SoLuongNhan<=SoLuong));
GO
-- Phiếu đã chốt bị trigger khóa sửa; tạm tắt đúng hai trigger rồi bật lại trong cùng transaction.
DISABLE TRIGGER dbo.TR_CTPN_Kho ON dbo.CHITIET_PN;
DISABLE TRIGGER dbo.TR_PurchaseLinesClosed ON dbo.CHITIET_PN;
UPDATE d SET SoLuongNhan=d.SoLuong
FROM dbo.CHITIET_PN d JOIN dbo.PHIEUNHAP p ON p.MaPN=d.MaPN
WHERE p.DaChot=1 AND d.SoLuongNhan IS NULL;
ENABLE TRIGGER dbo.TR_CTPN_Kho ON dbo.CHITIET_PN;
ENABLE TRIGGER dbo.TR_PurchaseLinesClosed ON dbo.CHITIET_PN;
GO
/* ===== 3. Trigger tồn/tổng theo cờ ghi kho, thực nhận ===== */
ALTER TRIGGER dbo.TR_CTPN_Kho ON dbo.CHITIET_PN AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    ;WITH delta AS (
      SELECT MaNVL,CONVERT(DECIMAL(18,4),SUM(SL)) SL FROM
        (SELECT MaNVL,CASE WHEN DaGhiKho=1 THEN COALESCE(SoLuongNhan,SoLuong) ELSE 0 END SL FROM inserted
         UNION ALL
         SELECT MaNVL,-CASE WHEN DaGhiKho=1 THEN COALESCE(SoLuongNhan,SoLuong) ELSE 0 END FROM deleted) x
      GROUP BY MaNVL)
    UPDATE n WITH (UPDLOCK) SET SoLuongTon=n.SoLuongTon+d.SL
    FROM dbo.NGUYENVATLIEU n JOIN delta d ON d.MaNVL=n.MaNVL
    WHERE d.SL<>0;
    UPDATE p SET TongTien=COALESCE(t.Tong,0)
    FROM dbo.PHIEUNHAP p OUTER APPLY
      (SELECT SUM(ROUND(COALESCE(SoLuongNhan,SoLuong)*DonGiaNhap,0)) Tong
       FROM dbo.CHITIET_PN WHERE MaPN=p.MaPN AND DaGhiKho=1) t
    WHERE p.MaPN IN (SELECT MaPN FROM inserted UNION SELECT MaPN FROM deleted);
END;
GO
ALTER TRIGGER dbo.TR_PhieuNhap_Tong ON dbo.PHIEUNHAP AFTER INSERT,UPDATE AS
BEGIN
    SET NOCOUNT ON;
    -- Chỉ đối chiếu dòng đã ghi kho; nháp luôn có TongTien=0.
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.TongTien<>
       COALESCE((SELECT SUM(ROUND(COALESCE(SoLuongNhan,SoLuong)*DonGiaNhap,0))
                 FROM dbo.CHITIET_PN WHERE MaPN=i.MaPN AND DaGhiKho=1),0))
        THROW 51015,N'Tong phieu nhap khong khop chi tiet.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.CONGNO_NCC c ON c.MaPN=i.MaPN WHERE c.TienNo>i.TongTien)
        THROW 51016,N'Khong duoc giam gia tri phieu nhap duoi cong no.',1;
END;
GO
ALTER TRIGGER dbo.TR_PurchaseClosed ON dbo.PHIEUNHAP AFTER UPDATE,DELETE AS
BEGIN
    IF EXISTS(SELECT 1 FROM deleted WHERE DaChot=1) THROW 52702,N'Phiếu nhập đã chốt không sửa/xóa.',1;
    IF EXISTS(SELECT 1 FROM deleted WHERE DaHuy=1) THROW 52702,N'Phiếu nhập đã huỷ không sửa.',1;
END;
GO
ALTER TRIGGER dbo.TR_PurchaseLinesClosed ON dbo.CHITIET_PN AFTER INSERT,UPDATE,DELETE AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.DaChot=1
        AND p.MaPN IN(SELECT MaPN FROM inserted UNION SELECT MaPN FROM deleted))
        THROW 52702,N'Chi tiết phiếu nhập đã chốt chỉ đọc.',1;
    IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.DaHuy=1
        AND p.MaPN IN(SELECT MaPN FROM inserted UNION SELECT MaPN FROM deleted))
        THROW 52702,N'Phiếu nhập đã huỷ không sửa chi tiết.',1;
    -- Đã kiểm hàng thì không đổi số đặt; thực nhận vẫn sửa được tới khi chốt.
    IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaPN=i.MaPN AND d.MaNVL=i.MaNVL
        JOIN dbo.PHIEUNHAP p ON p.MaPN=i.MaPN
        WHERE p.NgayKiem IS NOT NULL AND p.DaChot=0 AND i.SoLuong<>d.SoLuong)
        THROW 52704,N'Đã kiểm hàng; không đổi số đặt.',1;
END;
GO
/* ===== 4. Dòng nhận ===== */
CREATE TYPE dbo.PurchaseReceiveInput AS TABLE(MaNVL varchar(100),Quantity decimal(38,18),Reason nvarchar(200));
GO
GRANT EXECUTE,REFERENCES ON TYPE::dbo.PurchaseReceiveInput TO CafeManagerApp;
GO
/* ===== 5. Validate: số lượng nguyên ===== */
ALTER PROCEDURE dbo.sp_PurchaseValidate @Lines dbo.PurchaseLinesInput READONLY,@Total decimal(18,0) OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM @Lines) OR EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR UnitPrice IS NULL
   OR Quantity<=0 OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,0)
   OR UnitPrice<=0 OR UnitPrice>999999999999999999 OR UnitPrice<>ROUND(UnitPrice,0))
 OR EXISTS(SELECT MaNVL FROM @Lines GROUP BY MaNVL HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU n WHERE n.MaNVL=l.MaNVL))
   THROW 52701,N'Dòng nhập không hợp lệ hoặc trùng nguyên liệu; số lượng phải là số nguyên.',1;
 DECLARE @sum decimal(38,0)=(SELECT SUM(ROUND(CONVERT(decimal(18,0),Quantity)*CONVERT(decimal(18,0),UnitPrice),0)) FROM @Lines);
 IF @sum<=0 OR @sum>999999999999999999 THROW 52705,N'Tổng nhập ngoài giới hạn.',1;
 SET @Total=CONVERT(decimal(18,0),@sum);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchasePreview @Actor int,@Stamp binary(8),@Lines dbo.PurchaseLinesInput READONLY AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; DECLARE @total decimal(18,0); EXEC dbo.sp_PurchaseValidate @Lines,@total OUTPUT; SELECT @total Total;
END;
GO
/* ===== 6. Tạo phiếu chờ nhận (ADMIN) ===== */
CREATE PROCEDURE dbo.sp_TaoPhieuNhap @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Supplier varchar(max),@Date datetime2(0),
 @Due date=NULL,@Immediate decimal(38,18),@ExpectedTotal decimal(38,18),@Lines dbo.PurchaseLinesInput READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Date IS NULL OR @Supplier IS NULL OR LEN(@Supplier)<>5 OR @Immediate IS NULL OR @Immediate<0
  OR @Immediate>999999999999999999 OR @Immediate<>ROUND(@Immediate,0) THROW 52701,N'Thông tin nhập không hợp lệ.',1;
 IF CONVERT(date,@Date)>CONVERT(date,SYSDATETIME()) THROW 52701,N'Ngày nhập không được ở tương lai.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @json nvarchar(max)=(SELECT MaNVL,Quantity,UnitPrice FROM @Lines ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),@payload nvarchar(max),@code varchar(10)=NULL,@replay bit,@total decimal(18,0);
  SET @payload=(SELECT @Supplier s,@Date d,@Due due,@Immediate i,@ExpectedTotal e,@json l FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'PurchaseCreate',@payload,@code OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_PurchaseValidate @Lines,@total OUTPUT;
   IF @ExpectedTotal IS NULL OR @ExpectedTotal<>@total OR @Immediate>@total THROW 52703,N'Preview hoặc phần trả ngay không hợp lệ.',1;
   IF NOT EXISTS(SELECT 1 FROM dbo.NHACUNGCAP WITH(HOLDLOCK) WHERE MaNCC=@Supplier) THROW 52701,N'Nhà cung cấp không tồn tại.',1;
   DECLARE @employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor),@num bigint=NEXT VALUE FOR dbo.SeqPhieuNhap;
   SET @code='PN'+RIGHT('00000000'+CONVERT(varchar(20),@num),8);
   INSERT dbo.PHIEUNHAP(MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind,TraNgayDuKien,HanTraDuKien)
    VALUES(@code,@Date,@employee,@Supplier,0,0,'New',CONVERT(decimal(18,0),@Immediate),@Due);
   INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap,SoLuongNhan,LyDoLech,DaGhiKho)
    SELECT @code,MaNVL,CONVERT(decimal(18,4),Quantity),CONVERT(decimal(18,0),UnitPrice),NULL,NULL,0 FROM @Lines;
   DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code);
   EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PurchaseCreate',@payload,@code,@revision;
  END;
  COMMIT; SELECT @code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 7. Lưu số kiểm (ADMIN/STORE) ===== */
CREATE PROCEDURE dbo.sp_LuuKiemNhan @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),
 @ExpectedRevision binary(8),@Lines dbo.PurchaseReceiveInput READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Code IS NULL OR LEN(@Code)<>10 OR @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM @Lines)
  THROW 52701,N'Thông tin kiểm nhận không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR Quantity<0
   OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,0)) THROW 52701,N'Số thực nhận phải là số nguyên không âm.',1;
 IF EXISTS(SELECT MaNVL FROM @Lines GROUP BY MaNVL HAVING COUNT(*)>1) THROW 52701,N'Dòng kiểm nhận trùng nguyên liệu.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @json nvarchar(max)=(SELECT MaNVL,Quantity,REPLACE(Reason,NCHAR(13),N'') Reason FROM @Lines ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),
   @payload nvarchar(max),@receipt varchar(10)=NULL,@replay bit;
  SET @payload=(SELECT @Code c,CONVERT(varchar(16),@ExpectedRevision,2) r,@json l FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'PurchaseCount',@payload,@receipt OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaPN=@Code AND DaChot=0 AND DaHuy=0 AND Revision=@ExpectedRevision)
    THROW 52706,N'Phiếu đã đổi, đã huỷ hoặc đã chốt; tải lại trước khi lưu số kiểm.',1;
   IF (SELECT COUNT(*) FROM dbo.CHITIET_PN WHERE MaPN=@Code)<>(SELECT COUNT(*) FROM @Lines)
    OR EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PN d WHERE d.MaPN=@Code AND d.MaNVL=l.MaNVL))
    THROW 52701,N'Phải kiểm đủ đúng các dòng của phiếu.',1;
   IF EXISTS(SELECT 1 FROM @Lines l JOIN dbo.CHITIET_PN d ON d.MaPN=@Code AND d.MaNVL=l.MaNVL
     WHERE l.Quantity>d.SoLuong OR (l.Quantity<>d.SoLuong AND NULLIF(LTRIM(RTRIM(l.Reason)),N'') IS NULL))
    THROW 52704,N'Thực nhận không vượt số đặt; lệch phải ghi lý do.',1;
   UPDATE d SET SoLuongNhan=l.Quantity,LyDoLech=NULLIF(LTRIM(RTRIM(l.Reason)),N'')
   FROM dbo.CHITIET_PN d JOIN @Lines l ON l.MaNVL=d.MaNVL WHERE d.MaPN=@Code;
   UPDATE dbo.PHIEUNHAP SET NgayKiem=SYSDATETIME(),MaNVKiem=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor) WHERE MaPN=@Code;
   DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUNHAP WHERE MaPN=@Code);
   EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PurchaseCount',@payload,@Code,@revision;
  END;
  COMMIT; SELECT @Code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 8. Chốt nhận (ADMIN): cộng tồn, chi trả ngay, nợ theo thực nhận ===== */
CREATE PROCEDURE dbo.sp_ChotNhanHang @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),
 @ExpectedRevision binary(8),@Immediate decimal(38,18) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Code IS NULL OR LEN(@Code)<>10 OR @ExpectedRevision IS NULL OR @Immediate IS NULL
  OR @Immediate<0 OR @Immediate>999999999999999999 OR @Immediate<>ROUND(@Immediate,0) THROW 52701,N'Thông tin chốt nhận không hợp lệ.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max),@receipt varchar(10)=NULL,@replay bit;
  SET @payload=(SELECT @Code c,CONVERT(varchar(16),@ExpectedRevision,2) r,@Immediate i FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'PurchaseReceive',@payload,@receipt OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @date datetime2(0),@due date,@total decimal(18,0),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor);
   SELECT @date=p.NgayNhap,@due=p.HanTraDuKien FROM dbo.PHIEUNHAP p WITH(UPDLOCK,HOLDLOCK)
    WHERE p.MaPN=@Code AND p.DaChot=0 AND p.DaHuy=0 AND p.NgayKiem IS NOT NULL AND p.Revision=@ExpectedRevision;
   IF @date IS NULL THROW 52706,N'Phiếu phải được kiểm và chưa chốt; tải lại trước khi chốt.',1;
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_PN WITH(UPDLOCK,HOLDLOCK) WHERE MaPN=@Code AND (SoLuongNhan IS NULL OR DaGhiKho=1))
    THROW 52704,N'Chưa đủ số kiểm hoặc phiếu đã ghi kho.',1;
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_PN WHERE MaPN=@Code AND (SoLuongNhan<>ROUND(SoLuongNhan,0)
     OR (SoLuongNhan<>SoLuong AND NULLIF(LTRIM(RTRIM(LyDoLech)),N'') IS NULL)))
    THROW 52704,N'Số kiểm không hợp lệ hoặc thiếu lý do lệch.',1;
   UPDATE dbo.CHITIET_PN SET DaGhiKho=1 WHERE MaPN=@Code AND DaGhiKho=0;
   SELECT @total=TongTien FROM dbo.PHIEUNHAP WHERE MaPN=@Code;
   IF @total IS NULL OR @total<=0 THROW 52705,N'Tổng thực nhận không hợp lệ.',1;
   IF @Immediate>@total THROW 52703,N'Trả ngay vượt tổng thực nhận.',1;
   IF @Immediate>0 AND NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='ADMIN')
    THROW 51605,N'Chỉ quản lý được ghi chi trả ngay.',1;
   IF @Immediate>0
   BEGIN
    DECLARE @tc char(10),@money decimal(18,0)=CONVERT(decimal(18,0),@Immediate);
    EXEC dbo.sp_FinanceSystemExpense 'PurchaseImmediate',@employee,@money,@date,@Code,NULL,NULL,@tc OUTPUT;
   END;
   IF @Immediate<@total
   BEGIN
    DECLARE @num bigint=NEXT VALUE FOR dbo.SeqCongNo,@cn char(10);
    SET @cn='CN'+RIGHT('00000000'+CONVERT(varchar(20),@num),8);
    INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES(@cn,@Code,@total-CONVERT(decimal(18,0),@Immediate),@due);
   END;
   UPDATE dbo.PHIEUNHAP SET DaChot=1,NgayChot=SYSDATETIME() WHERE MaPN=@Code;
   DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUNHAP WHERE MaPN=@Code);
   EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PurchaseReceive',@payload,@Code,@revision;
  END;
  COMMIT; SELECT @Code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 9. Huỷ phiếu chưa chốt (ADMIN) ===== */
CREATE PROCEDURE dbo.sp_HuyPhieuNhap @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),
 @ExpectedRevision binary(8),@Reason nvarchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @Reason=NULLIF(LTRIM(RTRIM(@Reason)),N'');
 IF @@TRANCOUNT<>0 OR @Code IS NULL OR LEN(@Code)<>10 OR @ExpectedRevision IS NULL
  OR @Reason IS NULL OR LEN(@Reason)>200 THROW 52701,N'Cần lý do huỷ tối đa 200 ký tự.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max),@receipt varchar(10)=NULL,@replay bit;
  SET @payload=(SELECT @Code c,CONVERT(varchar(16),@ExpectedRevision,2) r,@Reason n FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'PurchaseCancel',@payload,@receipt OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WITH(UPDLOCK,HOLDLOCK) WHERE MaPN=@Code AND DaChot=0 AND DaHuy=0 AND Revision=@ExpectedRevision)
    THROW 52706,N'Chỉ huỷ phiếu chưa chốt và đúng bản đang mở; tải lại.',1;
   UPDATE dbo.PHIEUNHAP SET DaHuy=1,LyDoHuy=@Reason,NgayHuy=SYSDATETIME() WHERE MaPN=@Code;
   DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUNHAP WHERE MaPN=@Code);
   EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PurchaseCancel',@payload,@Code,@revision;
  END;
  COMMIT; SELECT @Code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 10. Đọc danh sách/chi tiết ===== */
CREATE OR ALTER VIEW dbo.vw_PurchaseRead AS
 SELECT p.MaPN Code,p.NgayNhap Date,p.MaNCC Supplier,p.TongTien Total,p.CutoverKind,
 CASE WHEN p.CutoverKind<>'New' THEN NULL WHEN p.DaChot=1 THEN COALESCE(f.SoTien,0) ELSE p.TraNgayDuKien END Immediate,
 f.MaTC FinanceCode,c.MaCN DebtCode,c.TienNo Debt,p.Revision,
 p.TrangThai,p.TraNgayDuKien,p.HanTraDuKien,p.NgayKiem,p.MaNVKiem,p.LyDoHuy,
 (SELECT d.MaNVL Code,n.TenNVL Name,n.DonViTinh Unit,d.SoLuong Quantity,d.SoLuongNhan Received,
   d.SoLuongNhan-d.SoLuong Diff,d.LyDoLech Reason,d.DonGiaNhap Price
  FROM dbo.CHITIET_PN d JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=d.MaNVL WHERE d.MaPN=p.MaPN ORDER BY d.MaNVL FOR JSON PATH) LinesJson
 FROM dbo.PHIEUNHAP p LEFT JOIN dbo.PHIEUTHUCHI f ON f.MaPN=p.MaPN LEFT JOIN dbo.CONGNO_NCC c ON c.MaPN=p.MaPN;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@Status varchar(10)=NULL AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
 IF @Skip<0 OR LEN(@Search)>100 OR (@Status IS NOT NULL AND @Status NOT IN('ChoNhan','DaKiem','DaChot','DaHuy')) THROW 52701,N'Bộ lọc sai.',1;
 SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_PurchaseRead
 WHERE (@Status IS NULL OR TrangThai=@Status)
   AND (Code LIKE N'%'+@Search+N'%' OR Supplier LIKE N'%'+@Search+N'%')
 ORDER BY Date DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; SELECT *,1 TotalRows FROM dbo.vw_PurchaseRead WHERE Code=@Code; END;
GO
CREATE PROCEDURE dbo.sp_PurchaseMaterialLookup @Actor int,@Stamp binary(8) AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
 SELECT n.MaNVL Code,n.TenNVL Name,n.DonViTinh Unit,n.SoLuongTon Stock,
  (SELECT TOP(1) d.DonGiaNhap FROM dbo.CHITIET_PN d JOIN dbo.PHIEUNHAP p ON p.MaPN=d.MaPN
   WHERE d.MaNVL=n.MaNVL AND p.DaChot=1 AND p.CutoverKind='New' ORDER BY p.NgayNhap DESC,p.MaPN DESC) LastPrice
 FROM dbo.NGUYENVATLIEU n ORDER BY n.TenNVL;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
 SELECT EntityCode FROM dbo.G03OperationReceipt WHERE ActorAccountId=@Actor AND RequestId=@RequestId; END;
GO
/* ===== 11. Báo cáo tồn dùng số thực nhận đã ghi kho ===== */
CREATE OR ALTER VIEW dbo.vw_ReportStockEvents AS
 WITH receipts AS(SELECT Operation,EntityCode,MIN(CompletedAt) At FROM dbo.G03OperationReceipt
  WHERE Operation IN('StockIssueCreate','SalesCheckout','StocktakeCommit') GROUP BY Operation,EntityCode),
 purchases AS(SELECT EntityCode,COALESCE(MAX(CASE WHEN Operation='PurchaseReceive' THEN CompletedAt END),
   MIN(CASE WHEN Operation='PurchaseCreate' THEN CompletedAt END)) At
  FROM dbo.G03OperationReceipt WHERE Operation IN('PurchaseCreate','PurchaseReceive') GROUP BY EntityCode)
 SELECT l.MaNVL Material,'Purchase' SourceType,p.MaPN Code,COALESCE(r.At,CONVERT(datetime2(7),p.NgayChot)) At,
  p.NgayNhap SourceDate,CONVERT(decimal(38,4),COALESCE(l.SoLuongNhan,l.SoLuong)) Quantity,
  CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END) FallbackTime
 FROM dbo.PHIEUNHAP p JOIN dbo.CHITIET_PN l ON l.MaPN=p.MaPN
  LEFT JOIN purchases r ON r.EntityCode=p.MaPN
 WHERE p.DaChot=1 AND p.CutoverKind='New' AND l.DaGhiKho=1
 UNION ALL
 SELECT l.MaNVL,'Issue',p.MaPX,COALESCE(r.At,CONVERT(datetime2(7),p.NgayChot)),p.NgayXuat,-CONVERT(decimal(38,4),l.SoLuongXuat),CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END)
 FROM dbo.PHIEUXUAT p JOIN dbo.CHITIET_PX l ON l.MaPX=p.MaPX LEFT JOIN receipts r ON r.Operation='StockIssueCreate' AND r.EntityCode=p.MaPX WHERE p.DaChot=1 AND p.CutoverKind='New'
 UNION ALL
 SELECT l.MaNVL,'Invoice',h.MaHD,COALESCE(r.At,CONVERT(datetime2(7),h.NgayThanhToan)),h.NgayThanhToan,-CONVERT(decimal(38,4),l.SoLuong),CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END)
 FROM dbo.HOADON h JOIN dbo.TIEUHAO_HOADON l ON l.MaHD=h.MaHD LEFT JOIN receipts r ON r.Operation='SalesCheckout' AND r.EntityCode=h.MaHD
 JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD AND s.Kind='Checkout' WHERE h.TrangThai=N'Da thanh toan'
 UNION ALL
 SELECT l.MaNVL,'Stocktake',p.MaKK,r.At,CONVERT(datetime2(0),p.NgayKK),CONVERT(decimal(38,4),l.TonThucTe-l.TonHeThong),CONVERT(bit,0)
 FROM dbo.PHIEUKIEMKE p JOIN dbo.CHITIET_KK l ON l.MaKK=p.MaKK LEFT JOIN receipts r ON r.Operation='StocktakeCommit' AND r.EntityCode=p.MaKK WHERE p.DaDieuChinh=1 AND p.CutoverKind='New';
GO
/* ===== 12. Gỡ contract một bước cũ, cấp quyền ===== */
IF OBJECT_ID(N'dbo.sp_LapPhieuNhap',N'P') IS NOT NULL DROP PROCEDURE dbo.sp_LapPhieuNhap;
GO
GRANT EXECUTE ON dbo.sp_TaoPhieuNhap TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LuuKiemNhan TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ChotNhanHang TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_HuyPhieuNhap TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchasePreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseLookups TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseMaterialLookup TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseReceipt TO CafeManagerApp;
GO
