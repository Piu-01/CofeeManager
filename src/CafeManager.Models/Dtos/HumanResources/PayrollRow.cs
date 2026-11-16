namespace CafeManager.Models.Dtos.HumanResources;

public sealed record PayrollIssue(string Message);
public sealed class PayrollSnapshot
{
    public string Employee { get; init; } = "";
    public string EmployeeName { get; init; } = "";
    public int Month { get; init; }
    public int Year { get; init; }
    public string Contract { get; init; } = "";
    public string PositionName { get; init; } = "";
    public bool Employed { get; init; }
    public decimal MonthlyRate { get; init; }
    public decimal HourlyRate { get; init; }
    public long TotalSeconds { get; init; }
    public decimal TotalHours { get; init; }
    public decimal RegularHours { get; init; }
    public decimal OvertimeHours { get; init; }
    public decimal BasePay { get; init; }
    public decimal PaidHours { get; init; }
    public decimal HourlyPayRate { get; init; }
    public decimal OvertimePay { get; init; }
    public decimal Allowance { get; init; }
    public decimal Bonus { get; init; }
    public decimal Deduction { get; init; }
    public decimal Amount { get; init; }
    public List<PayrollIssue> Issues { get; init; } = [];
    public List<PayrollAttendance> Attendance { get; init; } = [];
}
public sealed record PayrollAttendance(string Employee, DateTime WorkDate, string Shift, DateTime? ClockIn, DateTime? ClockOut, string Status);
public sealed record PayrollPreview(PayrollSnapshot Snapshot, byte[] Token);
public sealed record PayrollRow(int Id, string Employee, string EmployeeName, int Month, int Year, DateTime CommittedAt,
    decimal Amount, string SnapshotKind, PayrollSnapshot? Snapshot, string? ExpenseCode, DateTime? PaidAt, string Status, int TotalRows)
{
    public string StatusText => Status switch { "Paid" => L.T("Đã chi"), "NoExpense" => L.T("Không phát sinh chi"), "Unpaid" => L.T("Đã chốt / Chưa chi"), _ => L.T("Dữ liệu cũ — cần đối soát") };
}
