-- CASE T02-K09-success | Checkout records grouped consumption split payments and exact points once | 0 |
-- SETUP
EXEC #T02_Order 'ZPAYOK0001';
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES('ZPAYOK0001','SP002',2,30000,N'Separate note');
-- ACT
DECLARE @coffee DECIMAL(18,4),@milk DECIMAL(18,4),@sugar DECIMAL(18,4),@points INT;
SELECT @coffee=SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01';
SELECT @milk=SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02';
SELECT @sugar=SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03';
SELECT @points=DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH004';
DECLARE @tt dbo.DanhSachThanhToan;
INSERT @tt(MaPT,SoTien) VALUES('PT001',30000),('PT002',60000);
EXEC dbo.sp_ThanhToanHoaDon 'ZPAYOK0001',@tt,'2025-04-01';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='ZPAYOK0001' AND TrangThai=N'Da thanh toan' AND ThanhTien=90000) THROW 52009,'Checkout header',1;
IF (SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='ZPAYOK0001')<>3 THROW 52009,'Grouped consumption count',1;
IF NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='ZPAYOK0001' AND MaNVL='NVL01' AND SoLuong=0.0540) THROW 52009,'Coffee consumption',1;
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01' AND SoLuongTon=@coffee-0.0540) THROW 52009,'Coffee stock',1;
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02' AND SoLuongTon=@milk-0.3600) THROW 52009,'Milk stock',1;
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03' AND SoLuongTon=@sugar-0.0450) THROW 52009,'Sugar stock',1;
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH='KH004' AND DiemTichLuy=@points+90) THROW 52009,'Points',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='ZPAYOK0001')<>2 THROW 52009,'Payment allocations',1;
IF (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='ZPAYOK0001')<>1 THROW 52009,'One point award',1;
-- CASE T02-K09-stock | Checkout with one insufficient ingredient rolls back all 35 tables | 547 | CHK_NVL_TonKho
-- SETUP
INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES('ZS001',N'T02 enough',N'Kg',10),('ZS002',N'T02 short',N'Kg',0.5);
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('ZS001',N'T02 stock test','DM001',10000);
INSERT dbo.CONGTHUC(MaSP,MaNVL,DinhMuc) VALUES('ZS001','ZS001',0.25),('ZS001','ZS002',1);
EXEC #T02_Order 'ZSHORT0001',@Product='ZS001';
-- ACT
EXEC #T02_Pay 'ZSHORT0001';
-- CASE T02-K09-method | Invalid method after consumption insertion rolls back stock and all rows | 547 | FK_CTTT_PHUONGTHUC
-- SETUP
EXEC #T02_Order 'ZMETHOD001';
-- ACT
EXEC #T02_Pay 'ZMETHOD001',@Method='ZZ999';
-- CASE T02-K10-overpay | Debt overpayment creates neither expense nor repayment history | 51111 |
-- SETUP
EXEC #T02_Debt 'ZDEBT00001';
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH00001','NV001','PT001',1001;
-- CASE T02-K10-partial | Partial debt payment creates exactly one linked expense and correct balance | 0 |
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH00002','NV001','PT001',400;
-- ASSERT
IF NOT EXISTS(SELECT 1 FROM dbo.vw_CongNoNCC WHERE MaCN='ZDEBT00001' AND TienNo=1000 AND DaTra=400 AND ConNo=600) THROW 52010,'Partial debt balance',1;
IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WHERE MaTC='ZCASH00002' AND MaDMTC='TC011' AND SoTien=400) THROW 52010,'One source expense',1;
IF (SELECT COUNT(*) FROM dbo.THANHTOAN_CONGNO WHERE MaCN='ZDEBT00001')<>1 THROW 52010,'One repayment row',1;
-- CASE T02-K10-debt-lock | Debt with payment history cannot be edited | 51013 |
-- ACT
UPDATE dbo.CONGNO_NCC SET TienNo=900 WHERE MaCN='ZDEBT00001';
-- CASE T02-K10-history-update | Repayment history cannot be updated | 51017 |
-- ACT
UPDATE dbo.THANHTOAN_CONGNO SET MaPT='PT002' WHERE MaCN='ZDEBT00001';
-- CASE T02-K10-history-delete | Repayment history cannot be deleted | 51017 |
-- ACT
DELETE dbo.THANHTOAN_CONGNO WHERE MaCN='ZDEBT00001';
-- CASE T02-K10-expense-update | Linked expense cannot be edited | 51020 |
-- ACT
UPDATE dbo.PHIEUTHUCHI SET SoTien=1 WHERE MaTC='ZCASH00002';
-- CASE T02-K10-expense-delete | Linked expense delete is blocked by FK | 547 | FK_TTCN_TC
-- ACT
DELETE dbo.PHIEUTHUCHI WHERE MaTC='ZCASH00002';
-- CASE T02-K11-success | Count adjusts two ingredients once and marks header finalized | 0 |
-- SETUP
EXEC #T02_Count 'ZCOUNT0001';
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0001';
-- ASSERT
IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUKIEMKE WHERE MaKK='ZCOUNT0001' AND DaDieuChinh=1) THROW 52011,'Count header',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_KK k JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=k.MaNVL WHERE k.MaKK='ZCOUNT0001' AND n.SoLuongTon<>k.TonThucTe) THROW 52011,'Count quantities',1;
-- CASE T02-K11-repeat | Already finalized count cannot be applied again | 51112 |
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0001';
-- CASE T02-K11-old | Past-day count is rejected | 51112 |
-- SETUP
EXEC #T02_Count 'ZCOUNT0002',@DayOffset=-1;
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0002';
-- CASE T02-K11-stale | One stale snapshot in multi-row count rolls back all ingredients | 51114 |
-- SETUP
EXEC #T02_Count 'ZCOUNT0003';
UPDATE dbo.CHITIET_KK SET TonHeThong=TonHeThong+1 WHERE MaKK='ZCOUNT0003' AND MaNVL='NVL02';
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0003';
-- CASE T02-K11-detail-update | Finalized count detail cannot be edited | 51115 |
-- ACT
UPDATE dbo.CHITIET_KK SET TonThucTe=TonThucTe+1 WHERE MaKK='ZCOUNT0001';
-- CASE T02-K11-detail-delete | Finalized count detail cannot be deleted | 51115 |
-- ACT
DELETE dbo.CHITIET_KK WHERE MaKK='ZCOUNT0001';
-- CASE T02-K12-pay-missing | Checkout missing invoice hits 51101 | 51101 |
-- ACT
EXEC #T02_Pay 'ZZMISSING1';
-- CASE T02-K12-pay-paid | Checkout paid invoice is not charged again | 51101 |
-- ACT
EXEC #T02_Pay 'ZPAYOK0001';
-- CASE T02-K12-pay-canceled | Checkout canceled invoice is refused | 51101 |
-- SETUP
EXEC #T02_Order 'ZCANCEL001';
EXEC dbo.sp_HuyHoaDon 'ZCANCEL001';
-- ACT
EXEC #T02_Pay 'ZCANCEL001';
-- CASE T02-K12-pay-empty | Empty invoice hits 51102 | 51102 |
-- SETUP
EXEC #T02_Order 'ZEMPTY0001',@Quantity=0;
-- ACT
EXEC #T02_Pay 'ZEMPTY0001';
-- CASE T02-K12-pay-existing | Invoice with an earlier allocation hits 51103 | 51103 |
-- SETUP
EXEC #T02_Order 'ZEARLY0001';
INSERT dbo.CHITIET_THANHTOAN(MaHD,MaPT,SoTien) VALUES('ZEARLY0001','PT001',1);
-- ACT
EXEC #T02_Pay 'ZEARLY0001';
-- CASE T02-K12-pay-short | Insufficient TVP allocation hits 51104 | 51104 |
-- SETUP
EXEC #T02_Order 'ZALLOC0001';
-- ACT
EXEC #T02_Pay 'ZALLOC0001',@Amount=29999;
-- CASE T02-K12-pay-over | Excess TVP allocation hits 51104 | 51104 |
-- ACT
EXEC #T02_Pay 'ZALLOC0001',@Amount=30001;
-- CASE T02-K12-pay-promotion | Expired promotion hits 51105 | 51105 |
-- SETUP
EXEC #T02_Order 'ZPROMO0001',@Promotion='KM24TET';
-- ACT
EXEC #T02_Pay 'ZPROMO0001';
-- CASE T02-K12-pay-stopped | Stopped product hits 51106 | 51106 |
-- SETUP
EXEC #T02_Order 'ZSTOP00001';
UPDATE dbo.SANPHAM SET TrangThai=N'Ngung ban' WHERE MaSP='SP002';
-- ACT
EXEC #T02_Pay 'ZSTOP00001';
-- CLEANUP
UPDATE dbo.SANPHAM SET TrangThai=N'Dang ban' WHERE MaSP='SP002';
-- CASE T02-K12-pay-recipe | Product without recipe hits 51107 | 51107 |
-- SETUP
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('ZR001',N'T02 no recipe','DM001',10000);
EXEC #T02_Order 'ZRECIPE001',@Product='ZR001';
-- ACT
EXEC #T02_Pay 'ZRECIPE001';
-- CASE T02-K12-pay-date | Payment before invoice date hits 51108 | 51108 |
-- SETUP
EXEC #T02_Order 'ZDATE00001';
-- ACT
EXEC #T02_Pay 'ZDATE00001',@Date='2025-03-31';
-- CASE T02-K12-pay-late-fault | Failure at loyalty stage reverses financial close stock payments and points | 52901 |
-- SETUP
EXEC #T02_Order 'ZFAULT0001';
GO
CREATE TRIGGER dbo.TR_T02_FailPoint ON dbo.LICHSU_DIEM AFTER INSERT AS
BEGIN
    IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='ZFAULT0001') THROW 52901,'T02 injected loyalty failure',1;
END;
-- ACT
EXEC #T02_Pay 'ZFAULT0001';
-- CLEANUP
DROP TRIGGER IF EXISTS dbo.TR_T02_FailPoint;
-- CASE T02-K12-cancel-missing | Cancel missing invoice hits 51109 | 51109 |
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZZMISSING1';
-- CASE T02-K12-cancel-paid | Paid invoice cannot be canceled | 51109 |
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZPAYOK0001';
-- CASE T02-K12-cancel-canceled | Already canceled invoice cannot be canceled twice | 51109 |
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZCANCEL001';
-- CASE T02-K12-cancel-allocation | Cancel with payment allocation hits financial trigger | 51006 |
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZEARLY0001';
-- CASE T02-K12-cancel-consumption | Cancel with earlier consumption is refused | 51006 |
-- SETUP
EXEC #T02_Order 'ZUSED00001';
INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong) VALUES('ZUSED00001','NVL01',0.01);
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZUSED00001';
-- CASE T02-K12-cancel-fault | Cancel header failure preserves original status | 52904 |
-- SETUP
EXEC #T02_Order 'ZFAULT0004';
GO
CREATE TRIGGER dbo.TR_T02_FailCancel ON dbo.HOADON AFTER UPDATE AS
BEGIN
    IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='ZFAULT0004' AND TrangThai=N'Huy') THROW 52904,'T02 injected cancel failure',1;
END;
-- ACT
EXEC dbo.sp_HuyHoaDon 'ZFAULT0004';
-- CLEANUP
DROP TRIGGER IF EXISTS dbo.TR_T02_FailCancel;
-- CASE T02-K12-cancel-success | Valid cancellation preserves stock and releases table | 0 |
-- SETUP
INSERT dbo.HOADON(MaHD,MaNV,MaBan,LoaiDon) VALUES('ZCANCEL002','NV004','B0001',N'Tai quan');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('ZCANCEL002','SP001',1,25000);
-- ACT
DECLARE @stock DECIMAL(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01');
EXEC dbo.sp_HuyHoaDon 'ZCANCEL002';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='ZCANCEL002' AND TrangThai=N'Huy') THROW 52012,'Cancel status',1;
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01' AND SoLuongTon=@stock) THROW 52012,'Cancel must not restore/deduct stock',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0001' AND TrangThai=N'Trong') THROW 52012,'Table not released',1;
-- CASE T02-K12-debt-missing | Missing debt hits 51110 | 51110 |
-- ACT
EXEC dbo.sp_TraCongNo 'ZZMISSING1','ZCASH10001','NV001','PT001',1;
-- CASE T02-K12-debt-zero | Zero repayment hits 51111 | 51111 |
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10002','NV001','PT001',0;
-- CASE T02-K12-debt-negative | Negative repayment hits 51111 | 51111 |
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10003','NV001','PT001',-1;
-- CASE T02-K12-debt-employee | Invalid employee reverses expense creation | 547 | FK_PTC_NHANVIEN
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10004','ZZ999','PT001',1;
-- CASE T02-K12-debt-method | Invalid method after expense creation reverses both writes | 547 | FK_TTCN_PT
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10005','NV001','ZZ999',1;
-- CASE T02-K12-debt-duplicate | Duplicate expense code rolls back repayment | 2627 | PK_PHIEUTHUCHI
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH00002','NV001','PT001',1;
-- CASE T02-K12-debt-missing-category | Missing TC011 prevents expense and history creation | 547 | FK_PTC_DANHMUC
-- SETUP
INSERT dbo.DANHMUC_THUCHI(MaDMTC,TenDMTC,Loai) VALUES('ZT011',N'T02 holding category',N'Chi');
DISABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE dbo.PHIEUTHUCHI SET MaDMTC='ZT011' WHERE MaDMTC='TC011';
ENABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
DELETE dbo.DANHMUC_THUCHI WHERE MaDMTC='TC011';
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10006','NV001','PT001',1;
-- CLEANUP
INSERT dbo.DANHMUC_THUCHI(MaDMTC,TenDMTC,Loai) VALUES('TC011',N'Tra no nha cung cap',N'Chi');
DISABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE dbo.PHIEUTHUCHI SET MaDMTC='TC011' WHERE MaDMTC='ZT011';
ENABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
-- CASE T02-K12-debt-wrong-category | Wrong TC011 type is rejected by repayment trigger | 51018 |
-- SETUP
-- Existing repayment rows reference TC011; detach their category only in this fixture.
DISABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE dbo.PHIEUTHUCHI SET MaDMTC='ZT011' WHERE MaDMTC='TC011';
ENABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Thu' WHERE MaDMTC='TC011';
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10007','NV001','PT001',1;
-- CLEANUP
UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Chi' WHERE MaDMTC='TC011';
DISABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE dbo.PHIEUTHUCHI SET MaDMTC='TC011' WHERE MaDMTC='ZT011';
ENABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
-- CASE T02-K12-debt-fault | Failure on repayment link reverses newly inserted expense | 52902 |
-- SETUP
GO
CREATE TRIGGER dbo.TR_T02_FailDebt ON dbo.THANHTOAN_CONGNO AFTER INSERT AS
BEGIN
    IF EXISTS(SELECT 1 FROM inserted WHERE MaTC='ZCASH10008') THROW 52902,'T02 injected debt failure',1;
END;
-- ACT
EXEC dbo.sp_TraCongNo 'ZDEBT00001','ZCASH10008','NV001','PT001',1;
-- CLEANUP
DROP TRIGGER IF EXISTS dbo.TR_T02_FailDebt;
-- CASE T02-K12-count-missing | Missing count hits 51112 | 51112 |
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZZMISSING1';
-- CASE T02-K12-count-future | Future-day count hits 51112 | 51112 |
-- SETUP
EXEC #T02_Count 'ZCOUNT0004',@DayOffset=1;
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0004';
-- CASE T02-K12-count-empty | Empty current count hits 51113 | 51113 |
-- SETUP
INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV) VALUES('ZCOUNT0005',CONVERT(DATE,SYSDATETIME()),'NV001');
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0005';
-- CASE T02-K12-count-fault | Failure marking count finalized reverses prior stock updates | 52903 |
-- SETUP
EXEC #T02_Count 'ZCOUNT0006';
GO
CREATE TRIGGER dbo.TR_T02_FailCount ON dbo.PHIEUKIEMKE AFTER UPDATE AS
BEGIN
    IF EXISTS(SELECT 1 FROM inserted WHERE MaKK='ZCOUNT0006' AND DaDieuChinh=1) THROW 52903,'T02 injected count failure',1;
END;
-- ACT
EXEC dbo.sp_ChotKiemKe 'ZCOUNT0006';
-- CLEANUP
DROP TRIGGER IF EXISTS dbo.TR_T02_FailCount;
