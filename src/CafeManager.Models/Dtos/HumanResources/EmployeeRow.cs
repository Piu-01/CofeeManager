namespace CafeManager.Models.Dtos.HumanResources;

public sealed record EmployeeRow(string Code, string Name, string? Phone, string Contract, decimal MonthlyRate,
    decimal HourlyRate, string Position, string PositionName, bool Employed, byte[] Revision, int? AccountId,
    string? Username, string? Role, bool? Active, byte[]? AccountStamp, int TotalRows)
{
    public string EmploymentText => Employed ? L.T("Đang làm") : L.T("Đã nghỉ");
    public string AccountText => AccountId is null ? L.T("Chưa cấp") : Active == true ? L.T("Hoạt động") : L.T("Đã khóa");
}
public sealed record HumanResourcesLookup(string Code, string Name, decimal Allowance);
public sealed record HumanResourcesContext(string DatabaseId, string ServerName, string DatabaseName, int SchemaVersion)
{
    public string Scope => DatabaseId + "|" + ServerName.ToUpperInvariant() + "|" + DatabaseName.ToUpperInvariant();
}
public sealed record HumanResourcesReceipt(string Code, string Operation);
