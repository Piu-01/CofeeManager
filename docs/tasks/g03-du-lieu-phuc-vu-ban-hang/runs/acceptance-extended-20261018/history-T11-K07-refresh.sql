DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before TABLE(Code char(5),Name nvarchar(50),Area char(5),Status nvarchar(30),OrderCode char(10),Revision binary(8),ReadAt datetime2(7));
INSERT @before EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';
IF NOT EXISTS(SELECT 1 FROM @before WHERE OrderCode='HD20250410') THROW 52999,'Initial order wrong',1;
EXEC dbo.sp_HuyHoaDon 'HD20250410';
DELETE @before;INSERT @before EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';
IF NOT EXISTS(SELECT 1 FROM @before WHERE OrderCode IS NULL AND Status=N'Trong') THROW 52999,'Stale order returned',1;
