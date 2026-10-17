using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using Xunit;

namespace CafeManager.Tests.Catalogs;

[Trait("Group", "G02"), Trait("Task", "T08")]
public sealed class CatalogRulesTests
{
    [Theory]
    [InlineData(-1)][InlineData(100.01)][InlineData(0.001)]
    public void RatioDoesNotRoundOrAcceptOutOfRange(double input) =>
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new DeliveryPartnerRequest("DT999", "Đối tác", (decimal)input), true));
    [Theory]
    [InlineData(0)][InlineData(100)]
    public void RatioIncludesBothBoundaries(int input) => Assert.Equal(input,
        ((DeliveryPartnerRequest)CatalogService.Normalize(new DeliveryPartnerRequest("DT999", "Đối tác", input), true)).Ratio);
    [Theory]
    [InlineData("NCC00")][InlineData("NCC1")][InlineData("NCC100")][InlineData("NCC０1")]
    public void SupplierCodeRespectsFiniteAsciiContract(string code) =>
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new SupplierRequest(code, "NCC", "0123456"), true));
    [Theory]
    [InlineData(-1, 1)][InlineData(0, 0.99)][InlineData(1.5, 1)][InlineData(0, 1.001)]
    public void PositionDoesNotRoundInvalidFinancialFields(double allowance, double factor) =>
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new PositionRequest("CV999", "Vị trí", (decimal)allowance, (decimal)factor), true));
    [Fact]
    public void OvernightShiftIsAllowedButEqualOrSubMinuteTimesAreNot()
    {
        Assert.IsType<ShiftRequest>(CatalogService.Normalize(new ShiftRequest("CA999", "Qua đêm", TimeSpan.FromHours(22), TimeSpan.FromHours(6)), true));
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new ShiftRequest("CA999", "Bằng giờ", TimeSpan.Zero, TimeSpan.Zero), true));
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new ShiftRequest("CA999", "Giây", TimeSpan.FromSeconds(1), TimeSpan.FromHours(6)), true));
    }
    [Theory]
    [InlineData("TC005")][InlineData("TC011")][InlineData("TC012")]
    public void SystemExpenseCanRenameButNotCreateOrChangeType(string code)
    {
        Assert.IsType<ExpenseCategoryRequest>(CatalogService.Normalize(new ExpenseCategoryRequest(code, "Tên mới", ExpenseType.Chi), false));
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new ExpenseCategoryRequest(code, "Tên", ExpenseType.Thu), false));
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new ExpenseCategoryRequest(code, "Tên", ExpenseType.Chi), true));
    }
    [Fact]
    public void NewMethodCannotEnableSupportAndPointsNeverEnable()
    {
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new PaymentMethodRequest("PT999", "Mới", true), true));
        Assert.Throws<ArgumentException>(() => CatalogService.Normalize(new PaymentMethodRequest("PT010", "Điểm", true), false));
    }
    [Theory]
    [InlineData("")][InlineData(" ")][InlineData("\t")][InlineData("Tên\nkhác")]
    public void InvalidNamesAreRejected(string name) => Assert.Throws<ArgumentException>(() =>
        CatalogService.Normalize(new ProductCategoryRequest("", name, Guid.NewGuid()), true));
}
