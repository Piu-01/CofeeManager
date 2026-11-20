namespace CafeManager.Models.Requests.Sales;

public sealed record OrderHeaderInput(string Channel, string? TableCode = null, string? Customer = null,
    string? Promotion = null, string? Partner = null, string? Recipient = null, string? Phone = null,
    string? Address = null, decimal Ship = 0);
public sealed record CreateOrderRequest(string Code, Guid RequestId, OrderHeaderInput Header,
    string Product, int Quantity, string? Note);
public sealed record OrderLineRequest(string Code, byte[] Revision, Guid RequestId, string Action,
    int? LineId = null, string? Product = null, int? Quantity = null, string? Note = null);
public sealed record OrderHeaderRequest(string Code, byte[] Revision, Guid RequestId, OrderHeaderInput Header);
public sealed record SalesRevisionRequest(string Code, byte[] Revision, Guid RequestId);
