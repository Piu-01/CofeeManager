using CafeManager.Models.Dtos.Loyalty;

namespace CafeManager.Models.Dtos.Sales;

public sealed record SalesOrderHeader(string Code, DateTime Created, string Employee, string? Customer,
    string? TableCode, string? Partner, string? Promotion, string Channel, string? Recipient, string? Phone,
    string? Address, string? Delivery, DateTime? PaidAt, decimal PartnerPercent, decimal PartnerFee,
    decimal Total, decimal Ship, decimal Discount, decimal Payable, string Status, byte[] Revision,
    bool PreviewValid, string? PreviewSource, int? PreviewPercent, DateTime ReadAt,
    int? SessionCode, byte[]? SessionRevision, int PromotionCount = 0)
{
    public bool CanEdit => Status == "Chua thanh toan";
    // Đơn đã "In tạm": món đã ra quầy nên chỉ nối thêm/tăng số lượng, không xóa hay sửa ghi chú.
    public bool IsPending => Status == "Cho thanh toan";
    // Còn mở = còn thu/hủy: đơn chờ thanh toán vẫn thu hoặc hủy được.
    public bool IsPayable => Status is "Chua thanh toan" or "Cho thanh toan";
    public bool IsOpen => IsPayable;
    public bool CanAddLine => IsPayable;
    // F3 (khách/ưu đãi/ship) chốt ở bước thu tiền, còn sửa được cả khi đã In tạm.
    public bool CanEditHeader => IsPayable;
}
public sealed record SalesLine(int LineId, string Product, string Name, int Quantity, decimal Price,
    string? Note, decimal Amount, string? GiftPromotion = null);
public sealed record SalesPayment(int PaymentId, string Method, string Name, decimal Amount, string? Reference);
public sealed record SalesOrderDto(SalesOrderHeader Header, IReadOnlyList<SalesLine> Lines,
    IReadOnlyList<SalesPayment> Payments, IReadOnlyList<AppliedPromotion> Promotions)
{
    // Giữ call site cũ (test/in hóa đơn) không có danh sách ưu đãi.
    public SalesOrderDto(SalesOrderHeader header, IReadOnlyList<SalesLine> lines, IReadOnlyList<SalesPayment> payments)
        : this(header, lines, payments, []) { }
}
public sealed record SalesWriteResult(string Code, byte[] Revision);
public sealed record SalesReceipt(string Code, byte[]? Revision, string Operation, DateTime CompletedAt);
public sealed record SalesMenuItem(string Code, string Name, decimal Price, string Status, bool HasRecipe)
{
    public bool CanAdd => Status == "Dang ban" && HasRecipe;
}
public sealed record SalesLookup(string Code, string Name, decimal Ratio = 0);
public sealed record SalesOrderRow(string Code, DateTime Created, string Channel, string? TableCode,
    string? Customer, decimal Payable, string Status, string? Delivery, byte[] Revision, int TotalCount);
