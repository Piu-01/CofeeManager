DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @t dbo.LoyaltyTierInput;INSERT @t VALUES('DONG',0,-1);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;
