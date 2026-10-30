using System.Data;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess;
internal static class StockIssueTableBuilder
{
    public static DataTable Build(IEnumerable<StockIssueLine> lines)
    {
        var table=new DataTable();table.Columns.Add("MaNVL",typeof(string));table.Columns.Add("Quantity",typeof(decimal));
        foreach(var line in lines)table.Rows.Add(line.Code,line.Quantity);return table;
    }
}
