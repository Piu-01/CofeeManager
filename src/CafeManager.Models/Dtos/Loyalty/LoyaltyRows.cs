namespace CafeManager.Models.Dtos.Loyalty;
public sealed record PromotionRow(string Code,string Name,DateOnly StartDate,DateOnly? EndDate,int Discount,
 string Status,string Audience,string Scope,int ItemCount,byte[] Revision,
 string Kind="Giam",int GiftItemCount=0,int GiftQuantity=0);
public sealed record PromotionItem(string Code,string Name,decimal Price,string Status);
public sealed record PromotionGiftRow(string Code,string Name,decimal Price,string Status,int Quantity);
public sealed record PromotionDetail(PromotionRow Promotion,IReadOnlyList<string> Tiers,IReadOnlyList<PromotionItem> Items,
 IReadOnlyList<PromotionGiftRow> Gifts);
public sealed record AppliedPromotion(string Code,string Name,int Percent,decimal BaseAmount,decimal Discount);
public sealed record PreviewGift(string Code,string Name,string Product,string ProductName,int Quantity);
public sealed record TierRow(string Code,int MinimumPoints,int Discount,byte[] Revision);
public sealed record TierInput(string Code,int MinimumPoints,int Discount);
public sealed record PolicyPreview(byte[] Revision,byte[] PayloadHash,int AffectedCustomers);
public sealed record DiscountPreview(string Source,string? Tier,int PercentDiscount,int TierPercent,int PromotionPercent,
 decimal TierDiscount,decimal PromotionDiscount,int PromotionCount,decimal Discount,decimal Payable,
 DateTime ReadAt,byte[] PolicyRevision,IReadOnlyList<AppliedPromotion> Promotions,int GiftCount,
 IReadOnlyList<PreviewGift> Gifts);
public sealed record PointHistory(int Id,DateTime OccurredAt,string Direction,int Points,string Reason,string? OrderCode);
