using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Localization;
using CafeManager.Models.States;
using CafeManager.Tests.Localization;
using Xunit;

namespace CafeManager.Tests.Sales;

// "Chờ thanh toán" khóa sửa món nhưng vẫn thu/hủy được; nhãn theo ngôn ngữ hiện tại.
[Collection("Localization"), Trait("Category", "Unit")]
public sealed class PendingPaymentRulesTests
{
    private static SalesOrderHeader Header(string status) => new("HD00000001", new DateTime(2026, 11, 16), "NV001",
        null, "B0001", null, null, "Tai quan", null, null, null, null, null, 0, 0, 50000, 0, 0, 50000,
        status, new byte[8], true, null, null, DateTime.MinValue, null, null);

    [Theory]
    [InlineData("Chua thanh toan", true, true)]
    [InlineData("Cho thanh toan", false, true)]
    [InlineData("Da thanh toan", false, false)]
    [InlineData("Huy", false, false)]
    public void CanEditAndPayableFollowStatus(string status, bool canEdit, bool payable)
    {
        var header = Header(status);
        Assert.Equal(canEdit, header.CanEdit);
        Assert.Equal(payable, header.IsPayable);
        Assert.Equal(payable, header.IsOpen);
    }

    [Fact]
    public void PendingStatusLabelFollowsCurrentLanguage()
    {
        LanguageService.Reset();
        Assert.Equal("Chờ thanh toán", SalesStates.Label("Cho thanh toan"));
        Assert.Equal("Chưa thanh toán", SalesStates.Label("Chua thanh toan"));
        LanguageService.Set(AppLanguage.English);
        Assert.Equal("Awaiting payment", SalesStates.Label("Cho thanh toan"));
        LanguageService.Reset();
    }
}
