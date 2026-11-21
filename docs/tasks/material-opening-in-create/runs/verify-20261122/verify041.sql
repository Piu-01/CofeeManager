USE [CoffeeManager_Test_041_20261122_manual];
SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @admin int,@astamp binary(8),@store int,@sstamp binary(8),@today date=CONVERT(date,SYSDATETIME()),@next date=DATEADD(day,1,CONVERT(date,SYSDATETIME()));
SELECT @admin=MaTK,@astamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
SELECT @store=MaTK,@sstamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV010';

-- K01: tạo nguyên liệu không nhập tồn đầu -> mốc 0, nguồn mặc định, tồn 0
DECLARE @idA uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialCreate @admin,@astamp,'NVL95',@idA,NULL,N'Test 041 khong ton dau',N'Kg';
SELECT 'K01' T,
 CASE WHEN n.SoLuongTon=0 AND o.Quantity=0 AND o.Source=N'Khởi tạo nguyên liệu' AND o.Kind='Manual' AND o.UnitSnapshot=N'Kg' AND o.ActorAccountId=@admin THEN 1 ELSE 0 END Pass
FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL WHERE n.MaNVL='NVL95';

-- K02: tạo có tồn đầu và nguồn -> tồn đúng, mốc đúng
DECLARE @idB uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialCreate @admin,@astamp,'NVL94',@idB,NULL,N'Test 041 co ton dau',N'Kg',1.25,N'Kiem ke mo quan';
SELECT 'K02' T,
 CASE WHEN n.SoLuongTon=1.25 AND o.Quantity=1.25 AND o.UnitSnapshot=N'Kg' AND o.Source=N'Kiem ke mo quan' THEN 1 ELSE 0 END Pass
FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL WHERE n.MaNVL='NVL94';

-- K03: gửi lặp cùng RequestId/nội dung -> không nhân bản
EXEC dbo.sp_MaterialCreate @admin,@astamp,'NVL94',@idB,NULL,N'Test 041 co ton dau',N'Kg',1.25,N'Kiem ke mo quan';
SELECT 'K03' T,
 CASE WHEN (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL94')=1 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94')=1.25 THEN 1 ELSE 0 END Pass;

-- K04: thiếu nguồn khi có tồn đầu -> chặn, không tạo
DECLARE @rejectNoSource bit=0,@idD uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_MaterialCreate @admin,@astamp,'NVL93',@idD,NULL,N'Test 041 thieu nguon',N'Kg',2; END TRY
BEGIN CATCH SET @rejectNoSource=1; END CATCH;
SELECT 'K04' T, @rejectNoSource Rejected, CASE WHEN EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93') THEN 0 ELSE 1 END NotCreated;

-- K05: STORE tạo nguyên liệu -> được phép
DECLARE @idE uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialCreate @store,@sstamp,'NVL92',@idE,NULL,N'Test 041 kho tao',N'Kg';
SELECT 'K05' T,
 CASE WHEN n.SoLuongTon=0 AND o.Quantity=0 AND o.ActorAccountId=@store THEN 1 ELSE 0 END Pass
FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL WHERE n.MaNVL='NVL92';

-- K06: không còn nguyên liệu thiếu mốc
SELECT 'K06' T, CASE WHEN COUNT(*)=0 THEN 1 ELSE 0 END Pass FROM dbo.NGUYENVATLIEU n WHERE NOT EXISTS(SELECT 1 FROM dbo.MaterialOpening o WHERE o.MaNVL=n.MaNVL);

-- K07: kỳ bắt đầu SAU mốc -> Coverage Complete (chỉ xem NVL94)
SELECT 'K07' T FROM (SELECT 1 x) z;
EXEC dbo.sp_ReportInventory @admin,@astamp,@next,@next,@Material='NVL94';

-- K07b: kỳ gồm ngày tạo (mốc nằm trong kỳ) -> Incomplete đúng quy ước hiện có
SELECT 'K07b' T FROM (SELECT 1 x) z;
EXEC dbo.sp_ReportInventory @admin,@astamp,@today,@today,@Material='NVL94';

-- K08: nguồn kho của NVL94 có dòng Opening
SELECT 'K08' T FROM (SELECT 1 x) z;
EXEC dbo.sp_ReportSourceList @admin,@astamp,'Inventory',@today,@today,'NVL94';

-- K09: đổi đơn vị khi tồn đầu 0 -> cho phép, cập nhật snapshot
DECLARE @rev95 binary(8); SELECT @rev95=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';
DECLARE @idU uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialUpdate @admin,@astamp,'NVL95',@idU,@rev95,N'Test 041 khong ton dau',N'kg';
SELECT 'K09' T,
 CASE WHEN n.DonViTinh=N'kg' AND o.UnitSnapshot=N'kg' THEN 1 ELSE 0 END Pass
FROM dbo.NGUYENVATLIEU n JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL WHERE n.MaNVL='NVL95';

-- K10: đổi đơn vị khi đã có tồn đầu > 0 -> chặn
DECLARE @rev94 binary(8); SELECT @rev94=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94';
DECLARE @rejectUnit bit=0,@idV uniqueidentifier=NEWID();
BEGIN TRY EXEC dbo.sp_MaterialUpdate @admin,@astamp,'NVL94',@idV,@rev94,N'Test 041 co ton dau',N'Lit'; END TRY
BEGIN CATCH SET @rejectUnit=1; END CATCH;
SELECT 'K10' T, @rejectUnit Rejected, (SELECT DonViTinh FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94') UnitAfter;

-- K11/K12: xóa nguyên liệu chưa dùng -> xóa kèm mốc, không lỗi FK
DECLARE @rev95b binary(8); SELECT @rev95b=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';
DECLARE @idX uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialDelete @admin,@astamp,'NVL95',@idX,@rev95b;
SELECT 'K11' T, CASE WHEN (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')=0 AND (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL95')=0 THEN 1 ELSE 0 END Pass;
DECLARE @rev94b binary(8); SELECT @rev94b=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94';
DECLARE @idY uniqueidentifier=NEWID();
EXEC dbo.sp_MaterialDelete @admin,@astamp,'NVL94',@idY,@rev94b;
SELECT 'K12' T, CASE WHEN (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94')=0 AND (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL94')=0 THEN 1 ELSE 0 END Pass;

-- K13: app bị chặn sp_MaterialOpen nhưng vẫn tạo nguyên liệu được
DECLARE @rev92 binary(8); SELECT @rev92=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92';
DECLARE @openDenied bit=0,@idZ uniqueidentifier=NEWID();
EXECUTE AS USER='CoffeeManager_Development_Runtime';
BEGIN TRY EXEC dbo.sp_MaterialOpen @admin,@astamp,'NVL92',@idZ,@rev92,1.0,N'App open'; END TRY
BEGIN CATCH SET @openDenied=1; END CATCH;
REVERT;
DECLARE @appCreate bit=0,@idW uniqueidentifier=NEWID();
EXECUTE AS USER='CoffeeManager_Development_Runtime';
BEGIN TRY EXEC dbo.sp_MaterialCreate @admin,@astamp,'NVL91',@idW,NULL,N'App tao',N'Kg'; SET @appCreate=1; END TRY
BEGIN CATCH SET @appCreate=0; END CATCH;
REVERT;
SELECT 'K13' T, @openDenied OpenDenied, @appCreate AppCreateAllowed, (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL91') AppOpeningRows,
 CASE WHEN @openDenied=1 AND @appCreate=1 AND (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL91')=1 THEN 1 ELSE 0 END Pass;

-- K14: schema version
SELECT 'K14' T, CASE WHEN MAX(Version)=41 THEN 1 ELSE 0 END Pass FROM dbo.SchemaMigrations;
