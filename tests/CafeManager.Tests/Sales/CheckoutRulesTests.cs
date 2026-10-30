using CafeManager.Business.Services;
using CafeManager.Models.Requests;
using Xunit;

namespace CafeManager.Tests.Sales;

public sealed class CheckoutRulesTests
{
    [Theory]
    [InlineData(81000, 100000, 19000)]
    [InlineData(30000, 100000, 70000)]
    [InlineData(0, 0, 0)]
    public void ChangeOnlyUsesCashPortion(decimal cash, decimal tendered, decimal expected) =>
        Assert.Equal(expected, SalesService.CashChange(cash, tendered));
    [Fact]
    public void InsufficientTenderIsRejected() => Assert.Throws<ArgumentException>(() => SalesService.CashChange(81000, 80000));
    [Fact]
    public void FractionalMoneyCannotBeRoundedIntoAnAllocation() => Assert.Throws<ArgumentException>(() =>
        SalesService.NormalizeAllocations([new("PT001", 1.5m)]));
    [Fact]
    public void RepeatedMethodsArePreserved() => Assert.Equal(2,
        SalesService.NormalizeAllocations([new("PT002", 30000, "a"), new("PT002", 51000, "b")]).Count);
    [Fact]
    public void ZeroBillAllowsEmptyAllocations() => Assert.Empty(SalesService.NormalizeAllocations([]));
    [Fact]
    public void PointsCannotBeUsedAsPayment() => Assert.Throws<ArgumentException>(() =>
        SalesService.NormalizeAllocations([new("PT010", 1000)]));
}
