namespace CafeManager.Models.Dtos.Reports;

public interface IReportRow { string Code {get;} string Name {get;} }
public sealed record SalesReportRow(string Code,string Name,int Bills,decimal Gross,decimal Discount,decimal AfterDiscount,decimal Ship,decimal CustomerPaid,decimal PartnerFee) : IReportRow;
public sealed record ProductReportRow(string Code,string Name,int Bills,decimal Quantity,decimal BeforeBillDiscount) : IReportRow;
public sealed record PaymentReportRow(string Code,string Name,int Bills,decimal Amount) : IReportRow;
public sealed record DebtReportRow(string Code,string Name,DateTime? SourceDate,decimal Original,decimal Paid,decimal Remaining) : IReportRow;
public sealed record CashFlowReportRow(string Code,string Name,decimal Inflow,decimal Outflow,decimal Net) : IReportRow;
public sealed record LaborReportRow(string Code,string Name,int IncompleteShifts,decimal Hours,decimal RegularSnapshotHours,decimal OvertimeSnapshotHours,decimal Committed,decimal Paid,string Coverage) : IReportRow;
public sealed record AssetReportRow(string Code,string Name,DateTime? PurchasedOn,decimal Value,decimal RelatedExpenses,decimal PeriodDepreciation,decimal Accumulated,decimal Remaining) : IReportRow;
public sealed record InventoryReportRow(string Code,string Name,DateTime? OpenedAt,decimal? Opening,decimal Purchases,decimal Issues,decimal Consumption,decimal Adjustment,decimal? Closing,decimal Current,string Coverage) : IReportRow;
public sealed record ReportSource(string SourceType,string Code,string Name,DateTime? SourceDate,decimal? Amount,decimal? Quantity);
public sealed record ReportOption(string Kind,string Code,string Name)
{
    public override string ToString() => Code.Length==0?Name:Code+" · "+Name;
}
public sealed record DashboardRow(DateOnly BusinessDate,DateTime ReadAt,decimal Revenue,int OpenOrders,decimal Debt);

