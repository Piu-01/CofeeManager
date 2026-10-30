-- CASE T14-K01 three channels
EXEC dbo.G04New 'HD90000001'; EXEC dbo.G04New 'HD90000002',N'Tai quan',@table='B009'; EXEC dbo.G04New 'HD90000003',N'Giao hang',@ship=15000;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD BETWEEN 'HD90000001' AND 'HD90000003')<>3 THROW 52990,'Missing channel',1;
DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD90000002'; EXEC dbo.sp_HuyHoaDon @a,@s,'HD90000002',@r,@id;
-- CASE T14-K01 invalid header rollback
DECLARE @before nvarchar(max),@after nvarchar(max); EXEC dbo.G04State @before OUTPUT;
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD90000004'',N''Tai quan'';',52101;
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD90000004'',@table=''B009'';',52101;
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD90000004'',@ship=1;',52101;
EXEC dbo.G04State @after OUTPUT; IF @before<>@after THROW 52990,'Invalid header changed database',1;
-- CASE T14-K02 missing customer FK
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD90000004'',@customer=''ZZZZZ'';',52105;
IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000004') THROW 52990,'Orphan',1;
-- CASE T14-K04 foreign line
DECLARE @l int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD='HD90000003'),@sql nvarchar(max);
SET @sql=N'EXEC dbo.G04Edit ''HD90000001'',''Delete'',@line='+CONVERT(varchar,@l); EXEC dbo.G04Expect @sql,52101;
-- CASE T14-K05 price snapshot and quantity bounds
UPDATE dbo.SANPHAM SET GiaBan=30000 WHERE MaSP='SP001';
DECLARE @l int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD='HD90000001'); EXEC dbo.G04Edit 'HD90000001','Update',2,@l;
EXEC dbo.G04Edit 'HD90000001';
IF NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD='HD90000001' AND MaDong=@l AND GiaBan=25000 AND SoLuong=2) OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD='HD90000001' AND MaDong<>@l AND GiaBan=30000) THROW 52990,'Price snapshot',1;
EXEC dbo.G04Expect N'EXEC dbo.G04Edit ''HD90000001'',@qty=-1;',52101;
EXEC dbo.G04Expect N'EXEC dbo.G04Edit ''HD90000001'',@qty=2147483648;',52101;
UPDATE dbo.SANPHAM SET GiaBan=25000 WHERE MaSP='SP001';
-- CASE T14-K06 reduce 99 percent discount then delete
EXEC dbo.G04New 'HD90000005',@promotion='KM90000002',@qty=2;
DECLARE @l int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD='HD90000005'); EXEC dbo.G04Edit 'HD90000005','Update',1,@l;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000005' AND TongTien=25000 AND GiamGia=24750) THROW 52990,'Discount reduction',1;
EXEC dbo.G04Edit 'HD90000005','Delete',@line=@l;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000005' AND TongTien=0 AND GiamGia=0 AND TrangThai=N'Chua thanh toan') THROW 52990,'Empty discount',1;
-- CASE T14-K07 stopped product allows reduce/delete only
EXEC dbo.G04New 'HD90000006',@qty=2; UPDATE dbo.SANPHAM SET TrangThai=N'Ngung ban' WHERE MaSP='SP001';
EXEC dbo.G04Expect N'EXEC dbo.G04Edit ''HD90000006'';',52105;
DECLARE @l int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD='HD90000006'); EXEC dbo.G04Edit 'HD90000006','Update',1,@l;
DECLARE @sql nvarchar(max)=N'EXEC dbo.G04Edit ''HD90000006'',''Update'',2,'+CONVERT(varchar,@l); EXEC dbo.G04Expect @sql,52105;
EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000006'';',52204;
EXEC dbo.G04Edit 'HD90000006','Delete',@line=@l; UPDATE dbo.SANPHAM SET TrangThai=N'Dang ban' WHERE MaSP='SP001';
-- CASE T14-K08 paid and canceled immutable
EXEC dbo.G04Pay 'HD90000001';
DECLARE @before nvarchar(max),@after nvarchar(max); EXEC dbo.G04State @before OUTPUT;
EXEC dbo.G04Expect N'EXEC dbo.G04Edit ''HD90000001'';',52102; EXEC dbo.G04Expect N'EXEC dbo.G04Edit ''HD90000002'';',52102;
EXEC dbo.G04State @after OUTPUT; IF @before<>@after THROW 52990,'Closed edit changed state',1;
-- CASE T14-K12 empty checkout blocked then cancel
EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000005'';',51102;
DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD90000005'; EXEC dbo.sp_HuyHoaDon @a,@s,'HD90000005',@r,@id;
IF EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD90000005') THROW 52990,'Cancel empty consumed',1;
-- CASE T15-K02 stale preview when promotion same amount changes
EXEC dbo.G04New 'HD90000007',@promotion='KM90000002';
DECLARE @t binary(32)=(SELECT PreviewToken FROM dbo.fn_CheckoutPreview('HD90000007')); UPDATE dbo.KHUYENMAI SET TenKM=N'Revised same percent' WHERE MaKM='KM90000002';
DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(),@pay dbo.DanhSachThanhToan;
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD90000007'; INSERT @pay VALUES('PT001',250,NULL);
DECLARE @seen bit=0; BEGIN TRY EXEC dbo.sp_ThanhToanHoaDon @a,@s,'HD90000007',@r,@id,@t,@pay; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52201 THROW; SET @seen=1; END CATCH;
IF @seen=0 OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD90000007') THROW 52990,'Stale token accepted',1;
-- CASE T15-K03 allocation mismatch and unknown methods
EXEC dbo.G04New 'HD90000008'; DECLARE @before nvarchar(max),@after nvarchar(max); EXEC dbo.G04State @before OUTPUT;
EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000008'',@delta=1;',52202; EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000008'',@delta=-1;',52202;
EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000008'',@method=''XXXXX'';',52202; EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000008'',@method=''PT010'';',52202;
EXEC dbo.G04State @after OUTPUT; IF @before<>@after THROW 52990,'Rejected allocation changed state',1;
-- CASE T15-K04 zero bill consumes no payment
EXEC dbo.G04New 'HD90000009',@promotion='KM90000001'; EXEC dbo.G04Pay 'HD90000009';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000009' AND ThanhTien=0 AND TrangThai=N'Da thanh toan') OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD90000009') OR NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD90000009') THROW 52990,'Zero bill',1;
-- CASE T15-K05 full discount still pays ship no points
EXEC dbo.G04New 'HD90000010',N'Giao hang',@customer='KH001',@promotion='KM90000001',@ship=15000; EXEC dbo.G04Pay 'HD90000010';
IF (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD90000010')<>15000 OR EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaHD='HD90000010') THROW 52990,'Ship points mismatch',1;
-- CASE T15-K07 grouped decimal consumption
EXEC dbo.G04New 'HD90000011'; EXEC dbo.G04Edit 'HD90000011',@qty=2;
SELECT MaNVL,SoLuongTon BeforeStock INTO #stock FROM dbo.NGUYENVATLIEU;
EXEC dbo.G04Pay 'HD90000011';
IF EXISTS(SELECT 1 FROM dbo.CONGTHUC r JOIN dbo.TIEUHAO_HOADON x ON x.MaNVL=r.MaNVL AND x.MaHD='HD90000011' JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=r.MaNVL JOIN #stock b ON b.MaNVL=n.MaNVL WHERE r.MaSP='SP001' AND (x.SoLuong<>r.DinhMuc*3 OR n.SoLuongTon<>b.BeforeStock-x.SoLuong)) OR (SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD90000011')<>(SELECT COUNT(*) FROM dbo.CONGTHUC WHERE MaSP='SP001') THROW 52990,'Group consumption',1;
DROP TABLE #stock;
-- CASE T15-K08 points floor net excludes ship
DECLARE @p decimal(18,0)=999,@i int=12;
WHILE @i<=14 BEGIN
 UPDATE dbo.SANPHAM SET GiaBan=@p WHERE MaSP='SP001'; DECLARE @code varchar(10)='HD900000'+CONVERT(varchar,@i); EXEC dbo.G04New @code,N'Giao hang',@customer='KH001',@ship=15000; EXEC dbo.G04Pay @code;
 IF COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@code),0)<>FLOOR(@p/1000) THROW 52990,'Points floor',1;
 SELECT @i=@i+1,@p=CASE WHEN @p=999 THEN 1000 ELSE 1999 END;
END;
UPDATE dbo.SANPHAM SET GiaBan=25000 WHERE MaSP='SP001';
-- CASE T15-K09 partner fee 0 100 rounding and immutable
DECLARE @partner varchar(5)=(SELECT MIN(MaDT) FROM dbo.DOITACGIAOHANG),@original decimal(5,2); SELECT @original=ChietKhau FROM dbo.DOITACGIAOHANG WHERE MaDT=@partner;
UPDATE dbo.SANPHAM SET GiaBan=25001 WHERE MaSP='SP001'; UPDATE dbo.DOITACGIAOHANG SET ChietKhau=50 WHERE MaDT=@partner;
EXEC dbo.G04New 'HD90000015',N'Giao hang',@partner=@partner,@ship=15000; EXEC dbo.G04Pay 'HD90000015';
UPDATE dbo.DOITACGIAOHANG SET ChietKhau=0 WHERE MaDT=@partner; EXEC dbo.G04New 'HD90000016',N'Giao hang',@partner=@partner; EXEC dbo.G04Pay 'HD90000016';
UPDATE dbo.DOITACGIAOHANG SET ChietKhau=100 WHERE MaDT=@partner; EXEC dbo.G04New 'HD90000017',N'Giao hang',@partner=@partner; EXEC dbo.G04Pay 'HD90000017';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000015' AND PhiDoiTac=12501 AND ThanhTien=40001 AND PhanTramPhiDoiTac=50) OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000016' AND PhiDoiTac=0) OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD90000017' AND PhiDoiTac=25001) THROW 52990,'Partner snapshot',1;
UPDATE dbo.DOITACGIAOHANG SET ChietKhau=@original WHERE MaDT=@partner; UPDATE dbo.SANPHAM SET GiaBan=25000 WHERE MaSP='SP001';
-- CASE T18-K02 paid delivery moves without finance
DECLARE @before nvarchar(max)=(SELECT TongTien,GiamGia,PhiShip,PhiDoiTac,NgayThanhToan FROM dbo.HOADON WHERE MaHD='HD90000010' FOR JSON PATH),@a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='HD90000010';
EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD90000010',@r,N'Cho giao',N'Dang giao',@id;
IF @before<>(SELECT TongTien,GiamGia,PhiShip,PhiDoiTac,NgayThanhToan FROM dbo.HOADON WHERE MaHD='HD90000010' FOR JSON PATH) THROW 52990,'Delivery changed finance',1;
DECLARE @seen bit=0; SET @id=NEWID(); BEGIN TRY EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD90000010',@r,N'Cho giao',N'Dang giao',@id; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52103 THROW; SET @seen=1; END CATCH;
IF @seen=0 THROW 52990,'Stale delivery accepted',1;
-- CASE T15-K14 future creation rejects checkout
EXEC dbo.G04New 'HD90000018'; UPDATE dbo.HOADON SET NgayLap=DATEADD(day,1,SYSDATETIME()) WHERE MaHD='HD90000018';
EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD90000018'';',51108;
-- CASE T15-K13 runtime DML denied
CREATE USER G04Runtime WITHOUT LOGIN; ALTER ROLE CafeManagerApp ADD MEMBER G04Runtime;
DECLARE @sql nvarchar(max),@table sysname;
DECLARE t CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE name IN('HOADON','CHITIET_HD','TIEUHAO_HOADON','CHITIET_THANHTOAN','LICHSU_DIEM','HoaDonUuDaiSnapshot');
OPEN t; FETCH NEXT FROM t INTO @table;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'EXECUTE AS USER=''G04Runtime''; BEGIN TRY DELETE FROM dbo.'+QUOTENAME(@table)+N' WHERE 1=0; REVERT; THROW 52990,''Runtime DML accepted'',1; END TRY BEGIN CATCH DECLARE @e int=ERROR_NUMBER(); REVERT; IF @e<>229 THROW; END CATCH;';
 EXEC sys.sp_executesql @sql; FETCH NEXT FROM t INTO @table;
END; CLOSE t; DEALLOCATE t;
-- CASE T15-K13 role STORE denied
DECLARE @old varchar(5),@a int,@s binary(8),@seen bit=0; SELECT @a=MaTK,@s=SecurityStamp,@old=MaVT FROM dbo.TAIKHOAN WHERE MaNV='NV001';
UPDATE dbo.TAIKHOAN SET MaVT='STORE' WHERE MaTK=@a; SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@a;
BEGIN TRY EXEC dbo.sp_SalesMenu @a,@s; END TRY BEGIN CATCH IF ERROR_NUMBER()<>51605 THROW; SET @seen=1; END CATCH;
UPDATE dbo.TAIKHOAN SET MaVT=@old WHERE MaTK=@a; IF @seen=0 THROW 52990,'STORE could access sales',1;
-- CASE T15-K15 legacy snapshot unknown amount preserved
IF EXISTS(SELECT 1 FROM dbo.HOADON h LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD WHERE h.MaHD NOT LIKE 'HD9%' AND h.TrangThai=N'Da thanh toan' AND (s.Kind<>'LegacyUnknown' OR s.Source IS NOT NULL OR s.PercentDiscount IS NOT NULL OR s.Discount<>h.GiamGia)) THROW 52990,'Legacy snapshot fabricated',1;
