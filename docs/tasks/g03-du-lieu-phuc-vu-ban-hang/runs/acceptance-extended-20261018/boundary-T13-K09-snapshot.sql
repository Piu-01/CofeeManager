DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT MaHD,TongTien,GiamGia,ThanhTien,PhiShip,PhiDoiTac,PhanTramPhiDoiTac,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH),@code varchar(10),@sd date,@ed date;
SELECT TOP(1) @code=k.MaKM,@rev=k.Revision,@sd=k.NgayBD,@ed=k.NgayKT FROM dbo.KHUYENMAI k JOIN dbo.HOADON h ON h.MaKM=k.MaKM WHERE h.TrangThai=N'Da thanh toan';
EXEC dbo.sp_PromotionUpdate @a,@s,@code,@id,@rev,N'Acceptance snapshot change',@sd,@ed,25;
IF @before<>(SELECT MaHD,TongTien,GiamGia,ThanhTien,PhiShip,PhiDoiTac,PhanTramPhiDoiTac,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) THROW 52999,'Paid snapshot altered',1;
