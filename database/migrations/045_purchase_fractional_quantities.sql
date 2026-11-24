-- 045: cho phép số lượng nhập/số thực nhận là số thực tối đa 4 chữ số thập phân.
-- Đơn giá vẫn là số nguyên VND; tổng phiếu vẫn làm tròn về VND ở trigger/procedure.
-- Không đổi bảng/cột: SoLuong/SoLuongNhan đã là decimal(18,4), chỉ bỏ ràng buộc số nguyên.
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE Version=44)
    THROW 54501,N'Cần schema 044 trước migration 045.',1;
GO
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseValidate @Lines dbo.PurchaseLinesInput READONLY,@Total decimal(18,0) OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM @Lines) OR EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR UnitPrice IS NULL
   OR Quantity<=0 OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,4)
   OR UnitPrice<=0 OR UnitPrice>999999999999999999 OR UnitPrice<>ROUND(UnitPrice,0))
 OR EXISTS(SELECT MaNVL FROM @Lines GROUP BY MaNVL HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU n WHERE n.MaNVL=l.MaNVL))
   THROW 52701,N'Dòng nhập không hợp lệ hoặc trùng nguyên liệu; số lượng phải lớn hơn 0 và tối đa 4 số thập phân.',1;
 DECLARE @sum decimal(38,0)=(SELECT SUM(ROUND(CONVERT(decimal(18,4),Quantity)*CONVERT(decimal(18,0),UnitPrice),0)) FROM @Lines);
 IF @sum<=0 OR @sum>999999999999999999 THROW 52705,N'Tổng nhập ngoài giới hạn.',1;
 SET @Total=CONVERT(decimal(18,0),@sum);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_LuuKiemNhan @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),
 @ExpectedRevision binary(8),@Lines dbo.PurchaseReceiveInput READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Code IS NULL OR LEN(@Code)<>10 OR @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM @Lines)
  THROW 52701,N'Thông tin kiểm nhận không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR Quantity<0
   OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,4)) THROW 52701,N'Thực nhận phải là số thực không âm, tối đa 4 số thập phân.',1;
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
CREATE OR ALTER PROCEDURE dbo.sp_ChotNhanHang @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),
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
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_PN WHERE MaPN=@Code
     AND (SoLuongNhan<>SoLuong AND NULLIF(LTRIM(RTRIM(LyDoLech)),N'') IS NULL))
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
