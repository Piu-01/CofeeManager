DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT MaHD,GiamGia,ThanhTien,PhiShip,PhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH),@code varchar(10);
SELECT TOP(1) @code=k.MaKM,@rev=k.Revision FROM dbo.KHUYENMAI k JOIN dbo.HOADON h ON h.MaKM=k.MaKM WHERE h.TrangThai=N'Da thanh toan';
EXEC dbo.sp_PromotionUpdate @a,@s,@code,@id,@rev,N'Changed name and period','2026-10-18','2026-10-18',0;
IF @before<>(SELECT MaHD,GiamGia,ThanhTien,PhiShip,PhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) THROW 52999,'Paid snapshot changed',1;
