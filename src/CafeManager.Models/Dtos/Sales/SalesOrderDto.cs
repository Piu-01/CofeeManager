namespace CafeManager.Models.Dtos.Sales;

public sealed record SalesOrderHeader(string Code, DateTime Created, string Employee, string? Customer,
    string? TableCode, string? Partner, string? Promotion, string Channel, string? Recipient, string? Phone,
    string? Address, string? Delivery, DateTime? PaidAt, decimal PartnerPercent, decimal PartnerFee,
    decimal Total, decimal Ship, decimal Discount, decimal Payable, string Status, byte[] Revision,
    bool PreviewValid, string? PreviewSource, int? PreviewPercent, DateTime ReadAt)
{
    public bool IsOpen => Status == "Chua thanh toan";
}
public sealed record SalesLine(int LineId, string Product, string Name, int Quantity, decimal Price,
    string? Note, decimal Amount);
public sealed record SalesPayment(int PaymentId, string Method, string Name, decimal Amount, string? Reference);
public sealed record SalesOrderDto(SalesOrderHeader Header, IReadOnlyList<SalesLine> Lines,
    IReadOnlyList<SalesPayment> Payments);
public sealed record SalesWriteResult(string Code, byte[] Revision);
public sealed record SalesReceipt(string Code, byte[]? Revision, string Operation, DateTime CompletedAt);
public sealed record SalesMenuItem(string Code, string Name, decimal Price, string Status, bool HasRecipe)
{
    public bool CanAdd => Status == "Dang ban" && HasRecipe;
}
public sealed record SalesLookup(string Code, string Name, decimal Ratio = 0);
public sealed record SalesOrderRow(string Code, DateTime Created, string Channel, string? TableCode,
    string? Customer, decimal Payable, string Status, string? Delivery, byte[] Revision, int TotalCount);
