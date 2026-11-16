namespace CafeManager.Models.Dtos.HumanResources;

public sealed record AttendanceRow(string Employee, string EmployeeName, DateTime WorkDate, string Shift, string ShiftName,
    DateTime? ClockIn, DateTime? ClockOut, string Status, byte[] Revision, bool Incomplete, bool PeriodLocked, decimal? Hours, int TotalRows)
{
    public string StatusText => Incomplete ? L.T("Chưa hoàn tất") : Status switch { "Di lam" => L.T("Đi làm"), "Nghi phep" => L.T("Nghỉ phép"), _ => L.T("Vắng") };
    public string PeriodText => PeriodLocked ? L.T("Đã khóa kỳ") : L.T("Chưa chốt lương");
}
public sealed record AttendanceLookup(string Kind, string Code, string Name, bool Active);
