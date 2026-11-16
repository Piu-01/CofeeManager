using CafeManager.Business.Services;
using CafeManager.Models.Requests.Assets;
using Xunit;
namespace CafeManager.Tests.Assets;

public sealed class AssetRulesTests
{
    [Theory]
    [InlineData(100,3,1,33,33)]
    [InlineData(100,3,3,34,100)]
    [InlineData(100,3,4,0,100)]
    [InlineData(1,3,2,0,0)]
    [InlineData(1,3,3,1,1)]
    [InlineData(100,3,0,0,0)]
    public void DepreciationPaysRemainderOnlyInFinalMonth(int value,int count,int elapsed,int monthly,int accumulated)
    {
        var result=AssetDepreciationCalculator.Calculate(value,count,new(2026,1,15),new DateOnly(2025,12,1).AddMonths(elapsed));
        Assert.Equal(monthly,result.Monthly);Assert.Equal(accumulated,result.Accumulated);
    }
    [Fact] public void LargeMonthCountDoesNotOverflowCalendar()
    {
        var result=AssetDepreciationCalculator.Calculate(999999999999999999m,int.MaxValue,new(1,1,1),new(9999,12,31));
        Assert.InRange(result.Accumulated,0,999999999999999999m);
    }
    [Theory][InlineData(0,12)][InlineData(-1,12)][InlineData(1,0)][InlineData(1,-1)]
    public void InvalidValueOrMonthsAreRejected(decimal value,int months)=>Assert.Throws<ArgumentException>(()=>AssetService.Normalize(new("Máy pha",new(2026,1,1),value,months)));
    [Fact] public void FractionalAndOversizeMoneyAreNotRounded()
    {
        Assert.Throws<ArgumentException>(()=>AssetService.Normalize(new("Máy pha",new(2026,1,1),1.1m,12)));
        Assert.Throws<ArgumentException>(()=>AssetService.Normalize(new("Máy pha",new(2026,1,1),1000000000000000000m,12)));
    }
}
