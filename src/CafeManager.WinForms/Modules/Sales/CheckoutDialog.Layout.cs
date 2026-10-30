using CafeManager.Models.States;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed partial class CheckoutDialog
{
    private Label totalValue = null!, discountValue = null!, discountSource = null!, shipValue = null!, payableValue = null!;
    private Label allocatedValue = null!, receivedValue = null!, differenceValue = null!;
    private readonly Label cashHint = new() { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(16, 0, 16, 6), UseMnemonic = false };

    private void BuildPaymentLayout()
    {
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 0, 12) };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.Controls.Add(new Label { Text = "THANH TOÁN", AutoSize = true, Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Caramel, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        heading.Controls.Add(new Label { Text = "Xác nhận thanh toán", AutoSize = true, Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Text, Margin = new Padding(0, 0, 0, 8) }, 0, 1);
        var metadata = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        metadata.Controls.Add(new Label { Text = $"Hóa đơn {order.Header.Code}", AutoSize = true, Font = OrbitManagementTheme.Table, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(0, 3, 12, 0) });
        metadata.Controls.Add(new Label { Text = SalesStates.Label(order.Header.Channel), AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Caramel, BackColor = OrbitManagementTheme.Selected, Padding = new Padding(8, 3, 8, 3), Margin = Padding.Empty });
        heading.Controls.Add(metadata, 0, 2);

        var toolbar = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 8) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.Controls.Add(new Label { Text = "Phương thức thanh toán", AutoSize = true, Anchor = AnchorStyles.Left, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text }, 0, 0);
        var rowActions = SalesUi.Bar(add, remove); rowActions.WrapContents = false; rowActions.Margin = Padding.Empty; rowActions.Padding = Padding.Empty;
        toolbar.Controls.Add(rowActions, 1, 0);
        foreach (var button in new[] { add, remove }) OrbitManagementTheme.Button((OrbitButton)button, ManagementButtonStyle.Secondary, button == add ? "plus" : "trash");
        remove.Margin = new Padding(8, 0, 0, 0);

        var table = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(1), BackColor = OrbitManagementTheme.Border };
        table.Controls.Add(grid);
        var cashCard = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 10, 0, 0), BackColor = OrbitManagementTheme.Selected };
        cashCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tendered.TextAlign = HorizontalAlignment.Right;
        cashCard.Controls.Add(new OrbitDialogField("Tiền mặt khách đưa (VND)", tendered) { Margin = new Padding(16, 8, 16, 0) }, 0, 0);
        cashHint.Font = OrbitManagementTheme.Small; cashHint.ForeColor = OrbitManagementTheme.Muted;
        cashCard.Controls.Add(cashHint, 0, 1);

        state.Padding = new Padding(8); state.Margin = new Padding(0, 8, 0, 0);
        var input = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(16), Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        input.RowStyles.Add(new RowStyle(SizeType.AutoSize)); input.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        input.RowStyles.Add(new RowStyle(SizeType.AutoSize)); input.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        input.Controls.Add(toolbar, 0, 0); input.Controls.Add(table, 0, 1); input.Controls.Add(cashCard, 0, 2); input.Controls.Add(state, 0, 3);
        // Không dùng hàng Percent trong container AutoSize: giới hạn bảng rồi cuộn bên trong.
        void SizePaymentRows()
        {
            var count = Math.Clamp(grid.Rows.Count, 1, 3);
            var needsHorizontalScroll = grid.Columns.Cast<DataGridViewColumn>().Sum(c => c.MinimumWidth) + grid.RowHeadersWidth > grid.ClientSize.Width;
            input.RowStyles[1].Height = grid.ColumnHeadersHeight + grid.RowTemplate.Height * count + table.Padding.Vertical + (needsHorizontalScroll ? SystemInformation.HorizontalScrollBarHeight : 0);
        }
        grid.RowsAdded += (_, _) => SizePaymentRows(); grid.RowsRemoved += (_, _) => SizePaymentRows();
        grid.DataBindingComplete += (_, _) => SizePaymentRows(); grid.DpiChangedAfterParent += (_, _) => SizePaymentRows();
        grid.SizeChanged += (_, _) => SizePaymentRows();

        var summary = new OrbitSummaryPanel("Tổng kết hóa đơn");
        totalValue = summary.AddValue("Tiền món"); discountValue = summary.AddValue("Ưu đãi");
        discountSource = summary.AddNote();
        shipValue = summary.AddValue("Ship"); summary.AddDivider();
        payableValue = summary.AddValue("Phải thanh toán", prominent: true); summary.AddDivider();
        allocatedValue = summary.AddValue("Tổng số thu"); receivedValue = summary.AddValue("Khách đưa"); differenceValue = summary.AddValue("Tiền thối / thiếu");
        var body = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(1), BackColor = OrbitManagementTheme.Border };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.Controls.Add(input, 0, 0); body.Controls.Add(summary, 1, 0);
        var viewport = new Panel { AutoScroll = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        viewport.Controls.Add(body);

        var actions = SalesUi.Bar(check, confirm);
        var shell = new OrbitDialogLayout(this, viewport, new Label(), actions, heading, back);
        Controls.Add(shell);
        OrbitManagementTheme.Button((OrbitButton)back, ManagementButtonStyle.Secondary, "left");
        OrbitManagementTheme.Button((OrbitButton)check, ManagementButtonStyle.Secondary, "refresh");
        OrbitManagementTheme.Button((OrbitButton)confirm, ManagementButtonStyle.Primary, "check");
        check.Margin = Padding.Empty; confirm.Margin = new Padding(8, 0, 0, 0);
        // Nhóm CTA một hàng để cột AutoSize chừa đủ bề rộng và chiều cao footer.
        actions.WrapContents = false;
        SalesUi.Fit(this, new Size(920, 600));
        var area = Screen.FromControl(this).WorkingArea;
        MaximumSize = area.Size;
        MinimumSize = new Size(Math.Min(area.Width, OrbitManagementTheme.Px(this, 560)), Math.Min(area.Height, OrbitManagementTheme.Px(this, 380)));
        var arranging = false; var stacked = false;
        void ArrangePayment()
        {
            if (arranging || viewport.Width <= 0) return;
            arranging = true;
            try
            {
                var narrow = viewport.ClientSize.Width * 96F / DeviceDpi < 740;
                if (narrow != stacked)
                {
                    stacked = narrow; body.SuspendLayout(); body.ColumnStyles.Clear(); body.RowStyles.Clear();
                    body.ColumnCount = narrow ? 1 : 2; body.RowCount = narrow ? 2 : 1;
                    body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 100 : 68));
                    if (!narrow) body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
                    body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); if (narrow) body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    body.SetCellPosition(summary, narrow ? new TableLayoutPanelCellPosition(0, 1) : new TableLayoutPanelCellPosition(1, 0));
                    body.ResumeLayout(true);
                }
                body.Width = Math.Max(1, viewport.ClientSize.Width - (viewport.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                state.MaximumSize = new Size(Math.Max(1, input.ClientSize.Width - input.Padding.Horizontal), 0);
                cashHint.MaximumSize = new Size(Math.Max(1, cashCard.ClientSize.Width - cashHint.Margin.Horizontal), 0);
            }
            finally { arranging = false; }
        }
        viewport.SizeChanged += (_, _) => ArrangePayment(); input.SizeChanged += (_, _) => ArrangePayment();
        DpiChanged += (_, _) => ArrangePayment(); Shown += (_, _) => { SizePaymentRows(); ArrangePayment(); }; SizePaymentRows(); ArrangePayment();
    }
}
