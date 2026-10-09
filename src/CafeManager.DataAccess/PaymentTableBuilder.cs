using System.Data;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

internal static class PaymentTableBuilder
{
    public static DataTable Build(IEnumerable<PaymentAllocation> allocations)
    {
        var table = new DataTable();
        table.Columns.Add("MaPT", typeof(string));
        table.Columns.Add("SoTien", typeof(decimal));
        table.Columns.Add("MaThamChieu", typeof(string));
        try
        {
            foreach (var row in allocations)
            {
                var method = SqlParameters.Text("MaPT", SqlDbType.Char, 5, row.MaPT, required: true).Value;
                if (row.SoTien <= 0) throw new ArgumentOutOfRangeException(nameof(allocations), "Phân bổ phải lớn hơn 0.");
                var amount = SqlParameters.Decimal("SoTien", row.SoTien).Value;
                var reference = SqlParameters.Text("MaThamChieu", SqlDbType.NVarChar, 100, row.MaThamChieu).Value;
                table.Rows.Add(method, amount, reference);
            }
            return table;
        }
        catch { table.Dispose(); throw; }
    }

    public static SqlParameter Parameter(DataTable table) => new("@ThanhToan", SqlDbType.Structured)
    { TypeName = "dbo.DanhSachThanhToan", Value = table };
}
