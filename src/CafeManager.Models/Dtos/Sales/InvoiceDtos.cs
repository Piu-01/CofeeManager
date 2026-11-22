using CafeManager.Models.Dtos.Loyalty;

namespace CafeManager.Models.Dtos.Sales;
public sealed record InvoiceDetailDto(SalesOrderDto Order, string? SnapshotKind, string? Source,
    int? PercentDiscount, int? TierPercent, int? PromotionPercent, int Points, DateTime? SnapshotAt,
    decimal? TierDiscount, decimal? PromotionDiscount, int? PromotionCount, IReadOnlyList<AppliedPromotion> Promotions,
    IReadOnlyList<PreviewGift>? GiftProposals = null)
{
    // Giữ call site cũ (test in PDF) trước khi có breakdown ưu đãi.
    public InvoiceDetailDto(SalesOrderDto order, string? snapshotKind, string? source, int? percentDiscount,
        int? tierPercent, int? promotionPercent, int points, DateTime? snapshotAt)
        : this(order, snapshotKind, source, percentDiscount, tierPercent, promotionPercent, points, snapshotAt, null, null, null, []) { }
}
