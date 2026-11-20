DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'GUI khách kiểm thử',@Phone=NULL;
DECLARE @code char(5)=(SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id);
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.DiemTichLuy=0 AND h.DiemToiThieu=0) THROW 52999,'Wrong initial tier',1;
