using System.Data;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

internal static class SalesDraftTableBuilder
{
    public static DataTable Build(IReadOnlyList<SalesDraftLineInput> lines)
    {
        var table = new DataTable();
        table.Columns.Add("RowNo", typeof(int));
        table.Columns.Add("Product", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("Note", typeof(string));
        table.Columns.Add("Price", typeof(decimal));
        try
        {
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                var product = SqlParameters.Text("Product", SqlDbType.VarChar, 5, line.Product, required: true).Value;
                if (line.Quantity < 1) throw new ArgumentOutOfRangeException(nameof(lines), L.T("Số lượng phải dương."));
                var note = SqlParameters.Text("Note", SqlDbType.NVarChar, 100, line.Note).Value;
                var price = SqlParameters.Decimal("Price", line.Price).Value;
                table.Rows.Add(index + 1, product, line.Quantity, note, price);
            }
            return table;
        }
        catch { table.Dispose(); throw; }
    }

    public static SqlParameter Parameter(DataTable table) => new("@Lines", SqlDbType.Structured)
    { TypeName = "dbo.SalesDraftLines", Value = table };
}
