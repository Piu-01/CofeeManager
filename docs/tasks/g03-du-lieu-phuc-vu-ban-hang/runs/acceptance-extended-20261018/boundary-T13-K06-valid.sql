DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @old int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001');
DECLARE @preview TABLE(Balance bigint,CustomerRevision binary(8),PolicyRevision binary(8),Tier char(5));
INSERT @preview EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Cong',1;
DECLARE @cr binary(8),@pr binary(8);SELECT @cr=CustomerRevision,@pr=PolicyRevision FROM @preview;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Boundary add',@cr,@pr,@id;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Boundary add',@cr,@pr,@id;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>@old+1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001' AND LyDo=N'Boundary add')<>1 THROW 52999,'Double points',1;
