using System.Globalization;
namespace CafeManager.WinForms.UI;

// Định dạng dữ liệu G05 độc lập locale máy; không đổi culture hay luồng nhập của POS.
internal static class G05Display
{
    internal static readonly CultureInfo Culture=CultureInfo.GetCultureInfo("vi-VN");
    internal static string Text(FormattableString text)=>text.ToString(Culture);
    internal static void Grid(DataGridView grid)
    {
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
}
