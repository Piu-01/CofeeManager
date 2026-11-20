DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialCreate @a,@s,'NVL90',@id,NULL,N'Transaction test',N'Kg';
INSERT dbo.PHIEUNHAP(MaPN,MaNV,MaNCC)VALUES('PN09999001','NV001','NCC01');INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap)VALUES('PN09999001','NVL90',1,1000);
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL90')<>1 THROW 52999,'Baseline source failed',1;
