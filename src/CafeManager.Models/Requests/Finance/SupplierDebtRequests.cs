namespace CafeManager.Models.Requests.Finance;
public sealed record SupplierDebtRequest(Guid RequestId,string Code,string Method,decimal Amount,DateTime Date);
