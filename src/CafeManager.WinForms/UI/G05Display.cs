using System.Globalization;
namespace CafeManager.WinForms.UI;

// Định dạng dữ liệu G05 độc lập locale máy; không đổi culture hay luồng nhập của POS.
internal static class G05Display
{
    internal static readonly CultureInfo Culture=CultureInfo.GetCultureInfo("vi-VN");
    internal static string Text(FormattableString text)=>text.ToString(Culture);
    internal static void Grid(DataGridView grid)
    {
        if (localizedGrids.TryAdd(grid, new object())) grid.CellFormatting += (_, e) =>
        {
            if (e.Value is not string value || e.ColumnIndex < 0) return;
            var property = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (property is "Type" or "Kind" or "Status")
            {
                e.Value = property == "Type" ? CafeManager.Models.States.FinanceStates.TypeLabel(value)
                    : property == "Kind" ? CafeManager.Models.States.FinanceStates.IssueLabel(value) : CafeManager.Models.States.FinanceStates.DebtLabel(value);
                e.FormattingApplied = true;
            }
        };
        foreach(DataGridViewColumn column in grid.Columns)
        {
            column.DefaultCellStyle.FormatProvider=Culture;
            if(column.DataPropertyName is "Amount" or "Total" or "Immediate" or "Debt" or "Original" or "Paid" or "Remaining" or "Price")
            {column.DefaultCellStyle.Format="N0";column.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}
            if(column.DataPropertyName is "Quantity" or "Expected" or "Actual")
            {column.DefaultCellStyle.Format="N4";column.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}
            if(column.DataPropertyName is "Date" or "SnapshotAt")column.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
            if(column.DataPropertyName=="Due")column.DefaultCellStyle.Format="dd/MM/yyyy";
        }
    }
    private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<DataGridView, object> localizedGrids = new();
}
