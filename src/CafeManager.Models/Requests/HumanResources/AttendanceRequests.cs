namespace CafeManager.Models.Requests.HumanResources;

public sealed record AttendanceLine(int RowNo, string Action, string Employee, DateTime WorkDate, string Shift,
    DateTime? ClockIn, DateTime? ClockOut, string Status, byte[]? ExpectedRevision);
public sealed record AttendanceFilter(int Month, int Year, string? Employee = null, string? Status = null, string Search = "", int Skip = 0);
