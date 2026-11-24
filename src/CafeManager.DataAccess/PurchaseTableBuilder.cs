using System.Data;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess;
internal static class PurchaseTableBuilder
{
    public static DataTable Build(IEnumerable<PurchaseLine> lines)
    {
        var table=new DataTable(); table.Columns.Add("MaNVL",typeof(string));table.Columns.Add("Quantity",typeof(decimal));table.Columns.Add("UnitPrice",typeof(decimal));
        foreach(var line in lines)table.Rows.Add(line.Code,line.Quantity,line.Price);
        return table;
    }
    public static DataTable Receive(IEnumerable<PurchaseReceiveLine> lines)
    {
        var table=new DataTable();table.Columns.Add("MaNVL",typeof(string));table.Columns.Add("Quantity",typeof(decimal));table.Columns.Add("Reason",typeof(string));
        foreach(var line in lines)table.Rows.Add(line.Code,line.Quantity,(object?)line.Reason??DBNull.Value);
        return table;
    }
}
