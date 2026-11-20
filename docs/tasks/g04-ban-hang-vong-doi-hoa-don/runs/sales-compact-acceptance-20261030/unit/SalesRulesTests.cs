using CafeManager.Business.Services;
using CafeManager.Models.Requests.Sales;
using Xunit;

namespace CafeManager.Tests.Sales;

public sealed class SalesRulesTests
{
    [Theory]
    [InlineData("Tai quan", null)]
    [InlineData("Mang di", "B0001")]
    [InlineData("Giao hang", "B0001")]
    public void RejectsInvalidTableByChannel(string channel, string? table) =>
        Assert.Throws<ArgumentException>(() => SalesService.Normalize(new(channel, table)));

    [Fact]
    public void DeliveryPreservesPhoneAndNormalizesOptionalCustomer()
    {
        var header = SalesService.Normalize(new("Giao hang", Customer: "  ", Recipient: " Khách ", Phone: "0901234567", Address: " Địa chỉ "));
        Assert.Null(header.Customer); Assert.Equal("0901234567", header.Phone); Assert.Equal("Địa chỉ", header.Address);
    }
    [Fact]
    public void TakeawayCannotCarryDeliveryCharges() => Assert.Throws<ArgumentException>(() =>
        SalesService.Normalize(new("Mang di", Ship: 1)));
}
