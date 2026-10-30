namespace CafeManager.Models.Dtos.Sales;

public sealed record CheckoutPreview(string Code, byte[] Revision, decimal Total, decimal Ship, decimal Discount,
    decimal Payable, string Source, int PercentDiscount, decimal PartnerPercent, decimal PartnerFee, long Points,
    byte[] Token);
