EXEC dbo.G04New 'HD87000001';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD87000001' AND MaKH IS NULL AND MaNV='NV001' AND TongTien=25000) THROW 52990,'Anonymous customer or actor employee mapping wrong',1;
DECLARE @before nvarchar(max),@after nvarchar(max); EXEC dbo.G04State @before OUTPUT;
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD87000002'',@customer=''ZZZZZ'';',52105;
EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD87000002'',N''Tai quan'',@table=''ZZZZZ'';',52105;
EXEC dbo.G04State @after OUTPUT; IF @before<>@after THROW 52990,'Bad FK changed data',1;
DECLARE @mode int=0,@a int,@s binary(8),@request uniqueidentifier,@seen bit;
WHILE @mode<4
BEGIN
 UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001'; UPDATE dbo.NHANVIEN SET DangLamViec=1 WHERE MaNV='NV001';
 SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
 IF @mode=0 SET @a=2147483647;
 IF @mode=1 SET @s=0x0000000000000000;
 IF @mode=2 BEGIN UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV='NV001'; SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; END;
 IF @mode=3 UPDATE dbo.NHANVIEN SET DangLamViec=0 WHERE MaNV='NV001';
 SELECT @request=NEWID(),@seen=0; EXEC dbo.G04State @before OUTPUT;
 BEGIN TRY EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code='HD87000002',@RequestId=@request,@Channel=N'Mang di',@Product='SP001',@Quantity=1; END TRY
 BEGIN CATCH IF ERROR_NUMBER()<>51505 THROW; SET @seen=1; END CATCH;
 EXEC dbo.G04State @after OUTPUT;
 IF @seen=0 OR @before<>@after OR EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD87000002') OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@request) OR @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52990,'Bad actor not rejected atomically',1;
 SET @mode+=1;
END;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001'; UPDATE dbo.NHANVIEN SET DangLamViec=1 WHERE MaNV='NV001';
IF EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID('dbo.sp_TaoHoaDon') AND name='@MaNV') THROW 52990,'Runtime could inject employee',1;
