DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;BEGIN TRAN;DECLARE @preview TABLE(Balance bigint,CustomerRevision binary(8),PolicyRevision binary(8),Tier char(5));
INSERT @preview EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Cong',1;
DECLARE @cr binary(8),@pr binary(8);SELECT @cr=CustomerRevision,@pr=PolicyRevision FROM @preview;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Policy race adjust',@cr,@pr,@id;
