using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
namespace CafeManager.Business.Services;

public sealed class EmployeeService(EmployeeRepository repository, PermissionService permissions)
{
    public static EmployeeRequest Normalize(EmployeeRequest r)
    {
        var name = r.Name.Trim(); var phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        if (name.Length is < 1 or > 50 || name.Any(c => c is '\t' or '\r' or '\n')) throw new ArgumentException(L.T("Tên nhân viên cần1–50 ký tự."), "Name");
        if (phone is not null && (phone.Length > 15 || phone.Any(c => c is < '0' or > '9'))) throw new ArgumentException(L.T("SDT tối đa15 chữ số."), "Phone");
        if (r.Contract is not ("Full-time" or "Part-time")) throw new ArgumentException(L.T("Chọn hợp đồng hợp lệ."), "Contract");
        Money(r.MonthlyRate); Money(r.HourlyRate);
        if (r.Position.Length != 5) throw new ArgumentException(L.T("Chọn chức vụ."), "Position");
        return r with { Name = name, Phone = phone };
    }
    internal static void Money(decimal value) { if (value < 0 || value > 999999999999999999m || decimal.Truncate(value) != value) throw new ArgumentException(L.T("Tiền phải là nguyên VND không âm, tối đa18 chữ số.")); }
    private Task<T> Run<T>(SessionContext s, Func<CancellationToken, Task<T>> action, CancellationToken t) => HumanResourcesAccess.Run(permissions, s, AppPermission.EmployeeManage, action, t);
    public Task<HumanResourcesContext> ContextAsync(SessionContext s, CancellationToken t = default) => Run(s, ct => repository.ContextAsync(s.Identity, ct), t);
    public Task<List<EmployeeRow>> ListAsync(SessionContext s, EmployeeFilter f, CancellationToken t = default) => Run(s, ct => repository.ListAsync(s.Identity, f, ct), t);
    public Task<EmployeeRow?> GetAsync(SessionContext s, string code, CancellationToken t = default) => Run(s, ct => repository.GetAsync(s.Identity, code, ct), t);
    public Task<List<HumanResourcesLookup>> LookupsAsync(SessionContext s, CancellationToken t = default) => Run(s, ct => repository.LookupsAsync(s.Identity, ct), t);
    public Task<string> SaveAsync(SessionContext s, EmployeeRequest r, EmployeeRow? old, Guid id, CancellationToken t = default) => Run(s, ct => repository.WriteAsync(s.Identity, old is null ? "Create" : "Update", old, Normalize(r), null, id, ct), t);
    public Task<string> EmploymentAsync(SessionContext s, EmployeeRow old, bool employed, Guid id, CancellationToken t = default) => Run(s, ct => repository.WriteAsync(s.Identity, "Employment", old, null, employed, id, ct), t);
    public Task<string> DeleteAsync(SessionContext s, EmployeeRow old, Guid id, CancellationToken t = default) => Run(s, ct => repository.WriteAsync(s.Identity, "Delete", old, null, null, id, ct), t);
    public Task<HumanResourcesReceipt?> ReceiptAsync(SessionContext s, Guid id, string operation, CancellationToken t = default) => Run(s, ct => repository.ReceiptAsync(s.Identity, id, operation, ct), t);
}
