using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OpenOrdersDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly SalesService service;
    private readonly SessionContext session;
    private readonly TextBox search = new() { PlaceholderText = "Tìm mã đơn hoặc tên khách…" };
    private readonly OrbitDataGridView grid = new() { Profile = OrbitGridProfile.OpenOrders, Dock = DockStyle.Fill };
    private readonly OrbitNotice state = new();
    private readonly Label count = new() { AutoSize = true, Text = "— đơn đang mở", ForeColor = OrbitManagementTheme.Muted, Font = OrbitManagementTheme.Small, Anchor = AnchorStyles.Right };
    private readonly Label selection = new() { AutoSize = true, Text = "Chưa chọn đơn", ForeColor = OrbitManagementTheme.Muted, Font = OrbitManagementTheme.Table, UseMnemonic = false };
    private readonly Label pageInfo = new() { AutoSize = true, Text = "— / —", ForeColor = OrbitManagementTheme.Muted, Font = OrbitManagementTheme.Small, Anchor = AnchorStyles.None, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(8, 0, 8, 0) };
    private readonly OrbitButton previous, next, open;
    private readonly CancellationTokenSource lifetime = new();
    private bool resourcesDisposed, ready;
    private int page, generation, total;
    public string? SelectedCode { get; private set; }

    public OpenOrdersDialog(SalesService service, SessionContext session)
    {
        this.service = service; this.session = session; Text = "Đơn đang mở";
        foreach (var (property, caption) in new[] { ("Code", "Hóa đơn"), ("Created", "Ngày lập"), ("Channel", "Kênh"), ("Customer", "Khách"), ("Payable", "Phải trả") })
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = property, DataPropertyName = property, HeaderText = caption });
        var find = SalesUi.Button("Tìm", async (_, _) => { page = 0; await Reload(); });
        previous = SalesUi.Button("Trước", async (_, _) => { if (ready && page > 0) { page--; await Reload(); } });
        next = SalesUi.Button("Sau", async (_, _) => { if (ready && (page + 1) * 50 < total) { page++; await Reload(); } });
        open = SalesUi.Button("Mở đơn", (_, _) => SelectOrder());
        var heading = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 0, 0, 16) };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.Controls.Add(new Label { Text = "ĐƠN HÀNG", AutoSize = true, Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Caramel, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        heading.Controls.Add(new Label { Text = "Đơn đang mở", AutoSize = true, Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Text, Margin = new Padding(0, 0, 0, 8) }, 0, 1);
        var subtitle = new Label { Text = "Chọn đơn hàng để tiếp tục phục vụ và thanh toán.", AutoSize = true, Font = OrbitManagementTheme.Table, ForeColor = OrbitManagementTheme.Muted, Margin = Padding.Empty };
        heading.Controls.Add(subtitle, 0, 2);
        OrbitManagementTheme.DialogInput(search);
        var filter = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(16), Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filter.Controls.Add(new OrbitField(search, true) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) }, 0, 0);
        filter.Controls.Add(find, 1, 0); count.Margin = new Padding(16, 0, 0, 0); filter.Controls.Add(count, 2, 0);
        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), Margin = Padding.Empty, BackColor = OrbitManagementTheme.Border }; frame.Controls.Add(grid);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(filter, 0, 0); body.Controls.Add(frame, 0, 1);
        var pager = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, RowCount = 1, Anchor = AnchorStyles.None, Margin = Padding.Empty, Padding = Padding.Empty };
        for (var i = 0; i < 3; i++) pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pager.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        previous.Text = next.Text = "";
        previous.AccessibleName = "Trang trước"; next.AccessibleName = "Trang sau";
        previous.MinimumSize = next.MinimumSize = Size.Empty;
        previous.Anchor = next.Anchor = AnchorStyles.None;
        pager.Controls.Add(previous, 0, 0); pager.Controls.Add(pageInfo, 1, 0); pager.Controls.Add(next, 2, 0);
        var actions = SalesUi.Bar(pager, open);
        var shell = new OrbitDialogLayout(this, body, state, actions, heading, selection); Controls.Add(shell);
        OrbitManagementTheme.Button(find, ManagementButtonStyle.Secondary, "search");
        OrbitManagementTheme.Button(previous, ManagementButtonStyle.Pager, "left");
        OrbitManagementTheme.Button(next, ManagementButtonStyle.Pager, "right");
        OrbitManagementTheme.Button(open, ManagementButtonStyle.Primary, "right");
        previous.Margin = Padding.Empty; next.Margin = Padding.Empty; open.Margin = new Padding(16, 0, 0, 0);
        shell.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, shell.ClientSize.Width - shell.Padding.Horizontal);
            subtitle.MaximumSize = new Size(width, 0);
            selection.MaximumSize = new Size(Math.Max(1, width - actions.GetPreferredSize(Size.Empty).Width - OrbitManagementTheme.Px(this, 12)), 0);
        };
        grid.SelectionChanged += (_, _) => UpdateSelection();
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) SelectOrder(); };
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "Channel" && e.Value is string channel) { e.Value = SalesStates.Label(channel); e.FormattingApplied = true; }
            if (grid.Columns[e.ColumnIndex].Name == "Customer" && e.Value is null) { e.Value = "—"; e.FormattingApplied = true; }
        };
        Shown += async (_, _) => await Reload();
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 0; await Reload(); } };
        SalesUi.Fit(this, new Size(980, 560));
        var area = Screen.FromControl(this).WorkingArea; MaximumSize = area.Size;
        MinimumSize = new Size(Math.Min(area.Width, OrbitManagementTheme.Px(this, 560)), Math.Min(area.Height, OrbitManagementTheme.Px(this, 420)));
        UpdateSelection();
    }
    private void UpdateSelection()
    {
        var row = ready ? grid.CurrentRow?.DataBoundItem as SalesOrderRow : null;
        selection.Text = row is null ? "Chưa chọn đơn" : "Đang chọn " + row.Code;
        selection.ForeColor = row is null ? OrbitManagementTheme.Muted : OrbitManagementTheme.Caramel;
        open.Enabled = row is not null;
        previous.Enabled = ready && page > 0; next.Enabled = ready && (page + 1) * 50 < total;
    }
    private async Task Reload()
    {
        var current = ++generation; var query = search.Text;
        ready = false; grid.Enabled = false; UpdateSelection(); state.Kind = ModuleState.Loading; state.Text = "Đang tải đơn…";
        try
        {
            var rows = await service.OpenAsync(session, query, null, page * 50, lifetime.Token);
            if (IsDisposed || current != generation) return;
            if (rows.Count == 0 && page > 0)
            {
                page = 0; rows = await service.OpenAsync(session, query, null, 0, lifetime.Token);
                if (IsDisposed || current != generation) return;
            }
            total = rows.Count == 0 ? 0 : rows[0].TotalCount;
            grid.DataSource = rows; ready = true; grid.Enabled = true;
            if (grid.Rows.Count > 0) grid.CurrentCell = grid.Rows[0].Cells["Code"];
            count.Text = $"{total} đơn đang mở"; pageInfo.Text = $"{(total == 0 ? 0 : page + 1)} / {Math.Max(1, (total + 49) / 50)}";
            state.Kind = ModuleState.Empty; state.Text = rows.Count == 0 ? "Không có đơn trong bộ lọc này." : "";
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException)
        {
            if (IsDisposed || current != generation) return;
            count.Text = "Chưa cập nhật"; pageInfo.Text = "— / —";
            state.Kind = ModuleState.Error; state.Text = "Chưa cập nhật · " + ex.Message;
        }
        finally { if (!IsDisposed && current == generation) UpdateSelection(); }
    }
    private void SelectOrder()
    {
        if (!ready || grid.CurrentRow?.DataBoundItem is not SalesOrderRow row) return;
        SelectedCode = row.Code; DialogResult = DialogResult.OK; Close();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        if (keyData == Keys.Enter && grid.ContainsFocus) { SelectOrder(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void Dispose(bool disposing) { if (disposing && !resourcesDisposed) { resourcesDisposed = true; lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}

