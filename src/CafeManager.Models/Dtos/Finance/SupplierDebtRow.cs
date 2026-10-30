namespace CafeManager.Models.Dtos.Finance;
public sealed record SupplierDebtPayment(int Id,string Code,DateTime Date,decimal Amount,string Method);
public sealed record SupplierDebtRow(string Code,string Supplier,string PurchaseCode,DateTime SourceDate,decimal Original,decimal Paid,decimal Remaining,DateTime? Due,string Status,List<SupplierDebtPayment> History,int TotalRows);
