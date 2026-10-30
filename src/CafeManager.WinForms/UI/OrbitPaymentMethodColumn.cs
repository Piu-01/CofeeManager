namespace CafeManager.WinForms.UI;

// Giữ combobox native và binding chuẩn; căn editor vào giữa hàng thanh toán.
public sealed class OrbitPaymentMethodColumn : DataGridViewComboBoxColumn
{
    public OrbitPaymentMethodColumn() => CellTemplate = new CenteredMethodCell();

    private sealed class CenteredMethodCell : DataGridViewComboBoxCell
    {
        public CenteredMethodCell() { }

        public override void PositionEditingControl(bool setLocation, bool setSize, Rectangle cellBounds, Rectangle cellClip,
            DataGridViewCellStyle cellStyle, bool singleVerticalBorderAdded, bool singleHorizontalBorderAdded,
            bool isFirstDisplayedColumn, bool isFirstDisplayedRow)
        {
            base.PositionEditingControl(setLocation, setSize, cellBounds, cellClip, cellStyle,
                singleVerticalBorderAdded, singleHorizontalBorderAdded, isFirstDisplayedColumn, isFirstDisplayedRow);
            if (DataGridView?.EditingControl is ComboBox editor)
                editor.Top = Math.Max(0, (DataGridView.EditingPanel.ClientSize.Height - editor.Height) / 2);
        }
    }
}
