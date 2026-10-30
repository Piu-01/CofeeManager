-- T23: trả nợ tạo đúng một chi, lịch sử bất biến và số dư tại ngày.
SET NOCOUNT ON;SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=20) THROW 53007,N'Cần schema020.',1;
IF EXISTS(SELECT 1 FROM dbo.CONGNO_NCC c JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN WHERE c.TienNo>p.TongTien OR (SELECT COALESCE(SUM(tc.SoTien),0) FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=c.MaCN)>c.TienNo)
 OR EXISTS(SELECT 1 FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC JOIN dbo.CONGNO_NCC c ON c.MaCN=t.MaCN JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN WHERE tc.NgayLap<p.NgayNhap) THROW 53007,N'Lịch sử nợ sai số dư hoặc ngày nguồn.',1;
GO
ALTER TRIGGER dbo.TR_TraNo ON dbo.THANHTOAN_CONGNO AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted) THROW 51017,N'Lịch sử trả nợ không sửa/xóa.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.PHIEUTHUCHI p ON p.MaTC=i.MaTC JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC LEFT JOIN dbo.SystemExpenseCategory s ON s.MaDMTC=d.MaDMTC WHERE d.Loai<>N'Chi' OR ISNULL(s.UseCase,'')<>'SupplierDebt' OR p.OriginKind<>'SupplierDebt') THROW 53002,N'Trả nợ phải gắn đúng phiếu chi nguồn.',1;
 DECLARE @cn char(10),@original decimal(18,0),@paid decimal(38,0);
 DECLARE debts CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT MaCN FROM inserted ORDER BY MaCN;
 OPEN debts;FETCH NEXT FROM debts INTO @cn;
 WHILE @@FETCH_STATUS=0
 BEGIN
  SELECT @original=TienNo FROM dbo.CONGNO_NCC WITH(UPDLOCK,HOLDLOCK) WHERE MaCN=@cn;
  SELECT @paid=SUM(p.SoTien) FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI p ON p.MaTC=t.MaTC WHERE t.MaCN=@cn;
  IF @paid>@original THROW 53003,N'Số trả vượt nợ gốc.',1;
  FETCH NEXT FROM debts INTO @cn;
 END;CLOSE debts;DEALLOCATE debts;
END;
GO
ALTER PROCEDURE dbo.sp_TraCongNo @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),@Method varchar(max),@Amount decimal(38,18),@Date datetime2(0) AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Code IS NULL OR LEN(@Code)<>10 OR @Method IS NULL OR LEN(@Method)<>5 OR @Date IS NULL OR @Amount IS NULL OR @Amount<=0 OR @Amount>999999999999999999 OR @Amount<>ROUND(@Amount,0) THROW 53001,N'Thông tin trả nợ không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @payload nvarchar(max)=(SELECT @Code c,@Method m,@Amount a,@Date d FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@result varchar(10),@replay bit;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'SupplierDebtPay',@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  DECLARE @original decimal(18,0),@paid decimal(38,0),@source datetime2(0),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor);
  SELECT @original=c.TienNo,@source=p.NgayNhap FROM dbo.CONGNO_NCC c WITH(UPDLOCK,HOLDLOCK) JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN WHERE c.MaCN=@Code;
  IF @original IS NULL THROW 53002,N'Nợ không tồn tại.',1;
  SELECT @paid=COALESCE(SUM(tc.SoTien),0) FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=@Code;
  IF @Amount>@original-@paid THROW 53003,N'Số trả vượt còn nợ; tải lại số dư và sửa số tiền.',1;
  IF @Date<@source THROW 53001,N'Ngày trả trước ngày nhập nguồn.',1;
  IF @Method='PT010' OR NOT EXISTS(SELECT 1 FROM dbo.PaymentMethodSupport s WITH(HOLDLOCK) JOIN dbo.PHUONGTHUCTT p WITH(HOLDLOCK) ON p.MaPT=s.MaPT WHERE s.MaPT=@Method AND s.IsSupported=1) THROW 53001,N'Phương thức trả nợ chưa hỗ trợ.',1;
  DECLARE @tc char(10),@money decimal(18,0)=CONVERT(decimal(18,0),@Amount),@cn char(10)=@Code;
  EXEC dbo.sp_FinanceSystemExpense 'SupplierDebt',@employee,@money,@Date,NULL,NULL,@cn,@tc OUTPUT;
  INSERT dbo.THANHTOAN_CONGNO(MaCN,MaTC,MaPT) VALUES(@Code,@tc,@Method);
  SET @result=@tc;EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'SupplierDebtPay',@payload,@result;
 END;COMMIT;SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
CREATE VIEW dbo.vw_DebtRead AS SELECT v.MaCN Code,p.MaNCC Supplier,p.MaPN PurchaseCode,p.NgayNhap SourceDate,v.TienNo Original,v.DaTra Paid,v.ConNo Remaining,v.HanTra Due,v.TrangThai Status,
 (SELECT t.MaTT Id,tc.MaTC Code,tc.NgayLap Date,tc.SoTien Amount,t.MaPT Method FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=v.MaCN ORDER BY tc.NgayLap,t.MaTT FOR JSON PATH) HistoryJson
 FROM dbo.vw_CongNoNCC v JOIN dbo.PHIEUNHAP p ON p.MaPN=v.MaPN;
GO
CREATE PROCEDURE dbo.sp_DebtList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@AsOf date=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';IF @Skip<0 OR LEN(@Search)>100 THROW 53001,N'Bộ lọc sai.',1;
 IF @AsOf IS NULL
  SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_DebtRead WHERE Code LIKE N'%'+@Search+N'%' OR Supplier LIKE N'%'+@Search+N'%' OR PurchaseCode LIKE N'%'+@Search+N'%' ORDER BY SourceDate DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
 ELSE
 BEGIN
  DECLARE @until datetime2(0)=NULL;IF @AsOf<'99991231' SET @until=DATEADD(day,1,CONVERT(datetime2(0),@AsOf));
  SELECT c.MaCN Code,p.MaNCC Supplier,p.MaPN PurchaseCode,p.NgayNhap SourceDate,c.TienNo Original,COALESCE(a.Paid,0) Paid,c.TienNo-COALESCE(a.Paid,0) Remaining,c.HanTra Due,
   CASE WHEN c.TienNo=COALESCE(a.Paid,0) THEN N'Da tra' WHEN c.HanTra<@AsOf THEN N'Qua han' WHEN COALESCE(a.Paid,0)>0 THEN N'Tra mot phan' ELSE N'Chua tra' END Status,
   (SELECT t.MaTT Id,tc.MaTC Code,tc.NgayLap Date,tc.SoTien Amount,t.MaPT Method FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=c.MaCN AND (@until IS NULL OR tc.NgayLap<@until) ORDER BY tc.NgayLap,t.MaTT FOR JSON PATH) HistoryJson,COUNT(*) OVER() TotalRows
  FROM dbo.CONGNO_NCC c JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN OUTER APPLY(SELECT SUM(tc.SoTien) Paid FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=c.MaCN AND (@until IS NULL OR tc.NgayLap<@until)) a
  WHERE (@until IS NULL OR p.NgayNhap<@until) AND (c.MaCN LIKE N'%'+@Search+N'%' OR p.MaNCC LIKE N'%'+@Search+N'%' OR p.MaPN LIKE N'%'+@Search+N'%') ORDER BY p.NgayNhap DESC,c.MaCN DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
 END;
END;
GO
CREATE PROCEDURE dbo.sp_DebtGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';SELECT *,1 TotalRows FROM dbo.vw_DebtRead WHERE Code=@Code;END;
GO
CREATE PROCEDURE dbo.sp_DebtMethods @Actor int,@Stamp binary(8) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';SELECT p.MaPT Code,p.TenPT Name,CAST(NULL AS nvarchar(20)) Unit FROM dbo.PHUONGTHUCTT p JOIN dbo.PaymentMethodSupport s ON s.MaPT=p.MaPT WHERE s.IsSupported=1 AND p.MaPT<>'PT010' ORDER BY p.MaPT;END;
GO
CREATE PROCEDURE dbo.sp_DebtReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';SELECT EntityCode FROM dbo.G03OperationReceipt WHERE ActorAccountId=@Actor AND RequestId=@RequestId AND Operation='SupplierDebtPay';END;
GO
GRANT EXECUTE ON dbo.sp_TraCongNo TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_DebtList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_DebtGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_DebtMethods TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_DebtReceipt TO CafeManagerApp;
