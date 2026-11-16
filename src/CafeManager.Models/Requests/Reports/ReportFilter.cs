namespace CafeManager.Models.Requests.Reports;

public enum ReportKind { SalesDay, SalesChannel, Products, Payments, Inventory, Debts, CashFlow, Labor, Assets }
public sealed record ReportFilter(ReportKind Kind,DateOnly From,DateOnly To,string? Channel=null,string? Method=null,string? Supplier=null,string? Employee=null,string? Material=null,string? Asset=null,int Skip=0);
