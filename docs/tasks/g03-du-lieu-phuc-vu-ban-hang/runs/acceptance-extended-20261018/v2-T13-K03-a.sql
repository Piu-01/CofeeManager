DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
IF NOT EXISTS(SELECT 1 FROM dbo.fn_DiscountPreview(1,15000,'2026-10-18',NULL,NULL) WHERE Discount=0 AND Payable=15001) THROW 52999,'No discount wrong',1;
