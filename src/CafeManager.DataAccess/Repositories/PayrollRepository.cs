using System.Data;
using System.Text.Json;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class PayrollRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    private static SqlParameter[] Input(PayrollRequest r) => [EmployeeRepository.P("@Employee", SqlDbType.VarChar, r.Employee, -1), EmployeeRepository.P("@Month", SqlDbType.Int, r.Month), EmployeeRepository.P("@Year", SqlDbType.Int, r.Year),
        EmployeeRepository.Money("@Regular", r.Regular), EmployeeRepository.Money("@Overtime", r.OverTime), EmployeeRepository.Money("@OvertimePay", r.OverTimePay), EmployeeRepository.Money("@Allowance", r.Allowance), EmployeeRepository.Money("@Bonus", r.Bonus), EmployeeRepository.Money("@Deduction", r.Deduction)];
    public async Task<PayrollPreview> PreviewAsync(AccountIdentity a, PayrollRequest r, CancellationToken t) =>
        (await executor.ReadAsync("sp_PayrollPreview", [.. CatalogRepository.Actor(a), .. Input(r)], reader => new PayrollPreview(JsonSerializer.Deserialize<PayrollSnapshot>(reader.GetString(0)) ?? throw new InvalidDataException(L.T("Thiếu snapshot lương.")), (byte[])reader[1]), t)).Single();
    public async Task<string> CommitAsync(AccountIdentity a, PayrollRequest r, byte[] token, Guid id, CancellationToken t) =>
        (await executor.ReadAsync("sp_ChotBangLuong", [.. CatalogRepository.Actor(a), .. Input(r), EmployeeRepository.P("@RequestId", SqlDbType.UniqueIdentifier, id), EmployeeRepository.P("@ExpectedToken", SqlDbType.VarBinary, token, -1)], reader => reader.GetString(0), t, true)).Single();
    public async Task<string> PayAsync(AccountIdentity a, int id, DateTime date, decimal amount, Guid requestId, CancellationToken t) =>
        (await executor.ReadAsync("sp_ChiLuong", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@Id", SqlDbType.Int, id), EmployeeRepository.P("@Date", SqlDbType.DateTime2, date), EmployeeRepository.Money("@ExpectedAmount", amount), EmployeeRepository.P("@RequestId", SqlDbType.UniqueIdentifier, requestId)], reader => reader.GetString(0), t, true)).Single();
    public Task<List<PayrollRow>> ListAsync(AccountIdentity a, PayrollFilter f, CancellationToken t) => executor.ReadAsync("sp_PayrollList", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@Month", SqlDbType.Int, f.Month), EmployeeRepository.P("@Year", SqlDbType.Int, f.Year), EmployeeRepository.P("@Search", SqlDbType.NVarChar, f.Search, -1), EmployeeRepository.P("@Status", SqlDbType.VarChar, f.Status, 20), EmployeeRepository.P("@Skip", SqlDbType.Int, f.Skip)], Map, t);
    public async Task<PayrollRow?> GetAsync(AccountIdentity a, int id, CancellationToken t) => (await executor.ReadAsync("sp_PayrollGet", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@Id", SqlDbType.Int, id)], Map, t)).SingleOrDefault();
    private static PayrollRow Map(SqlDataReader r) => new(r.GetInt32(0), r.GetString(1).TrimEnd(), r.GetString(2), r.GetByte(3), r.GetInt16(4), r.GetDateTime(5), r.GetDecimal(6), r.GetString(7),
        r.IsDBNull(8) ? null : JsonSerializer.Deserialize<PayrollSnapshot>(r.GetString(8)), r.IsDBNull(9) ? null : r.GetString(9).TrimEnd(), r.IsDBNull(10) ? null : r.GetDateTime(10), r.GetString(11), r.GetInt32(12));
}
