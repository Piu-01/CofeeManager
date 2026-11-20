IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G06[_]%' OR NOT EXISTS(SELECT 1 FROM dbo.G06TestMarker) THROW 53400,N'Sai DB test.',1;
DECLARE @code char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE TenDangNhap='smoke.account'),@id int,@expense char(10);
SELECT @id=b.MaBL,@expense=p.MaTC FROM dbo.BANGLUONG b JOIN dbo.PHIEUTHUCHI p ON p.MaBL=b.MaBL WHERE b.MaNV=@code;
IF @id IS NULL OR @expense IS NULL OR (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaBL=@id)<>1 THROW 53403,N'T26 thiếu hoặc trùng chi.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.BANGLUONG b JOIN dbo.PHIEUTHUCHI p ON p.MaBL=b.MaBL WHERE b.MaBL=@id AND b.ThucLinh=280000 AND p.SoTien=b.ThucLinh AND p.OriginKind='Payroll' AND p.MaDMTC='TC005' AND b.ContractSnapshot='Part-time' AND b.RegularHours=8 AND b.SoGioLam=8) THROW 53403,N'T26 tiền/snapshot sai.',1;
IF (SELECT COUNT(*) FROM dbo.PayrollAttendanceSnapshot WHERE MaBL=@id)<>1 THROW 53403,N'T26 nguồn công sai.',1;
IF EXISTS(SELECT 1 FROM dbo.BANGLUONG WHERE SnapshotKind='LegacyUnknown' AND SourceSnapshot IS NOT NULL) THROW 53403,N'T26 giả snapshot legacy.',1;
