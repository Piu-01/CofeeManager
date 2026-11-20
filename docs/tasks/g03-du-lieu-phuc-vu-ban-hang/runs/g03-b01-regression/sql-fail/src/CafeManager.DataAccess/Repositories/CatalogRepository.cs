using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

public sealed class CatalogRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    internal static SqlParameter[] Actor(AccountIdentity actor) =>
        [AuthRepository.Parameter("@Actor", SqlDbType.Int, actor.AccountId), AuthRepository.Parameter("@Stamp", SqlDbType.Binary, actor.SecurityStamp, 8)];
    private static string Procedure(CatalogKind kind, string action) => kind switch
    {
        CatalogKind.ProductCategory => "sp_CatalogProductCategory" + action,
        CatalogKind.Supplier => "sp_CatalogSupplier" + action,
        CatalogKind.PaymentMethod => "sp_CatalogPaymentMethod" + action,
        CatalogKind.DeliveryPartner => "sp_CatalogDeliveryPartner" + action,
        CatalogKind.ExpenseCategory => "sp_CatalogExpenseCategory" + action,
        CatalogKind.Position => "sp_CatalogPosition" + action,
        CatalogKind.Shift => "sp_CatalogShift" + action,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public Task<List<CatalogRow>> ListAsync(AccountIdentity actor, CatalogKind kind, string search, int skip, CancellationToken token) =>
        executor.ReadAsync(Procedure(kind, "List"), [.. Actor(actor), AuthRepository.Parameter("@Search", SqlDbType.NVarChar, search, 100),
            AuthRepository.Parameter("@Skip", SqlDbType.Int, skip)], r => Map(kind, r), token);
    public async Task<CatalogRow?> GetAsync(AccountIdentity actor, CatalogKind kind, string code, CancellationToken token) =>
        (await executor.ReadAsync(Procedure(kind, "Get"), [.. Actor(actor), AuthRepository.Parameter("@Code", SqlDbType.Char, code, 5)], r => Map(kind, r), token)).SingleOrDefault();
    public async Task<CatalogWriteResult> SaveAsync(AccountIdentity actor, CatalogKind kind, CatalogRequest request, byte[]? revision, CancellationToken token)
    {
        List<SqlParameter> parameters = [.. Actor(actor), AuthRepository.Parameter("@Code", SqlDbType.Char, request.Code, 5),
            AuthRepository.Parameter("@Name", SqlDbType.NVarChar, request.Name, 100), AuthRepository.Parameter("@ExpectedRevision", SqlDbType.Binary, revision, 8)];
        switch (request)
        {
            case ProductCategoryRequest p: parameters.Add(AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, p.RequestId)); break;
            case SupplierRequest s: parameters.Add(AuthRepository.Parameter("@Phone", SqlDbType.VarChar, s.Phone, 15)); break;
            case PaymentMethodRequest p: parameters.Add(AuthRepository.Parameter("@Supported", SqlDbType.Bit, p.Supported)); break;
            case DeliveryPartnerRequest d: parameters.Add(Decimal("@Ratio", d.Ratio, 5, 2)); break;
            case ExpenseCategoryRequest e: parameters.Add(AuthRepository.Parameter("@ExpenseType", SqlDbType.NVarChar, e.Type.ToString(), 10)); break;
            case PositionRequest p: parameters.Add(Decimal("@Allowance", p.Allowance, 18, 0)); parameters.Add(Decimal("@Factor", p.Factor, 5, 2)); break;
            case ShiftRequest s: parameters.Add(AuthRepository.Parameter("@StartTime", SqlDbType.Time, s.StartTime)); parameters.Add(AuthRepository.Parameter("@EndTime", SqlDbType.Time, s.EndTime)); break;
        }
        return (await executor.ReadAsync(Procedure(kind, revision is null ? "Create" : "Update"), parameters.ToArray(), Result, token, true)).Single();
    }
    public async Task<CatalogWriteResult> DeleteAsync(AccountIdentity actor, CatalogKind kind, string code, byte[] revision, CancellationToken token) =>
        (await executor.ReadAsync(Procedure(kind, "Delete"), [.. Actor(actor), AuthRepository.Parameter("@Code", SqlDbType.Char, code, 5),
            AuthRepository.Parameter("@ExpectedRevision", SqlDbType.Binary, revision, 8)], Result, token, true)).Single();
    internal static CatalogWriteResult Result(SqlDataReader r) => new(r.GetString(0).TrimEnd(), r.IsDBNull(1) ? null : (byte[])r[1]);
    public async Task<string?> ReceiptAsync(AccountIdentity actor, Guid requestId, CancellationToken token) =>
        (await executor.ReadAsync("sp_CatalogProductCategoryReceipt", [.. Actor(actor),
            AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, requestId)], r => r.GetString(0).TrimEnd(), token)).SingleOrDefault();
    private static SqlParameter Decimal(string name, decimal value, byte precision, byte scale) => new(name, SqlDbType.Decimal) { Value = value, Precision = precision, Scale = scale };
    private static CatalogRow Map(CatalogKind kind, SqlDataReader r)
    {
        var code = r.GetString(0).TrimEnd(); var name = r.GetString(1); var revision = (byte[])r[10];
        return kind switch
        {
            CatalogKind.ProductCategory => new ProductCategoryRow(code, name, revision),
            CatalogKind.Supplier => new SupplierRow(code, name, r.GetString(2), revision),
            CatalogKind.PaymentMethod => new PaymentMethodRow(code, name, r.GetBoolean(9), revision),
            CatalogKind.DeliveryPartner => new DeliveryPartnerRow(code, name, r.GetDecimal(3), revision),
            CatalogKind.ExpenseCategory => new ExpenseCategoryRow(code, name, Enum.Parse<ExpenseType>(r.GetString(8)), revision),
            CatalogKind.Position => new PositionRow(code, name, r.GetDecimal(4), r.GetDecimal(5), revision),
            CatalogKind.Shift => new ShiftRow(code, name, r.GetTimeSpan(6), r.GetTimeSpan(7), revision),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
    private Task<List<T>> Lookup<T>(AccountIdentity actor, CatalogKind kind, Func<SqlDataReader, T> map, CancellationToken token) =>
        executor.ReadAsync("sp_CatalogLookup", [.. Actor(actor), AuthRepository.Parameter("@Kind", SqlDbType.VarChar, kind.ToString(), 30)], map, token);
    public Task<List<NameLookup>> NamesAsync(AccountIdentity actor, CatalogKind kind, CancellationToken token) => Lookup(actor, kind, r => new NameLookup(r.GetString(0).TrimEnd(), r.GetString(1)), token);
    public Task<List<SupplierLookup>> SuppliersAsync(AccountIdentity actor, CancellationToken token) => Lookup(actor, CatalogKind.Supplier, r => new SupplierLookup(r.GetString(0).TrimEnd(), r.GetString(1), r.GetString(2)), token);
    public Task<List<ExpenseLookup>> ExpensesAsync(AccountIdentity actor, CancellationToken token) => Lookup(actor, CatalogKind.ExpenseCategory, r => new ExpenseLookup(r.GetString(0).TrimEnd(), r.GetString(1), Enum.Parse<ExpenseType>(r.GetString(2))), token);
    public Task<List<ShiftLookup>> ShiftsAsync(AccountIdentity actor, CancellationToken token) => Lookup(actor, CatalogKind.Shift, r => new ShiftLookup(r.GetString(0).TrimEnd(), r.GetString(1), r.GetTimeSpan(2), r.GetTimeSpan(3)), token);
}
