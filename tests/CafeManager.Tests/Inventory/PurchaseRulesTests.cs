using CafeManager.Business.Services;
using CafeManager.Models.Requests.Inventory;
using Xunit;
namespace CafeManager.Tests.Inventory;
public sealed class PurchaseRulesTests
{
    [Fact] public void RejectsDuplicateMaterialInsteadOfCombiningLines()=>Assert.Throws<ArgumentException>(()=>PurchaseService.ValidateLines([new("NVL01",1,10000),new("NVL01",2,15000)]));
    [Theory][InlineData("0.00001","10000")][InlineData("0","10000")][InlineData("1","0.5")][InlineData("1.5","10000")][InlineData("1","10000.5")]
    public void RejectsUnrepresentableOrZeroInputs(string quantity,string price)=>Assert.Throws<ArgumentException>(()=>PurchaseService.ValidateLines([new("NVL01",decimal.Parse(quantity,global::System.Globalization.CultureInfo.InvariantCulture),decimal.Parse(price,global::System.Globalization.CultureInfo.InvariantCulture))]));
    [Fact] public void AcceptsIntegerQuantityAndVndPrice()=>PurchaseService.ValidateLines([new("NVL01",3,12500),new("NVL02",7,9000)]);
}
