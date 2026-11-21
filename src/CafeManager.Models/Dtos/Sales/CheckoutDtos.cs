namespace CafeManager.Models.Dtos.Sales;

public sealed record CheckoutPreview(string Code, byte[] Revision, decimal Total, decimal Ship, decimal Discount,
    decimal Payable, string Source, int PercentDiscount, int TierPercent, int PromotionPercent, decimal PartnerPercent,
    decimal PartnerFee, long Points, byte[] Token);
