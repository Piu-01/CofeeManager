namespace CafeManager.Models.Requests.Inventory;
public sealed record PurchaseLine(string Code,decimal Quantity,decimal Price);
public sealed record PurchaseRequest(Guid RequestId,string Supplier,DateTime Date,DateTime? Due,decimal Immediate,decimal ExpectedTotal,IReadOnlyList<PurchaseLine> Lines);
public sealed record PurchaseReceiveLine(string Code,decimal Quantity,string? Reason);
public sealed record PurchaseCountRequest(Guid RequestId,string Code,byte[] Revision,IReadOnlyList<PurchaseReceiveLine> Lines);
public sealed record PurchasePostRequest(Guid RequestId,string Code,byte[] Revision,decimal Immediate);
public sealed record PurchaseCancelRequest(Guid RequestId,string Code,byte[] Revision,string Reason);
