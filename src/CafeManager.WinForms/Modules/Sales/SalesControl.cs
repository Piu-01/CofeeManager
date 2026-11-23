using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.Requests.Tables;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Forms;

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
    private readonly Button find, previous, next;
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
    private readonly Button printTemp, pay, header, cancel, closeTable, reconcile;
    private readonly List<Button> actions = [];
    private readonly CancellationTokenSource lifetime = new();
    private SalesOrderDto? order;
    // Header của đơn sẽ tạo ở lần thêm món đầu; không phải nháp dòng món, rời màn không hỏi.
    private OrderHeaderInput? context;
    private OrderHeaderInput? headerDraft;
    private Guid pendingId;
    private string? pendingCode, pendingOperation;
    private IReadOnlyList<string> noteHistory = [];
    // Task của lần nạp gần nhất để luồng mở bàn từ màn Bàn chờ nạp xong thay vì bị guard busy nuốt mất.
    private Task loading = Task.CompletedTask;
    private bool busy, unknown, initialized, changingLayout, disposed, layoutReady, suppressChannel, suppressTableOpen, openingTable;
    private int menuGeneration, menuPage;
    // Thông tin F3 của đơn mới chưa tạo chỉ nằm trong context, chưa có bản ghi SQL.
    private bool HasMeaningfulNewOrderContext => order is null && context is { } c &&
        (!string.IsNullOrWhiteSpace(c.Customer) || !string.IsNullOrWhiteSpace(c.Partner) ||
         !string.IsNullOrWhiteSpace(c.Recipient) || !string.IsNullOrWhiteSpace(c.Phone) ||
         !string.IsNullOrWhiteSpace(c.Address) || c.Ship != 0);
    public bool HasUnsavedChanges => headerDraft is not null || orderPanel.HasPendingNote || HasMeaningfulNewOrderContext;
    public bool IsWriting => busy || unknown;
    public void DiscardChanges()
    {
        context = null; headerDraft = null; orderPanel.CancelNoteEdit();
        // Không còn nháp dòng món; chỉ cần làm sạch panel khi chưa có đơn đã lưu.
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
        var reload = Action(L.T("Làm mới"), ReloadAll, icon: "refresh");
        channelField = new OrbitFilterBox(channel) { LogicalWidth = 110 };
        areaField = new OrbitFilterBox(area) { LogicalWidth = 125 }; tableField = new OrbitFilterBox(table) { LogicalWidth = 180 };
        topActions.Controls.AddRange([channelField, newOrder, open, reload]);
        tableHost.Controls.AddRange([tableCaption, areaField, tableField, tableStatus]);
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
        printTemp = Action(L.T("In tạm"), PrintTemp, ManagementButtonStyle.Primary, "ticket");
        pay = Action(L.T("F8 · Thanh toán"), Checkout, ManagementButtonStyle.Primary, "receipt"); header = Action(L.T("F3 · Khách / Ưu đãi"), EditHeader, ManagementButtonStyle.Segment, "user");
        cancel = Action(L.T("Hủy đơn mở"), CancelOrder, ManagementButtonStyle.Danger, "trash");
        closeTable = Action(L.T("Trả bàn"), CloseTable, ManagementButtonStyle.Segment, "armchair");
        reconcile = SalesUi.Button(L.T("Kiểm tra kết quả"), async (_, _) => await Guard(Reconcile));
        reconcile.MinimumSize = Size.Empty; OrbitManagementTheme.Button((OrbitButton)reconcile, ManagementButtonStyle.Action, "history");
        orderPanel.HeaderActions.Controls.AddRange([header, closeTable, cancel]); orderPanel.PaymentActions.Controls.AddRange([printTemp, pay, reconcile]);
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
        // Chọn bàn trong ô chọn bàn là mở bàn luôn (không còn nút "Chọn bàn");
        // gán bàn bằng code dùng SetTableSelection để không tự mở trùng.
        table.SelectedValueChanged += async (_, _) => await Guard(OpenSelectedTableAsync);
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
            if (suppressChannel) return;
            UpdateTableStatus();
            if (busy || unknown || order is not null || channel.SelectedValue is not string target) return;
            var previous = context?.Channel ?? target;
            if (target == previous) return;
            // Đơn mới chưa có trong SQL: đổi kênh chỉ đổi ngữ cảnh, không ghi DB.
            var header = context ?? new OrderHeaderInput(previous, previous == "Tai quan" ? table.SelectedValue as string : null);
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
            SetTableSelection(targetTable);
            context = header with
            {
                Channel = target, TableCode = targetTable,
                Partner = null, Recipient = null, Phone = null, Address = null, Ship = 0
            };
            orderPanel.ShowOrder(null); UpdateTableStatus(); SetBusy(false);
            var tableName = targetTable is null ? null : tableRows.FirstOrDefault(x => x.Code == targetTable)?.Name;
            state.Text = target == "Tai quan"
                ? L.F("Đã chọn {0} cho đơn mới. Thêm món đầu để tạo đơn.", tableName ?? "—")
                : L.F("Đã chuyển sang {0}. Thêm món đầu để tạo đơn.", Models.States.SalesStates.Label(target));
        };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; menuPage = 0; await Guard(LoadMenu); } };
        Load += async (_, _) => { if (!initialized) { initialized = true; loading = Guard(ReloadAll); await loading; } };
        VisibleChanged += async (_, _) => { if (Visible && initialized && !disposed && !busy && !unknown) { loading = Guard(ReloadAll); await loading; } };
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
        products.Enabled = !busy && !unknown && (order is null || order.Header.CanAddLine);
        // Đơn đã In tạm thì cùng nút này thành "In lại"; chưa in thì in tạm tính và lưu đơn Chờ thanh toán.
        printTemp.Text = L.T(order?.Header.IsPending == true ? "In lại" : "In tạm");
        printTemp.Enabled = !busy && !unknown && order is not null && !HasUnsavedChanges && order.Lines.Count > 0
            && (order.Header.IsPending || order.Header.CanEdit && order.Header.PreviewValid);
        pay.Enabled = !busy && !unknown && order?.Header.IsPayable == true && !HasUnsavedChanges
            && order.Lines.Count > 0 && order.Header.PreviewValid;
        cancel.Enabled = !busy && !unknown && order?.Header.IsPayable == true;
        closeTable.Enabled = !busy && !unknown && currentSession is not null && currentSessionRevision is { Length: 8 }
            && order?.Header.IsPayable != true && !HasUnsavedChanges;
        header.Enabled = !busy && !unknown && (order is null || order.Header.CanEditHeader);
        orderPanel.SetInteractive(!busy && !unknown && order?.Header.CanEdit == true);
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
            if (order is not null)
            {
                await LoadOrder(order.Header.Code);
                if (!disposed) state.Text = L.T("Đã tải từ SQL · ") + DateTime.Now.ToString("HH:mm");
            }
            else if (!disposed) state.Text = await StartNewOrderAsync(false);
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
        if (selected is not null) SetTableSelection(selected);
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
            foreach (Control control in new Control[] { areaField, tableField })
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
        if (!await ConfirmPendingInputAsync(this, L.T("mở đơn khác"), false)) return;
        await Guard(async () => { SetBusy(true); try { await LoadOrder(code); DiscardChanges(); } finally { if (!disposed) SetBusy(false); } });
        // Mở đơn xong phải thay câu gợi ý của ngữ cảnh trước (bàn trống/tải lại) để không còn chữ cũ.
        if (!disposed && order is not null) state.Text = L.F("Đã mở đơn {0}.", code);
    }
    private async Task LoadOrder(string code)
    {
        var actual = await service.GetAsync(session, code, lifetime.Token); if (disposed) return;
        order = actual; orderPanel.ShowOrder(order);
        if (order is not null)
        {
            channel.SelectedValue = order.Header.Channel;
            // Đồng bộ ô chọn bàn theo bàn của đơn vừa mở (kể cả khi mở từ sơ đồ bàn) để nhãn bàn khớp đơn.
            if (order.Header.Channel == "Tai quan" && order.Header.TableCode is { } tableCode) SetTableSelection(tableCode);
        }
        if (order is null) state.Text = L.T("Hóa đơn không tồn tại. Tải lại danh sách.");
        UpdateTableStatus();
    }
    // Cơ chế chọn bàn chuẩn của Đơn mới: giữ bàn đang chọn nếu trống, ngược lại lấy bàn trống đầu tiên.
    private TableRow? TableForNewOrder()
    {
        var current = tableRows.FirstOrDefault(x => x.Code == table.SelectedValue as string);
        return current is { OrderCode: null } ? current : tableRows.FirstOrDefault(x => x.OrderCode is null);
    }
    // Bắt đầu ngữ cảnh đơn mới: tự chọn bàn trống khi Tại quán; không ghi SQL, không tạo hóa đơn rỗng.
    private async Task<string> StartNewOrderAsync(bool notifyNoTable)
    {
        order = null; headerDraft = null; context = null; orderPanel.CancelNoteEdit(); orderPanel.ShowOrder(null);
        var target = channel.SelectedValue as string ?? "Tai quan";
        if (target != "Tai quan")
        {
            SetTableSelection(null); context = new(target, null);
            UpdateTableStatus(); SetBusy(false);
            return L.F("Bắt đầu đơn {0} mới. Thêm món đầu để tạo đơn.", Models.States.SalesStates.Label(target));
        }
        if (tableRows.Count == 0)
        {
            SetTableSelection(null); context = new("Tai quan", null);
            UpdateTableStatus(); SetBusy(false);
            return L.T("Chưa có dữ liệu bàn. Bấm Làm mới rồi thử lại.");
        }
        var free = TableForNewOrder();
        if (free is null)
        {
            SetTableSelection(null); context = new("Tai quan", null);
            UpdateTableStatus(); SetBusy(false);
            if (notifyNoTable) await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
                L.T("Không còn bàn trống để tạo đơn mới. Hãy xử lý đơn đang mở hoặc chọn đơn Mang đi/Giao hàng."),
                L.T("Hết bàn trống"), MessageBoxButtons.OK, L.T("Đã hiểu"));
            return L.T("Không còn bàn trống. Chọn Mang đi/Giao hàng hoặc xử lý đơn đang mở.");
        }
        SetTableSelection(free.Code); context = new("Tai quan", free.Code);
        UpdateTableStatus(); SetBusy(false);
        return L.F("Đã chọn {0} (trống). Thêm món đầu để tạo đơn.", free.Name);
    }
    private async Task NewOrder()
    {
        if (!await ConfirmPendingInputAsync(this, L.T("tạo đơn khác"), false)) return;
        state.Text = await StartNewOrderAsync(true);
    }
    // Chỉ hỏi khi có input thật chưa ghi: header F3 (đơn đã lưu hoặc đơn mới) hoặc ghi chú đang mở.
    // keepNewOrderContext: rời màn có thể giữ F3 đơn mới trong màn cache; chuyển đơn/bàn thì không có nơi lưu.
    private async Task<bool> ConfirmPendingInputAsync(Control owner, string action, bool keepNewOrderContext)
    {
        if (busy || unknown) return false;
        if (!HasUnsavedChanges) return true;
        var header = headerDraft;
        var note = orderPanel.PendingNote();
        if (header is null && note is null)
        {
            // Editor ghi chú mở nhưng chưa sửa gì: không có gì để hỏi.
            if (!HasMeaningfulNewOrderContext) return true;
            var contextChoice = keepNewOrderContext
                ? await OrbitSaveChangesDialog.ShowAsync(owner,
                    L.T("Đơn mới chưa có món nên thông tin chỉ giữ trong màn Bán hàng. Giữ lại khi rời?"),
                    L.T("Thông tin đơn mới"), L.T("Giữ thông tin"), L.T("Bỏ thông tin"), L.T("Ở lại"))
                : await OrbitSaveChangesDialog.ShowAsync(owner,
                    L.T("Đơn mới chưa có món nên không lưu được thông tin đã nhập. Bỏ thông tin và tiếp tục?"),
                    L.T("Thông tin đơn mới"), null, L.T("Bỏ thông tin"), L.T("Ở lại"));
            if (contextChoice == SaveChangesChoice.Stay) return false;
            if (contextChoice == SaveChangesChoice.Discard) DiscardChanges();
            return true;
        }
        var choice = await OrbitSaveChangesDialog.ShowAsync(owner,
            L.F("Có thông tin chưa ghi trong đơn. Lưu trước khi {0}?", action),
            L.T("Thay đổi chưa lưu"), L.T("Lưu"), L.T("Bỏ thay đổi"), L.T("Ở lại"));
        if (choice == SaveChangesChoice.Stay) return false;
        if (choice == SaveChangesChoice.Discard) { DiscardChanges(); return true; }
        // Ghi chú trước vì Write thành công làm sạch draft; header đã chụp từ trước để ghi tiếp.
        if (note is { } pending && !await SaveNote(pending.Line, pending.Note)) return false;
        if (header is not null && !await SaveHeaderDraftAsync(header)) return false;
        return true;
    }
    // Lưu input F3 của đơn đã lưu trước khi rời/chuyển; false nếu ghi lỗi để ở lại POS.
    private async Task<bool> SaveHeaderDraftAsync(OrderHeaderInput input)
    {
        if (order?.Header is not { } h) return true;
        return await Write(h.Code, "SalesHeader", id => service.HeaderAsync(session, new(h.Code, h.Revision, id, input), lifetime.Token));
    }
    public Task<bool> ConfirmLeaveAsync(Control owner) => ConfirmPendingInputAsync(owner, L.T("rời màn Bán hàng"), true);
    // Món đầu tiên tạo header + dòng đầu trong một transaction (contract T14); chỉ gọi khi bàn đang trống.
    private async Task CreateFirstLine(OrderHeaderInput input, SalesMenuItem item)
    {
        pendingId = Guid.NewGuid(); pendingCode = null; pendingOperation = "SalesCreate";
        SetBusy(true); var committed = false;
        try
        {
            var code = await service.AllocateAsync(session, lifetime.Token); pendingCode = code;
            await service.CreateAsync(session, new(code, pendingId, input, item.Code, 1, null), lifetime.Token);
            committed = true; context = input;
            await LoadOrder(code); if (disposed) return;
            await LoadTables();
            if (!disposed) state.Text = L.F("Đã tạo đơn {0} và tải lại từ SQL.", code);
        }
        catch (UserOperationException ex)
        {
            unknown = committed || ex.Error.RequiresReconciliation;
            if (disposed) return;
            state.Text = ex.Message + (unknown ? L.F(" · {0} · Kiểm tra kết quả", pendingCode ?? "") : "");
            if (!unknown && ex.Error.Number == 52104) await HandleOccupiedTableAsync(input.TableCode);
        }
        catch (OperationCanceledException)
        {
            unknown = true;
            if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", pendingCode ?? "");
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task HandleOccupiedTableAsync(string? tableCode)
    {
        await LoadTables();
        var row = tableRows.FirstOrDefault(x => x.Code == tableCode && x.OrderCode is not null)
            ?? tableRows.FirstOrDefault(x => x.OrderCode is not null);
        if (row?.OrderCode is not { } code)
        {
            if (!disposed) state.Text = L.T("Bàn đang có đơn. Chọn bàn trống khác hoặc chọn bàn đang có đơn trong ô chọn bàn để mở đơn hiện có.");
            return;
        }
        var name = tableRows.FirstOrDefault(x => x.Code == tableCode)?.Name ?? tableCode ?? "—";
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this,
            L.F("Bàn {0} vừa có đơn ở phiên khác. Mở đơn hiện có?", name), L.T("Bàn đã có đơn"), MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            DiscardChanges(); await LoadOrder(code);
            if (!disposed) state.Text = L.F("Đã mở đơn {0}.", code);
        }
        else if (!disposed)
            state.Text = L.T("Đã giữ nguyên bàn đang chọn. Chọn bàn trống khác hoặc chọn bàn đang có đơn trong ô chọn bàn để mở đơn hiện có.");
    }
    // Bàn thực của ngữ cảnh đang mở: đơn đã lưu ưu tiên, ngược lại là bàn của đơn mới; không đọc combo (đã bị binding đổi).
    private string? ActiveTableCode() => order?.Header.TableCode ?? (context is { Channel: "Tai quan" } open ? open.TableCode : null);
    // Gán bàn bằng code (tải lại, Đơn mới, đổi kênh, mở đơn) không kích hoạt tự mở bàn.
    private void SetTableSelection(string? code)
    {
        suppressTableOpen = true;
        try { if (code is null) table.SelectedIndex = -1; else table.SelectedValue = code; }
        finally { suppressTableOpen = false; }
    }
    // Người dùng chọn bàn trong ô chọn bàn: mở bàn/đơn ngay, thay cho nút "Chọn bàn" cũ.
    private async Task OpenSelectedTableAsync()
    {
        if (suppressTableOpen || openingTable || !initialized || busy || unknown || disposed) return;
        if (table.SelectedValue is not string code || string.IsNullOrWhiteSpace(code)) return;
        // Bàn đang mở sẵn: chỉ cập nhật trạng thái, không mở lại đơn.
        if (order?.Header.TableCode == code) return;
        if (order is null && context is { Channel: "Tai quan" } open && open.TableCode == code) return;
        openingTable = true;
        table.Enabled = area.Enabled = false;
        try { await OpenTableAsync(code); }
        finally
        {
            openingTable = false;
            if (!disposed) table.Enabled = area.Enabled = !busy && !unknown;
        }
    }
    // Mở bàn sau khi lần nạp đang chạy xong (chuyển từ màn Bàn sang POS); tránh mất yêu cầu do busy.
    public async Task OpenTableAfterLoadAsync(string code)
    {
        if (!loading.IsCompleted)
        {
            try { await loading; }
            catch { /* Lỗi nạp đã hiện ở state; vẫn mở bàn theo yêu cầu của người dùng. */ }
        }
        if (disposed || busy || unknown) return;
        await OpenTableAsync(code);
    }
    public async Task OpenTableAsync(string code)
    {
        if (busy || unknown) return;
        var current = await tables.SelectAsync(session, code, lifetime.Token); if (disposed || current is null) return;
        // Ô chọn bàn đã bị binding đổi sang bàn vừa bấm; giữ bàn thực của ngữ cảnh hiện tại để trả lại khi hủy.
        var previous = ActiveTableCode();
        if (current.OrderCode is not null)
        {
            await OpenOrderAsync(current.OrderCode); if (disposed) return;
            SetTableSelection(ActiveTableCode() ?? previous); UpdateTableStatus();
            return;
        }
        if (!await ConfirmPendingInputAsync(this, L.T("chọn bàn khác"), false))
        {
            if (!disposed) { SetTableSelection(previous); UpdateTableStatus(); }
            return;
        }
        order = null; headerDraft = null; orderPanel.CancelNoteEdit();
        suppressChannel = true;
        try { channel.SelectedValue = "Tai quan"; } finally { suppressChannel = false; }
        SetTableSelection(code); context = new("Tai quan", code);
        orderPanel.ShowOrder(null); UpdateTableStatus(); SetBusy(false);
        state.Text = current.Retained
            ? L.T("Bàn đang giữ · khách đã trả. Thêm món sẽ tạo hóa đơn mới trong cùng phiên; trả bàn khi khách về.")
            : L.F("Đã chọn {0}. Thêm món đầu để tạo đơn.", current.Name);
    }
    private OrderHeaderInput CurrentHeader() => order is null ? context ?? new(channel.SelectedValue as string ?? "Mang di", (channel.SelectedValue as string) == "Tai quan" ? table.SelectedValue as string : null) :
        new(order.Header.Channel, order.Header.TableCode, order.Header.Customer, order.Header.Partner, order.Header.Recipient, order.Header.Phone, order.Header.Address, order.Header.Ship);
    private async Task EditHeader()
    {
        if (busy || unknown) return;
        using var dialog = new OrderHeaderDialog(headerDraft ?? CurrentHeader(), service, customers, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK || dialog.Result is null) return;
        if (order is null)
        {
            context = dialog.Result;
            orderPanel.ShowOrder(null); UpdateTableStatus(); SetBusy(false);
            state.Text = L.T("Đã nhập thông tin đơn mới; thêm món đầu để tạo đơn.");
            return;
        }
        var h = order.Header; var input = dialog.Result; headerDraft = input;
        await Write(h.Code, "SalesHeader", id => service.HeaderAsync(session, new(h.Code, h.Revision, id, input), lifetime.Token));
    }
    private async Task AddProduct(SalesMenuItem item)
    {
        if (busy || unknown || !item.CanAdd || order is not null && !order.Header.CanAddLine) return;
        if (order is null)
        {
            var input = CurrentHeader();
            if (input.Channel == "Tai quan")
            {
                if (input.TableCode is null)
                {
                    state.Text = L.T("Chọn bàn trước khi thêm món cho đơn tại quán.");
                    return;
                }
                var row = tableRows.FirstOrDefault(x => x.Code == input.TableCode);
                if (row is null || row.OrderCode is not null)
                {
                    state.Text = row is null
                        ? L.T("Chưa có dữ liệu bàn. Bấm Làm mới rồi thử lại.")
                        : L.F("Bàn {0} đang có đơn. Chọn bàn trống khác hoặc chọn bàn đang có đơn trong ô chọn bàn để mở đơn hiện có.", row.Name);
                    return;
                }
            }
            if (input.Channel == "Giao hang" && (input.Recipient is null || input.Phone is null || input.Address is null))
            {
                await EditHeader(); if (disposed) return;
                input = CurrentHeader();
                // Người dùng có thể đã hủy hộp F3; không tạo đơn giao hàng thiếu người nhận/SĐT/địa chỉ.
                if (input.Channel == "Giao hang" && (input.Recipient is null || input.Phone is null || input.Address is null)) return;
            }
            context = input;
            await CreateFirstLine(input, item);
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
        if (busy || unknown || order is null) return;
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
    private async Task<bool> SaveNote(SalesLine line, string? note)
    {
        if (busy || unknown || order is null) return false;
        if (order.Header.CanEdit != true) return false;
        var h = order.Header;
        var ok = await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Update", line.LineId, Quantity: line.Quantity, Note: note), lifetime.Token));
        if (ok) RememberNote(note);
        return ok;
    }
    private async Task DeleteLine(SalesLine line)
    {
        if (busy || unknown || order is null) return;
        if (order.Header.CanEdit != true) return;
        var h = order.Header;
        await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Delete", line.LineId), lifetime.Token));
        // SQL giữ header mở khi xóa dòng cuối (plan T14); UI phải nói rõ để người dùng thêm món hoặc hủy.
        if (!disposed && !unknown && order is not null && order.Lines.Count == 0)
            state.Text = L.T("Đơn đã hết món. Thêm món hoặc Hủy đơn mở.");
    }
    // Trả true khi đã ghi và tải lại thành công; callers hỏi Lưu cần biết để không rời màn khi lỗi.
    private async Task<bool> Write(string code, string operation, Func<Guid, Task<SalesWriteResult>> send)
    {
        if (busy || unknown) return false; pendingId = Guid.NewGuid(); pendingCode = code; pendingOperation = operation; SetBusy(true);
        var committed = false; var ok = false;
        try
        {
            await send(pendingId); committed = true; DiscardChanges(); await LoadOrder(code); await LoadTables(); ok = !unknown;
            if (!disposed) state.Text = L.T("Đã ghi và tải kết quả từ SQL.");
        }
        catch (UserOperationException ex) { unknown = committed || ex.Error.RequiresReconciliation; if (!disposed) state.Text = ex.Message + (unknown ? L.F(" · {0} · Kiểm tra kết quả", code) : ""); }
        catch (OperationCanceledException) { unknown = true; if (!disposed) state.Text = L.F("Chưa xác định kết quả {0}; kiểm tra lại.", code); }
        finally { if (!disposed) SetBusy(false); }
        return ok;
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
    // "In tạm": lưu đơn sang Chờ thanh toán rồi mở xem trước ngay, không hỏi xác nhận.
    // Đơn Chờ vẫn nối thêm món/tăng số lượng và sửa F3; chỉ khóa xóa/giảm/đổi ghi chú.
    private async Task PrintTemp()
    {
        if (busy || unknown || order is null) return;
        if (order.Header.IsPending) { await ReprintPending(); return; }
        if (order.Header.CanEdit != true) return;
        var h = order.Header;
        if (HasUnsavedChanges) { state.Text = L.T("Lưu hoặc bỏ ghi chú đang nhập trước khi in tạm."); return; }
        if (order.Lines.Count == 0 || !h.PreviewValid) { state.Text = L.T("Đơn cần có món hợp lệ trước khi in tạm."); return; }
        pendingId = Guid.NewGuid(); pendingCode = h.Code; pendingOperation = "SalesRequestPay";
        var committed = false;
        var message = L.F("Đơn {0} đã in tạm; khách gọi thêm thì bấm thêm món để nối vào đơn, F3 để đổi ưu đãi.", h.Code);
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
        await PrintPending(h.Code, message, reprint: false);
    }
    // Đơn đã Chờ (đã in tạm) vẫn in lại được sau khi nối thêm món; chỉ đọc, không đổi trạng thái.
    private async Task ReprintPending()
    {
        var code = order!.Header.Code;
        await PrintPending(code, L.F("Đơn {0}", code), reprint: true);
    }
    private async Task PrintPending(string code, string prefix, bool reprint)
    {
        try
        {
            var detail = invoices is null ? null : await invoices.DetailAsync(session, code, lifetime.Token);
            if (disposed) return;
            if (invoices is null || detail is null) throw new InvalidOperationException(L.T("Chưa tải được dữ liệu hóa đơn để in."));
            state.Text = prefix + (await InvoicePrinting.PreviewAsync(this, detail, invoices, session, reprint)
                ? L.T(" · Đã mở xem trước tạm tính.") : L.T(" · Đã đóng xem trước tạm tính."));
        }
        catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception
            or InvalidOperationException or IOException or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
        {
            // Đã đổi trạng thái thành công; lỗi in không được làm mất trạng thái đã ghi.
            if (!disposed) state.Text = prefix + L.T(" · Chưa in được tạm tính. ") + ex.Message;
        }
    }
    private async Task CancelOrder()
    {
        if (order?.Header.IsPayable != true || busy || unknown) return; var h = order.Header;
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.F("Hủy đơn mở {0}, bàn {1}, {2}? Không hoàn tiền/kho. Nếu đã pha, nhờ quản lý/kho lập xuất hủy ở phân hệ kho khi có chức năng T21.", h.Code, h.TableCode ?? "—", SalesUi.Money(h.Payable)), L.T("Hủy đơn mở"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        await Write(h.Code, "SalesCancel", id => service.CancelAsync(session, h, id, lifetime.Token));
        if (disposed || unknown) return;
        if (order?.Header.SessionCode is not int sessionCode || order.Header.SessionRevision is not { Length: 8 } revision)
        {
            await LoadTables(); await StartNewOrderAsync(false);
            return;
        }
        var note = await AskCloseTableAsync()
            ? await CloseSessionNowAsync(sessionCode, revision, L.T("Đã hủy đơn; chưa trả bàn được. ")) : "";
        if (!disposed && note.Length > 0) state.Text += note;
        await LoadTables();
        if (!disposed) await StartNewOrderAsync(false);
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
        order = null; headerDraft = null; context = null; orderPanel.CancelNoteEdit(); orderPanel.ShowOrder(null);
        await LoadTables();
        await StartNewOrderAsync(false);
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
            if (!disposed)
            {
                order = null; headerDraft = null; context = null;
                orderPanel.CancelNoteEdit(); orderPanel.ShowOrder(null); UpdateTableStatus();
            }
        }
        // Đã thu tiền xong mới hỏi trả bàn; lỗi trả bàn không làm mất kết quả thanh toán.
        if (!disposed && paid.Header.SessionCode is int closedSession && paid.Header.SessionRevision is { Length: 8 } closedRevision
            && await AskCloseTableAsync())
            message += await CloseSessionNowAsync(closedSession, closedRevision, L.T("Đã thu tiền; chưa trả bàn được. "));
        if (disposed) return;
        try { await LoadTables(); if (!disposed) await StartNewOrderAsync(false); }
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




