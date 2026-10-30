using System.Data;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess;
internal static class StocktakeTableBuilder
{
    public static DataTable Scope(IEnumerable<string> codes){var t=new DataTable();t.Columns.Add("MaNVL",typeof(string));foreach(var c in codes)t.Rows.Add(c);return t;}
    public static DataTable Counts(IEnumerable<StocktakeCount> lines){var t=new DataTable();t.Columns.Add("MaNVL",typeof(string));t.Columns.Add("Quantity",typeof(decimal));t.Columns.Add("Reason",typeof(string));foreach(var l in lines)t.Rows.Add(l.Code,l.Quantity is null?DBNull.Value:l.Quantity.Value,(object?)l.Reason??DBNull.Value);return t;}
}
