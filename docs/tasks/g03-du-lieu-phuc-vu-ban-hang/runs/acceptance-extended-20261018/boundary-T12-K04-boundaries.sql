DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @values TABLE(Points int,Threshold int);INSERT @values VALUES(299,0),(300,300),(301,300),(799,300),(800,800),(801,800);
WHILE EXISTS(SELECT 1 FROM @values)
BEGIN
 DECLARE @points int,@threshold int,@code char(5),@r binary(8);
 SELECT TOP(1) @points=Points,@threshold=Threshold FROM @values ORDER BY Points;
 SET @id=NEWID();EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Boundary customer',@Phone=NULL;
 SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;SELECT @r=Revision FROM dbo.KHACHHANG WHERE MaKH=@code;
 SET @id=NEWID();EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code=@code,@ExpectedRevision=@r,@Points=@points,@Source=N'Boundary acceptance';
 IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.DiemTichLuy=@points AND h.DiemToiThieu=@threshold) THROW 52999,'Boundary tier mismatch',1;
 DELETE @values WHERE Points=@points;
END;
