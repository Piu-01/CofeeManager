DECLARE @a int,@s binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);
INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @rev binary(8),@hash binary(32),@id uniqueidentifier='11111111-1111-1111-1111-111111111113';SELECT @rev=Revision,@hash=PayloadHash FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@rev,@hash,@id;
