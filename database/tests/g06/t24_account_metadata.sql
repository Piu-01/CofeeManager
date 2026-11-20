-- Chỉ runner G06 sau typed setup fixture, không chạy development.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G06[_]%' OR NOT EXISTS(SELECT 1 FROM dbo.G06TestMarker) THROW 53400,N'Sai DB test.',1;
DECLARE @actor int=1,@stamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1),@code char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE TenDangNhap='smoke.account');
IF @code IS NULL THROW 53400,N'Thiếu fixture nhân viên.',1;
EXECUTE AS USER='G06Runtime';
BEGIN TRY
 EXEC dbo.sp_EmployeeGet @actor,@stamp,@code;
 DECLARE @rows TABLE(Code char(5),Name nvarchar(50),Phone varchar(15),Contract nvarchar(20),MonthlyRate decimal(18,0),HourlyRate decimal(18,0),Position char(5),PositionName nvarchar(50),Employed bit,Revision binary(8),AccountId int,Username varchar(50),Role varchar(5),Active bit,AccountStamp binary(8),TotalRows int);
 INSERT @rows EXEC dbo.sp_EmployeeGet @actor,@stamp,@code;
 IF NOT EXISTS(SELECT 1 FROM @rows WHERE Role='CASH' AND Username='smoke.account' AND Active=1 AND Phone IS NULL AND HourlyRate=35000) THROW 53401,N'T24 metadata sai.',1;
 REVERT;
END TRY BEGIN CATCH REVERT; THROW; END CATCH;
IF @@TRANCOUNT<>0 THROW 53401,N'T24 để transaction mở.',1;
