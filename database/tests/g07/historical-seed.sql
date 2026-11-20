-- Fixture lịch sử qua baseline trước migration; không sửa đơn đã chốt/không tắt trigger.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07[_]%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL THROW 53501,N'Chỉ DBtest G07.',1;
INSERT dbo.HOADON(MaHD,NgayLap,MaNV,TongTien,PhiShip,GiamGia,TrangThai,LoaiDon)
VALUES('HD20259999','2025-04-01T12:00:00','NV004',0,0,0,N'Chua thanh toan',N'Mang di');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('HD20259999','SP001',1,25000);
DECLARE @tt dbo.DanhSachThanhToan;
INSERT @tt(MaPT,SoTien) VALUES('PT001',25000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20259999',@ThanhToan=@tt,@NgayTT='2025-04-02T23:59:59';
