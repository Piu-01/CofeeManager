DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialCreate @a,@s,'NVL88',@id,NULL,N'Maximum test',N'Kg';SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88';SET @id=NEWID();
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL88',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=99999999999999.9999,@Source=N'Maximum';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88')<>99999999999999.9999 THROW 52999,'Precision lost',1;
