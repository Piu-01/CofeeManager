UPDATE dbo.TAIKHOAN SET HoatDong=1;
INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('KM90000001',N'Fixture 100%','20260101','20271231',100),('KM90000002',N'Fixture 99%','20260101','20271231',99);
INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('KM90000003',N'Fixture net không giảm','20260101','20271231',0);
INSERT dbo.BAN(MaBan,TenBan,MaKV) SELECT 'B009',N'Fixture race',MIN(MaKV) FROM dbo.KHUVUC;
GO
CREATE PROCEDURE dbo.G04New @code varchar(10),@channel nvarchar(20)=N'Mang di',@table varchar(5)=NULL,@customer varchar(5)=NULL,@promotion varchar(10)=NULL,@ship decimal(18,0)=0,@partner varchar(5)=NULL,@qty int=1 AS
BEGIN
 DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@n nvarchar(100),@p varchar(15),@ad nvarchar(250);
 SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
 IF @channel=N'Giao hang' SELECT @n=N'Khách test',@p='0901234567',@ad=N'Địa chỉ test';
 EXEC dbo.sp_TaoHoaDon @a,@s,@code,@id,@channel,@table,@customer,@promotion,@partner,@n,@p,@ad,@ship,'SP001',@qty,N'Ghi chú tiếng Việt';
END;
GO
CREATE PROCEDURE dbo.G04Pay @code varchar(10),@method varchar(5)='PT001',@delta int=0,@token binary(32)=NULL,@request uniqueidentifier=NULL,@revision binary(8)=NULL AS
BEGIN
 DECLARE @a int,@s binary(8),@r binary(8),@t binary(32),@amount decimal(18,0),@pay dbo.DanhSachThanhToan;
 SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
 SELECT @r=COALESCE(@revision,h.Revision),@t=COALESCE(@token,p.PreviewToken),@amount=p.Payable FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p WHERE h.MaHD=@code;
 IF @amount+@delta<>0 INSERT @pay VALUES(@method,@amount+@delta,NULL);
 SET @request=COALESCE(@request,NEWID());
 EXEC dbo.sp_ThanhToanHoaDon @a,@s,@code,@r,@request,@t,@pay;
END;
GO
CREATE PROCEDURE dbo.G04Edit @code varchar(10),@action varchar(10)='Add',@qty bigint=1,@line int=NULL,@product varchar(5)='SP001' AS
BEGIN
 DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID();
 SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
 EXEC dbo.sp_SuaDongHoaDon @a,@s,@code,@r,@id,@action,@line,@product,@qty;
END;
GO
CREATE PROCEDURE dbo.G04Expect @sql nvarchar(max),@error int AS
BEGIN
 DECLARE @seen bit=0;
 BEGIN TRY EXEC sys.sp_executesql @sql; END TRY BEGIN CATCH IF ERROR_NUMBER()<>@error THROW; SET @seen=1; END CATCH;
 IF @seen=0 THROW 52990,'Expected rejection missing',1;
 IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52990,'Transaction leaked',1;
END;
GO
CREATE PROCEDURE dbo.G04State @s nvarchar(max) OUTPUT AS
SELECT @s=(SELECT
 (SELECT * FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) h,
 (SELECT * FROM dbo.CHITIET_HD ORDER BY MaDong FOR JSON PATH,INCLUDE_NULL_VALUES) d,
 (SELECT * FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES) n,
 (SELECT * FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH,INCLUDE_NULL_VALUES) k,
 (SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH,INCLUDE_NULL_VALUES) p,
 (SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES) x,
 (SELECT * FROM dbo.LICHSU_DIEM ORDER BY MaGD FOR JSON PATH,INCLUDE_NULL_VALUES) l,
 (SELECT * FROM dbo.HoaDonUuDaiSnapshot ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) u,
 (SELECT * FROM dbo.G03OperationReceipt ORDER BY RequestId FOR JSON PATH,INCLUDE_NULL_VALUES) r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
GO
