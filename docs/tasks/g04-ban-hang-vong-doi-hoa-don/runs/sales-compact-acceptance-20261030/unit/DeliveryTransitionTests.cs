using CafeManager.Business.Services;
using CafeManager.Business.Security;
using Xunit;
namespace CafeManager.Tests.Sales;
public sealed class DeliveryTransitionTests
{
    [Theory]
    [InlineData("Cho giao", "Dang giao")]
    [InlineData("Dang giao", "Da giao")]
    [InlineData("Da giao", null)]
    [InlineData("Huy giao", null)]
    public void OnlyOffersOneForwardStep(string current, string? next) => Assert.Equal(next, DeliveryService.Next(current));
    [Fact]
    public void StoreCannotOperateSales() => Assert.False(PermissionService.Allows("STORE", AppPermission.Sales));
}
