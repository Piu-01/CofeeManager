namespace CafeManager.Models.Dtos.Sales;
public sealed record InvoiceDetailDto(SalesOrderDto Order, string? SnapshotKind, string? Source,
    int? PercentDiscount, int? TierPercent, int? PromotionPercent, int Points, DateTime? SnapshotAt);
