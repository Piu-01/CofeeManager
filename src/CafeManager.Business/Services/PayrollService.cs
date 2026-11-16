using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
namespace CafeManager.Business.Services;

public sealed class PayrollService(PayrollRepository repository, EmployeeRepository employees, PermissionService permissions)
{
    public static void Validate(PayrollRequest r)
    {
        if (r.Employee.Length != 5 || r.Month is < 1 or > 12 || r.Year is < 2000 or > 9999) throw new ArgumentException(L.T("Nhân viên/kỳ lương không hợp lệ."));
        foreach (var hours in new[] { r.Regular, r.OverTime }) if (hours is { } h && (h < 0 || h > 99999999.99m || decimal.Round(h, 2) != h)) throw new ArgumentException(L.T("Giờ cần không âm và tối đa2 số lẻ."));
        EmployeeService.Money(r.OverTimePay); EmployeeService.Money(r.Bonus); EmployeeService.Money(r.Deduction); if (r.Allowance is { } a) EmployeeService.Money(a);
    }
    private Task<T> Run<T>(SessionContext s, Func<CancellationToken, Task<T>> action, CancellationToken t) => HumanResourcesAccess.Run(permissions, s, AppPermission.PayrollManage, action, t);
    public Task<PayrollPreview> PreviewAsync(SessionContext s, PayrollRequest r, CancellationToken t = default) => Run(s, ct => { Validate(r); return repository.PreviewAsync(s.Identity, r, ct); }, t);
    public Task<string> CommitAsync(SessionContext s, PayrollRequest r, byte[] token, Guid id, CancellationToken t = default) => Run(s, ct => { Validate(r); if (token.Length != 32 || id == Guid.Empty) throw new ArgumentException(L.T("Thiếu preview/mã yêu cầu.")); return repository.CommitAsync(s.Identity, r, token, id, ct); }, t);
    public Task<string> PayAsync(SessionContext s, PayrollRow row, DateTime date, Guid id, CancellationToken t = default) => Run(s, ct => { if (row.Status != "Unpaid" || row.Amount <= 0) throw new ArgumentException(L.T("Bảng lương không được chi ở trạng thái này.")); return repository.PayAsync(s.Identity, row.Id, date, row.Amount, id, ct); }, t);
    public Task<List<PayrollRow>> ListAsync(SessionContext s, PayrollFilter f, CancellationToken t = default) => Run(s, ct => repository.ListAsync(s.Identity, f, ct), t);
    public Task<PayrollRow?> GetAsync(SessionContext s, int id, CancellationToken t = default) => Run(s, ct => repository.GetAsync(s.Identity, id, ct), t);
    public Task<HumanResourcesReceipt?> ReceiptAsync(SessionContext s, Guid id, string operation, CancellationToken t = default) => Run(s, ct => employees.ReceiptAsync(s.Identity, id, operation, ct), t);
}
