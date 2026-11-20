DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH),@tiers dbo.LoyaltyTierInput;
INSERT @tiers VALUES('DONG',0,0),('RICH',100,5),('VIP',1000,10);
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @r binary(8),@hash binary(32),@affected int;SELECT @r=Revision,@hash=PayloadHash,@affected=AffectedCustomers FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@r,@hash,@id;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaHang<>CASE WHEN DiemTichLuy>=1000 THEN 'VIP' WHEN DiemTichLuy>=100 THEN 'RICH' ELSE 'DONG' END) THROW 52999,'Reclassification mismatch',1;
IF @before<>(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) THROW 52999,'Policy changed points',1;
IF (SELECT ResultCount FROM dbo.G03OperationReceipt WHERE RequestId=@id)<>@affected THROW 52999,'Receipt count mismatch',1;
