SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_040_%' THROW 52999,'Test DB 040 required',1;
DECLARE @a INT,@s BINARY(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN';
DECLARE @t TABLE(Loai VARCHAR(20),Ma CHAR(10));INSERT @t EXEC dbo.sp_CapMa 'SANPHAM';
DECLARE @code VARCHAR(10)=(SELECT Ma FROM @t),@r UNIQUEIDENTIFIER=NEWID(),@recipe dbo.RecipeLineInput;
INSERT @recipe VALUES('NVL01',0.0500);
EXEC dbo.sp_ProductCreate @a,@s,@code,@r,@recipe,NULL,N'SP retry',N'DM001',1000,N'Dang ban';
BEGIN TRY
 EXEC dbo.sp_ProductCreate @a,@s,@code,@r,@recipe,NULL,N'SP retry',N'DM001',1000,N'Dang ban';
 SELECT 'R01' CaseCode,CASE WHEN (SELECT COUNT(*) FROM dbo.SANPHAM WHERE MaSP=@code)=1 THEN 'PASS' ELSE 'FAIL' END Result,'Gửi lặp cùng RequestId trả cùng mã '+@code Detail;
END TRY BEGIN CATCH
 SELECT 'R01' CaseCode,'FAIL' Result,'Gửi lặp lỗi '+CONVERT(VARCHAR(10),ERROR_NUMBER())+': '+ERROR_MESSAGE() Detail;
END CATCH;
BEGIN TRY
 EXEC dbo.sp_ProductCreate @a,@s,@code,@r,@recipe,NULL,N'SP retry khác nội dung',N'DM001',1000,N'Dang ban';
 SELECT 'R02' CaseCode,'FAIL' Result,'RequestId dùng lại khác nội dung vẫn được nhận' Detail;
END TRY BEGIN CATCH
 SELECT 'R02' CaseCode,CASE WHEN (SELECT COUNT(*) FROM dbo.SANPHAM WHERE MaSP=@code)=1 THEN 'PASS' ELSE 'FAIL' END Result,'RequestId dùng lại bị chặn: '+CONVERT(VARCHAR(10),ERROR_NUMBER()) Detail;
END CATCH;
