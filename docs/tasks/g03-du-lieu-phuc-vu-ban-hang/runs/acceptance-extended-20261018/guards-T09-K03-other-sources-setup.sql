DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialCreate @a,@s,'NVL85',@id,NULL,N'Consumption source',N'Kg';SET @id=NEWID();EXEC dbo.sp_MaterialCreate @a,@s,'NVL86',@id,NULL,N'Count source',N'Kg';SET @id=NEWID();EXEC dbo.sp_MaterialCreate @a,@s,'NVL87',@id,NULL,N'Export source',N'Kg';
INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap)VALUES('PN09999001','NVL85',2,1000),('PN09999001','NVL86',2,1000),('PN09999001','NVL87',2,1000);
INSERT dbo.PHIEUXUAT(MaPX,MaNV,LoaiXuat)VALUES('PX09999001','NV001',N'Huy do hong');INSERT dbo.CHITIET_PX(MaPX,MaNVL,SoLuongXuat)VALUES('PX09999001','NVL87',1);
DECLARE @date date=CONVERT(date,SYSDATETIME());INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV)VALUES('KK09999001',@date,'NV001');INSERT dbo.CHITIET_KK(MaKK,MaNVL,TonHeThong,TonThucTe)VALUES('KK09999001','NVL86',2,2);
INSERT dbo.HOADON(MaHD,MaNV,LoaiDon)VALUES('HD09999301','NV001',N'Mang di');INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong)VALUES('HD09999301','NVL85',1);
