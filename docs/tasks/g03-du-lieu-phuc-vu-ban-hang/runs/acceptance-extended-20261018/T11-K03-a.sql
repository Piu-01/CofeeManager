DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0003' AND TrangThai=N'Dang phuc vu' AND MaHD='HD20250410') THROW 52999,'Seed mismatch',1;EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';
