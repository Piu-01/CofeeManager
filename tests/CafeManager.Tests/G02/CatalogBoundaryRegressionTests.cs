using System.Data;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.G02;

[Collection("G02Sql"), Trait("Group", "G02"), Trait("Task", "T08")]
public sealed class CatalogBoundaryRegressionTests(G02DatabaseFixture db)
{
    public static IEnumerable<object[]> InvalidNames()
    {
        foreach (var kind in Enum.GetValues<CatalogKind>())
        foreach (var action in new[] { "Create", "Update" })
        foreach (var boundary in new[] { "Empty", "Spaces", "Tab", "MaxPlusOne", "Long" })
            yield return [kind, action, boundary];
    }

    private CatalogService Service => new(new CatalogRepository(db.AppFactory()), new PermissionService(db.Auth));
    private static int MaxName(CatalogKind kind) => kind == CatalogKind.Supplier ? 100 : kind == CatalogKind.Shift ? 30 : 50;
    private static CatalogRequest Request(CatalogKind kind, string name) => kind switch
    {
        CatalogKind.ProductCategory => new ProductCategoryRequest("", name, Guid.NewGuid()),
        CatalogKind.Supplier => new SupplierRequest("NCC89", name, "018900000000001"),
        CatalogKind.PaymentMethod => new PaymentMethodRequest("PT889", name),
        CatalogKind.DeliveryPartner => new DeliveryPartnerRequest("DT889", name, 100),
        CatalogKind.ExpenseCategory => new ExpenseCategoryRequest("TC889", name, ExpenseType.Chi),
        CatalogKind.Position => new PositionRequest("CV889", name, 0, 1),
        CatalogKind.Shift => new ShiftRequest("CA889", name, TimeSpan.FromHours(22), TimeSpan.FromHours(6)),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static SqlCommand WriteCommand(SqlConnection connection, SessionContext session, CatalogKind kind,
        string action, CatalogRequest request, byte[]? revision, string? phone = null)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_Catalog" + kind + action;
        cmd.Parameters.Add(new("@Actor", SqlDbType.Int) { Value = session.Identity.AccountId });
        cmd.Parameters.Add(new("@Stamp", SqlDbType.Binary, 8) { Value = session.Identity.SecurityStamp });
        cmd.Parameters.Add(new("@Code", SqlDbType.Char, 5) { Value = request.Code });
        // Do not clamp client parameter sizes: exercise the SQL boundary with the full original input.
        cmd.Parameters.Add(new("@Name", SqlDbType.NVarChar, -1) { Value = request.Name });
        cmd.Parameters.Add(new("@ExpectedRevision", SqlDbType.Binary, 8) { Value = (object?)revision ?? DBNull.Value });
        switch (request)
        {
            case ProductCategoryRequest p: cmd.Parameters.AddWithValue("@RequestId", p.RequestId); break;
            case SupplierRequest s: cmd.Parameters.Add(new("@Phone", SqlDbType.VarChar, -1) { Value = (object?)(phone ?? s.Phone) ?? DBNull.Value }); break;
            case PaymentMethodRequest p: cmd.Parameters.AddWithValue("@Supported", p.Supported); break;
            case DeliveryPartnerRequest d: cmd.Parameters.AddWithValue("@Ratio", d.Ratio); break;
            case ExpenseCategoryRequest e: cmd.Parameters.AddWithValue("@ExpenseType", e.Type.ToString()); break;
            case PositionRequest p: cmd.Parameters.AddWithValue("@Allowance", p.Allowance); cmd.Parameters.AddWithValue("@Factor", p.Factor); break;
            case ShiftRequest s: cmd.Parameters.AddWithValue("@StartTime", s.StartTime); cmd.Parameters.AddWithValue("@EndTime", s.EndTime); break;
        }
        return cmd;
    }

    private static async Task AssertRejected(SqlCommand cmd, int expectedNumber)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Contains(error.Errors.Cast<SqlError>(), x => x.Number == expectedNumber);
        Assert.Equal("0:0", await TestDatabaseFixture.ScalarAsync(cmd.Connection!,
            "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE(); SELECT CONCAT(@t,':',@s)"));
    }

    [Theory, MemberData(nameof(InvalidNames))]
    public async Task B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(CatalogKind kind, string action, string boundary)
    {
        using var session = await db.Login();
        var name = boundary switch
        {
            "Empty" => "", "Spaces" => "   ", "Tab" => "\t",
            "MaxPlusOne" => new string('X', MaxName(kind) + 1), _ => new string('X', 1001)
        };
        var valid = Request(kind, "Boundary " + kind);
        CatalogRow? row = null;
        if (action == "Update")
        {
            var created = await Service.SaveAsync(session, valid, null);
            row = (await Service.GetAsync(session, kind, created.Code))!;
            valid = valid with { Code = row.Code };
        }
        var before = await Service.ListAsync(session, kind, "", 0);
        var factory = db.AppFactory();
        await using var app = factory.CreateConnection();
        await factory.OpenAsync(app, default);
        await using var cmd = WriteCommand(app, session, kind, action, valid with { Name = name }, row?.Revision);
        await AssertRejected(cmd, 51501);
        var after = await Service.ListAsync(session, kind, "", 0);
        Assert.Equal(before.Select(x => (x.Code, x.Name, Convert.ToHexString(x.Revision))),
            after.Select(x => (x.Code, x.Name, Convert.ToHexString(x.Revision))));
        if (kind == CatalogKind.ProductCategory && action == "Create")
            Assert.Null(await new CatalogRepository(db.AppFactory()).ReceiptAsync(session.Identity, ((ProductCategoryRequest)valid).RequestId, default));
        if (row is not null) await Service.DeleteAsync(session, kind, row);
    }

    [Theory]
    [InlineData("Create")][InlineData("Update")]
    public async Task B02_SqlRejectsPhone16WithoutTruncating(string action)
    {
        using var session = await db.Login();
        var request = (SupplierRequest)Request(CatalogKind.Supplier, "Phone boundary");
        CatalogRow? row = null;
        if (action == "Update")
        {
            var created = await Service.SaveAsync(session, request, null);
            row = (await Service.GetAsync(session, CatalogKind.Supplier, created.Code))!;
        }
        var factory = db.AppFactory();
        await using var app = factory.CreateConnection();
        await factory.OpenAsync(app, default);
        await using var cmd = WriteCommand(app, session, CatalogKind.Supplier, action, request, row?.Revision, "0189000000000001");
        await AssertRejected(cmd, 51501);
        var current = await Service.GetAsync(session, CatalogKind.Supplier, request.Code);
        if (row is null) Assert.Null(current);
        else
        {
            Assert.Equal(row.Revision, current!.Revision);
            Assert.Equal(request.Phone, ((SupplierRow)current).Phone);
            await Service.DeleteAsync(session, CatalogKind.Supplier, current);
        }
    }

    [Theory]
    [InlineData(CatalogKind.ProductCategory)][InlineData(CatalogKind.Supplier)][InlineData(CatalogKind.PaymentMethod)]
    [InlineData(CatalogKind.DeliveryPartner)][InlineData(CatalogKind.ExpenseCategory)][InlineData(CatalogKind.Position)][InlineData(CatalogKind.Shift)]
    public async Task B02_MaxNameAndValidFieldsStillPersist(CatalogKind kind)
    {
        using var session = await db.Login();
        var request = Request(kind, new string('Y', MaxName(kind)));
        var created = await Service.SaveAsync(session, request, null);
        var row = (await Service.GetAsync(session, kind, created.Code))!;
        Assert.Equal(request.Name, row.Name);
        if (row is SupplierRow supplier) Assert.Equal("018900000000001", supplier.Phone);
        var updated = await Service.SaveAsync(session, request with { Code = row.Code, Name = new string('Z', MaxName(kind)) }, row.Revision);
        var current = (await Service.GetAsync(session, kind, row.Code))!;
        Assert.Equal(new string('Z', MaxName(kind)), current.Name);
        Assert.Equal(updated.Revision, current.Revision);
        await Service.DeleteAsync(session, kind, current);
    }
}
