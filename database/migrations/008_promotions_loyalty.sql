-- T13. Apply after007 using the migration runner.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=7) THROW 52007,N'Migration007 bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(TenKM)),N'') IS NULL) THROW 52007,N'Tên khuyến mãi cũ không hợp lệ.',1;
ALTER TABLE dbo.KHUYENMAI ADD Revision rowversion;
CREATE TABLE dbo.LoyaltyPolicyState(Id int NOT NULL PRIMARY KEY CHECK(Id=1),Revision rowversion);
INSERT dbo.LoyaltyPolicyState(Id) VALUES(1);
ALTER TABLE dbo.G03OperationReceipt ADD ResultCount int NULL;
DENY INSERT,UPDATE,DELETE ON dbo.KHUYENMAI TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.HANGTHANHVIEN TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.LoyaltyPolicyState TO CafeManagerApp;
GO
CREATE TYPE dbo.LoyaltyTierInput AS TABLE(Code varchar(100) NOT NULL,MinimumPoints bigint NOT NULL,DiscountPercent bigint NOT NULL);
GO
CREATE FUNCTION dbo.fn_DiscountPreview(@Money decimal(18,0),@Ship decimal(18,0),@BusinessDate date,@Customer char(5),@Promotion char(10))
RETURNS TABLE AS RETURN
(
 SELECT x.Source,x.Tier,x.Promotion,x.PercentDiscount,
 CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@Money)*CONVERT(decimal(5,2),x.PercentDiscount)/100,0)) Discount,
 CONVERT(decimal(38,0),@Money)+@Ship-CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@Money)*CONVERT(decimal(5,2),x.PercentDiscount)/100,0)) Payable
 FROM (SELECT CASE WHEN @Promotion IS NOT NULL THEN 'Promotion' WHEN @Customer IS NOT NULL THEN 'Tier' ELSE 'None' END Source,
 k.MaHang Tier,p.MaKM Promotion,COALESCE(CASE WHEN @Promotion IS NOT NULL THEN p.PhanTramGiam ELSE h.PhanTramGiam END,0) PercentDiscount
 FROM (VALUES(1)) seed(n) LEFT JOIN dbo.KHACHHANG k ON k.MaKH=@Customer
 LEFT JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang
 LEFT JOIN dbo.KHUYENMAI p ON p.MaKM=@Promotion AND @BusinessDate BETWEEN p.NgayBD AND p.NgayKT) x
);
GO
CREATE PROCEDURE dbo.sp_DiscountPreview @Actor int,@Stamp binary(8),@Money decimal(38,18),@Ship decimal(38,18),@BusinessDate date,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @lock int; EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
  IF @Money IS NULL OR @Ship IS NULL OR @Money<0 OR @Money>999999999999999999 OR ROUND(@Money,0)<>@Money OR @Ship<0 OR @Ship>999999999999999999 OR ROUND(@Ship,0)<>@Ship OR @BusinessDate IS NULL
   OR (@Customer IS NOT NULL AND (DATALENGTH(@Customer)>5 OR NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Customer)))
   OR (@Promotion IS NOT NULL AND (DATALENGTH(@Promotion)>10 OR NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(HOLDLOCK) WHERE MaKM=@Promotion AND @BusinessDate BETWEEN NgayBD AND NgayKT))) THROW 52001,N'Dữ liệu preview hoặc chương trình không hiệu lực.',1;
  DECLARE @result TABLE(Source varchar(20),Tier char(5),Promotion char(10),PercentDiscount int,Discount decimal(18,0),Payable decimal(38,0));
  INSERT @result SELECT * FROM dbo.fn_DiscountPreview(CONVERT(decimal(18,0),@Money),CONVERT(decimal(18,0),@Ship),@BusinessDate,CONVERT(char(5),@Customer),CONVERT(char(10),@Promotion));
  IF EXISTS(SELECT 1 FROM @result WHERE Payable>999999999999999999) THROW 52001,N'Phải trả vượt giới hạn tiền.',1;
  SELECT r.Source,r.Tier,r.Promotion,r.PercentDiscount,r.Discount,CONVERT(decimal(18,0),r.Payable) Payable,SYSDATETIME() ReadAt,s.Revision PolicyRevision FROM @result r CROSS JOIN dbo.LoyaltyPolicyState s;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_PromotionList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@OnDate date=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 52001,N'Bộ lọc không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='CASH') SET @OnDate=COALESCE(@OnDate,CONVERT(date,SYSDATETIME()));
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaKM Code,TenKM Name,NgayBD StartDate,NgayKT EndDate,PhanTramGiam Discount,Revision FROM dbo.KHUYENMAI
 WHERE (TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~') AND (@OnDate IS NULL OR @OnDate BETWEEN NgayBD AND NgayKT)
 ORDER BY MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_PromotionGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; SELECT MaKM Code,TenKM Name,NgayBD StartDate,NgayKT EndDate,PhanTramGiam Discount,Revision FROM dbo.KHUYENMAI WHERE MaKM=@Code; END;
GO
CREATE PROCEDURE dbo.sp_PromotionWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Action NOT IN('Create','Update','Delete') THROW 52001,N'Thao tác không hợp lệ.',1;
  SET @Name=LTRIM(RTRIM(@Name));SET @Code=UPPER(LTRIM(RTRIM(@Code)));
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'KM[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52001,N'Mã KM và8chữ số.',1;
  IF @Action<>'Delete' AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>200 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%' OR @StartDate IS NULL OR @EndDate IS NULL OR @EndDate<@StartDate OR @Discount IS NULL OR @Discount<0 OR @Discount>100) THROW 52001,N'Thông tin chương trình không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Name n,@StartDate sd,@EndDate ed,@Discount d,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,@revision binary(8),@replay bit,@operation varchar(40)='Promotion'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create' INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES(@Code,@Name,@StartDate,@EndDate,CONVERT(int,@Discount));
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Chương trình đã thay đổi.',1;
    IF @Action='Update' UPDATE dbo.KHUYENMAI SET TenKM=@Name,NgayBD=@StartDate,NgayKT=@EndDate,PhanTramGiam=CONVERT(int,@Discount) WHERE MaKM=@Code;
    ELSE DELETE dbo.KHUYENMAI WHERE MaKM=@Code;
   END;
   SELECT @revision=Revision FROM dbo.KHUYENMAI WHERE MaKM=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyPolicyGet @Actor int,@Stamp binary(8) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN'; SELECT h.MaHang Code,h.DiemToiThieu MinimumPoints,h.PhanTramGiam Discount,s.Revision FROM dbo.HANGTHANHVIEN h CROSS JOIN dbo.LoyaltyPolicyState s ORDER BY h.DiemToiThieu; END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyValidateTiers @Tiers dbo.LoyaltyTierInput READONLY AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM @Tiers WHERE MinimumPoints=0) OR EXISTS(SELECT 1 FROM @Tiers WHERE MinimumPoints<0 OR MinimumPoints>2147483647 OR DiscountPercent<0 OR DiscountPercent>100 OR DATALENGTH(Code)=0 OR DATALENGTH(Code)>5 OR Code COLLATE Latin1_General_100_BIN2 LIKE '%[^A-Z0-9]%')
 OR EXISTS(SELECT Code FROM @Tiers GROUP BY Code HAVING COUNT(*)>1) OR EXISTS(SELECT MinimumPoints FROM @Tiers GROUP BY MinimumPoints HAVING COUNT(*)>1) THROW 52001,N'Chính sách cần ngưỡng0, mã/ngưỡng duy nhất, điểm không âm, tỷ lệ0..100.',1;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyPolicyPreview @Actor int,@Stamp binary(8),@Tiers dbo.LoyaltyTierInput READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';EXEC dbo.sp_LoyaltyValidateTiers @Tiers;
  DECLARE @lock int;EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
  DECLARE @payload nvarchar(max)=(SELECT Code,MinimumPoints,DiscountPercent FROM @Tiers ORDER BY Code FOR JSON PATH);
  SELECT s.Revision,CONVERT(binary(32),HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload))) PayloadHash,
  (SELECT COUNT(*) FROM dbo.KHACHHANG k CROSS APPLY(SELECT TOP(1) Code FROM @Tiers WHERE MinimumPoints<=k.DiemTichLuy ORDER BY MinimumPoints DESC) p WHERE k.MaHang<>p.Code) AffectedCustomers FROM dbo.LoyaltyPolicyState s;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyPolicyReplace @Actor int,@Stamp binary(8),@Tiers dbo.LoyaltyTierInput READONLY,@ExpectedRevision binary(8),@PayloadHash binary(32),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';EXEC dbo.sp_LoyaltyValidateTiers @Tiers;
  DECLARE @payload nvarchar(max)=(SELECT Code,MinimumPoints,DiscountPercent FROM @Tiers ORDER BY Code FOR JSON PATH);
  IF HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload))<>@PayloadHash OR @PayloadHash IS NULL THROW 52001,N'Nội dung chính sách khác preview.',1;
  DECLARE @receiptPayload nvarchar(max)=(SELECT @payload p,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@receiptPayload)),@code varchar(10)='POLICY',@revision binary(8),@replay bit,@lock int,@affected int;
  EXEC dbo.sp_G03BeginReceipt @Actor,'PolicyReplace',@RequestId,@hash,@code OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
   IF @lock<0 THROW 52004,N'Chính sách đang được sử dụng.',1;
   IF NOT EXISTS(SELECT 1 FROM dbo.LoyaltyPolicyState WITH(UPDLOCK,HOLDLOCK) WHERE Id=1 AND Revision=@ExpectedRevision) THROW 51603,N'Chính sách đã thay đổi.',1;
   -- Dành ngưỡng tạm không trùng cả chính sách cũ lẫn mới; giữ UNIQUE trong suốt transaction.
   DECLARE @moves TABLE(Code char(5) PRIMARY KEY,TemporaryThreshold int UNIQUE);
   DECLARE @candidate int=2147483647,@moveCode char(5);
   WHILE EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @moves m WHERE m.Code=h.MaHang))
   BEGIN
    SELECT TOP(1) @moveCode=MaHang FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @moves m WHERE m.Code=h.MaHang) ORDER BY MaHang;
    WHILE EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu=@candidate) OR EXISTS(SELECT 1 FROM @Tiers WHERE MinimumPoints=@candidate) OR EXISTS(SELECT 1 FROM @moves WHERE TemporaryThreshold=@candidate)
    BEGIN
     IF @candidate=0 THROW 52001,N'Không còn ngưỡng tạm cho chính sách.',1;
     SET @candidate-=1;
    END;
    INSERT @moves VALUES(@moveCode,@candidate);
   END;
   UPDATE h SET DiemToiThieu=m.TemporaryThreshold FROM dbo.HANGTHANHVIEN h JOIN @moves m ON m.Code=h.MaHang;
   UPDATE h SET DiemToiThieu=CONVERT(int,t.MinimumPoints),PhanTramGiam=CONVERT(int,t.DiscountPercent) FROM dbo.HANGTHANHVIEN h JOIN @Tiers t ON t.Code=h.MaHang;
   INSERT dbo.HANGTHANHVIEN(MaHang,DiemToiThieu,PhanTramGiam) SELECT Code,CONVERT(int,MinimumPoints),CONVERT(int,DiscountPercent) FROM @Tiers t WHERE NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE h.MaHang=t.Code);
   UPDATE k SET MaHang=p.Code FROM dbo.KHACHHANG k CROSS APPLY(SELECT TOP(1) Code FROM @Tiers WHERE MinimumPoints<=k.DiemTichLuy ORDER BY MinimumPoints DESC) p WHERE k.MaHang<>p.Code;
   SET @affected=@@ROWCOUNT;
   DELETE h FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @Tiers t WHERE t.Code=h.MaHang);

   UPDATE dbo.LoyaltyPolicyState SET Id=1 WHERE Id=1;SELECT @revision=Revision FROM dbo.LoyaltyPolicyState WHERE Id=1;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision,ResultCount) VALUES(@RequestId,@Actor,'PolicyReplace','POLICY',@hash,@revision,@affected);
  END;
  SELECT @affected=ResultCount FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
  COMMIT; SELECT @code Code,@revision Revision,@affected AffectedCustomers;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyAdjustPreview @Actor int,@Stamp binary(8),@Code varchar(max),@Direction nvarchar(10),@Points bigint AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  DECLARE @lock int;EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
  IF @Points<=0 OR @Points>2147483647 OR @Direction NOT IN(N'Cong',N'Tru') OR @Points IS NULL OR @Direction IS NULL THROW 52001,N'Điểm điều chỉnh không hợp lệ.',1;
  DECLARE @balance bigint,@customerRevision binary(8);
  SELECT @balance=CONVERT(bigint,DiemTichLuy)+CASE WHEN @Direction=N'Cong' THEN @Points ELSE -@Points END,@customerRevision=Revision FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Code;
  IF @balance IS NULL OR @balance<0 OR @balance>2147483647 THROW 52002,N'Không đủ điểm hoặc vượt giới hạn điểm.',1;
  SELECT @balance Balance,@customerRevision CustomerRevision,s.Revision PolicyRevision,(SELECT TOP(1) MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=@balance ORDER BY DiemToiThieu DESC) Tier FROM dbo.LoyaltyPolicyState s;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyAdjust @Actor int,@Stamp binary(8),@Code varchar(max),@Direction nvarchar(10),@Points bigint,@Reason nvarchar(max),@ExpectedCustomerRevision binary(8),@ExpectedPolicyRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  SET @Reason=LTRIM(RTRIM(@Reason));
  IF @Points IS NULL OR @Points<=0 OR @Points>2147483647 OR @Direction IS NULL OR @Direction NOT IN(N'Cong',N'Tru') OR @Reason IS NULL OR LEN(@Reason)=0 OR DATALENGTH(@Reason)>200 OR @Reason LIKE N'%'+NCHAR(9)+N'%' OR @Reason LIKE N'%'+NCHAR(10)+N'%' OR @Reason LIKE N'%'+NCHAR(13)+N'%' THROW 52001,N'Điểm/loại/lý do không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Direction d,@Points p,@Reason reason,@ExpectedCustomerRevision cr,@ExpectedPolicyRevision pr FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@codeResult varchar(10)=@Code,@revision binary(8),@replay bit,@lock int,@balance bigint;
  EXEC dbo.sp_G03BeginReceipt @Actor,'PointAdjust',@RequestId,@hash,@codeResult OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
   IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
   IF NOT EXISTS(SELECT 1 FROM dbo.LoyaltyPolicyState WHERE Revision=@ExpectedPolicyRevision) THROW 51603,N'Chính sách đã đổi; preview lại.',1;
   SELECT @balance=CONVERT(bigint,DiemTichLuy)+CASE WHEN @Direction=N'Cong' THEN @Points ELSE -@Points END FROM dbo.KHACHHANG WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@Code AND Revision=@ExpectedCustomerRevision;
   IF @balance IS NULL THROW 51603,N'Điểm khách đã đổi; preview lại.',1;
   IF @balance<0 OR @balance>2147483647 THROW 52002,N'Không đủ điểm hoặc vượt giới hạn.',1;
   INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo) VALUES(@Code,NULL,@Direction,CONVERT(int,@Points),@Reason);
   SELECT @revision=Revision FROM dbo.KHACHHANG WHERE MaKH=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,'PointAdjust',@Code,@hash,@revision);
  END;
  COMMIT;SELECT @codeResult Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_LoyaltyHistory @Actor int,@Stamp binary(8),@Code varchar(max),@Skip int=0 AS
BEGIN SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';IF @Skip<0 THROW 52001,N'Trang không hợp lệ.',1;
 SELECT MaGD,NgayGD,Loai,SoDiem,LyDo,MaHD FROM dbo.LICHSU_DIEM WHERE MaKH=@Code ORDER BY MaGD DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;END;
GO

CREATE PROCEDURE dbo.sp_PromotionCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL AS
BEGIN SET NOCOUNT ON;EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Create',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount;END;
GO
GRANT EXECUTE ON dbo.sp_PromotionCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_PromotionUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL AS
BEGIN SET NOCOUNT ON;EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Update',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount;END;
GO
GRANT EXECUTE ON dbo.sp_PromotionUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_PromotionDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL AS
BEGIN SET NOCOUNT ON;EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Delete',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount;END;
GO
GRANT EXECUTE ON dbo.sp_PromotionDelete TO CafeManagerApp;
GO
ALTER TRIGGER TR_LichSuDiem ON LICHSU_DIEM AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted WHERE NULLIF(LTRIM(RTRIM(LyDo)),N'') IS NULL OR LyDo LIKE N'%'+NCHAR(9)+N'%' OR LyDo LIKE N'%'+NCHAR(10)+N'%' OR LyDo LIKE N'%'+NCHAR(13)+N'%' OR (MaHD IS NOT NULL AND Loai<>N'Cong')) THROW 52001,N'Lý do hoặc loại điểm mua không hợp lệ.',1;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51011,N'Lich su diem khong sua/xoa; tao giao dich dieu chinh.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN HOADON h ON h.MaHD=i.MaHD
        WHERE h.MaKH IS NULL OR h.MaKH<>i.MaKH OR h.TrangThai<>N'Da thanh toan')
        THROW 51012,N'Hoa don tich diem phai da thanh toan va dung khach hang.',1;
    ;WITH delta AS (SELECT MaKH,SUM(CASE WHEN Loai=N'Cong' THEN SoDiem ELSE -SoDiem END) Diem
                  FROM inserted GROUP BY MaKH)
    UPDATE k WITH (UPDLOCK) SET DiemTichLuy=k.DiemTichLuy+d.Diem
    FROM KHACHHANG k JOIN delta d ON d.MaKH=k.MaKH;
    UPDATE k SET MaHang=h.MaHang
    FROM KHACHHANG k CROSS APPLY
      (SELECT TOP (1) MaHang FROM HANGTHANHVIEN WHERE DiemToiThieu<=k.DiemTichLuy
       ORDER BY DiemToiThieu DESC) h
    WHERE k.MaKH IN (SELECT MaKH FROM inserted);
END;
GO

ALTER PROCEDURE dbo.sp_ThanhToanHoaDon
    @MaHD CHAR(10), @ThanhToan dbo.DanhSachThanhToan READONLY,
    @NgayTT DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @policyLock int;
        EXEC @policyLock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
        IF @policyLock<0 THROW 52004,N'Chính sách đang cập nhật; đối chiếu trước thử lại.',1;
        DECLARE @recipeLock int;
        EXEC @recipeLock=sys.sp_getapplock @Resource=N'CafeManager:Recipes',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
        IF @recipeLock<0 THROW 51704,N'Công thức đang được cập nhật; đối chiếu trước thử lại.',1;
        DECLARE @TrangThai NVARCHAR(30),@Tong DECIMAL(18,0),@Giam DECIMAL(18,0),
                @PhaiTra DECIMAL(18,0),@MaKH CHAR(5),@MaDT CHAR(5),@MaKM CHAR(10),@Ngay DATE;
        SELECT @TrangThai=TrangThai,@Tong=TongTien,@Giam=GiamGia,@PhaiTra=ThanhTien,
               @MaKH=MaKH,@MaDT=MaDT,@MaKM=MaKM,@Ngay=CONVERT(DATE,NgayLap)
        FROM HOADON WITH (UPDLOCK,HOLDLOCK) WHERE MaHD=@MaHD;
        IF @TrangThai IS NULL OR @TrangThai<>N'Chua thanh toan'
            THROW 51101,N'Hoa don khong ton tai hoac da chot/huy.',1;
        IF @Tong<=0 THROW 51102,N'Hoa don phai co mon.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_THANHTOAN WHERE MaHD=@MaHD)
            THROW 51103,N'Thu tuc nhan toan bo phan bo TT; don nay da co dong TT.',1;
        IF @PhaiTra<>COALESCE((SELECT SUM(SoTien) FROM @ThanhToan),0)
            THROW 51104,N'Tong phan bo thanh toan khong bang ThanhTien.',1;
        IF @MaKM IS NOT NULL AND NOT EXISTS
           (SELECT 1 FROM KHUYENMAI WHERE MaKM=@MaKM AND @Ngay BETWEEN NgayBD AND NgayKT)
            THROW 51105,N'Khuyen mai khong con hieu luc tai ngay lap don.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d JOIN SANPHAM s ON s.MaSP=d.MaSP
                   WHERE d.MaHD=@MaHD AND s.TrangThai<>N'Dang ban')
            THROW 51106,N'Hoa don co mon da ngung ban.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d WHERE d.MaHD=@MaHD
                   AND NOT EXISTS (SELECT 1 FROM CONGTHUC c WHERE c.MaSP=d.MaSP))
            THROW 51107,N'Mon chua co cong thuc. Hang ban san can nguyen lieu thanh pham.',1;
        SET @NgayTT=COALESCE(@NgayTT,SYSDATETIME());
        IF @NgayTT < (SELECT NgayLap FROM HOADON WHERE MaHD=@MaHD)
            THROW 51108,N'Ngay thanh toan khong duoc truoc ngay lap.',1;
        INSERT INTO TIEUHAO_HOADON(MaHD,MaNVL,SoLuong)
        SELECT @MaHD,c.MaNVL,SUM(d.SoLuong*c.DinhMuc)
        FROM CHITIET_HD d JOIN CONGTHUC c ON c.MaSP=d.MaSP
        WHERE d.MaHD=@MaHD GROUP BY c.MaNVL;
        INSERT INTO CHITIET_THANHTOAN(MaHD,MaPT,SoTien,NgayTT,MaThamChieu)
        SELECT @MaHD,MaPT,SoTien,@NgayTT,MaThamChieu FROM @ThanhToan;
        DECLARE @Phi DECIMAL(5,2)=0;
        IF @MaDT IS NOT NULL SELECT @Phi=ChietKhau FROM DOITACGIAOHANG WHERE MaDT=@MaDT;
        UPDATE HOADON SET TrangThai=N'Da thanh toan',NgayThanhToan=@NgayTT,
               PhanTramPhiDoiTac=@Phi,PhiDoiTac=ROUND((@Tong-@Giam)*@Phi/100,0)
        WHERE MaHD=@MaHD;
        DECLARE @Diem INT=CONVERT(INT,FLOOR((@Tong-@Giam)/1000));
        IF @MaKH IS NOT NULL AND @Diem>0
            INSERT INTO LICHSU_DIEM(MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo)
            VALUES(@MaKH,@MaHD,@NgayTT,N'Cong',@Diem,N'Tich diem mua hang');
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO


GRANT EXECUTE ON dbo.sp_DiscountPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyPolicyGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyPolicyPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyPolicyReplace TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyAdjustPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyAdjust TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_LoyaltyHistory TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.LoyaltyTierInput TO CafeManagerApp;


