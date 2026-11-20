DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @t dbo.LoyaltyTierInput;INSERT @t SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @p TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);INSERT @p EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;
DECLARE @r binary(8),@h binary(32);SELECT @r=Revision,@h=PayloadHash FROM @p;EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@t,@r,@h,@id;
SET @id=NEWID();EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@t,@r,@h,@id;
