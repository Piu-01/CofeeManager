using CafeManager.Business.Services;
using CafeManager.Models.Requests.Inventory;
using Xunit;
namespace CafeManager.Tests.Inventory;
public sealed class MaterialRulesTests
{
    [Theory]
    [InlineData("-0.0001")]
    [InlineData("0.00001")]
    [InlineData("100000000000000")]
    public void RejectsInvalidQuantity(string value) => Assert.Throws<ArgumentException>(() => MaterialService.Quantity(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));
    [Fact]
    public void KeepsCanonicalUnitAndName()
    {
        var value = MaterialService.Normalize(new MaterialRequest(" nvl01 ", " Cà phê ", " Kg "));
        Assert.Equal(new MaterialRequest("NVL01", "Cà phê", "Kg"), value);
        MaterialService.Quantity(0.0180m);
    }
}
