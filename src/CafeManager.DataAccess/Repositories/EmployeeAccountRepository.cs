using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Requests.HumanResources;
namespace CafeManager.DataAccess.Repositories;

public sealed class EmployeeAccountRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    // Hash chỉ đi qua tham số lệnh; không trả credential lên UI hoặc ghi log.
    public async Task<string> WriteAsync(AccountIdentity a, string action, AccountRequest value, string? newHash, Guid id, CancellationToken t) =>
        (await executor.ReadAsync("sp_AccountWrite", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@Action", SqlDbType.VarChar, action, 10),
        EmployeeRepository.P("@RequestId", SqlDbType.UniqueIdentifier, id), EmployeeRepository.P("@Employee", SqlDbType.VarChar, value.Employee, -1),
        EmployeeRepository.P("@ExpectedStamp", SqlDbType.Binary, value.ExpectedStamp, 8), EmployeeRepository.P("@Username", SqlDbType.NVarChar, value.Username, -1),
        EmployeeRepository.P("@NewHash", SqlDbType.VarChar, newHash, -1), EmployeeRepository.P("@Role", SqlDbType.VarChar, value.Role, -1),
        EmployeeRepository.P("@Active", SqlDbType.Bit, value.Active)], r => r.GetString(0), t, true)).Single();
}
