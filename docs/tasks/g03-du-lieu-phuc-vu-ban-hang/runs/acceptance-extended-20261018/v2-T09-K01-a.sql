DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialCreate @a,@s,'NVL95',@id,NULL,N'Kiểm thử cà phê',N'Kg';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')<>0 THROW 52999,'Initial not zero',1;
SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95';SET @id=NEWID();
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=0.0180,@Source=N'Acceptance';
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL95',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=0.0180,@Source=N'Acceptance';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL95')<>0.0180 OR (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL95')<>1 THROW 52999,'Opening mismatch',1;
