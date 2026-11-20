DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);
INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @r binary(8),@hash binary(32);SELECT @r=Revision,@hash=PayloadHash FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@r,@hash,@id;
