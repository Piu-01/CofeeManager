-- G04-CONC-01: payment validation must not range-lock neighboring orders.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=15)
 THROW 52207,N'Migration015 và transaction migration bắt buộc.',1;
GO
ALTER TRIGGER dbo.TR_CTTT_KiemTra ON dbo.CHITIET_THANHTOAN AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @code char(10),@status nvarchar(30),@payable decimal(38,0),@allocated decimal(38,0);
 -- Khóa đúng khóa chính hiện hữu, không lọc trạng thái trước khi lấy khóa.
 -- Thứ tự mã ổn định cũng giữ contract cho batch tác động nhiều hóa đơn.
 DECLARE orders CURSOR LOCAL FAST_FORWARD FOR
  SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted ORDER BY MaHD;
 OPEN orders; FETCH NEXT FROM orders INTO @code;
 WHILE @@FETCH_STATUS=0
 BEGIN
  SELECT @status=NULL,@payable=NULL;
  SELECT @status=TrangThai,@payable=ThanhTien
  FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK,INDEX(PK_HOADON),FORCESEEK)
  WHERE MaHD=@code;
  IF @status IS NULL OR @status<>N'Chua thanh toan'
   THROW 51002,N'Khong duoc sua thanh toan cua hoa don da chot/huy.',1;
  SELECT @allocated=SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code;
  IF @allocated>@payable
   THROW 51003,N'Tien phan bo thanh toan vuot so phai tra (khong luu tien khach dua).',1;
  FETCH NEXT FROM orders INTO @code;
 END;
 CLOSE orders; DEALLOCATE orders;
END;
