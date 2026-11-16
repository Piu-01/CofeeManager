using CafeManager.Business.Services;
using CafeManager.Models.Requests.Reports;
using Xunit;
namespace CafeManager.Tests.Reports;

public sealed class ReportRulesTests
{
    [Fact] public void ReportRejectsMethodFilterOnFullBillRevenue()=>Assert.Throws<ArgumentException>(()=>ReportService.Normalize(new(ReportKind.SalesDay,new(2026,1,1),new(2026,1,2),Method:"PT001")));
    [Fact] public void InvalidPeriodAndUnsupportedRoleSpecificFiltersAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>ReportService.Normalize(new(ReportKind.Debts,new(2026,2,1),new(2026,1,1))));
        Assert.Throws<ArgumentException>(()=>ReportService.Normalize(new(ReportKind.Inventory,new(2026,1,1),new(2026,1,1),Supplier:"NCC01")));
    }
    [Fact] public void DateUpperBoundaryIsAcceptedWithoutIncrementingCalendar()=>Assert.Equal(new DateOnly(9999,12,31),ReportService.Normalize(new(ReportKind.Assets,new(1,1,1),new(9999,12,31))).To);
}
