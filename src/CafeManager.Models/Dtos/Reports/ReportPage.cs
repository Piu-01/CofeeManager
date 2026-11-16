using CafeManager.Models.Requests.Reports;
namespace CafeManager.Models.Dtos.Reports;

public sealed record ReportMetadata(ReportKind Kind,DateOnly From,DateOnly To,DateTime ReadAt,int TotalCount,string DateBasis,string Note,bool Complete);
public sealed record ReportTotals(decimal Gross,decimal Discount,decimal Ship,decimal Amount,decimal Fees,decimal Inflow,decimal Outflow,decimal Committed,decimal Paid,decimal Depreciation,decimal Accumulated,decimal Remaining);
public sealed record ReportPage(ReportMetadata Metadata,ReportTotals Totals,List<IReportRow> Rows);
public sealed record ReportSourcePage(int TotalCount,DateTime ReadAt,List<ReportSource> Rows);
