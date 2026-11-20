DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99100003',@RequestId=@id,@Name=N'Boundary date',@StartDate='2026-10-17',@EndDate='2026-10-19',@Discount=5;
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-17',NULL,'KM99100003';
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-19',NULL,'KM99100003';
EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-18',NULL,'KM99100002';
