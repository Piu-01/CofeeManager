namespace CafeManager.Models.Dtos.Sales;
public sealed record InvoiceDetailDto(SalesOrderDto Order, string? SnapshotKind, string? Source,
    int? PercentDiscount, int Points, DateTime? SnapshotAt);
