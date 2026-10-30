namespace CafeManager.Models.Requests.Inventory;
public sealed record PurchaseLine(string Code,decimal Quantity,decimal Price);
public sealed record PurchaseRequest(Guid RequestId,string Supplier,DateTime Date,DateTime? Due,decimal Immediate,decimal ExpectedTotal,IReadOnlyList<PurchaseLine> Lines);
