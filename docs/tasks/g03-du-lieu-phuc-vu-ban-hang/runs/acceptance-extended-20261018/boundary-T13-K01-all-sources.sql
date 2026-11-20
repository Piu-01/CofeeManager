DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @d date='2026-10-18';
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,NULL,NULL) WHERE Source='None' AND Discount=0 AND Payable=115001) THROW 52999,'None formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,'KH001',NULL) WHERE Source='Tier' AND PercentDiscount=10 AND Discount=10000 AND Payable=105001) THROW 52999,'Tier formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,NULL,'KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Promotion formula',1;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(100001,15000,@d,'KH001','KM99100002') WHERE Source='Promotion' AND PercentDiscount=20 AND Discount=20000 AND Payable=95001) THROW 52999,'Stacked formula',1;
