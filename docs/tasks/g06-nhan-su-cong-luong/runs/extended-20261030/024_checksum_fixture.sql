-- T26: snapshot lương, không sửa computed column hoặc suy lịch sử thiếu bằng chứng.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=23) THROW 53307,N'Cần schema023 và runner transaction.',1;
IF EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI p JOIN dbo.BANGLUONG b ON b.MaBL=p.MaBL LEFT JOIN dbo.SystemExpenseCategory s ON s.MaDMTC=p.MaDMTC
 WHERE p.OriginKind<>'Payroll' OR ISNULL(s.UseCase,'')<>'Payroll' OR p.SoTien<>b.ThucLinh OR p.NgayLap<b.NgayChot)
 THROW 53307,N'Liên kết chi lương cũ cần đối soát.',1;
ALTER TABLE dbo.BANGLUONG ADD SnapshotKind varchar(20) NOT NULL CONSTRAINT DF_PayrollKind DEFAULT 'LegacyUnknown',
 ContractSnapshot nvarchar(20) NULL,EmployeeNameSnapshot nvarchar(50) NULL,PositionSnapshot char(5) NULL,PositionNameSnapshot nvarchar(50) NULL,
 MonthlyRateSnapshot decimal(18,0) NULL,HourlyRateSnapshot decimal(18,0) NULL,TotalHours decimal(10,2) NULL,RegularHours decimal(10,2) NULL,
 OvertimeHours decimal(10,2) NULL,TotalSeconds bigint NULL,ActorAccountId int NULL REFERENCES dbo.TAIKHOAN(MaTK),SourceSnapshot nvarchar(max) NULL;
GO
ALTER TABLE dbo.BANGLUONG ADD CONSTRAINT CK_PayrollSnapshot CHECK(SnapshotKind='LegacyUnknown' OR (SnapshotKind='New'
 AND ContractSnapshot IS NOT NULL AND ContractSnapshot IN('Full-time','Part-time') AND EmployeeNameSnapshot IS NOT NULL AND PositionSnapshot IS NOT NULL
 AND MonthlyRateSnapshot IS NOT NULL AND HourlyRateSnapshot IS NOT NULL AND TotalHours IS NOT NULL AND RegularHours IS NOT NULL AND OvertimeHours IS NOT NULL AND TotalSeconds IS NOT NULL AND ActorAccountId IS NOT NULL
 AND RegularHours>=0 AND OvertimeHours>=0 AND RegularHours+OvertimeHours=TotalHours AND TotalSeconds>=0 AND ISJSON(SourceSnapshot)=1
 AND ((ContractSnapshot='Full-time' AND SoGioLam=0 AND DonGiaGio=0 AND LuongCoBan=MonthlyRateSnapshot)
 OR (ContractSnapshot='Part-time' AND LuongCoBan=0 AND DonGiaGio=HourlyRateSnapshot AND SoGioLam=RegularHours))));
CREATE TABLE dbo.PayrollAttendanceSnapshot(MaBL int NOT NULL REFERENCES dbo.BANGLUONG(MaBL),MaNV char(5) NOT NULL,NgayLam date NOT NULL,MaCa char(5) NOT NULL,
 GioVao datetime2(0) NULL,GioRa datetime2(0) NULL,TrangThai nvarchar(30) NOT NULL,SourceRevision binary(8) NOT NULL,PRIMARY KEY(MaBL,MaNV,NgayLam,MaCa));
DENY INSERT,UPDATE,DELETE ON dbo.BANGLUONG TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.PayrollAttendanceSnapshot TO CafeManagerApp;
GO
CREATE TRIGGER dbo.TR_PayrollImmutable ON dbo.BANGLUONG AFTER UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 53302,N'Bảng lương chốt bất biến.',1; END;
GO
CREATE TRIGGER dbo.TR_PayrollAttendanceImmutable ON dbo.PayrollAttendanceSnapshot AFTER UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 53302,N'Nguồn công snapshot bất biến.',1; END;
GO
CREATE TRIGGER dbo.TR_PayrollExpense ON dbo.PHIEUTHUCHI AFTER INSERT AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.BANGLUONG b WITH(UPDLOCK,HOLDLOCK) ON b.MaBL=i.MaBL
 WHERE i.OriginKind='Payroll' AND (b.SnapshotKind<>'New' OR b.ThucLinh<=0 OR i.SoTien<>b.ThucLinh OR i.NgayLap<b.NgayChot)) THROW 53302,N'Chi lương phải đúng thực lĩnh/snapshot/ngày chốt.',1;
END;
GO
CREATE PROCEDURE dbo.sp_PayrollCalculate @Employee varchar(max),@Month int,@Year int,@Regular decimal(38,18)=NULL,@Overtime decimal(38,18)=0,
 @OvertimePay decimal(38,18)=0,@Allowance decimal(38,18)=NULL,@Bonus decimal(38,18)=0,@Deduction decimal(38,18)=0,
 @Snapshot nvarchar(max) OUTPUT,@Token binary(32) OUTPUT,@Amount decimal(18,0) OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF @Employee IS NULL OR LEN(@Employee)<>5 OR @Month IS NULL OR @Month NOT BETWEEN 1 AND 12 OR @Year IS NULL OR @Year NOT BETWEEN 2000 AND 9999 THROW 53301,N'Nhân viên/kỳ lương không hợp lệ.',1;
 DECLARE @from date=DATEFROMPARTS(@Year,@Month,1),@last date=EOMONTH(DATEFROMPARTS(@Year,@Month,1));
 IF @from>CONVERT(date,SYSDATETIME()) THROW 53301,N'Không chốt kỳ tương lai.',1;
 DECLARE @contract nvarchar(20),@name nvarchar(50),@position char(5),@positionName nvarchar(50),@monthly decimal(18,0),@hourly decimal(18,0),@employeeVersion binary(8),@positionVersion binary(8),@employed bit;
 SELECT @contract=n.LoaiHopDong,@name=n.TenNV,@position=n.MaCV,@positionName=c.TenCV,@monthly=n.LuongCung,@hourly=n.LuongTheoGio,@employeeVersion=n.Revision,@positionVersion=c.CatalogRevision,@Allowance=COALESCE(@Allowance,c.PhuCap),@employed=n.DangLamViec
 FROM dbo.NHANVIEN n WITH(HOLDLOCK) JOIN dbo.CHUCVU c WITH(HOLDLOCK) ON c.MaCV=n.MaCV WHERE n.MaNV=@Employee;
 IF @contract IS NULL THROW 53301,N'Nhân viên không tồn tại.',1;
 DECLARE @seconds bigint=(SELECT COALESCE(SUM(DATEDIFF_BIG(second,GioVao,GioRa)),0) FROM dbo.CHAMCONG WITH(HOLDLOCK) WHERE MaNV=@Employee AND NgayLam>=@from AND NgayLam<=@last AND TrangThai=N'Di lam' AND GioVao IS NOT NULL AND GioRa IS NOT NULL),@hours decimal(38,2);
 SET @hours=ROUND(CONVERT(decimal(26,6),@seconds)/3600,2);
 SET @Regular=COALESCE(@Regular,@hours); SET @Overtime=COALESCE(@Overtime,0);
 IF @hours>99999999.99 OR @Regular<0 OR @Overtime<0 OR @Regular<>ROUND(@Regular,2) OR @Overtime<>ROUND(@Overtime,2) OR @Regular+@Overtime<>@hours THROW 53301,N'Giờ thường + tăng ca phải bằng tổng giờ công, tối đa2 số lẻ.',1;
 IF @OvertimePay IS NULL OR @Allowance IS NULL OR @Bonus IS NULL OR @Deduction IS NULL OR EXISTS(SELECT 1 FROM(VALUES(@OvertimePay),(@Allowance),(@Bonus),(@Deduction)) a(v) WHERE v<0 OR v>999999999999999999 OR v<>ROUND(v,0)) THROW 53301,N'Các khoản tiền cần nguyên VND không âm.',1;
 DECLARE @base decimal(18,0)=CASE WHEN @contract='Full-time' THEN @monthly ELSE 0 END,@rate decimal(18,0)=CASE WHEN @contract='Part-time' THEN @hourly ELSE 0 END,
 @paidHours decimal(10,2)=CASE WHEN @contract='Part-time' THEN @Regular ELSE 0 END,@gross decimal(38,0);
 SET @gross=CONVERT(decimal(38,0),@base)+ROUND(CONVERT(decimal(10,2),@paidHours)*CONVERT(decimal(18,0),@rate),0)+@OvertimePay+@Allowance+@Bonus;
 IF @gross>999999999999999999 OR @Deduction>@gross THROW 53305,N'Thực lĩnh vượt giới hạn hoặc khấu trừ vượt tổng.',1;
 SET @Amount=CONVERT(decimal(18,0),@gross-@Deduction);
 DECLARE @issues nvarchar(max)=(SELECT Message FROM(
 SELECT N'Công Đi làm chưa đủ giờ vào/ra; quay về Chấm công để xử lý.' Message WHERE EXISTS(SELECT 1 FROM dbo.CHAMCONG WHERE MaNV=@Employee AND NgayLam>=@from AND NgayLam<=@last AND TrangThai=N'Di lam' AND (GioVao IS NULL OR GioRa IS NULL))
 UNION ALL SELECT N'Ca có khoảng giờ chồng nhau; xử lý trước khi chốt.' WHERE EXISTS(SELECT 1 FROM dbo.CHAMCONG a JOIN dbo.CHAMCONG b ON a.MaNV=b.MaNV AND (a.NgayLam<b.NgayLam OR(a.NgayLam=b.NgayLam AND a.MaCa<b.MaCa)) WHERE a.MaNV=@Employee AND (a.NgayLam BETWEEN @from AND @last OR b.NgayLam BETWEEN @from AND @last) AND a.GioVao IS NOT NULL AND b.GioVao IS NOT NULL AND(a.GioRa IS NULL OR b.GioVao<a.GioRa) AND(b.GioRa IS NULL OR a.GioVao<b.GioRa))
 UNION ALL SELECT N'Nhân viên/kỳ này đã có bảng lương.' WHERE EXISTS(SELECT 1 FROM dbo.BANGLUONG WHERE MaNV=@Employee AND Thang=@Month AND Nam=@Year)) x FOR JSON PATH);
 DECLARE @attendance nvarchar(max)=(SELECT MaNV Employee,NgayLam WorkDate,MaCa Shift,GioVao ClockIn,GioRa ClockOut,TrangThai Status,Revision FROM dbo.CHAMCONG WITH(HOLDLOCK) WHERE MaNV=@Employee AND NgayLam>=@from AND NgayLam<=@last ORDER BY NgayLam,MaCa FOR JSON PATH,INCLUDE_NULL_VALUES);
 SET @Snapshot=(SELECT @Employee Employee,@name EmployeeName,@Month Month,@Year Year,@contract Contract,@position Position,@positionName PositionName,@monthly MonthlyRate,@hourly HourlyRate,@employed Employed,
 @employeeVersion EmployeeVersion,@positionVersion PositionVersion,@seconds TotalSeconds,CONVERT(decimal(10,2),@hours) TotalHours,CONVERT(decimal(10,2),@Regular) RegularHours,CONVERT(decimal(10,2),@Overtime) OvertimeHours,
 @base BasePay,@rate HourlyPayRate,@paidHours PaidHours,CONVERT(decimal(18,0),@OvertimePay) OvertimePay,CONVERT(decimal(18,0),@Allowance) Allowance,CONVERT(decimal(18,0),@Bonus) Bonus,CONVERT(decimal(18,0),@Deduction) Deduction,@Amount Amount,
 JSON_QUERY(COALESCE(NULLIF(@issues,N''),N'[]')) Issues,JSON_QUERY(COALESCE(NULLIF(@attendance,N''),N'[]')) Attendance FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
 SET @Token=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@Snapshot));
END;
GO
CREATE PROCEDURE dbo.sp_PayrollPreview @Actor int,@Stamp binary(8),@Employee varchar(max),@Month int,@Year int,@Regular decimal(38,18)=NULL,@Overtime decimal(38,18)=0,
 @OvertimePay decimal(38,18)=0,@Allowance decimal(38,18)=NULL,@Bonus decimal(38,18)=0,@Deduction decimal(38,18)=0 AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @nv char(5)=@Employee; IF LEN(@Employee)<>5 THROW 53301,N'Mã nhân viên không hợp lệ.',1;
 EXEC dbo.sp_HRLockEmployee @nv;
 DECLARE @snapshot nvarchar(max),@token binary(32),@amount decimal(18,0);
 EXEC dbo.sp_PayrollCalculate @Employee,@Month,@Year,@Regular,@Overtime,@OvertimePay,@Allowance,@Bonus,@Deduction,@snapshot OUTPUT,@token OUTPUT,@amount OUTPUT;
 COMMIT; SELECT @snapshot Snapshot,@token Token;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_ChotBangLuong @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Employee varchar(max),@Month int,@Year int,@ExpectedToken varbinary(max),
 @Regular decimal(38,18),@Overtime decimal(38,18),@OvertimePay decimal(38,18),@Allowance decimal(38,18),@Bonus decimal(38,18),@Deduction decimal(38,18) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @ExpectedToken IS NULL OR DATALENGTH(@ExpectedToken)<>32 OR @Employee IS NULL OR LEN(@Employee)<>5 THROW 53301,N'Thiếu preview hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @payload nvarchar(max)=(SELECT @Employee e,@Month m,@Year y,@ExpectedToken t,@Regular r,@Overtime o,@OvertimePay p,@Allowance a,@Bonus b,@Deduction d FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@result varchar(10),@replay bit;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'PayrollCommit',@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  DECLARE @nv char(5)=@Employee; EXEC dbo.sp_HRLockEmployee @nv;
  IF EXISTS(SELECT 1 FROM dbo.BANGLUONG WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@Employee AND Thang=@Month AND Nam=@Year) THROW 53302,N'Kỳ đã có bảng lương.',1;
  DECLARE @snapshot nvarchar(max),@token binary(32),@amount decimal(18,0);
  EXEC dbo.sp_PayrollCalculate @Employee,@Month,@Year,@Regular,@Overtime,@OvertimePay,@Allowance,@Bonus,@Deduction,@snapshot OUTPUT,@token OUTPUT,@amount OUTPUT;
  IF @token<>@ExpectedToken THROW 53303,N'Preview đã thay đổi; xác nhận lại.',1;
  IF EXISTS(SELECT 1 FROM OPENJSON(@snapshot,'$.Issues')) THROW 53302,N'Công chưa hợp lệ; xử lý trước khi chốt.',1;
  INSERT dbo.BANGLUONG(MaNV,Thang,Nam,LuongCoBan,SoGioLam,DonGiaGio,TienTangCa,PhuCap,Thuong,KhauTru,SnapshotKind,ContractSnapshot,EmployeeNameSnapshot,PositionSnapshot,PositionNameSnapshot,MonthlyRateSnapshot,HourlyRateSnapshot,TotalHours,RegularHours,OvertimeHours,TotalSeconds,ActorAccountId,SourceSnapshot)
  SELECT @Employee,@Month,@Year,BasePay,PaidHours,HourlyPayRate,OvertimePay,Allowance,Bonus,Deduction,'New',Contract,EmployeeName,Position,PositionName,MonthlyRate,HourlyRate,TotalHours,RegularHours,OvertimeHours,TotalSeconds,@Actor,@snapshot
  FROM OPENJSON(@snapshot) WITH(BasePay decimal(18,0),PaidHours decimal(10,2),HourlyPayRate decimal(18,0),OvertimePay decimal(18,0),Allowance decimal(18,0),Bonus decimal(18,0),Deduction decimal(18,0),Contract nvarchar(20),EmployeeName nvarchar(50),Position char(5),PositionName nvarchar(50),MonthlyRate decimal(18,0),HourlyRate decimal(18,0),TotalHours decimal(10,2),RegularHours decimal(10,2),OvertimeHours decimal(10,2),TotalSeconds bigint);
  DECLARE @id int=CONVERT(int,SCOPE_IDENTITY()); SET @result=CONVERT(varchar(10),@id);
  INSERT dbo.PayrollAttendanceSnapshot(MaBL,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,SourceRevision)
  SELECT @id,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,Revision FROM dbo.CHAMCONG WHERE MaNV=@Employee AND NgayLam>=DATEFROMPARTS(@Year,@Month,1) AND NgayLam<=EOMONTH(DATEFROMPARTS(@Year,@Month,1));
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PayrollCommit',@payload,@result;
 END; COMMIT; SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE VIEW dbo.vw_PayrollRead AS
SELECT b.MaBL Id,b.MaNV Employee,COALESCE(b.EmployeeNameSnapshot,n.TenNV) EmployeeName,b.Thang Month,b.Nam Year,b.NgayChot CommittedAt,b.ThucLinh Amount,b.SnapshotKind,
 b.SourceSnapshot Snapshot,p.MaTC ExpenseCode,p.NgayLap PaidAt,
 CASE WHEN p.MaTC IS NOT NULL THEN 'Paid' WHEN b.SnapshotKind='LegacyUnknown' THEN 'LegacyUnknown' WHEN b.ThucLinh=0 THEN 'NoExpense' ELSE 'Unpaid' END Status
FROM dbo.BANGLUONG b JOIN dbo.NHANVIEN n ON n.MaNV=b.MaNV LEFT JOIN dbo.PHIEUTHUCHI p ON p.MaBL=b.MaBL;
GO
CREATE PROCEDURE dbo.sp_PayrollList @Actor int,@Stamp binary(8),@Month int,@Year int,@Search nvarchar(max)=N'',@Status varchar(20)=NULL,@Skip int=0 AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Month IS NULL OR @Year IS NULL OR @Month NOT BETWEEN 1 AND 12 OR @Year NOT BETWEEN 2000 AND 9999 OR @Skip<0 OR LEN(@Search)>100 THROW 53301,N'Bộ lọc lương không hợp lệ.',1;
 SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_PayrollRead WHERE Month=@Month AND Year=@Year AND(@Status IS NULL OR Status=@Status) AND(Employee LIKE N'%'+@Search+N'%' OR EmployeeName LIKE N'%'+@Search+N'%') ORDER BY Id DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_PayrollGet @Actor int,@Stamp binary(8),@Id int AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN'; SELECT *,1 TotalRows FROM dbo.vw_PayrollRead WHERE Id=@Id; END;
GO
CREATE PROCEDURE dbo.sp_ChiLuong @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Id int,@Date datetime2(7),@ExpectedAmount decimal(38,18) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Date IS NULL OR DATEPART(nanosecond,@Date)<>0 OR @Date>SYSDATETIME() OR @ExpectedAmount IS NULL THROW 53301,N'Ngày/số tiền xác nhận không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @payload nvarchar(max)=(SELECT @Id i,@Date d,@ExpectedAmount a FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@result varchar(10),@replay bit;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'PayrollPay',@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  DECLARE @nv char(5)=(SELECT MaNV FROM dbo.BANGLUONG WHERE MaBL=@Id);
  IF @nv IS NULL THROW 53302,N'Bảng lương không tồn tại.',1;
  EXEC dbo.sp_HRLockEmployee @nv;
  DECLARE @amount decimal(18,0),@chot datetime2(0),@kind varchar(20);
  SELECT @amount=ThucLinh,@chot=NgayChot,@kind=SnapshotKind FROM dbo.BANGLUONG WITH(UPDLOCK,HOLDLOCK) WHERE MaBL=@Id;
  IF @kind<>'New' OR @amount<=0 OR @ExpectedAmount<>@amount OR @Date<@chot OR EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WITH(UPDLOCK,HOLDLOCK) WHERE MaBL=@Id) THROW 53302,N'Legacy cần đối soát, không phát sinh chi hoặc bảng lương đã chi.',1;
  DECLARE @actorEmployee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor),@tc char(10),@date0 datetime2(0)=@Date;
  EXEC dbo.sp_FinanceSystemExpense @UseCase='Payroll',@Employee=@actorEmployee,@Amount=@amount,@Date=@date0,@MaBL=@Id,@Code=@tc OUTPUT;
  SET @result=CONVERT(varchar(10),@Id); EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PayrollPay',@payload,@result;
 END; COMMIT; SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_PayrollPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ChotBangLuong TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ChiLuong TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PayrollList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PayrollGet TO CafeManagerApp;

-- isolated checksum fixture
