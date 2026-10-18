namespace CafeManager.WinForms.UI;

internal static class OrbitFieldLayout
{
    // Giữ thứ tự input, chuyển nhãn lên trên khi không đủ chỗ cho hai cột.
    public static void Attach(TableLayoutPanel fields)
    {
        var cells = fields.Controls.Cast<Control>().Select(control =>
            (Control: control, Cell: fields.GetPositionFromControl(control), Span: fields.GetColumnSpan(control))).ToArray();
        var rows = fields.RowCount; var stacked = false; var arranging = false;
        void Arrange()
        {
            if (arranging || fields.Width <= 0) return;
            var narrow = fields.Width * 96F / fields.DeviceDpi < 480;
            if (narrow == stacked) return;
            arranging = true; stacked = narrow; fields.SuspendLayout();
            try
            {
                foreach (var cell in cells) fields.SetColumnSpan(cell.Control, 1);
                fields.ColumnStyles.Clear(); fields.RowStyles.Clear();
                fields.ColumnCount = narrow ? 1 : 2; fields.RowCount = narrow ? rows * 2 : rows;
                if (!narrow) fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                for (var row = 0; row < fields.RowCount; row++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                foreach (var cell in cells)
                {
                    fields.SetCellPosition(cell.Control, narrow ? new TableLayoutPanelCellPosition(0, cell.Cell.Row * 2 + cell.Cell.Column) : cell.Cell);
                    fields.SetColumnSpan(cell.Control, narrow ? 1 : cell.Span);
                }
            }
            finally { fields.ResumeLayout(true); arranging = false; }
        }
        fields.SizeChanged += (_, _) => Arrange(); Arrange();
    }
}
