using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.Requests.Tables;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Printing;

namespace CafeManager.WinForms.Modules.Sales;

public sealed class SalesControl : UserControl, IUnsavedChanges, IAsyncLeaveConfirmation
{
    private readonly SalesService service;
    private readonly TableService tables;
    private readonly CustomerService customers;
    private readonly LoyaltyService loyalty;
    private readonly SessionContext session;
    private readonly InvoiceService? invoices;
    private readonly ComboBox channel = SalesUi.Combo(), area = SalesUi.Combo(), table = SalesUi.Combo();
    private readonly OrbitSearchBox searchBox = new();
    private TextBox search => searchBox.Input;
    private readonly FlowLayoutPanel products = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private readonly OrderPanel orderPanel = new();
    private readonly Label state = new() { AutoEllipsis = true, ForeColor = OrbitManagementTheme.Caramel, Font = OrbitManagementTheme.Small };
    private readonly OrbitSurface tableHost = new(), menuSurface = new();
    private readonly Label tableCaption = new() { Text = L.T("Khu vực / Bàn"), Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted };
    private readonly Label tableStatus = new() { Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Caramel, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true };
    private readonly Label eyebrow = new() { Text = "SALES WORKSPACE", Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Caramel };
    private readonly Label title = new() { Text = L.T("Bán hàng"), Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Text };
    private readonly OrbitSectionTabs categoryTabs = new() { Underline = true, Wrap = true, Dock = DockStyle.None, BackColor = OrbitManagementTheme.Surface };
    private readonly Panel topActions = new(), menuTools = new(), menuFooter = new();
    private readonly OrbitFilterBox areaField, tableField, channelField;
    private readonly Button selectTable, find, previous, next;
    private readonly ToolTip tips = new();
    private List<TableRow> tableRows = [];
    private int? currentSession;
    private byte[]? currentSessionRevision;
    private List<SalesLookup> categoryItems = [];
    private string categoryCode = "";
    private bool rebuildingTabs;
    private readonly TabControl compactTabs = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel workspace = new() { ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Panel menuPanel = new() { Dock = DockStyle.Fill };
    private readonly Button requestPay, pay, reopen, header, cancel, closeTable, reconcile;
    private readonly List<Button> actions = [];
    private readonly CancellationTokenSource lifetime = new();
    private SalesOrderDto? order;
    private OrderHeaderInput? draft;
    private OrderHeaderInput? headerDraft;
    private readonly List<SalesDraftLine> draftLines = [];
    private int nextDraftKey;
    private Guid pendingId;
    private string? pendingCode, pendingOperation;
    private IReadOnlyList<string> noteHistory = [];
    private bool busy, unknown, initialized, changingLayout, disposed, layoutReady;
    private int menuGeneration, menuPage;
    public bool HasUnsavedChanges => headerDraft is not null || order is null && (draft is not null || draftLines.Count > 0) || orderPanel.HasPendingNote;
    public bool IsWriting => busy || unknown;
    public void DiscardChanges()
    {
        draft = null; headerDraft = null; draftLines.Clear(); orderPanel.CancelNoteEdit();
        // Bỏ nháp phải làm sạch panel, nếu không lưới giữ dòng cũ nhưng đã khóa thao tác.
        // Đơn đã lưu vẫn giữ lưới của đơn.
        if (order is null && !disposed) { orderPanel.ShowOrder(null); UpdateTableStatus(); }
    }
    public SalesControl(SalesService service, TableService tables, CustomerService customers, LoyaltyService loyalty, SessionContext session, InvoiceService? invoices = null)
    {
        this.service = service; this.tables = tables; this.customers = customers; this.loyalty = loyalty; this.session = session;
        this.invoices = invoices;
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96); Dock = DockStyle.Fill;
        channel.DataSource = new[] { new SalesLookup("Tai quan", L.T("Tại quán")), new SalesLookup("Mang di", L.T("Mang đi")), new SalesLookup("Giao hang", L.T("Giao hàng")) };
        Button Action(string text, Func<Task> callback, ManagementButtonStyle style = ManagementButtonStyle.Action, string icon = "")
        {
            var b = SalesUi.Button(text, async (_, _) => await Guard(callback)); b.MinimumSize = Size.Empty;
            OrbitManagementTheme.Button(b, style, icon); actions.Add(b); return b;
        }
        var newOrder = Action(L.T("Đơn mới"), NewOrder, ManagementButtonStyle.Primary, "plus"); var open = Action(L.T("F4 · Đơn mở"), OpenOrders, icon: "book");
        selectTable = Action(L.T("Chọn bàn"), SelectTable, icon: "pin"); var reload = Action(L.T("Làm mới"), ReloadAll, icon: "refresh");
        channelField = new OrbitFilterBox(channel) { LogicalWidth = 110 };
        areaField = new OrbitFilterBox(area) { LogicalWidth = 125 }; tableField = new OrbitFilterBox(table) { LogicalWidth = 180 };
        topActions.Controls.AddRange([channelField, newOrder, open, reload]);
        tableHost.Controls.AddRange([tableCaption, areaField, tableField, selectTable, tableStatus]);
        search.PlaceholderText = L.T("F2 · Tìm món theo tên…"); search.AccessibleName = L.T("Tìm món bán hàng");
        channel.AccessibleName = L.T("Loại đơn"); area.AccessibleName = L.T("Khu vực"); table.AccessibleName = L.T("Bàn");
        find = Action(L.T("Tìm món"), async () => { menuPage = 0; await LoadMenu(); }, icon: "search");
        previous = Action("", async () => { menuPage = Math.Max(0, menuPage - 1); await LoadMenu(); }, ManagementButtonStyle.Pager, "left");
        next = Action("", async () => { menuPage++; await LoadMenu(); }, ManagementButtonStyle.Pager, "right");
        previous.AccessibleName = L.T("Trang trước"); next.AccessibleName = L.T("Trang sau");
        tips.SetToolTip(previous, L.T("Trang trước")); tips.SetToolTip(next, L.T("Trang sau"));
        products.Dock = DockStyle.None; products.BackColor = OrbitManagementTheme.Surface;
        menuTools.Controls.AddRange([searchBox, find]); menuFooter.Controls.AddRange([previous, next]);
        menuSurface.Dock = DockStyle.Fill; menuSurface.Controls.AddRange([products, menuTools, categoryTabs, menuFooter]); menuPanel.Controls.Add(menuSurface);
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16)); workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 345));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        menuPanel.Margin = orderPanel.Margin = Padding.Empty;
        workspace.Controls.Add(menuPanel, 0, 0); workspace.Controls.Add(orderPanel, 2, 0);
        requestPay = Action(L.T("Yêu cầu thanh toán"), RequestPayment, ManagementButtonStyle.Segment, "ticket");
        pay = Action(L.T("F8 · Thanh toán"), Checkout, ManagementButtonStyle.Primary, "receipt"); header = Action(L.T("F3 · Khách / Ưu đãi"), EditHeader, ManagementButtonStyle.Segment, "user");
        reopen = Action(L.T("Mở lại đơn"), ReopenOrder, ManagementButtonStyle.Segment, "refresh");
        cancel = Action(L.T("Hủy đơn mở"), CancelOrder, ManagementButtonStyle.Danger, "trash");
        closeTable = Action(L.T("Trả bàn"), CloseTable, ManagementButtonStyle.Segment, "armchair");
        reconcile = SalesUi.Button(L.T("Kiểm tra kết quả"), async (_, _) => await Guard(Reconcile));
        reconcile.MinimumSize = Size.Empty; OrbitManagementTheme.Button((OrbitButton)reconcile, ManagementButtonStyle.Action, "history");
        orderPanel.HeaderActions.Controls.AddRange([header, closeTable, cancel]); orderPanel.PaymentActions.Controls.AddRange([requestPay, pay, reopen, reconcile]);
        Controls.AddRange([workspace, tableHost, eyebrow, title, topActions, state]);
        compactTabs.TabPages.Add(L.T("Món")); compactTabs.TabPages.Add(L.T("Đơn")); compactTabs.Visible = false; Controls.Add(compactTabs); compactTabs.BringToFront();
        BackColor = OrbitManagementTheme.Background; Font = OrbitManagementTheme.Body;
        foreach (Control panel in new Control[] { topActions, workspace, menuPanel, menuTools, menuFooter }) panel.BackColor = panel == topActions || panel == workspace || panel == menuPanel ? OrbitManagementTheme.Background : OrbitManagementTheme.Surface;
        OrbitField.ApplyInputBehavior(this); orderPanel.ShowOrder(null);
        orderPanel.IncreaseRequested += line => _ = Guard(() => ChangeQuantity(line, 1));
        orderPanel.DecreaseRequested += line => _ = Guard(() => ChangeQuantity(line, -1));
        orderPanel.DeleteRequested += line => _ = Guard(() => DeleteLine(line));
        orderPanel.NoteSaveRequested += (line, note) => _ = Guard(() => SaveNote(line, note));
        table.SelectedValueChanged += (_, _) => UpdateTableStatus();
        // Tab phân loại lọc ngay khi bấm; bỏ qua lúc dựng lại tab hoặc đang bận.
        categoryTabs.SelectedIndexChanged += async (_, _) =>
        {
            if (rebuildingTabs || busy || unknown) return;
            var index = categoryTabs.SelectedIndex;
            categoryCode = index > 0 && index - 1 < categoryItems.Count ? categoryItems[index - 1].Code : "";
            menuPage = 0;
            await Guard(LoadMenu);
        };
        area.SelectedValueChanged += async (_, _) => { if (initialized && !busy && !unknown) await Guard(LoadTables); };
        channel.SelectedValueChanged += async (_, _) =>
        {
            UpdateTableStatus();
            if (busy || unknown || order is not null || channel.SelectedValue is not string target) return;
            if (draft is null && draftLines.Count == 0) return;
            var previous = draft?.Channel ?? target;
            if (target == previous) return;
            // Nháp chưa có trong SQL nên đổi kênh chỉ đổi ngữ cảnh tại chỗ: giữ dòng món,
            // không ghi SQL và không hỏi Giữ/Không giữ. Nhờ vậy không phát sinh xung đột bàn.
            var header = draft ?? new OrderHeaderInput(previous, previous == "Tai quan" ? table.SelectedValue as string : null);
            var clearsDelivery = target != "Giao hang" && (header.Partner is not null || header.Recipient is not null
                || header.Phone is not null || header.Address is not null || header.Ship != 0);
            if (clearsDelivery && await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
                L.T("Đổi kênh sẽ xóa thông tin người nhận/địa chỉ/phí ship đã nhập. Tiếp tục?"),
                L.T("Đổi loại đơn"), MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                channel.SelectedValue = previous; return;
            }
            string? targetTable = null;
            if (target == "Tai quan")
            {
                // Vào Tại quán dùng đúng cơ chế chọn bàn của Đơn mới; chỉ chặn khi đã hết bàn trống.
                if (tableRows.Count == 0)
                {
                    channel.SelectedValue = previous;
                    state.Text = L.T("Chưa có dữ liệu bàn. Bấm Làm mới rồi thử lại.");
                    return;
                }
                var free = TableForNewOrder();
                if (free is null)
                {
                    channel.SelectedValue = previous;
                    state.Text = L.T("Không còn bàn trống. Chọn Mang đi/Giao hàng hoặc xử lý đơn đang mở.");
                    await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
                        L.T("Không còn bàn trống để chuyển sang Tại quán. Hãy xử lý đơn đang mở hoặc chọn Mang đi/Giao hàng."),
                        L.T("Hết bàn trống"), MessageBoxButtons.OK, L.T("Đã hiểu"));
                    return;
                }
                targetTable = free.Code;
            }
            orderPanel.CancelNoteEdit();
            if (targetTable is null) table.SelectedIndex = -1; else table.SelectedValue = targetTable;
            draft = header with
            {
                Channel = target, TableCode = targetTable,
                Partner = null, Recipient = null, Phone = null, Address = null, Ship = 0
            };
            ShowDraft(); SetBusy(false);
            var tableName = targetTable is null ? null : tableRows.FirstOrDefault(x => x.Code == targetTable)?.Name;
            state.Text = draftLines.Count == 0
                ? L.F("Đã chọn {0}. Thêm món đầu mới tạo đơn.", Models.States.SalesStates.Label(target))
                : target == "Tai quan"
                    ? L.F("Nháp đã chuyển sang Tại quán · {0}; chưa ghi SQL.", tableName ?? "—")
                    : L.F("Nháp đã chuyển sang {0}; chưa ghi SQL.", Models.States.SalesStates.Label(target));
        };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; menuPage = 0; await Guard(LoadMenu); } };
        Load += async (_, _) => { if (!initialized) { initialized = true; await Guard(ReloadAll); } };
        VisibleChanged += async (_, _) => { if (Visible && initialized && !disposed && !busy && !unknown) await Guard(ReloadAll); };
        SizeChanged += (_, _) => Arrange(); menuSurface.SizeChanged += (_, _) => Arrange(); products.SizeChanged += (_, _) => ArrangeCards();
        state.TextChanged += (_, _) => { tips.SetToolTip(state, state.Text); Arrange(); };
        layoutReady = true; SetBusy(false); Arrange();
    }
    private async Task Guard(Func<Task> action)
    {
        if (disposed || session.IsRevoked) return;
        try { await action(); }
        catch (Exception ex) when (ex is UserOperationException or ArgumentException or UnauthorizedAccessException or OperationCanceledException or OverflowException)
        { if (!disposed) state.Text = L.T("Chưa cập nhật · ") + ex.Message; }
    }
    private void SetBusy(bool value)
    {
        busy = value;
        foreach (var b in actions) b.Enabled = !busy && !unknown;
        channel.Enabled = area.Enabled = table.Enabled = search.Enabled = products.Enabled = !busy && !unknown;
        categoryTabs.Enabled = !busy && !unknown;
        channel.Enabled = !busy && !unknown && order is null;
        products.Enabled = !busy && !unknown && (order is null || order.Header.CanEdit);
        requestPay.Enabled = !busy && !unknown && order?.Header.CanEdit == true && !HasUnsavedChanges
            && order.Lines.Count > 0 && order.Header.PreviewValid;
        pay.Enabled = !busy && !unknown && ((order?.Header.IsPayable == true && !HasUnsavedChanges && order.Lines.Count > 0 && order.Header.PreviewValid)
            || (order is null && draftLines.Count > 0 && !orderPanel.HasPendingNote));
        reopen.Enabled = !busy && !unknown && order?.Header.Status == "Cho thanh toan" && session.Identity.Role == "ADMIN";
        cancel.Enabled = !busy && !unknown && order?.Header.IsPayable == true;
        closeTable.Enabled = !busy && !unknown && currentSession is not null && currentSessionRevision is { Length: 8 }
            && order?.Header.IsPayable != true && draftLines.Count == 0 && !HasUnsavedChanges;
        header.Enabled = !busy && !unknown && (order is null || order.Header.CanEdit);
        orderPanel.SetInteractive(!busy && !unknown && (order?.Header.CanEdit == true || order is null && draftLines.Count > 0));
        reconcile.Enabled = !busy && unknown;
        orderPanel.SetResultUnknown(unknown);
    }
    private async Task ReloadAll()
    {
        if (busy || unknown) return; SetBusy(true);
        try
        {
            var areas = new List<AreaRow>();
            for (var skip = 0; ; skip += 50) { var page = await tables.AreasAsync(session, "", skip, lifetime.Token); areas.AddRange(page); if (page.Count < 50) break; }
            var categories = await service.CategoriesAsync(session, lifetime.Token); if (disposed) return;
            var selectedArea = area.SelectedValue as string;
            area.DataSource = new[] { new SalesLookup("", L.T("Tất cả khu vực")) }.Concat(areas.Select(x => new SalesLookup(x.Code, x.Name))).ToList();
            area.SelectedValue = selectedArea ?? "";
            BuildCategoryTabs(categories);
            await LoadTables(); await LoadMenu();
            await LoadNoteHistory();
            if (order is not null) await LoadOrder(order.Header.Code);
            else if (draftLines.Count > 0) ShowDraft();
            if (!disposed) state.Text = L.T("Đã tải từ SQL · ") + DateTime.Now.ToString("HH:mm");
        }
        finally { if (!disposed) SetBusy(false); }
    }
    // Dựng lại tab danh mục từ DANHMUC_SP; giữ tab đang chọn nếu danh mục còn tồn tại.
    private void BuildCategoryTabs(IReadOnlyList<SalesLookup> categories)
    {
        rebuildingTabs = true;
        try
        {
            categoryItems = categories.ToList();
            categoryTabs.Clear();
            categoryTabs.Add(L.T("Tất cả"));
            foreach (var item in categoryItems) categoryTabs.Add(item.Name);
            var index = categoryItems.FindIndex(x => x.Code == categoryCode);
            if (index < 0) categoryCode = "";
            categoryTabs.SelectedIndex = index + 1;
        }
        finally { rebuildingTabs = false; }
        Arrange();
    }
    private async Task LoadTables()
    {
        var rows = new List<TableRow>(); var selected = table.SelectedValue as string;
        for (var skip = 0; ; skip += 50) { var page = await tables.ListAsync(session, "", skip, string.IsNullOrEmpty(area.SelectedValue as string) ? null : area.SelectedValue as string, lifetime.Token); rows.AddRange(page); if (page.Count < 50) break; }
        if (disposed) return;
        tableRows = rows;
        table.DataSource = rows.Select(x => new SalesLookup(x.Code, $"{x.Name} · {(x.Retained ? L.T("Đã trả · giữ bàn") : x.OrderCode is null ? L.T("Trống") : x.OrderCode)}")).ToList();
        if (selected is not null) table.SelectedValue = selected;
        UpdateTableStatus();
    }
    private async Task LoadMenu()
    {
        var generation = ++menuGeneration;
        var rows = await service.MenuAsync(session, search.Text, string.IsNullOrEmpty(categoryCode) ? null : categoryCode, menuPage * 50, lifetime.Token);
        if (disposed || generation != menuGeneration) return;
        foreach (Control c in products.Controls.Cast<Control>().ToArray()) c.Dispose(); products.Controls.Clear();
        foreach (var item in rows) products.Controls.Add(new ProductCardControl(item, async (_, _) => await Guard(() => AddProduct(item))));
        ArrangeCards(); state.Text = rows.Count == 0 ? L.T("Không có món trong trang/bộ lọc này.") : L.F("Trang món {0} · {1} món · tồn chỉ là tham khảo trước checkout.", menuPage + 1, rows.Count);
    }
    private void ArrangeCards()
    {
        var width = Math.Max(1, products.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        var logical = width * 96F / DeviceDpi; var columns = logical >= 390 ? 2 : 1;
        foreach (Control card in products.Controls) { card.Margin = new Padding(OrbitManagementTheme.Px(this, 5)); card.Size = new Size(Math.Max(100, width / columns - card.Margin.Horizontal), OrbitManagementTheme.Px(this, 105)); }
    }
    private void UpdateTableStatus()
    {
        // Phiên bàn thuộc bàn đang chọn; đơn đã có trong SQL mang mã phiên đã lưu.
        if (order is not null) { currentSession = order.Header.SessionCode; currentSessionRevision = order.Header.SessionRevision; }
        else
        {
            var row = tableRows.FirstOrDefault(x => x.Code == table.SelectedValue as string);
            currentSession = row?.SessionCode; currentSessionRevision = row?.SessionRevision;
        }
        if (channel.SelectedValue as string != "Tai quan") { tableStatus.Text = L.T("Không dùng bàn · ") + Models.States.SalesStates.Label(channel.SelectedValue as string); return; }
        var current = tableRows.FirstOrDefault(x => x.Code == table.SelectedValue as string);
        tableStatus.Text = current is null ? L.T("Chưa chọn bàn")
            : current.Name + (current.Retained ? L.T(" · Đã trả · giữ bàn") : current.OrderCode is null ? L.T(" · Trống") : L.T(" · Đang dùng"));
    }
    // Chiều cao dải tab theo số hàng thực tế khi xuống dòng ở bề rộng hiện tại.
    private int CategoryTabsHeight(int width)
    {
        var gap = OrbitManagementTheme.Px(this, 8); var rowGap = OrbitManagementTheme.Px(this, 6);
        int used = 0, rows = 1, height = OrbitManagementTheme.Px(this, 38);
        foreach (OrbitButton tab in categoryTabs.Controls)
        {
            var size = tab.GetPreferredSize(Size.Empty);
            if (used > 0 && used + size.Width > width) { rows++; used = 0; }
            used += size.Width + gap; height = Math.Max(height, size.Height);
        }
        return rows * (height + rowGap);
    }
    protected override void OnLayout(LayoutEventArgs levent) { base.OnLayout(levent); Arrange(); }
    private void Arrange()
    {
        if (!layoutReady || changingLayout || disposed) return; changingLayout = true;
        try
        {
            int P(float n) => OrbitManagementTheme.Px(this, n);
            var left = P(24); var width = Math.Max(1, ClientSize.Width - P(48));
            var toolbarWidth = 0;
            foreach (Control control in topActions.Controls)
            {
                var w = control is OrbitFilterBox filter ? P(filter.LogicalWidth) : control.GetPreferredSize(Size.Empty).Width;
                control.SetBounds(toolbarWidth, 0, w, P(40)); toolbarWidth += w + P(8);
            }
            toolbarWidth = Math.Max(0, toolbarWidth - P(8)); var wrapHeader = width < toolbarWidth + P(180);
            var toolbarX = 0; var toolbarY = 0;
            if (toolbarWidth > width)
                foreach (Control control in topActions.Controls)
                {
                    if (toolbarX > 0 && toolbarX + control.Width > width) { toolbarX = 0; toolbarY += P(48); }
                    control.Location = new Point(toolbarX, toolbarY); toolbarX += control.Width + P(8);
                }
            eyebrow.SetBounds(left, P(16), width, P(16)); title.SetBounds(left, P(34), wrapHeader ? width : Math.Max(1, width - toolbarWidth), P(38));
            topActions.SetBounds(wrapHeader ? left : left + width - toolbarWidth, wrapHeader ? P(80) : P(30), Math.Min(width, toolbarWidth), toolbarY + P(40));
            var tableTop = wrapHeader ? topActions.Bottom + P(12) : P(80);
            var tableX = P(14); var tableY = P(10); tableCaption.SetBounds(tableX, P(12), P(90), P(32)); tableX += P(96);
            foreach (Control control in new Control[] { areaField, tableField, selectTable })
            {
                var w = control is OrbitFilterBox filter ? P(filter.LogicalWidth) : control.GetPreferredSize(Size.Empty).Width;
                if (tableX > P(14) && tableX + w > width - P(14)) { tableX = P(14); tableY += P(44); }
                control.SetBounds(tableX, tableY, Math.Min(w, width - P(28)), P(36)); tableX += w + P(8);
            }
            var wrapStatus = width - tableX < P(155);
            var statusY = tableY + (wrapStatus ? P(40) : 0);
            tableStatus.SetBounds(wrapStatus ? P(14) : tableX, statusY, Math.Max(1, width - (wrapStatus ? P(28) : tableX + P(14))), P(32));
            tableHost.SetBounds(left, tableTop, width, Math.Max(tableY + P(46), statusY + P(40)));
            var statusHeight = Math.Clamp(TextRenderer.MeasureText(state.Text, state.Font, new Size(width, 0), TextFormatFlags.WordBreak).Height + P(4), P(22), P(44));
            state.SetBounds(left, Height - statusHeight - P(6), width, statusHeight);
            var workTop = tableHost.Bottom + P(12); var workHeight = Math.Max(1, state.Top - P(8) - workTop);
            workspace.SetBounds(left, workTop, width, workHeight); compactTabs.Dock = DockStyle.None; compactTabs.SetBounds(left, workTop, width, workHeight);
            workspace.ColumnStyles[1].Width = P(16); workspace.ColumnStyles[2].Width = P(width * 96F / DeviceDpi < 840 ? 330 : 345);
            var compact = width * 96F / DeviceDpi < 680;
            if (compact && menuPanel.Parent != compactTabs.TabPages[0]) { compactTabs.TabPages[0].Controls.Add(menuPanel); compactTabs.TabPages[1].Controls.Add(orderPanel); }
            else if (!compact && menuPanel.Parent != workspace) { workspace.Controls.Add(menuPanel, 0, 0); workspace.Controls.Add(orderPanel, 2, 0); }
            compactTabs.Visible = compact; workspace.Visible = !compact;
            if (compact) { compactTabs.BringToFront(); tableHost.BringToFront(); state.BringToFront(); }
            var menuWidth = Math.Max(1, menuSurface.Width - P(24));
            var findWidth = find.GetPreferredSize(Size.Empty).Width;
            var wrapSearch = menuWidth < findWidth + P(150);
            menuTools.SetBounds(P(12), P(12), menuWidth, P(wrapSearch ? 88 : 40));
            searchBox.SetBounds(0, 0, wrapSearch ? menuWidth : Math.Max(1, menuWidth - findWidth - P(8)), P(40));
            find.SetBounds(wrapSearch ? 0 : menuWidth - findWidth, wrapSearch ? P(48) : 0, findWidth, P(40));
            categoryTabs.SetBounds(P(12), menuTools.Bottom + P(8), menuWidth, CategoryTabsHeight(menuWidth));
            menuFooter.SetBounds(P(12), menuSurface.Height - P(44), menuWidth, P(32));
            var nextWidth = next.GetPreferredSize(Size.Empty).Width; var prevWidth = previous.GetPreferredSize(Size.Empty).Width;
            next.SetBounds(menuWidth - nextWidth, 0, nextWidth, P(30)); previous.SetBounds(next.Left - P(8) - prevWidth, 0, prevWidth, P(30));
            products.SetBounds(P(8), categoryTabs.Bottom + P(6), Math.Max(1, menuSurface.Width - P(16)), Math.Max(1, menuFooter.Top - categoryTabs.Bottom - P(12)));
            ArrangeCards();
        }
        finally { changingLayout = false; }
    }
    public async Task OpenOrderAsync(string code)
    {
        if (busy || unknown) return;
        if (!await ConfirmDraftAsync(this, L.T("mở đơn khác"))) return;
        await Guard(async () => { SetBusy(true); try { await LoadOrder(code); DiscardChanges(); } finally { if (!disposed) SetBusy(false); } });
    }
    private async Task LoadOrder(string code)
    {
        var actual = await service.GetAsync(session, code, lifetime.Token); if (disposed) return;
        order = actual; orderPanel.ShowOrder(order);
        if (order is not null) channel.SelectedValue = order.Header.Channel;
        if (order is null) state.Text = L.T("Hóa đơn không tồn tại. Tải lại danh sách.");
        UpdateTableStatus();
    }
    // Cơ chế chọn bàn chuẩn của Đơn mới: giữ bàn đang chọn nếu trống, ngược lại lấy bàn trống đầu tiên.
    private TableRow? TableForNewOrder()
    {
        var current = tableRows.FirstOrDefault(x => x.Code == table.SelectedValue as string);
        return current is { OrderCode: null } ? current : tableRows.FirstOrDefault(x => x.OrderCode is null);
    }
    private async Task NewOrder()
    {
        if (!await BeginNewOrder() || channel.SelectedValue as string != "Tai quan") return;
        if (tableRows.Count == 0)
        {
            state.Text = L.T("Chưa có dữ liệu bàn. Bấm Làm mới rồi thử lại.");
            return;
        }
        var current = tableRows.FirstOrDefault(x => x.Code == table.SelectedValue as string);
        if (current is { OrderCode: null })
        {
            state.Text = L.F("Đang chọn {0} (trống). Thêm món đầu để tạo đơn.", current.Name);
            return;
        }
        var free = TableForNewOrder();
        if (free is null)
        {
            table.SelectedIndex = -1;
            state.Text = L.T("Không còn bàn trống. Chọn Mang đi/Giao hàng hoặc xử lý đơn đang mở.");
            await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
                L.T("Không còn bàn trống để tạo đơn mới. Hãy xử lý đơn đang mở hoặc chọn đơn Mang đi/Giao hàng."),
                L.T("Hết bàn trống"), MessageBoxButtons.OK, L.T("Đã hiểu"));
            return;
        }
        table.SelectedValue = free.Code;
        state.Text = L.F("Đã chuyển sang {0} (trống). Thêm món đầu để tạo đơn.", free.Name);
    }
    private async Task<bool> BeginNewOrder()
    {
        if (busy || unknown) return false;
        if (!await ConfirmDraftAsync(this, L.T("tạo đơn khác"))) return false;
        ResetDraft(); SetBusy(false); return true;
    }
    private void ResetDraft()
    {
        order = null; DiscardChanges(); orderPanel.ShowOrder(null); UpdateTableStatus();
    }
    private SalesDraftLine? FindDraftLine(SalesLine line) => draftLines.FirstOrDefault(x => x.Key == line.LineId);
    private void AddDraftLine(SalesMenuItem item)
    {
        var existing = draftLines.FirstOrDefault(x => x.Product == item.Code && x.Price == item.Price && string.IsNullOrWhiteSpace(x.Note));
        if (existing is not null) { existing.Quantity++; return; }
        draftLines.Add(new SalesDraftLine { Key = ++nextDraftKey, Product = item.Code, Name = item.Name, Price = item.Price, Quantity = 1 });
    }
    private List<SalesLine> DraftRows() => draftLines
        .Select(x => new SalesLine(x.Key, x.Product, x.Name, x.Quantity, x.Price, x.Note, x.Amount)).ToList();
    private void ShowDraft()
    {
        var header = draft ?? CurrentHeader();
        orderPanel.ShowDraft(DraftRows(), L.T("Đơn nháp"),
            L.F("{0} · Bàn {1} · Khách {2}", Models.States.SalesStates.Label(header.Channel), header.TableCode ?? "—", header.Customer ?? L.T("Vãng lai")),
            draftLines.Sum(x => x.Amount));
    }
    private async Task<bool> ConfirmDraftAsync(Control owner, string action)
    {
        if (busy || unknown) return false;
        if (order is null && draftLines.Count > 0)
        {
            var decision = await PosDraftDecisionDialog.ShowAsync(owner,
                L.F("Đơn nháp chưa ghi SQL. Giữ đơn rồi {0}, hay bỏ nháp?", action), L.T("Giữ đơn nháp?"));
            if (decision == DraftDecision.Stay) return false;
            if (decision == DraftDecision.Discard) { DiscardChanges(); return true; }
            return await FlushDraftAsync();
        }
        if (!HasUnsavedChanges) return true;
        var message = order is null ? L.T("Bỏ thông tin nháp để tiếp tục?") : L.T("Bỏ input chưa ghi để tiếp tục?");
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(owner, message, L.T("Nháp chưa lưu"), MessageBoxButtons.OKCancel) != DialogResult.OK) return false;
        DiscardChanges(); return true;
    }
    public Task<bool> ConfirmLeaveAsync(Control owner) => ConfirmDraftAsync(owner, L.T("rời màn Bán hàng"));
    private async Task<bool> FlushDraftAsync()
    {
        if (busy || unknown || order is not null || draftLines.Count == 0) return false;
        OrderHeaderInput input;
        try { input = SalesService.Normalize(draft ?? CurrentHeader()); }
        catch (Exception ex) when (ex is ArgumentException or UserOperationException) { state.Text = ex.Message; return false; }
        SetBusy(true);
        pendingId = Guid.NewGuid(); pendingCode = null; pendingOperation = "SalesCreate";
        var committed = false;
        try
        {
            var code = await service.AllocateAsync(session, lifetime.Token); pendingCode = code;
            var lines = draftLines.Select(x => new SalesDraftLineInput(x.Product, x.Quantity, x.Note, x.Price)).ToArray();
            await service.CreateDraftAsync(session, new(code, pendingId, input, lines), lifetime.Token);
            committed = true;
            draftLines.Clear(); draft = null; headerDraft = null; orderPanel.CancelNoteEdit();
            await LoadOrder(code); if (disposed) return false;
            await LoadTables();
            if (!disposed) state.Text = L.F("Đã lưu đơn {0} và tải lại từ SQL.", code);
            return true;
        }
        catch (UserOperationException ex)
        {
            unknown = committed || ex.Error.RequiresReconciliation;
            if (!disposed)
            {
                state.Text = ex.Message + (unknown ? L.F(" · {0} · Kiểm tra kết quả", pendingCode ?? "") : "");
                if (!unknown && ex.Error.Number == 52104 && draft?.Channel == "Tai quan") await HandleDraftConflictAsync();
            }
            return false;
        }
        catch (OperationCanceledException)
        {
            unknown = true;
            if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", pendingCode ?? "");
            return false;
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task HandleDraftConflictAsync()
    {
        var tableCode = draft?.TableCode;
        var choice = await PosDraftConflictDialog.ShowAsync(this,
            L.F("Bàn {0} vừa có đơn ở phiên khác. Nháp vẫn được giữ; chọn cách xử lý.", tableCode ?? "—"), L.T("Bàn đã có đơn"));
        if (choice == DraftConflictChoice.OpenExisting)
        {
            await LoadTables();
            var row = tableRows.FirstOrDefault(x => x.Code == tableCode && x.OrderCode is not null)
                ?? tableRows.FirstOrDefault(x => x.OrderCode is not null);
            if (row?.OrderCode is { } code) { DiscardChanges(); await LoadOrder(code); }
            else if (!disposed) state.Text = L.T("Không tải được đơn hiện có; nháp vẫn được giữ.");
        }
        else if (choice == DraftConflictChoice.ChangeTable)
        {
            await LoadTables();
            if (!disposed) state.Text = L.T("Chọn bàn trống khác rồi bấm Chọn bàn; nháp được giữ.");
        }
        else if (choice == DraftConflictChoice.Discard)
        {
            DiscardChanges(); orderPanel.ShowOrder(null); UpdateTableStatus();
            if (!disposed) state.Text = L.T("Đã bỏ nháp; không ghi SQL.");
        }
    }
    private async Task SelectTable()
    {
        if (busy || unknown || table.SelectedValue is not string code) return;
        await OpenTableAsync(code);
    }
    public async Task OpenTableAsync(string code)
    {
        if (busy || unknown) return;
        var current = await tables.SelectAsync(session, code, lifetime.Token); if (disposed || current is null) return;
        if (current.OrderCode is not null) { await OpenOrderAsync(current.OrderCode); return; }
        if (draftLines.Count > 0 && draft?.Channel == "Tai quan")
        {
            channel.SelectedValue = "Tai quan"; table.SelectedValue = code;
            draft = (draft ?? new("Tai quan", code)) with { TableCode = code };
            ShowDraft(); SetBusy(false);
            state.Text = L.F("Đã chuyển nháp sang {0}; chưa ghi SQL.", current.Name);
            return;
        }
        if (!await BeginNewOrder()) return;
        channel.SelectedValue = "Tai quan"; table.SelectedValue = code; draft = new("Tai quan", code);
        state.Text = current.Retained
            ? L.T("Bàn đang giữ · khách đã trả. Thêm món sẽ tạo hóa đơn mới trong cùng phiên; trả bàn khi khách về.")
            : L.F("Đã chọn {0}. Thêm món đầu mới tạo đơn.", current.Name);
    }
    private OrderHeaderInput CurrentHeader() => order is null ? draft ?? new(channel.SelectedValue as string ?? "Mang di", (channel.SelectedValue as string) == "Tai quan" ? table.SelectedValue as string : null) :
        new(order.Header.Channel, order.Header.TableCode, order.Header.Customer, order.Header.Promotion, order.Header.Partner, order.Header.Recipient, order.Header.Phone, order.Header.Address, order.Header.Ship);
    private async Task EditHeader()
    {
        if (busy || unknown) return;
        using var dialog = new OrderHeaderDialog(headerDraft ?? CurrentHeader(), order?.Header.Created ?? DateTime.Now, service, customers, loyalty, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK || dialog.Result is null) return;
        if (order is null)
        {
            draft = dialog.Result;
            if (draftLines.Count > 0) { ShowDraft(); SetBusy(false); }
            state.Text = L.T("Thông tin còn là nháp; thêm món rồi chọn Giữ để lưu.");
            return;
        }
        var h = order.Header; var input = dialog.Result; headerDraft = input;
        await Write(h.Code, "SalesHeader", id => service.HeaderAsync(session, new(h.Code, h.Revision, id, input), lifetime.Token));
    }
    private async Task AddProduct(SalesMenuItem item)
    {
        if (busy || unknown || !item.CanAdd || order is not null && !order.Header.CanEdit) return;
        if (order is null)
        {
            var input = CurrentHeader();
            if (input.Channel == "Tai quan" && input.TableCode is null)
            {
                state.Text = L.T("Chọn bàn trước khi thêm món cho đơn tại quán.");
                return;
            }
            if (input.Channel == "Giao hang" && (input.Recipient is null || input.Phone is null || input.Address is null))
            {
                await EditHeader(); if (draft is null) return;
                input = CurrentHeader();
            }
            draft = input;
            AddDraftLine(item);
            ShowDraft(); SetBusy(false);
            state.Text = L.T("Đã thêm vào nháp; chưa ghi SQL. Chọn Giữ hoặc Thanh toán để lưu.");
            return;
        }
        var h = order.Header;
        var existing = order.Lines.FirstOrDefault(x => x.Product == item.Code && x.Price == item.Price && string.IsNullOrWhiteSpace(x.Note));
        if (existing is not null)
            await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Update", existing.LineId, Quantity: existing.Quantity + 1, Note: existing.Note), lifetime.Token));
        else
            await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Add", Product: item.Code, Quantity: 1), lifetime.Token));
    }
    private async Task ChangeQuantity(SalesLine line, int delta)
    {
        if (busy || unknown) return;
        if (order is null)
        {
            if (FindDraftLine(line) is not { } draftLine) return;
            if (draftLine.Quantity + delta < 1) { state.Text = L.T("Số lượng tối thiểu là 1. Dùng thùng rác để xóa dòng."); return; }
            draftLine.Quantity += delta; ShowDraft(); SetBusy(false);
            return;
        }
        if (order.Header.CanEdit != true) return;
        var quantity = line.Quantity + delta;
        if (quantity < 1)
        {
            state.Text = L.T("Số lượng tối thiểu là 1. Dùng thùng rác để xóa dòng.");
            return;
        }
        var h = order.Header;
        await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Update", line.LineId, Quantity: quantity, Note: line.Note), lifetime.Token));
    }
    private async Task LoadNoteHistory()
    {
        // Gợi ý chỉ là phụ trợ: lỗi đọc lịch sử không chặn POS và không đổi trạng thái màn.
        try
        {
            var notes = await service.NoteSuggestionsAsync(session, "", lifetime.Token);
            if (disposed) return;
            noteHistory = notes; orderPanel.SetNoteHistory(noteHistory);
        }
        catch (UserOperationException) { }
        catch (OperationCanceledException) { }
    }
    private void RememberNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;
        var text = note.Trim();
        if (noteHistory.Any(x => string.Equals(x, text, StringComparison.CurrentCultureIgnoreCase))) return;
        noteHistory = new[] { text }.Concat(noteHistory).Take(30).ToArray();
        orderPanel.SetNoteHistory(noteHistory);
    }
    private async Task SaveNote(SalesLine line, string? note)
    {
        if (busy || unknown) return;
        if (order is null)
        {
            if (FindDraftLine(line) is { } draftLine) { draftLine.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(); RememberNote(draftLine.Note); ShowDraft(); SetBusy(false); }
            return;
        }
        if (order.Header.CanEdit != true) return;
        var h = order.Header;
        await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Update", line.LineId, Quantity: line.Quantity, Note: note), lifetime.Token));
        RememberNote(note);
    }
    private async Task DeleteLine(SalesLine line)
    {
        if (busy || unknown) return;
        if (order is null)
        {
            if (draftLines.RemoveAll(x => x.Key == line.LineId) > 0)
            {
                if (draftLines.Count > 0) ShowDraft(); else orderPanel.ShowOrder(null);
                SetBusy(false);
            }
            return;
        }
        if (order.Header.CanEdit != true) return;
        var h = order.Header;
        await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Delete", line.LineId), lifetime.Token));
    }
    private async Task Write(string code, string operation, Func<Guid, Task<SalesWriteResult>> send)
    {
        if (busy || unknown) return; pendingId = Guid.NewGuid(); pendingCode = code; pendingOperation = operation; SetBusy(true);
        var committed = false;
        try
        {
            await send(pendingId); committed = true; DiscardChanges(); await LoadOrder(code); await LoadTables();
            if (!disposed) state.Text = L.T("Đã ghi và tải kết quả từ SQL.");
        }
        catch (UserOperationException ex) { unknown = committed || ex.Error.RequiresReconciliation; if (!disposed) state.Text = ex.Message + (unknown ? L.F(" · {0} · Kiểm tra kết quả", code) : ""); }
        catch (OperationCanceledException) { unknown = true; if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", code); }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task Reconcile()
    {
        if (busy || pendingCode is null || pendingOperation is null) return; SetBusy(true);
        try
        {
            var receipt = await service.ReconcileAsync(session, pendingId, pendingCode, pendingOperation, lifetime.Token);
            await LoadOrder(pendingCode); if (disposed) return;
            unknown = false; if (receipt is not null) DiscardChanges(); state.Text = receipt is null ? L.T("Đã phân xử yêu cầu cũ; chưa có receipt. Input được giữ để tham khảo; kiểm tra đơn và xác nhận lại nếu cần.") : L.T("Đã xác minh giao dịch. Dữ liệu hiện tại đã tải lại.");
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task OpenOrders()
    {
        if (busy || unknown) return;
        using var dialog = new OpenOrdersDialog(service, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.SelectedCode is not null) await OpenOrderAsync(dialog.SelectedCode);
    }
    // Chốt "Chờ thanh toán": khóa sửa món nhưng vẫn thu/hủy được; in tạm tính chỉ là phụ trợ sau commit.
    private async Task RequestPayment()
    {
        if (busy || unknown || order?.Header.CanEdit != true) return;
        var h = order.Header;
        if (HasUnsavedChanges) { state.Text = L.T("Lưu hoặc bỏ ghi chú đang nhập trước khi chốt đơn."); return; }
        if (order.Lines.Count == 0) { state.Text = L.T("Đơn cần có món trước khi yêu cầu thanh toán."); return; }
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
            L.F("Chốt đơn {0} để chờ thanh toán? Sau bước này không sửa món, chỉ thu hoặc hủy; quản lý có thể mở lại.", h.Code),
            L.T("Yêu cầu thanh toán"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        pendingId = Guid.NewGuid(); pendingCode = h.Code; pendingOperation = "SalesRequestPay";
        var committed = false; var message = L.F("Đơn {0} đang chờ thanh toán; đã khóa sửa món.", h.Code);
        SetBusy(true);
        try
        {
            await service.RequestPaymentAsync(session, new(h.Code, h.Revision, pendingId), lifetime.Token);
            committed = true;
            DiscardChanges(); await LoadOrder(h.Code); await LoadTables();
        }
        catch (UserOperationException ex)
        {
            unknown = committed || ex.Error.RequiresReconciliation;
            if (!disposed) state.Text = ex.Message + (unknown ? L.F(" · {0} · Kiểm tra kết quả", h.Code) : "");
            return;
        }
        catch (OperationCanceledException)
        {
            unknown = true;
            if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", h.Code);
            return;
        }
        finally { if (!disposed) SetBusy(false); }
        if (disposed) return;
        state.Text = message;
        try
        {
            if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
                L.F("Đơn {0} đã chuyển sang chờ thanh toán. In tạm tính?", h.Code), L.T("In tạm tính"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var detail = invoices is null ? null : await invoices.DetailAsync(session, h.Code, lifetime.Token);
            if (disposed) return;
            if (invoices is null || detail is null) throw new InvalidOperationException(L.T("Chưa tải được dữ liệu hóa đơn để in."));
            state.Text = message + (await InvoicePrinting.PreviewAsync(this, detail, invoices, session, reprint: false)
                ? L.T(" · Đã mở xem trước tạm tính.") : L.T(" · Đã đóng xem trước tạm tính."));
        }
        catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception
            or InvalidOperationException or IOException or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
        {
            // Đã đổi trạng thái thành công; lỗi in không được làm mất trạng thái đã ghi.
            if (!disposed) state.Text = message + L.T(" · Chưa in được tạm tính. ") + ex.Message;
        }
    }
    private async Task ReopenOrder()
    {
        if (busy || unknown || order?.Header.Status != "Cho thanh toan") return;
        var h = order.Header;
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
            L.F("Mở lại đơn {0} để sửa món và ưu đãi? Chỉ quản lý được mở lại đơn chờ thanh toán.", h.Code),
            L.T("Mở lại đơn"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        await Write(h.Code, "SalesReopen", id => service.ReopenAsync(session, new(h.Code, h.Revision, id), lifetime.Token));
    }
    private async Task CancelOrder()
    {
        if (order?.Header.IsPayable != true || busy || unknown) return; var h = order.Header;
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.F("Hủy đơn mở {0}, bàn {1}, {2}? Không hoàn tiền/kho. Nếu đã pha, nhờ quản lý/kho lập xuất hủy ở phân hệ kho khi có chức năng T21.", h.Code, h.TableCode ?? "—", SalesUi.Money(h.Payable)), L.T("Hủy đơn mở"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        await Write(h.Code, "SalesCancel", id => service.CancelAsync(session, h, id, lifetime.Token));
        if (disposed || unknown || order?.Header.SessionCode is not int sessionCode || order.Header.SessionRevision is not { Length: 8 } revision) return;
        if (!await AskCloseTableAsync()) return;
        var note = await CloseSessionNowAsync(sessionCode, revision, L.T("Đã hủy đơn; chưa trả bàn được. "));
        if (!disposed) state.Text += note;
        await LoadTables();
    }
    // Trả bàn là hành động thủ công: chỉ đóng phiên khi không còn đơn Chưa/Chờ (SQL kiểm tra lần cuối).
    private async Task CloseTable()
    {
        if (busy || unknown || currentSession is not int sessionCode || currentSessionRevision is not { Length: 8 } revision) return;
        var tableCode = order?.Header.TableCode ?? table.SelectedValue as string;
        var tableName = tableRows.FirstOrDefault(x => x.Code == tableCode)?.Name ?? tableCode ?? "—";
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.F("Trả bàn {0}? Bàn sẽ trống cho khách mới.", tableName), L.T("Trả bàn"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        SetBusy(true);
        try
        {
            await tables.CloseSessionAsync(session, new(sessionCode, revision, Guid.NewGuid()), lifetime.Token);
        }
        catch (UserOperationException ex)
        {
            if (!disposed) state.Text = ex.Message;
            await LoadTables();
            return;
        }
        catch (OperationCanceledException)
        {
            if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", sessionCode);
            await LoadTables();
            return;
        }
        finally { if (!disposed) SetBusy(false); }
        ResetDraft(); await LoadTables();
        if (!disposed) state.Text = L.F("Đã trả bàn phiên {0}.", sessionCode);
    }
    private async Task<bool> AskCloseTableAsync() => await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
        L.T("Trả bàn ngay?"), L.T("Trả bàn"), MessageBoxButtons.YesNo) == DialogResult.Yes;
    private async Task<string> CloseSessionNowAsync(int sessionCode, byte[] revision, string failurePrefix)
    {
        try
        {
            await tables.CloseSessionAsync(session, new(sessionCode, revision, Guid.NewGuid()), lifetime.Token);
            return L.F(" · Đã trả bàn phiên {0}.", sessionCode);
        }
        catch (UserOperationException ex) { return " · " + failurePrefix + ex.Message; }
        catch (OperationCanceledException) { return " · " + failurePrefix + L.T("Chưa xác định kết quả; tải lại bàn để đối chiếu."); }
    }
    private async Task Checkout()
    {
        if (busy || unknown) return;
        // Thanh toán yêu cầu đơn đã có trong SQL: đơn nháp phải lưu trước, lỗi thì giữ nháp.
        if (order is null && draftLines.Count > 0 && !await FlushDraftAsync()) return;
        if (HasUnsavedChanges || order?.Header.IsPayable != true) return; SetBusy(true);
        try
        {
            var current = await service.GetAsync(session, order.Header.Code, lifetime.Token); if (current is null) return;
            var preview = await service.PreviewAsync(session, current.Header, lifetime.Token); var methods = await service.PaymentMethodsAsync(session, lifetime.Token);
            if (disposed) return;
            using var dialog = new CheckoutDialog(service, session, current, preview, methods);
            if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.ResultOrder is not null)
            {
                await FinishCheckout(dialog.ResultOrder, dialog.Change);
            }
            else await LoadOrder(current.Header.Code);
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task FinishCheckout(SalesOrderDto paid, decimal? change)
    {
        var message = L.F("Đã thanh toán {0} · Thu {1}", paid.Header.Code, SalesUi.Money(paid.Payments.Sum(x => x.Amount)))
            + (change is not null ? L.F(" · Thối {0}", SalesUi.Money(change.Value)) : "");
        var printRequested = false;
        try
        {
            printRequested = await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.F("Thanh toán thành công {0}. Bạn có muốn in hóa đơn không?", paid.Header.Code),
                L.T("In hóa đơn"), MessageBoxButtons.YesNo) == DialogResult.Yes;
            var detail = invoices is null ? null : await invoices.DetailAsync(session, paid.Header.Code, lifetime.Token);
            if (disposed) return;
            if (detail is not null) message += L.F(" · Điểm mua đã cộng {0}", detail.Points);
            if (printRequested)
            {
                if (invoices is null || detail is null) throw new InvalidOperationException(L.T("Chưa tải được dữ liệu hóa đơn để in."));
                message += await InvoicePrinting.PreviewAsync(this, detail, invoices, session, reprint: false)
                    ? L.T(" · Đã mở xem trước hóa đơn.") : L.T(" · Đã đóng xem trước hóa đơn.");
            }
        }
        catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception
            or InvalidOperationException or IOException or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
        {
            message += (printRequested ? L.T(" · Chưa in được hóa đơn. ") : L.T(" · Chưa tải được chi tiết hóa đơn. ")) + ex.Message;
        }
        finally
        {
            // Chỉ vào đây sau kết quả đã chốt; lỗi/hủy in không giữ đơn đã trả trên POS.
            if (!disposed) ResetDraft();
        }
        // Đã thu tiền xong mới hỏi trả bàn; lỗi trả bàn không làm mất kết quả thanh toán.
        if (!disposed && paid.Header.SessionCode is int closedSession && paid.Header.SessionRevision is { Length: 8 } closedRevision
            && await AskCloseTableAsync())
            message += await CloseSessionNowAsync(closedSession, closedRevision, L.T("Đã thu tiền; chưa trả bàn được. "));
        if (disposed) return;
        try { await LoadTables(); }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException)
        { message += L.T(" · Chưa tải lại được bàn. ") + ex.Message; }
        if (!disposed) state.Text = message + L.T(" Tra cứu hoặc in lại tại Lịch sử hóa đơn.");
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (busy || unknown || FindForm() is Forms.MainForm { Dialogs.IsOpen: true } || FindForm()?.OwnedForms.Any(x => x.Visible) == true) return base.ProcessCmdKey(ref msg, keyData);
        if (keyData == Keys.F2) { search.Focus(); return true; }
        if (keyData == Keys.F3) { _ = Guard(EditHeader); return true; }
        if (keyData == Keys.F4) { _ = Guard(OpenOrders); return true; }
        if (keyData == Keys.F8) { _ = Guard(Checkout); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed) { disposed = true; lifetime.Cancel(); lifetime.Dispose(); tips.Dispose(); }
        base.Dispose(disposing);
    }
}




