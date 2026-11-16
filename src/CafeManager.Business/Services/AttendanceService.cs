using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
namespace CafeManager.Business.Services;

public sealed class AttendanceService(AttendanceRepository repository, PermissionService permissions)
{
    public static void Validate(IReadOnlyList<AttendanceLine> lines)
    {
        if (lines.Count is < 1 or > 200) throw new ArgumentException(L.T("Batch cần1–200 dòng."));
        var keys = new HashSet<(string, DateTime, string)>(); var rows = new HashSet<int>();
        foreach (var l in lines)
        {
            if (!keys.Add((l.Employee, l.WorkDate.Date, l.Shift)) || !rows.Add(l.RowNo)) throw new ArgumentException(L.T("Batch có dòng trùng key/số thứ tự."));
            if (l.Employee.Length != 5 || l.Shift.Length != 5 || l.Action is not ("Insert" or "Update" or "Delete") || l.WorkDate.Year < 2000 || l.Status is not ("Di lam" or "Nghi phep" or "Vang")) throw new ArgumentException(L.F("Dòng {0}: nhân viên, ca, ngày hoặc trạng thái không hợp lệ.", l.RowNo));
            if (l.ClockOut is not null && (l.ClockIn is null || l.ClockOut <= l.ClockIn) || l.ClockIn is { } start && start.Date != l.WorkDate.Date || l.Status != "Di lam" && (l.ClockIn is not null || l.ClockOut is not null)) throw new ArgumentException(L.F("Dòng {0}: kiểm tra ngày/giờ vào-ra và trạng thái.", l.RowNo));
            if (l.ClockIn?.Ticks % TimeSpan.TicksPerSecond is > 0 || l.ClockOut?.Ticks % TimeSpan.TicksPerSecond is > 0) throw new ArgumentException(L.T("Giờ công nhận đến giây, không tự làm tròn phần lẻ."));
            if (l.Action != "Insert" && l.ExpectedRevision?.Length != 8) throw new ArgumentException(L.T("Thiếu phiên bản công; tải lại."));
        }
    }
    private Task<T> Run<T>(SessionContext s, Func<CancellationToken, Task<T>> action, CancellationToken t) => HumanResourcesAccess.Run(permissions, s, AppPermission.AttendanceManage, action, t);
    public Task<List<AttendanceRow>> ListAsync(SessionContext s, AttendanceFilter f, CancellationToken t = default) => Run(s, ct => repository.ListAsync(s.Identity, f, ct), t);
    public Task<List<AttendanceLookup>> LookupsAsync(SessionContext s, CancellationToken t = default) => Run(s, ct => repository.LookupsAsync(s.Identity, ct), t);
    public Task<string> SaveAsync(SessionContext s, IReadOnlyList<AttendanceLine> lines, Guid id, CancellationToken t = default) => Run(s, ct => { Validate(lines); return repository.SaveAsync(s.Identity, lines, id, ct); }, t);
    public Task<HumanResourcesReceipt?> ReceiptAsync(SessionContext s, Guid id, CancellationToken t = default) => Run(s, ct => repository.ReceiptAsync(s.Identity, id, ct), t);
}
