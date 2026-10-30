using CafeManager.Business.Services;
using CafeManager.Models.Requests.Finance;
using Xunit;
namespace CafeManager.Tests.Finance;
public sealed class FinanceRulesTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.5")]
    [InlineData("1000000000000000000")]
    public void RejectsAmountsThatCannotBePosted(string amount)
    {
        var request=new FinanceRequest(Guid.NewGuid(),DateTime.Today,"TC001",decimal.Parse(amount,global::System.Globalization.CultureInfo.InvariantCulture),null,null);
        Assert.Throws<ArgumentException>(()=>FinanceService.Normalize(request));
    }
    [Fact]
    public void OptionalEmptyInputsBecomeNull()
    {
        var request=FinanceService.Normalize(new(Guid.NewGuid(),DateTime.Today," TC001 ",81000,"  "," "));
        Assert.Null(request.Note); Assert.Null(request.Asset); Assert.Equal("TC001",request.Category);
    }
}
