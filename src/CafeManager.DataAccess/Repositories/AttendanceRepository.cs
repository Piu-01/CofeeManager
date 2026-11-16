using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class AttendanceRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    public Task<List<AttendanceRow>> ListAsync(AccountIdentity a, AttendanceFilter f, CancellationToken t) => executor.ReadAsync("sp_AttendanceList",
        [.. CatalogRepository.Actor(a), EmployeeRepository.P("@Month", SqlDbType.Int, f.Month), EmployeeRepository.P("@Year", SqlDbType.Int, f.Year),
        EmployeeRepository.P("@Employee", SqlDbType.VarChar, f.Employee, -1), EmployeeRepository.P("@Status", SqlDbType.NVarChar, f.Status, 30),
        EmployeeRepository.P("@Search", SqlDbType.NVarChar, f.Search, -1), EmployeeRepository.P("@Skip", SqlDbType.Int, f.Skip)], Map, t);
    public Task<List<AttendanceLookup>> LookupsAsync(AccountIdentity a, CancellationToken t) => executor.ReadAsync("sp_AttendanceLookups", CatalogRepository.Actor(a), r => new AttendanceLookup(r.GetString(0), r.GetString(1).TrimEnd(), r.GetString(2), r.GetBoolean(3)), t);
    public async Task<string> SaveAsync(AccountIdentity a, IReadOnlyList<AttendanceLine> lines, Guid id, CancellationToken t)
    {
        using var data = new DataTable(); data.Columns.Add("RowNo", typeof(int)); data.Columns.Add("Action", typeof(string)); data.Columns.Add("MaNV", typeof(string));
        data.Columns.Add("NgayLam", typeof(DateTime)); data.Columns.Add("MaCa", typeof(string)); data.Columns.Add("GioVao", typeof(DateTime)); data.Columns.Add("GioRa", typeof(DateTime));
        data.Columns.Add("TrangThai", typeof(string)); data.Columns.Add("ExpectedRevision", typeof(byte[]));
        foreach (var l in lines) data.Rows.Add(l.RowNo, l.Action, l.Employee, l.WorkDate.Date, l.Shift, (object?)l.ClockIn ?? DBNull.Value, (object?)l.ClockOut ?? DBNull.Value, l.Status, (object?)l.ExpectedRevision ?? DBNull.Value);
        return (await executor.ReadAsync("sp_AttendanceSaveBatch", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@RequestId", SqlDbType.UniqueIdentifier, id),
            new SqlParameter("@Lines", SqlDbType.Structured) { TypeName = "dbo.AttendanceBatch", Value = data }], r => r.GetString(0), t, true)).Single();
    }
    public async Task<HumanResourcesReceipt?> ReceiptAsync(AccountIdentity a, Guid id, CancellationToken t) =>
        (await executor.ReadAsync("sp_AttendanceReceipt", [.. CatalogRepository.Actor(a), EmployeeRepository.P("@RequestId", SqlDbType.UniqueIdentifier, id)], r => new HumanResourcesReceipt(r.GetString(0), r.GetString(1)), t)).SingleOrDefault();
    private static AttendanceRow Map(SqlDataReader r) => new(r.GetString(0).TrimEnd(), r.GetString(1), r.GetDateTime(2), r.GetString(3).TrimEnd(), r.GetString(4),
        r.IsDBNull(5) ? null : r.GetDateTime(5), r.IsDBNull(6) ? null : r.GetDateTime(6), r.GetString(7), (byte[])r[8], r.GetBoolean(9), r.GetBoolean(10), r.IsDBNull(11) ? null : r.GetDecimal(11), r.GetInt32(12));
}
