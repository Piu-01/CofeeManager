-- CASE T02-K06-product-fk | Product category FK rejects missing code | 547 | FK_SP_DANHMUC
-- ACT
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('ZZ001',N'T02 FK','ZZ999',1000);
-- CASE T02-K06-order-fk | Invoice employee FK rejects missing code | 547 | FK_HD_NHANVIEN
-- ACT
INSERT dbo.HOADON(MaHD,MaNV,LoaiDon) VALUES('ZZFK000001','ZZ999',N'Mang di');
-- CASE T02-K06-negative-money | Negative product price violates CHECK | 547 | CHK_SP_GiaBan
-- ACT
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('ZZ002',N'T02 price','DM001',-1);
-- CASE T02-K06-negative-stock | Negative stock violates CHECK | 547 | CHK_NVL_TonKho
-- ACT
INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES('ZZ003',N'T02 stock',N'Kg',-1);
-- CASE T02-K06-zero-line | Zero invoice quantity violates CHECK | 547 | CHK_CTHD_SoLuong
-- SETUP
EXEC #T02_Order 'ZZZERO0001',@Quantity=0;
-- ACT
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('ZZZERO0001','SP001',0,25000);
-- CASE T02-K06-zero-receipt | Zero receipt quantity violates CHECK | 547 | CHK_CTPN_SoLuong
-- SETUP
INSERT dbo.PHIEUNHAP(MaPN,MaNV,MaNCC) VALUES('ZZZERO0002','NV001','NCC01');
-- ACT
INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap) VALUES('ZZZERO0002','NVL01',0,1000);
-- CASE T02-K06-zero-export | Zero export quantity violates CHECK | 547 | CHK_CTPX_SoLuong
-- SETUP
INSERT dbo.PHIEUXUAT(MaPX,MaNV,LoaiXuat) VALUES('ZZZERO0003','NV001',N'Huy do hong');
-- ACT
INSERT dbo.CHITIET_PX(MaPX,MaNVL,SoLuongXuat) VALUES('ZZZERO0003','NVL01',0);
-- CASE T02-K06-reverse-promotion | Reversed promotion dates violate CHECK | 547 | CHK_KM_NgayHopLe
-- ACT
INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('ZZDATE0001',N'T02 reversed','2025-04-02','2025-04-01',10);
-- CASE T02-K06-same-day | Same-day promotion is valid | 0 |
-- ACT
INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('ZZDATE0002',N'T02 same day','2025-04-01','2025-04-01',10);
-- ASSERT
IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='ZZDATE0002' AND NgayBD=NgayKT) THROW 52006,'Same-day promotion missing',1;
-- CASE T02-K07-null-phones | Two NULL phones each for customers and employees are accepted | 0 |
-- ACT
INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,MaHang) VALUES('ZN001',N'T02 NULL 1',NULL,'DONG'),('ZN002',N'T02 NULL 2',NULL,'DONG');
INSERT dbo.NHANVIEN(MaNV,TenNV,SDT,LoaiHopDong,MaCV) VALUES('ZN001',N'T02 NULL NV1',NULL,'Part-time','CV004'),('ZN002',N'T02 NULL NV2',NULL,'Part-time','CV004');
-- ASSERT
IF (SELECT COUNT(*) FROM dbo.KHACHHANG WHERE MaKH IN ('ZN001','ZN002') AND SDT IS NULL)<>2 THROW 52007,'Customer NULLs missing',1;
IF (SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('ZN001','ZN002') AND SDT IS NULL)<>2 THROW 52007,'Employee NULLs missing',1;
-- CASE T02-K07-customer-duplicate | Customer non-NULL phone duplicates are rejected | 2601 | UX_KH_SDT
-- ACT
INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,MaHang) VALUES('ZN003',N'T02 duplicate','0911111111','DONG');
-- CASE T02-K07-employee-duplicate | Employee non-NULL phone duplicates are rejected | 2601 | UX_NV_SDT
-- ACT
INSERT dbo.NHANVIEN(MaNV,TenNV,SDT,LoaiHopDong,MaCV) VALUES('ZN003',N'T02 duplicate','0901111111','Part-time','CV004');
-- CASE T02-K07-customer-update | Updating a phone to an existing customer phone is rejected | 2601 | UX_KH_SDT
-- ACT
UPDATE dbo.KHACHHANG SET SDT='0911111111' WHERE MaKH='ZN001';
-- CASE T02-K07-employee-update | Updating a phone to an existing employee phone is rejected | 2601 | UX_NV_SDT
-- ACT
UPDATE dbo.NHANVIEN SET SDT='0901111111' WHERE MaNV='ZN001';
