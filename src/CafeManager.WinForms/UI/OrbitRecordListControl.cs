using System.Collections;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Trình bày Orbit; module giữ query skip50, quyền và handler nghiệp vụ.
public class OrbitRecordListControl : UserControl
{
    protected readonly OrbitDataGridView Grid = new() { Dock = DockStyle.Fill };
    protected readonly OrbitListWorkspace Workspace;
    protected Label State => Workspace.Status;
    protected TextBox Search => searchBox.Input;
    protected bool Paginated = true;
    protected Func<string?>? EmptyHint;
    protected SessionContext? Session;
    protected object? Selected => Grid.CurrentRow?.DataBoundItem;
    internal OrbitListWorkspace PageWorkspace => Workspace;
    private readonly OrbitSearchBox searchBox = new() { Dock = DockStyle.Fill };
    private readonly Panel searchHost = new() { BackColor = OrbitManagementTheme.Surface };
    private readonly Label selectionCaption = new() { Text = "Chưa chọn khách hàng", Font = OrbitManagementTheme.Table, ForeColor = OrbitManagementTheme.Muted, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly OrbitEmptyState empty = new() { Visible = false };
    private readonly OrbitButton refresh = new() { AccessibleName = "Tải lại danh sách" };
    private readonly CancellationTokenSource lifetime = new();
    private readonly List<(OrbitButton Button, bool NeedsRow, string Command)> buttons = [];
    private readonly HashSet<Button> running = [];
    private CancellationTokenSource? reading;
    private Func<string, int, CancellationToken, Task<IList>>? query;
    private int page, generation, rowCount, dataPage;
    private bool ready, shuttingDown, busy;
    private string createCaption = "Thêm", noun = "dòng";
    public OrbitRecordListControl()
    {
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        BackColor = OrbitManagementTheme.Background; Font = OrbitManagementTheme.Body;
        searchHost.Controls.Add(searchBox); Workspace = new OrbitListWorkspace(searchHost);
        Workspace.Body.Controls.AddRange([Grid, empty]); Controls.Add(Workspace);
        Workspace.Body.SizeChanged += (_, _) => PositionEmpty();
        Grid.ColumnHeadersHeightChanged += (_, _) => PositionEmpty();
        OrbitManagementTheme.Button(refresh, ManagementButtonStyle.Icon, "refresh"); Workspace.Filters.Controls.Add(refresh);
        Register(refresh, "Tải lại", async () => { page = 0; await Reload(); });
        Register(Workspace.Pagination.Previous, "Trước", async () => { page = Math.Max(0, page - 1); await Reload(); });
        Register(Workspace.Pagination.Next, "Sau", async () => { page++; await Reload(); });
        Search.PlaceholderText = "Tìm mã hoặc tên…";
        Search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && !busy) { e.SuppressKeyPress = true; await ResetAndReload(); } };
        Grid.SelectionChanged += (_, _) => UpdateActions();
        Grid.CellFormatting += (_, e) =>
        {
            if (e.Value is not string value) return;
            var translated = value switch { "Dang ban" => "Đang bán", "Ngung ban" => "Ngừng bán", "Trong" => "Trống", "Dang phuc vu" => "Đang phục vụ", "Chua thanh toan" => "Chưa thanh toán", "Da thanh toan" => "Đã thanh toán", "Huy" => "Đã hủy", "Cong" => "Cộng", "Tru" => "Trừ", _ => value };
            if (translated != value) { e.Value = translated; e.FormattingApplied = true; }
        };
        Load += async (_, _) => { UpdateActions(); if (!ready && query is not null) { ready = true; await Reload(); } };
        VisibleChanged += async (_, _) =>
        {
            if (shuttingDown || Disposing || IsDisposed) return;
            if (Visible && FindForm() is Forms.MainForm main) main.SetWorkspaceTitle(Workspace.Title.Text);
            if (!Visible) { reading?.Cancel(); generation++; ready = false; }
            else if (!ready && query is not null) { ready = true; await Reload(); }
        };
    }
    protected void Configure(string caption, SessionContext session, Func<string, int, CancellationToken, Task<IList>> read, params (string Property, string Header)[] columns)
    {
        Session = session; query = read;
        var profile = caption switch { "Menu và công thức" => OrbitGridProfile.Menu, "Bàn và trạng thái hiện tại" => OrbitGridProfile.Tables, "Khu vực" => OrbitGridProfile.Areas, "Khách hàng" => OrbitGridProfile.Customers, "Khuyến mãi và preview SQL" => OrbitGridProfile.Promotions, "Chính sách hạng" => OrbitGridProfile.Ranks, "Lịch sử điểm (chỉ đọc)" => OrbitGridProfile.Points, _ => OrbitGridProfile.Catalogs };
        Workspace.Title.Text = profile switch { OrbitGridProfile.Menu => "Menu & công thức", OrbitGridProfile.Tables or OrbitGridProfile.Areas => "Bàn & khu vực", OrbitGridProfile.Promotions => "Khuyến mãi", OrbitGridProfile.Points => "Lịch sử điểm", _ => caption };
        Workspace.Eyebrow.Text = profile switch { OrbitGridProfile.Menu => "MENU WORKSPACE", OrbitGridProfile.Tables or OrbitGridProfile.Areas => "SPACE WORKSPACE", OrbitGridProfile.Customers => "CUSTOMER WORKSPACE", OrbitGridProfile.Promotions => "PROMOTION WORKSPACE", OrbitGridProfile.Ranks or OrbitGridProfile.Points => "LOYALTY WORKSPACE", _ => "REFERENCE WORKSPACE" };
        Workspace.Description.Text = profile switch { OrbitGridProfile.Menu => "Quản lý sản phẩm và công thức pha chế.", OrbitGridProfile.Tables or OrbitGridProfile.Areas => "Theo dõi trạng thái bàn và quản lý khu vực.", OrbitGridProfile.Customers => "Quản lý khách hàng, điểm tích lũy và hạng thành viên.", OrbitGridProfile.Promotions => "Quản lý chương trình khuyến mãi và xem trước ưu đãi.", OrbitGridProfile.Ranks => "Thiết lập ngưỡng điểm và ưu đãi theo hạng thành viên.", OrbitGridProfile.Points => "Tra cứu lịch sử điểm khách hàng · Chỉ đọc.", _ => "Tra cứu và quản lý dữ liệu hiện tại." };
        createCaption = profile switch { OrbitGridProfile.Menu => "Thêm sản phẩm", OrbitGridProfile.Tables => "Thêm bàn", OrbitGridProfile.Areas => "Thêm khu vực", OrbitGridProfile.Customers => "Thêm khách hàng", OrbitGridProfile.Promotions => "Thêm khuyến mãi", _ => "Thêm" };
        noun = profile switch { OrbitGridProfile.Menu => "sản phẩm", OrbitGridProfile.Tables => "bàn", OrbitGridProfile.Areas => "khu vực", OrbitGridProfile.Customers => "khách hàng", OrbitGridProfile.Promotions => "chương trình", OrbitGridProfile.Ranks => "hạng thành viên", OrbitGridProfile.Points => "giao dịch", _ => "dòng" };
        foreach (var col in columns) Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = col.Property, DataPropertyName = col.Property, HeaderText = col.Header });
        Grid.Profile = profile;
        Search.PlaceholderText = profile switch { OrbitGridProfile.Menu => "Tìm tên hoặc mã sản phẩm…", OrbitGridProfile.Tables => "Tìm mã hoặc tên bàn…", OrbitGridProfile.Areas => "Tìm mã hoặc tên khu vực…", OrbitGridProfile.Promotions => "Tìm mã hoặc tên chương trình…", _ => Search.PlaceholderText };
        if (profile == OrbitGridProfile.Ranks)
        {
            Workspace.ShowTools = false; Workspace.Filters.Controls.Remove(refresh); Workspace.RowActions.Controls.Add(refresh);
            refresh.Text = "Tải lại"; OrbitManagementTheme.Button(refresh, ManagementButtonStyle.Secondary, "refresh");
        }
        if (profile == OrbitGridProfile.Points)
        {
            Workspace.ShowMeta = false; searchBox.Visible = false; searchHost.Controls.Add(selectionCaption);
            empty.HistoryPrompt = true; searchHost.Layout += (_, _) => ArrangeCustomerSelector();
        }
        Workspace.PerformLayout();
    }
    protected void AddFilter(Control control, int logicalWidth = 140)
    {
        Control presented = control;
        if (control is ComboBox combo) presented = new OrbitFilterBox(combo) { LogicalWidth = logicalWidth };
        else { control.Font = OrbitManagementTheme.Table; control.ForeColor = OrbitManagementTheme.Muted; control.BackColor = OrbitManagementTheme.Surface; control.AutoSize = false; control.Size = new Size(OrbitManagementTheme.Px(this, logicalWidth), OrbitManagementTheme.Px(this, 40)); }
        Workspace.Filters.Controls.Add(presented); Workspace.Filters.Controls.SetChildIndex(refresh, Workspace.Filters.Controls.Count - 1);
    }
    protected OrbitButton AddAction(string text, Func<Task> action, bool visible = true)
    {
        var primary = text is "Thêm" or "Thay chính sách" or "Điều chỉnh điểm";
        var button = new OrbitButton { Text = text == "Thêm" ? createCaption : text, Visible = visible, AccessibleName = text };
        var icon = text switch { "Thêm" => "plus", "Sửa" => "pencil", "Xóa" => "trash", "Công thức" => "book", "Mở sổ điểm" => "notebook", "Hóa đơn" => "receipt", "Chọn khách" => "user", "Chọn bàn" => "pin", "Thay chính sách" or "Điều chỉnh điểm" => "settings", "Preview ưu đãi" => "history", _ => "" };
        OrbitManagementTheme.Button(button, primary ? ManagementButtonStyle.Primary : text == "Xóa" ? ManagementButtonStyle.Danger : text == "Chọn khách" ? ManagementButtonStyle.Secondary : ManagementButtonStyle.Action, icon);
        if (primary) Workspace.PrimaryActions.Controls.Add(button);
        else if (text == "Chọn khách" && Grid.Profile == OrbitGridProfile.Points) { searchHost.Controls.Add(button); ArrangeCustomerSelector(); }
        else Workspace.RowActions.Controls.Add(button);
        Register(button, text, action); OrderActions(); return button;
    }
    private void OrderActions()
    {
        int Order(string command) => command switch { "Công thức" or "Mở sổ điểm" or "Preview ưu đãi" or "Chọn bàn" => 0, "Hóa đơn" => 1, "Sửa" => 10, "Xóa" => 11, _ => 2 };
        var ordered = buttons.Where(item => item.Button.Parent == Workspace.RowActions).OrderBy(item => Order(item.Command)).ToArray();
        for (var i = 0; i < ordered.Length; i++) Workspace.RowActions.Controls.SetChildIndex(ordered[i].Button, i);
    }
    private void ArrangeCustomerSelector()
    {
        var button = searchHost.Controls.OfType<OrbitButton>().FirstOrDefault(); if (button is null) return;
        button.Size = button.GetPreferredSize(Size.Empty); button.Location = new Point(0, (searchHost.Height - button.Height) / 2);
        var gap = OrbitManagementTheme.Px(this, 10); selectionCaption.SetBounds(button.Right + gap, 0, Math.Max(1, searchHost.Width - button.Right - gap), searchHost.Height);
    }
    protected void SetSelectionCaption(string? caption) => selectionCaption.Text = caption ?? "Chưa chọn khách hàng";
    private void Register(OrbitButton button, string command, Func<Task> action)
    {
        buttons.Add((button, command is "Sửa" or "Xóa" or "Công thức" or "Mở sổ điểm" or "Hóa đơn" or "Chọn bàn", command));
        button.Click += async (_, _) =>
        {
            if (!button.Enabled) return; running.Add(button); UpdateActions();
            try { await action(); }
            catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!IsDisposed) State.Text = ex.Message; }
            catch (OperationCanceledException) { }
            finally { running.Remove(button); if (!IsDisposed) UpdateActions(); }
        };
    }
    private void UpdateActions()
    {
        foreach (var (button, needsRow, command) in buttons)
            button.Enabled = !busy && !running.Contains(button) && (!needsRow || Selected is not null) && (command != "Trước" || Paginated && page > 0) && (command != "Sau" || Paginated && rowCount == 50);
    }
    protected async Task ResetAndReload() { page = 0; await Reload(); }
    protected async Task Reload()
    {
        if (shuttingDown || query is null || Session is null) return;
        reading?.Cancel(); reading?.Dispose(); reading = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, Session.Token);
        var current = ++generation; State.Text = "Đang tải…"; Grid.Enabled = false; busy = true; empty.Visible = false; UpdateActions();
        try
        {
            var rows = await query(Search.Text.Trim(), page * 50, reading.Token); if (IsDisposed || current != generation) return;
            rowCount = rows.Count; dataPage = page; Grid.DataSource = rows;
            var hint = EmptyHint?.Invoke(); empty.Text = hint ?? "Chưa có dữ liệu trong bộ lọc này."; PositionEmpty(); empty.Visible = rowCount == 0; if (rowCount == 0) empty.BringToFront();
            Workspace.Count.Text = $"{rowCount} {noun}" + (rowCount == 50 ? " trong trang" : "");
            Workspace.Pagination.SetServerPage(page, rowCount, 50, noun, Paginated);
            if (hint is not null) Workspace.Pagination.Range.Text = "0 giao dịch";
            State.Text = $"Đã cập nhật {DateTime.Now:HH:mm:ss}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!IsDisposed && current == generation) { page = dataPage; State.Text = "Chưa cập nhật · " + ex.Message; } }
        finally { if (!IsDisposed && current == generation) { busy = false; Grid.Enabled = true; UpdateActions(); } }
    }
    private void PositionEmpty() => empty.SetBounds(0, Grid.ColumnHeadersHeight, Workspace.Body.Width, Math.Max(0, Workspace.Body.Height - Grid.ColumnHeadersHeight));
    protected override void Dispose(bool disposing)
    {
        if (disposing && !shuttingDown) { shuttingDown = true; generation++; lifetime.Cancel(); reading?.Cancel(); reading?.Dispose(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
