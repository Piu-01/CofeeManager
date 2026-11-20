-- T25: công có version, khóa theo nhân viên, batch nguyên tử.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=22) THROW 53207,N'Cần migration022 và runner transaction.',1;
IF EXISTS(SELECT 1 FROM dbo.CHAMCONG a JOIN dbo.CHAMCONG b ON a.MaNV=b.MaNV AND (a.NgayLam<b.NgayLam OR (a.NgayLam=b.NgayLam AND a.MaCa<b.MaCa))
 WHERE a.GioVao IS NOT NULL AND b.GioVao IS NOT NULL AND (a.GioRa IS NULL OR b.GioVao<a.GioRa) AND (b.GioRa IS NULL OR a.GioVao<b.GioRa))
 THROW 53207,N'Công cũ có khoảng giờ chồng nhau; cần đối soát.',1;
ALTER TABLE dbo.CHAMCONG ADD Revision rowversion;
CREATE TABLE dbo.AttendanceBatchReceipt(RequestId uniqueidentifier NOT NULL PRIMARY KEY,Actor int NOT NULL REFERENCES dbo.TAIKHOAN(MaTK),PayloadHash binary(32) NOT NULL,RecordedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE dbo.AttendanceBatchReceiptLine(RequestId uniqueidentifier NOT NULL REFERENCES dbo.AttendanceBatchReceipt(RequestId),RowNo int NOT NULL,Action varchar(10) NOT NULL,MaNV char(5) NOT NULL,NgayLam date NOT NULL,MaCa char(5) NOT NULL,Revision binary(8) NULL,PRIMARY KEY(RequestId,RowNo));
DENY INSERT,UPDATE,DELETE ON dbo.CHAMCONG TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.AttendanceBatchReceipt TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.AttendanceBatchReceiptLine TO CafeManagerApp;
GO
CREATE TYPE dbo.AttendanceBatch AS TABLE(RowNo int NOT NULL PRIMARY KEY,Action varchar(20) NOT NULL,MaNV varchar(100) NOT NULL,NgayLam date NOT NULL,MaCa varchar(100) NOT NULL,GioVao datetime2(7) NULL,GioRa datetime2(7) NULL,TrangThai nvarchar(100) NOT NULL,ExpectedRevision binary(8) NULL);
GO
CREATE TRIGGER dbo.TR_HRReceiptRequestScope ON dbo.G03OperationReceipt AFTER INSERT AS
BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.AttendanceBatchReceipt a ON a.RequestId=i.RequestId) THROW 51608,N'Request đã dùng cho batch công.',1; END;
GO
CREATE TRIGGER dbo.TR_AttendanceClosedPeriod ON dbo.CHAMCONG AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @nv char(5); DECLARE employees CURSOR LOCAL FAST_FORWARD FOR SELECT MaNV FROM(SELECT MaNV FROM inserted UNION SELECT MaNV FROM deleted) n ORDER BY MaNV;
 OPEN employees; FETCH NEXT FROM employees INTO @nv;
 WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.sp_HRLockEmployee @nv; FETCH NEXT FROM employees INTO @nv; END;
 CLOSE employees; DEALLOCATE employees;
 IF EXISTS(SELECT 1 FROM (SELECT MaNV,NgayLam FROM inserted UNION SELECT MaNV,NgayLam FROM deleted) c JOIN dbo.BANGLUONG b WITH(HOLDLOCK) ON b.MaNV=c.MaNV AND b.Thang=MONTH(c.NgayLam) AND b.Nam=YEAR(c.NgayLam))
 THROW 53202,N'Kỳ đã chốt lương; không được thay công.',1;
END;
GO
CREATE VIEW dbo.vw_AttendanceRead AS
SELECT c.MaNV Employee,n.TenNV EmployeeName,c.NgayLam WorkDate,c.MaCa Shift,s.TenCa ShiftName,c.GioVao ClockIn,c.GioRa ClockOut,c.TrangThai Status,c.Revision,
 CAST(CASE WHEN c.TrangThai=N'Di lam' AND (c.GioVao IS NULL OR c.GioRa IS NULL) THEN 1 ELSE 0 END AS bit) Incomplete,
 CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.BANGLUONG b WHERE b.MaNV=c.MaNV AND b.Thang=MONTH(c.NgayLam) AND b.Nam=YEAR(c.NgayLam)) THEN 1 ELSE 0 END AS bit) PeriodLocked,
 CASE WHEN c.GioVao IS NOT NULL AND c.GioRa IS NOT NULL THEN CONVERT(decimal(10,2),ROUND(CONVERT(decimal(20,4),DATEDIFF_BIG(second,c.GioVao,c.GioRa))/3600,2)) END Hours
FROM dbo.CHAMCONG c JOIN dbo.NHANVIEN n ON n.MaNV=c.MaNV JOIN dbo.CALAMVIEC s ON s.MaCa=c.MaCa;
GO
CREATE PROCEDURE dbo.sp_AttendanceList @Actor int,@Stamp binary(8),@Month int,@Year int,@Employee varchar(max)=NULL,@Status nvarchar(30)=NULL,@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Month IS NULL OR @Year IS NULL OR @Month NOT BETWEEN 1 AND 12 OR @Year NOT BETWEEN 2000 AND 9999 OR @Skip<0 OR LEN(@Search)>100 THROW 53201,N'Bộ lọc công không hợp lệ.',1;
 DECLARE @from date=DATEFROMPARTS(@Year,@Month,1),@last date=EOMONTH(DATEFROMPARTS(@Year,@Month,1));
 SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_AttendanceRead WHERE WorkDate>=@from AND WorkDate<=@last AND (@Employee IS NULL OR Employee=@Employee)
 AND (@Status IS NULL OR (@Status=N'Incomplete' AND Incomplete=1) OR (@Status=N'Complete' AND Status=N'Di lam' AND Incomplete=0) OR Status=@Status)
 AND (Employee LIKE N'%'+@Search+N'%' OR EmployeeName LIKE N'%'+@Search+N'%' OR ShiftName LIKE N'%'+@Search+N'%') ORDER BY WorkDate DESC,Employee,Shift OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_AttendanceGet @Actor int,@Stamp binary(8),@Employee varchar(max),@Date date,@Shift varchar(max) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN'; SELECT *,1 TotalRows FROM dbo.vw_AttendanceRead WHERE Employee=@Employee AND WorkDate=@Date AND Shift=@Shift; END;
GO
CREATE PROCEDURE dbo.sp_AttendanceLookups @Actor int,@Stamp binary(8) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT 'Employee' Kind,MaNV Code,TenNV Name,DangLamViec Active FROM dbo.NHANVIEN
 UNION ALL SELECT 'Shift',MaCa,TenCa,CONVERT(bit,1) FROM dbo.CALAMVIEC ORDER BY Kind,Code;
END;
GO
CREATE PROCEDURE dbo.sp_AttendanceSaveBatch @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Lines dbo.AttendanceBatch READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @RequestId IS NULL OR (SELECT COUNT(*) FROM @Lines) NOT BETWEEN 1 AND 200 THROW 53201,N'Batch cần1–200 dòng và request ID.',1;
 IF EXISTS(SELECT 1 FROM @Lines WHERE Action NOT IN('Insert','Update','Delete') OR LEN(MaNV)<>5 OR LEN(MaCa)<>5 OR YEAR(NgayLam)<2000
 OR TrangThai NOT IN(N'Di lam',N'Nghi phep',N'Vang') OR (GioRa IS NOT NULL AND (GioVao IS NULL OR GioRa<=GioVao)) OR (GioVao IS NOT NULL AND CONVERT(date,GioVao)<>NgayLam)
 OR (TrangThai<>N'Di lam' AND (GioVao IS NOT NULL OR GioRa IS NOT NULL)) OR DATEPART(nanosecond,GioVao)<>0 OR DATEPART(nanosecond,GioRa)<>0
 OR (Action<>'Insert' AND ExpectedRevision IS NULL)) OR EXISTS(SELECT 1 FROM @Lines GROUP BY MaNV,NgayLam,MaCa HAVING COUNT(*)>1)
 THROW 53201,N'Dòng công/key/giờ/trạng thái không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @json nvarchar(max)=(SELECT * FROM @Lines ORDER BY RowNo FOR JSON PATH,INCLUDE_NULL_VALUES),@hash binary(32),@r int,@resource nvarchar(255)=N'CafeManager:Request:'+CONVERT(nvarchar(36),@RequestId);
 SET @hash=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@json));
 EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @r<0 THROW 52603,N'Yêu cầu đang xử lý.',1;
 IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId) THROW 51608,N'Request đã dùng cho nghiệp vụ khác.',1;
 IF EXISTS(SELECT 1 FROM dbo.AttendanceBatchReceipt WHERE RequestId=@RequestId)
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceBatchReceipt WHERE RequestId=@RequestId AND Actor=@Actor AND PayloadHash=@hash) THROW 51608,N'Yêu cầu khác nội dung.',1;
 END
 ELSE
 BEGIN
  DECLARE @nv char(5); DECLARE employees CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT MaNV FROM @Lines ORDER BY MaNV;
  OPEN employees; FETCH NEXT FROM employees INTO @nv;
  WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.sp_HRLockEmployee @nv; FETCH NEXT FROM employees INTO @nv; END;
  CLOSE employees; DEALLOCATE employees;
  IF EXISTS(SELECT 1 FROM @Lines l LEFT JOIN dbo.NHANVIEN n WITH(UPDLOCK,HOLDLOCK) ON n.MaNV=l.MaNV LEFT JOIN dbo.CALAMVIEC s WITH(HOLDLOCK) ON s.MaCa=l.MaCa
   WHERE n.MaNV IS NULL OR s.MaCa IS NULL OR (l.Action<>'Delete' AND n.DangLamViec=0)) THROW 53202,N'Nhân viên đã nghỉ hoặc ca không tồn tại.',1;
  IF EXISTS(SELECT 1 FROM @Lines l JOIN dbo.BANGLUONG b WITH(UPDLOCK,HOLDLOCK) ON b.MaNV=l.MaNV AND b.Thang=MONTH(l.NgayLam) AND b.Nam=YEAR(l.NgayLam)) THROW 53202,N'Kỳ đã chốt lương.',1;
  IF EXISTS(SELECT 1 FROM @Lines l LEFT JOIN dbo.CHAMCONG c WITH(UPDLOCK,HOLDLOCK) ON c.MaNV=l.MaNV AND c.NgayLam=l.NgayLam AND c.MaCa=l.MaCa
   WHERE (l.Action='Insert' AND c.MaNV IS NOT NULL) OR (l.Action<>'Insert' AND (c.MaNV IS NULL OR c.Revision<>l.ExpectedRevision))) THROW 53203,N'Công đã thay đổi hoặc trùng key; tải lại.',1;
  DELETE c FROM dbo.CHAMCONG c JOIN @Lines l ON c.MaNV=l.MaNV AND c.NgayLam=l.NgayLam AND c.MaCa=l.MaCa WHERE l.Action='Delete';
  UPDATE c SET GioVao=l.GioVao,GioRa=l.GioRa,TrangThai=l.TrangThai FROM dbo.CHAMCONG c JOIN @Lines l ON c.MaNV=l.MaNV AND c.NgayLam=l.NgayLam AND c.MaCa=l.MaCa WHERE l.Action='Update';
  INSERT dbo.CHAMCONG(MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai) SELECT MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai FROM @Lines WHERE Action='Insert';
  -- Kiểm trạng thái cuối của batch, gồm ca qua đêm và ca đang mở.
  IF EXISTS(SELECT 1 FROM dbo.CHAMCONG a JOIN dbo.CHAMCONG b ON a.MaNV=b.MaNV AND (a.NgayLam<b.NgayLam OR(a.NgayLam=b.NgayLam AND a.MaCa<b.MaCa))
   WHERE a.MaNV IN(SELECT MaNV FROM @Lines) AND a.GioVao IS NOT NULL AND b.GioVao IS NOT NULL AND (a.GioRa IS NULL OR b.GioVao<a.GioRa) AND(b.GioRa IS NULL OR a.GioVao<b.GioRa)) THROW 53203,N'Ca chồng giờ; toàn batch chưa được ghi.',1;
  INSERT dbo.AttendanceBatchReceipt(RequestId,Actor,PayloadHash) VALUES(@RequestId,@Actor,@hash);
  INSERT dbo.AttendanceBatchReceiptLine(RequestId,RowNo,Action,MaNV,NgayLam,MaCa,Revision)
  SELECT @RequestId,l.RowNo,l.Action,l.MaNV,l.NgayLam,l.MaCa,c.Revision FROM @Lines l LEFT JOIN dbo.CHAMCONG c ON c.MaNV=l.MaNV AND c.NgayLam=l.NgayLam AND c.MaCa=l.MaCa;
 END;
 COMMIT; SELECT CONVERT(varchar(36),@RequestId) EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_AttendanceReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @r int,@resource nvarchar(255)=N'CafeManager:Request:'+CONVERT(nvarchar(36),@RequestId);
 EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;
 IF @r<0 THROW 52603,N'Yêu cầu còn xử lý.',1;
 SELECT CONVERT(varchar(36),RequestId) EntityCode,'AttendanceBatch' Operation FROM dbo.AttendanceBatchReceipt WHERE RequestId=@RequestId;
 COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_AttendanceList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AttendanceGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AttendanceLookups TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AttendanceSaveBatch TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AttendanceReceipt TO CafeManagerApp;
GRANT EXECUTE ON TYPE::dbo.AttendanceBatch TO CafeManagerApp;
GRANT REFERENCES ON TYPE::dbo.AttendanceBatch TO CafeManagerApp;
