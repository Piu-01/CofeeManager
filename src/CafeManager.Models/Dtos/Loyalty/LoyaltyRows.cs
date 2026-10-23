namespace CafeManager.Models.Dtos.Loyalty;
public sealed record PromotionRow(string Code,string Name,DateOnly StartDate,DateOnly EndDate,int Discount,byte[] Revision);
public sealed record TierRow(string Code,int MinimumPoints,int Discount,byte[] Revision);
public sealed record TierInput(string Code,int MinimumPoints,int Discount);
public sealed record PolicyPreview(byte[] Revision,byte[] PayloadHash,int AffectedCustomers);
public sealed record PointPreview(long Balance,byte[] CustomerRevision,byte[] PolicyRevision,string Tier);
public sealed record DiscountPreview(string Source,string? Tier,string? Promotion,int PercentDiscount,decimal Discount,decimal Payable,DateTime ReadAt,byte[] PolicyRevision);
public sealed record PointHistory(int Id,DateTime OccurredAt,string Direction,int Points,string Reason,string? OrderCode);
