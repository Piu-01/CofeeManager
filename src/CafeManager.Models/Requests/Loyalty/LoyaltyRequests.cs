namespace CafeManager.Models.Requests.Loyalty;
public sealed record PromotionRequest(string Code,string Name,DateOnly StartDate,DateOnly? EndDate,int Discount,
 string Status="DangChay",string Audience="ALL",IReadOnlyList<string>? Tiers=null,IReadOnlyList<string>? Items=null);
public sealed record PreviewLine(string Product,int Quantity);
public sealed record DiscountPreviewRequest(decimal Money,decimal Ship,DateOnly BusinessDate,string? Customer,
 IReadOnlyList<PreviewLine> Lines);
