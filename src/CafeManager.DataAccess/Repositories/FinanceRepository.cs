using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Requests.Finance;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class FinanceRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    public Task<List<FinanceCategory>> CategoriesAsync(AccountIdentity actor, CancellationToken token) =>
        executor.ReadAsync("sp_FinanceCategories", CatalogRepository.Actor(actor), r => new FinanceCategory(r.GetString(0).TrimEnd(), r.GetString(1), r.GetString(2)), token);
    public Task<List<FinanceRow>> ListAsync(AccountIdentity actor, FinanceFilter f, CancellationToken token) =>
        executor.ReadAsync("sp_FinanceList", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@From", SqlDbType.Date, f.From.Date),
            AuthRepository.Parameter("@To", SqlDbType.Date, f.To.Date), AuthRepository.Parameter("@Search", SqlDbType.NVarChar, f.Search, -1),
            AuthRepository.Parameter("@Type", SqlDbType.NVarChar, f.Type, 10), AuthRepository.Parameter("@Origin", SqlDbType.VarChar, f.Origin, 20),
            AuthRepository.Parameter("@Skip", SqlDbType.Int, f.Skip)], Map, token);
    public async Task<FinanceRow?> GetAsync(AccountIdentity actor, string code, CancellationToken token) =>
        (await executor.ReadAsync("sp_FinanceGet", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@Code", SqlDbType.VarChar, code, -1)], Map, token)).SingleOrDefault();
    public async Task<string> CreateAsync(AccountIdentity actor, FinanceRequest request, CancellationToken token) =>
        (await executor.ReadAsync("sp_FinanceCreate", [.. CatalogRepository.Actor(actor),
            AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, request.RequestId), AuthRepository.Parameter("@Date", SqlDbType.DateTime2, request.Date),
            AuthRepository.Parameter("@Category", SqlDbType.VarChar, request.Category, -1),
            new SqlParameter("@Amount", SqlDbType.Decimal) { Precision=38, Scale=18, Value=request.Amount },
            AuthRepository.Parameter("@Note", SqlDbType.NVarChar, request.Note, -1), AuthRepository.Parameter("@Asset", SqlDbType.VarChar, request.Asset, -1)], r => r.GetString(0).TrimEnd(), token, true)).Single();
    public async Task<string?> ReceiptAsync(AccountIdentity actor, Guid request, CancellationToken token) =>
        (await executor.ReadAsync("sp_FinanceReceipt", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, request)], r => r.GetString(0).TrimEnd(), token)).SingleOrDefault();
    private static FinanceRow Map(SqlDataReader r) => new(r.GetString(0).TrimEnd(), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetDecimal(4),
        Text(r,5), r.GetString(6), Text(r,7), r.IsDBNull(8) ? null : r.GetInt32(8), Text(r,9), Text(r,10), r.GetDecimal(11), r.GetDecimal(12), r.GetInt32(13));
    private static string? Text(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i).TrimEnd();
}
