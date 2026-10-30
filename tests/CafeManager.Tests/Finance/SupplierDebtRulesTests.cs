using CafeManager.Business.Services;
using Xunit;
namespace CafeManager.Tests.Finance;
public sealed class SupplierDebtRulesTests
{
    [Fact] public void RejectsPointsAsSupplierPayment()=>Assert.Throws<ArgumentException>(()=>SupplierDebtService.Normalize(new(Guid.NewGuid(),"CN00000001","PT010",1000,DateTime.Today)));
    [Theory][InlineData("0")][InlineData("-1")][InlineData("1000.5")]
    public void RequiresWholePositiveVnd(string value)=>Assert.Throws<ArgumentException>(()=>SupplierDebtService.Normalize(new(Guid.NewGuid(),"CN00000001","PT001",decimal.Parse(value,global::System.Globalization.CultureInfo.InvariantCulture),DateTime.Today)));
}
