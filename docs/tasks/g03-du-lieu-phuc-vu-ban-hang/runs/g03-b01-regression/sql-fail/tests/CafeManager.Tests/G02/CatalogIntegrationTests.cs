using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using Xunit;

namespace CafeManager.Tests.G02;

[Collection("G02Sql"), Trait("Group", "G02"), Trait("Task", "T08")]
public sealed class CatalogIntegrationTests(G02DatabaseFixture db)
{
    private CatalogService Service => new(new CatalogRepository(db.AppFactory()), new PermissionService(db.Auth));
    public static IEnumerable<object[]> UnauthorizedCalls()
    {
        foreach (var role in new[] { "CASH", "STORE" })
        foreach (var kind in Enum.GetValues<CatalogKind>())
        foreach (var action in new[] { "Read", "Create", "Update", "Delete" }) yield return [role, kind, action];
    }
    [Theory, MemberData(nameof(UnauthorizedCalls))]
    public async Task T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(string role, CatalogKind kind, string action)
    {
        using var session = await db.Login(role);
        var request = (CatalogRequest)Catalogs().Single(x => CatalogService.KindOf((CatalogRequest)x[0]) == kind)[0];
        Func<Task> call = action switch
        {
            "Read" => () => Service.ListAsync(session, kind, "", 0),
            "Create" => () => Service.SaveAsync(session, request, null),
            "Update" => () => Service.SaveAsync(session, request with { Code = "DM999" }, new byte[8]),
            _ => () => Service.DeleteAsync(session, kind, new ProductCategoryRow("DM999", "Probe", new byte[8]))
        };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(call);
    }
    [Theory]
    [InlineData(CatalogKind.ProductCategory,"DM990")][InlineData(CatalogKind.Supplier,"NCC90")]
    [InlineData(CatalogKind.PaymentMethod,"PT990")][InlineData(CatalogKind.DeliveryPartner,"DT990")]
    [InlineData(CatalogKind.ExpenseCategory,"TC990")][InlineData(CatalogKind.Position,"CV990")][InlineData(CatalogKind.Shift,"CA990")]
    public async Task T08_K05_EachCatalogWithHistoryCannotDelete(CatalogKind kind, string code)
    {
        using var session = await db.Login(); var row = (await Service.GetAsync(session, kind, code))!;
        await Assert.ThrowsAsync<UserOperationException>(() => Service.DeleteAsync(session, kind, row));
        Assert.NotNull(await Service.GetAsync(session, kind, code));
    }
    [Fact]
    public async Task T08_K10_PartnerEditLeavesPaidOrderSnapshotUnchanged()
    {
        using var session = await db.Login();
        await using var owner = await G02DatabaseFixture.OpenOwner(db.Connection);
        const string sql = "SELECT (SELECT PhanTramPhiDoiTac,PhiDoiTac,TongTien,GiamGia,PhiShip,ThanhTien,TrangThai FROM dbo.HOADON WHERE MaHD='HD20991201' FOR JSON PATH)";
        var before = await TestDatabaseFixture.ScalarAsync(owner, sql);
        var row = (DeliveryPartnerRow)(await Service.GetAsync(session, CatalogKind.DeliveryPartner, "DT990"))!;
        await Service.SaveAsync(session, new DeliveryPartnerRequest(row.Code, row.Name, 25), row.Revision);
        Assert.Equal(before, await TestDatabaseFixture.ScalarAsync(owner, sql));
    }
    public static IEnumerable<object[]> Catalogs()
    {
        yield return [new ProductCategoryRequest("", "Tiếng Việt %_[ ' DM", Guid.NewGuid())];
        yield return [new SupplierRequest("NCC99", "Tiếng Việt %_[ ' NCC", "012345678901234")];
        yield return [new PaymentMethodRequest("PT999", "Tiếng Việt %_[ ' PT")];
        yield return [new DeliveryPartnerRequest("DT999", "Tiếng Việt %_[ ' DT", 100)];
        yield return [new ExpenseCategoryRequest("TC999", "Tiếng Việt %_[ ' TC", ExpenseType.Thu)];
        yield return [new PositionRequest("CV999", "Tiếng Việt %_[ ' CV", 0, 1)];
        yield return [new ShiftRequest("CA999", "Tiếng Việt %_[ ' CA", TimeSpan.FromHours(22), TimeSpan.FromHours(6))];
    }
    [Theory, MemberData(nameof(Catalogs))]
    public async Task T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(CatalogRequest request)
    {
        using var session = await db.Login(); var service = Service; var kind = CatalogService.KindOf(request);
        var created = await service.SaveAsync(session, request, null);
        var row = (await service.GetAsync(session, kind, created.Code))!;
        Assert.Equal(request.Name, row.Name);
        Assert.Contains(await service.ListAsync(session, kind, "%_[ '", 0), x => x.Code == row.Code);
        var edited = request with { Code = row.Code, Name = "Đã sửa " + kind };
        var updated = await service.SaveAsync(session, edited, row.Revision);
        var stale = await Assert.ThrowsAsync<UserOperationException>(() => service.SaveAsync(session, edited with { Name = "Không ghi đè" }, row.Revision));
        Assert.Equal(51503, stale.Error.Number);
        var current = (await service.GetAsync(session, kind, created.Code))!;
        Assert.Equal(edited.Name, current.Name);
        Assert.Equal(updated.Revision, current.Revision);
        await service.DeleteAsync(session, kind, current);
        Assert.Null(await service.GetAsync(session, kind, current.Code));
    }
    [Theory]
    [InlineData("TC005")][InlineData("TC011")][InlineData("TC012")]
    public async Task T08_K07_UnusedSystemExpenseCannotDeleteOrChangeType(string code)
    {
        using var session = await db.Login(); var row = (await Service.GetAsync(session, CatalogKind.ExpenseCategory, code))!;
        await Assert.ThrowsAsync<ArgumentException>(() => Service.DeleteAsync(session, CatalogKind.ExpenseCategory, row));
        await Assert.ThrowsAsync<ArgumentException>(() => Service.SaveAsync(session, new ExpenseCategoryRequest(code, row.Name, ExpenseType.Thu), row.Revision));
        var repository = new CatalogRepository(db.AppFactory());
        await Assert.ThrowsAsync<CafeManager.DataAccess.DataAccessException>(() => repository.DeleteAsync(session.Identity, CatalogKind.ExpenseCategory, code, row.Revision, default));
        Assert.Equal(row.Name, (await Service.GetAsync(session, CatalogKind.ExpenseCategory, code))!.Name);
    }
    [Fact]
    public async Task T08_K08_ManualSupportIsExplicitAndNameIndependent()
    {
        using var session = await db.Login(); var service = Service;
        var payments = new PaymentMethodService(new PaymentMethodRepository(db.AppFactory()), new PermissionService(db.Auth));
        var created = await service.SaveAsync(session, new PaymentMethodRequest("PT998", "Không hỗ trợ mới"), null);
        await Assert.ThrowsAsync<ArgumentException>(() => payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT998"));
        var enabled = await payments.SetSupportAsync(session, "PT998", true, created.Revision!);
        await payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT998");
        var renamed = await service.SaveAsync(session, new PaymentMethodRequest("PT998", "Tên mới", true), enabled.Revision);
        await payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT998");
        await payments.SetSupportAsync(session, "PT998", false, renamed.Revision!);
        await Assert.ThrowsAsync<ArgumentException>(() => payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT998"));
        await Assert.ThrowsAsync<ArgumentException>(() => payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT010"));
        await Assert.ThrowsAsync<ArgumentException>(() => payments.ValidateMethodAsync(session, PaymentPurpose.Checkout, "PT404"));
    }
    [Fact]
    public async Task T08_K01_CreateReceiptReplaysOnlyTheSameRequestPayload()
    {
        using var session = await db.Login(); var service = Service;
        var request = new ProductCategoryRequest("", "Receipt " + Guid.NewGuid().ToString("N"), Guid.NewGuid());
        var first = await service.SaveAsync(session, request, null);
        var replay = await service.SaveAsync(session, request, null);
        Assert.Equal(first.Code, replay.Code);
        Assert.Equal(first.Code, (await service.ReconcileAsync(session, request, true))!.Code);
        var mismatch = await Assert.ThrowsAsync<UserOperationException>(() => service.SaveAsync(session, request with { Name = "Khác payload" }, null));
        Assert.Equal(51508, mismatch.Error.Number);
    }
}
