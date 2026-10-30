-- CASE T14-K03 observe zero header inside atomic create
CREATE TABLE dbo.G04HeaderAudit(Code char(10),InitialTotal decimal(18,0),InitialDiscount decimal(18,0),TranCount int);
EXEC(N'CREATE TRIGGER dbo.G04ObserveHeader ON dbo.HOADON AFTER INSERT AS BEGIN INSERT dbo.G04HeaderAudit SELECT MaHD,TongTien,GiamGia,@@TRANCOUNT FROM inserted WHERE MaHD=''HD93000001''; END;');
EXEC dbo.G04New 'HD93000001';
IF (SELECT COUNT(*) FROM dbo.G04HeaderAudit WHERE Code='HD93000001' AND InitialTotal=0 AND InitialDiscount=0 AND TranCount>0)<>1 OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD93000001' AND TongTien=25000) THROW 52990,'Initial header or trigger sum mismatch',1;
DROP TRIGGER dbo.G04ObserveHeader;
-- CASE T15-K02 policy revision changed with same amount requires re-confirm
EXEC dbo.G04New 'HD93000002'; DECLARE @token binary(32)=(SELECT PreviewToken FROM dbo.fn_CheckoutPreview('HD93000002')),@b nvarchar(max),@e nvarchar(max),@seen bit=0;
UPDATE dbo.LoyaltyPolicyState SET Id=Id;
EXEC dbo.G04State @b OUTPUT;
BEGIN TRY EXEC dbo.G04Pay 'HD93000002',@token=@token; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52201 THROW; SET @seen=1; END CATCH;
EXEC dbo.G04State @e OUTPUT; IF @seen=0 OR @b<>@e OR @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52990,'Policy token not enforced',1;
-- CASE T15-K02 customer revision changed requires re-confirm
EXEC dbo.G04New 'HD93000003',@customer='KH001'; DECLARE @token binary(32)=(SELECT PreviewToken FROM dbo.fn_CheckoutPreview('HD93000003')),@b nvarchar(max),@e nvarchar(max),@seen bit=0;
UPDATE dbo.KHACHHANG SET TenKH=TenKH WHERE MaKH='KH001'; EXEC dbo.G04State @b OUTPUT;
BEGIN TRY EXEC dbo.G04Pay 'HD93000003',@token=@token; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52201 THROW; SET @seen=1; END CATCH;
EXEC dbo.G04State @e OUTPUT; IF @seen=0 OR @b<>@e THROW 52990,'Customer token not enforced',1;
-- CASE T15-K02 partner revision changed requires re-confirm
DECLARE @partner varchar(5)=(SELECT MIN(MaDT) FROM dbo.DOITACGIAOHANG); EXEC dbo.G04New 'HD93000004',N'Giao hang',@partner=@partner;
DECLARE @token binary(32)=(SELECT PreviewToken FROM dbo.fn_CheckoutPreview('HD93000004')),@b nvarchar(max),@e nvarchar(max),@seen bit=0;
UPDATE dbo.DOITACGIAOHANG SET ChietKhau=ChietKhau WHERE MaDT=@partner; EXEC dbo.G04State @b OUTPUT;
BEGIN TRY EXEC dbo.G04Pay 'HD93000004',@token=@token; END TRY BEGIN CATCH IF ERROR_NUMBER()<>52201 THROW; SET @seen=1; END CATCH;
EXEC dbo.G04State @e OUTPUT; IF @seen=0 OR @b<>@e THROW 52990,'Partner token not enforced',1;
-- CASE T15-K07 multiple products shared materials decimal4 snapshot
DECLARE @n1 char(5)=(SELECT MIN(MaNVL) FROM dbo.NGUYENVATLIEU),@n2 char(5); SELECT @n2=MIN(MaNVL) FROM dbo.NGUYENVATLIEU WHERE MaNVL>@n1;
DELETE dbo.CONGTHUC WHERE MaSP IN('SP001','SP002'); INSERT dbo.CONGTHUC(MaSP,MaNVL,DinhMuc) VALUES('SP001',@n1,0.0180),('SP001',@n2,1.2345),('SP002',@n1,1.2345),('SP002',@n2,1);
UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=10000 WHERE MaNVL IN(@n1,@n2); UPDATE dbo.SANPHAM SET TrangThai=N'Dang ban' WHERE MaSP='SP002';
EXEC dbo.G04New 'HD93000005'; EXEC dbo.G04Edit 'HD93000005',@qty=2; EXEC dbo.G04Edit 'HD93000005',@product='SP002'; EXEC dbo.G04Pay 'HD93000005';
IF (SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD93000005')<>2 OR NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD93000005' AND MaNVL=@n1 AND SoLuong=1.2885) OR NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD93000005' AND MaNVL=@n2 AND SoLuong=4.7035) THROW 52990,'Decimal consumption mismatch',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL=@n1)<>9998.7115 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL=@n2)<>9995.2965 THROW 52990,'Decimal stock mismatch',1;
UPDATE dbo.CONGTHUC SET DinhMuc=10 WHERE MaSP='SP001'; IF (SELECT SUM(SoLuong) FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD93000005')<>5.9920 THROW 52990,'Recipe edit changed snapshot',1;
-- CASE T15-K08 customer points overflow rolls back
DECLARE @old int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001'); UPDATE dbo.KHACHHANG SET DiemTichLuy=2147483647 WHERE MaKH='KH001';
EXEC dbo.G04New 'HD93000006',@customer='KH001',@promotion='KM90000003';
DECLARE @b nvarchar(max),@e nvarchar(max); EXEC dbo.G04State @b OUTPUT; EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD93000006'';',52205; EXEC dbo.G04State @e OUTPUT; IF @b<>@e THROW 52990,'Points overflow leaked',1;
UPDATE dbo.KHACHHANG SET DiemTichLuy=@old WHERE MaKH='KH001';
-- CASE T18-K06 ADMIN CASH allowed STORE forbidden at SQL boundary
DECLARE @a int,@s binary(8),@role varchar(5),@i int=0,@code varchar(10),@r binary(8),@id uniqueidentifier,@seen bit;
SELECT @a=MaTK,@role=MaVT FROM dbo.TAIKHOAN WHERE MaNV='NV001';
WHILE @i<3 BEGIN
 UPDATE dbo.TAIKHOAN SET MaVT='ADMIN' WHERE MaTK=@a; SET @code='HD930000'+CONVERT(varchar,10+@i); EXEC dbo.G04New @code,N'Giao hang';
 UPDATE dbo.TAIKHOAN SET MaVT=CASE @i WHEN 0 THEN 'ADMIN' WHEN 1 THEN 'CASH' ELSE 'STORE' END WHERE MaTK=@a;
 SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@a; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code; SELECT @id=NEWID(),@seen=0;
 BEGIN TRY EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,@code,@r,N'Cho giao',N'Dang giao',@id; END TRY BEGIN CATCH IF @i<>2 OR ERROR_NUMBER()<>51605 THROW; SET @seen=1; END CATCH;
 IF (@i<2 AND NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThaiGiao=N'Dang giao')) OR (@i=2 AND (@seen=0 OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id))) THROW 52990,'Delivery role boundary',1;
 SET @i+=1;
END; UPDATE dbo.TAIKHOAN SET MaVT=@role WHERE MaTK=@a;
