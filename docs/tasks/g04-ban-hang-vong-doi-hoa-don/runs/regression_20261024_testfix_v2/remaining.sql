-- CASE T17 fixture dates channels and pages
DECLARE @today datetime2(0)=CONVERT(date,SYSDATETIME());
EXEC dbo.G04New 'HD97100001'; EXEC dbo.G04New 'HD97100002',@customer='KH001'; EXEC dbo.G04New 'HD97100003';
UPDATE dbo.HOADON SET NgayLap=DATEADD(second,86399,@today) WHERE MaHD='HD97100001';
UPDATE dbo.HOADON SET NgayLap=DATEADD(hour,-12,@today) WHERE MaHD='HD97100002';
EXEC dbo.G04Pay 'HD97100002';
UPDATE dbo.HOADON SET NgayLap=DATEADD(hour,12,@today) WHERE MaHD='HD97100003';
DECLARE @i int=1,@code varchar(10); WHILE @i<=51 BEGIN SET @code='HD98'+RIGHT('000000'+CONVERT(varchar,@i),6); EXEC dbo.G04New @code; UPDATE dbo.HOADON SET NgayLap=DATEADD(hour,12,@today) WHERE MaHD=@code; SET @i+=1; END;
-- CASE T17-K02 stored money unchanged after price and promotion edits
DECLARE @before nvarchar(max)=(SELECT TongTien,GiamGia,ThanhTien FROM dbo.HOADON WHERE MaHD='HD97100002' FOR JSON PATH);
UPDATE dbo.SANPHAM SET GiaBan=30000 WHERE MaSP='SP001'; UPDATE dbo.KHUYENMAI SET PhanTramGiam=50 WHERE MaKM='KM90000001';
IF @before<>(SELECT TongTien,GiamGia,ThanhTien FROM dbo.HOADON WHERE MaHD='HD97100002' FOR JSON PATH) OR (SELECT GiaBan FROM dbo.CHITIET_HD WHERE MaHD='HD97100002')<>25000 THROW 52990,'Historical money changed',1;
UPDATE dbo.SANPHAM SET GiaBan=25000 WHERE MaSP='SP001';
-- CASE T18-K01 non-delivery access rejected
DECLARE @a int,@s binary(8),@seen bit=0; SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
BEGIN TRY EXEC dbo.sp_DeliveryGet @a,@s,'HD97000001'; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52501 THROW; SET @seen=1; END CATCH;
IF @seen=0 THROW 52990,'Non delivery accepted',1;
-- CASE T18-K02 K03 paid next steps preserve all financial rows
EXEC dbo.G04New 'HD97000004',N'Giao hang'; EXEC dbo.G04Pay 'HD97000004';
DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(),@seen bit=0; SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD97000004';
BEGIN TRY EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD97000004',@r,N'Cho giao',N'Da giao',@id; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52502 THROW; SET @seen=1; END CATCH; IF @seen=0 THROW 52990,'Skipped step',1;
SET @id=NEWID(); EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD97000004',@r,N'Cho giao',N'Dang giao',@id; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD97000004'; SET @id=NEWID(); EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD97000004',@r,N'Dang giao',N'Da giao',@id;
IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD97000004')<>1 THROW 52990,'Delivery duplicated finance',1;
-- CASE T18-K04 open delivery cancellation no refund paid cancellation refused
EXEC dbo.G04New 'HD97000006',N'Giao hang';
DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD97000006'; EXEC dbo.sp_HuyHoaDon @a,@s,'HD97000006',@r,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD97000006' AND TrangThai=N'Huy' AND TrangThaiGiao=N'Huy giao') OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD97000006') THROW 52990,'Cancel delivery',1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD97000004'; SET @id=NEWID(); DECLARE @seen bit=0; BEGIN TRY EXEC dbo.sp_HuyHoaDon @a,@s,'HD97000004',@r,@id; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52102 THROW; SET @seen=1; END CATCH; IF @seen=0 THROW 52990,'Paid canceled',1;
-- CASE Investigation points capture actual stored net independently
UPDATE dbo.SANPHAM SET GiaBan=1000 WHERE MaSP='SP001'; EXEC dbo.G04New 'HD97000005',N'Giao hang',@customer='KH001',@ship=15000; EXEC dbo.G04Pay 'HD97000005';
IF COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD='HD97000005'),0)<>(SELECT FLOOR((TongTien-GiamGia)/1000) FROM dbo.HOADON WHERE MaHD='HD97000005') THROW 52990,'Actual net points defect',1;
UPDATE dbo.SANPHAM SET GiaBan=25000 WHERE MaSP='SP001';
-- CASE T15-K06 insufficient stock rollback all tables
EXEC dbo.G04New 'HD97000007',@customer='KH001';
UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=0 WHERE MaNVL=(SELECT MIN(MaNVL) FROM dbo.CONGTHUC WHERE MaSP='SP001');
DECLARE @b nvarchar(max),@e nvarchar(max); EXEC dbo.G04State @b OUTPUT; EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD97000007'';',52203; EXEC dbo.G04State @e OUTPUT; IF @b<>@e THROW 52990,'Stock error rollback changed state',1;
