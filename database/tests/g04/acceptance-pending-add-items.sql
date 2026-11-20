-- Đợt kiểm thử "In tạm và nối thêm món" (20/11/2026, requirements-cho-thanh-toan-phien-ban.md §9).
-- Chỉ chạy trên DB test thuộc sở hữu G04; không thay thế toàn bộ checklist T33-T35.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @a int,@s binary(8),@id uniqueidentifier,@code char(10),@rev binary(8),@stale binary(8);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
DECLARE @payments dbo.DanhSachThanhToan;

-- NA01: In tạm từ Chưa thanh toán — sang Chờ, có receipt, chưa thu tiền/chưa ghi tiêu hao.
INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
SET @id=NEWID(); EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Chua thanh toan') THROW 52920,'NA01 create mismatch',1;
SELECT @stale=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @a,@s,@code,@stale,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Cho thanh toan' AND NgayThanhToan IS NULL AND Revision<>@stale)
 THROW 52920,'NA01 status/revision mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesRequestPay' AND EntityCode=@code AND ActorAccountId=@a)
 THROW 52920,'NA01 receipt missing',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code)
 THROW 52920,'NA01 money/consumption leaked',1;

-- NA02: thêm món khác sau In tạm — cùng MaHD, thêm dòng, tổng tăng, vẫn Chờ, có receipt.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @totalBefore decimal(18,0)=(SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code);
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP002',@Quantity=1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 THROW 52920,'NA02 add in Cho missing',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Cho thanh toan') THROW 52920,'NA02 status changed',1;
IF (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<=@totalBefore THROW 52920,'NA02 total not increased',1;
IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesLine' AND EntityCode=@code)
 THROW 52920,'NA02 receipt missing',1;

-- NA03: thêm lại món cũ bằng tăng số lượng — gộp một dòng SL2.
DECLARE @line1 int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code AND MaSP='SP001');
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@line1,@Quantity=2;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code AND MaSP='SP001')<>1 THROW 52920,'NA03 duplicate line',1;
IF (SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=@line1)<>2 THROW 52920,'NA03 quantity not merged',1;

-- NA04: replay cùng RequestId không nhân đôi dòng/revision.
DECLARE @revMerged binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@code),@totalMerged decimal(18,0)=(SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code);
EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@line1,@Quantity=2;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 OR (SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=@line1)<>2
 OR (SELECT Revision FROM dbo.HOADON WHERE MaHD=@code)<>@revMerged OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<>@totalMerged
 THROW 52920,'NA04 replay changed state',1;

-- NA05: revision cũ (hai phiên) bị từ chối 52103.
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@stale,@RequestId=@id,@Action='Add',@Product='SP002',@Quantity=1;
 THROW 52920,'NA05 stale revision accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52103 THROW; END CATCH;

-- NA06: giảm SL, xóa dòng, đổi ghi chú khi Chờ — từ chối 52110, không ghi một phần.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@line1,@Quantity=1;
 THROW 52920,'NA06 decrease accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Delete',@LineId=@line1;
 THROW 52920,'NA06 delete accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@line1,@Quantity=2,@Note=N'it da';
 THROW 52920,'NA06 note edit accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 OR (SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=@line1)<>2
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaDong=@line1 AND GhiChu IS NOT NULL)
 THROW 52920,'NA06 blocked edit leaked',1;

-- NA07: trigger chặn thẳng bảng khi Chờ (phòng thủ ngoài procedure), vẫn đúng 52110.
BEGIN TRY UPDATE dbo.CHITIET_HD SET SoLuong=1 WHERE MaDong=@line1; THROW 52920,'NA07 direct decrease accepted',1; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY UPDATE dbo.CHITIET_HD SET GhiChu=N'x' WHERE MaDong=@line1; THROW 52920,'NA07 direct note accepted',1; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY DELETE dbo.CHITIET_HD WHERE MaDong=@line1; THROW 52920,'NA07 direct delete accepted',1; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;

-- NA08: F3 sau In tạm — khách/ưu đãi vẫn sửa, giảm giá tính lại trên toàn đơn (KH001 hạng BKIM 10%).
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @totalF3 decimal(18,0)=(SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code);
SET @id=NEWID(); EXEC dbo.sp_SuaThongTinHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Customer='KH001';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND MaKH='KH001') THROW 52920,'NA08 header edit missing',1;
IF (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<>@totalF3 THROW 52920,'NA08 header edit changed total',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON h CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p
              WHERE h.MaHD=@code AND h.GiamGia=p.Discount AND p.PercentDiscount=10 AND p.Discount>0)
 THROW 52920,'NA08 discount not recalculated over whole order',1;

-- NA09: In lại chỉ đọc — không đổi trạng thái, tổng, revision.
DECLARE @revF3 binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@code);
EXEC dbo.sp_InvoiceDetail @Actor=@a,@Stamp=@s,@Code=@code;
IF (SELECT Revision FROM dbo.HOADON WHERE MaHD=@code)<>@revF3
 OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<>@totalF3
 OR (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@code)<>N'Cho thanh toan'
 THROW 52920,'NA09 reprint changed state',1;

-- NA10: thu tiền sau khi nối món — đủ tiền, tiêu hao và điểm theo toàn đơn.
DECLARE @pointsBefore int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001');
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revF3;
DECLARE @token binary(32),@payable decimal(38,0);
SELECT @token=PreviewToken,@payable=Payable FROM @preview;
IF @payable<>CONVERT(decimal(38,0),(SELECT ThanhTien FROM dbo.HOADON WHERE MaHD=@code)) THROW 52920,'NA10 preview payable mismatch',1;
INSERT @payments VALUES('PT001',@payable,NULL);
SET @id=NEWID(); EXEC dbo.sp_ThanhToanHoaDon @a,@s,@code,@revF3,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Da thanh toan' AND NgayThanhToan IS NOT NULL)
 THROW 52920,'NA10 checkout failed',1;
IF (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code)<>@payable THROW 52920,'NA10 payment sum mismatch',1;
IF (SELECT COUNT(DISTINCT MaNVL) FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code)<2 THROW 52920,'NA10 consumption missing for added items',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code AND Kind='Checkout') THROW 52920,'NA10 snapshot missing',1;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<=@pointsBefore THROW 52920,'NA10 points missing',1;

-- NA11: đơn đã thu vẫn khóa sửa dòng ở procedure và trigger.
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revF3,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
 THROW 52920,'NA11 paid add accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52102 THROW; END CATCH;
BEGIN TRY UPDATE dbo.CHITIET_HD SET SoLuong=3 WHERE MaDong=@line1; THROW 52920,'NA11 direct paid update accepted',1; END TRY BEGIN CATCH IF ERROR_NUMBER()<>51001 THROW; END CATCH;
BEGIN TRY DELETE dbo.CHITIET_HD WHERE MaDong=@line1; THROW 52920,'NA11 direct paid delete accepted',1; END TRY BEGIN CATCH IF ERROR_NUMBER()<>51001 THROW; END CATCH;

-- NA12: hủy đơn Chờ đã nối món — về Huy, không phát sinh tiền/tiêu hao.
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
SET @id=NEWID(); EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @a,@s,@code,@rev,@id;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP002',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_HuyHoaDon @a,@s,@code,@rev,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Huy') THROW 52920,'NA12 cancel failed',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code)
 THROW 52920,'NA12 cancel leaked money/consumption',1;

IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52920,'Transaction not clean',1;
