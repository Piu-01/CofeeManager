DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @date date='2026-10-18';EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100002',@RequestId=@id,@Name=N'Acceptance20%',@StartDate=@date,@EndDate=@date,@Discount=20;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@date,'KH001','KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Formula mismatch',1;
