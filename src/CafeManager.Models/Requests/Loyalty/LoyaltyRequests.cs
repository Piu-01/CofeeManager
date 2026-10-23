namespace CafeManager.Models.Requests.Loyalty;
public sealed record PromotionRequest(string Code,string Name,DateOnly StartDate,DateOnly EndDate,int Discount);
public sealed record PointAdjustmentRequest(string Code,string Direction,int Points,string Reason);
public sealed record DiscountPreviewRequest(decimal Money,decimal Ship,DateOnly BusinessDate,string? Customer,string? Promotion);
