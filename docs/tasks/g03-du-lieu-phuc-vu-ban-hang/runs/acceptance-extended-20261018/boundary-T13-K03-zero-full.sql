DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100004',@RequestId=@id,@Name=N'Zero',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=0;
SET @id=NEWID();EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100005',@RequestId=@id,@Name=N'Full',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=100;
SET @id=NEWID();EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100006',@RequestId=@id,@Name=N'Half',@StartDate='2026-10-18',@EndDate='2026-10-18',@Discount=50;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(1,15000,'2026-10-18',NULL,'KM99100006') WHERE Discount=1 AND Payable=15000) THROW 52999,'Half rounding',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,'2026-10-18',NULL,'KM99100005') WHERE Discount=100001 AND Payable=15000) THROW 52999,'Ship discounted',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,'2026-10-18',NULL,'KM99100004') WHERE Discount=0 AND Payable=115001) THROW 52999,'Zero wrong',1;
