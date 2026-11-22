using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OrderHeaderDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly ComboBox partner = SalesUi.Combo();
    private readonly TextBox search = new() { PlaceholderText = L.T("Tên, số điện thoại hoặc mã khách") };
    private readonly TextBox recipient = new() { MaxLength = 100 }, phone = new() { MaxLength = 15 }, address = new() { MaxLength = 250 }, ship = new() { Text = "0" };
    private readonly DataGridView results;
    private readonly OrbitButton walkIn;
    private readonly Label selection = new() { AutoSize = false, AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };
    private readonly Label state = new() { Dock = DockStyle.Top, AutoSize = true };
    private readonly global::System.Windows.Forms.Timer typing = new() { Interval = 350 };
    private readonly OrderHeaderInput original;
    private readonly CustomerService customers;
    private readonly SessionContext session;
    private readonly ErrorProvider errors;
    private readonly CancellationTokenSource lifetime = new();
    private bool resourcesDisposed;
    private int generation;
    private bool ready, dirty, binding, suppressSearch;
    private List<CustomerRow> rows = [];
    private CustomerRow? selected;
    public OrderHeaderInput? Result { get; private set; }
    public OrderHeaderDialog(OrderHeaderInput original, SalesService sales, CustomerService customers, SessionContext session)
    {
        Text = original.Channel == "Giao hang" ? L.T("Khách hàng / Giao hàng") : L.T("Khách hàng");
        this.original = original; this.customers = customers; this.session = session;
        errors = new ErrorProvider { ContainerControl = this, BlinkStyle = ErrorBlinkStyle.NeverBlink };

        // Kết quả tìm khách là bảng chọn một dòng; giữ nguyên khách đã chọn khi gõ lại từ khóa.
        results = SalesUi.Grid(("Name", L.T("Khách hàng")), ("Phone", L.T("Số điện thoại")), ("Tier", L.T("Hạng")), ("Points", L.T("Điểm")));
        results.Columns["Name"]!.FillWeight = 46; results.Columns["Phone"]!.FillWeight = 24;
        results.Columns["Tier"]!.FillWeight = 14; results.Columns["Points"]!.FillWeight = 16;
        results.Columns["Points"]!.DefaultCellStyle.Format = "N0";
        results.Columns["Points"]!.DefaultCellStyle.FormatProvider = SalesUi.Culture;
        results.Columns["Points"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        OrbitManagementTheme.DialogInput(results);
        results.AccessibleName = L.T("Kết quả tìm khách");
        results.SelectionChanged += (_, _) =>
        {
            if (binding || !ready) return;
            if (results.CurrentRow?.DataBoundItem is CustomerRow row && row.Code != selected?.Code) SelectCustomer(row);
        };

        OrbitManagementTheme.DialogInput(search);
        var searchFrame = new OrbitField(search, true) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        var find = SalesUi.Button(L.T("Tìm"), async (_, _) => await SubmitSearch());
        OrbitManagementTheme.Button(find, ManagementButtonStyle.Secondary, "search");
        var add = SalesUi.Button(L.T("Thêm khách"), async (_, _) => await CreateCustomer());
        OrbitManagementTheme.Button(add, ManagementButtonStyle.Secondary, "plus");
        find.Anchor = add.Anchor = AnchorStyles.Left; find.Margin = Padding.Empty; add.Margin = new Padding(8, 0, 0, 0);
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchRow.RowStyles.Add(new RowStyle(SizeType.Absolute, OrbitManagementTheme.Px(this, 40)));
        searchRow.Controls.Add(searchFrame, 0, 0); searchRow.Controls.Add(find, 1, 0); searchRow.Controls.Add(add, 2, 0);

        walkIn = SalesUi.Button(L.T("Khách vãng lai"), (_, _) => SelectCustomer(null));
        OrbitManagementTheme.Button(walkIn, ManagementButtonStyle.Secondary, "user");
        walkIn.Anchor = AnchorStyles.Right; walkIn.Enabled = false;
        selection.Font = OrbitManagementTheme.Table; selection.ForeColor = OrbitManagementTheme.Muted;
        var selectRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selectRow.RowStyles.Add(new RowStyle(SizeType.Absolute, OrbitManagementTheme.Px(this, 40)));
        selectRow.Controls.Add(selection, 0, 0); selectRow.Controls.Add(walkIn, 1, 0);

        // Giữ cấu trúc field có nhãn như các editor khác: nhãn trên, control dưới.
        var searchField = new OrbitDialogField(L.T("Tìm khách"), searchRow);
        var gridHost = new Panel { Dock = DockStyle.Top, Height = OrbitManagementTheme.Px(this, 200), Margin = Padding.Empty, Padding = new Padding(1), BackColor = OrbitManagementTheme.Border };
        gridHost.Controls.Add(results);
        var resultsField = new OrbitDialogField(L.T("Kết quả tìm khách"), gridHost) { Margin = new Padding(16, 8, 16, 0), AutoSize = false, Height = OrbitManagementTheme.Px(this, 238) };
        // OrbitDialogField tô lại nền control; đặt lại viền 1px sau khi field tạo xong.
        gridHost.BackColor = OrbitManagementTheme.Border;
        var selectionField = new OrbitDialogField(L.T("Khách đang chọn"), selectRow);
        DpiChangedAfterParent += (_, _) => { gridHost.Height = OrbitManagementTheme.Px(this, 200); resultsField.Height = OrbitManagementTheme.Px(this, 238); };

        // Cột khách: tìm → bảng kết quả → khách đang chọn. Giao hàng xếp cột phải để
        // hộp không cao quá màn hình 1366×768 tại 125%.
        var customerPane = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        customerPane.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 3; row++) customerPane.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        customerPane.Controls.Add(searchField, 0, 0); customerPane.Controls.Add(resultsField, 0, 1); customerPane.Controls.Add(selectionField, 0, 2);

        var body = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = original.Channel == "Giao hang" ? 2 : 1, RowCount = 1, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.Controls.Add(customerPane, 0, 0);
        if (original.Channel == "Giao hang")
        {
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            customerPane.Margin = new Padding(0, 0, 16, 0);
            var deliveryPane = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 16, 0), BackColor = OrbitManagementTheme.Surface };
            deliveryPane.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            deliveryPane.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            deliveryPane.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            deliveryPane.Controls.Add(new Label { Text = L.T("Giao hàng"), AutoSize = true, Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Caramel, Margin = new Padding(0, 12, 0, 4) }, 0, 0);
            var delivery = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
            delivery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            delivery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            for (var row = 0; row < 3; row++) delivery.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Người nhận/SĐT, địa chỉ giãn hết hàng, đối tác/phí ship.
            var recipientField = new OrbitDialogField(L.T("Tên người nhận"), recipient) { Margin = new Padding(0, 0, 8, 0) };
            var phoneField = new OrbitDialogField(L.T("Số điện thoại"), phone) { Margin = new Padding(8, 0, 0, 0) };
            var addressField = new OrbitDialogField(L.T("Địa chỉ"), address) { Margin = Padding.Empty };
            var partnerField = new OrbitDialogField(L.T("Đối tác (Tự giao nếu bỏ chọn)"), partner) { Margin = new Padding(0, 0, 8, 0) };
            var shipField = new OrbitDialogField(L.T("Phí ship (VND)"), ship) { Margin = new Padding(8, 0, 0, 0) };
            delivery.Controls.Add(recipientField, 0, 0); delivery.Controls.Add(phoneField, 1, 0);
            delivery.Controls.Add(addressField, 0, 1); delivery.SetColumnSpan(addressField, 2);
            delivery.Controls.Add(partnerField, 0, 2); delivery.Controls.Add(shipField, 1, 2);
            deliveryPane.Controls.Add(delivery, 0, 1);
            body.Controls.Add(deliveryPane, 1, 0);
        }

        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, AutoSize = true, ColumnCount = 1, BackColor = OrbitManagementTheme.Surface };
        fields.Enabled = false;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.Controls.Add(body, 0, 0);
        recipient.Text = original.Recipient; phone.Text = original.Phone; address.Text = original.Address; ship.Text = original.Ship.ToString("0", SalesUi.Culture);
        search.TabIndex = 0; find.TabIndex = 1; add.TabIndex = 2; results.TabIndex = 3; walkIn.TabIndex = 4;
        recipient.TabIndex = 5; phone.TabIndex = 6; address.TabIndex = 7; partner.TabIndex = 8; ship.TabIndex = 9;

        var save = SalesUi.Button(L.T("Áp dụng"), (_, _) => Save()); save.Enabled = false; save.IsPrimary = true;
        var back = SalesUi.Button(L.T("Quay lại"), (_, _) => Close()); CancelButton = back;
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = Padding.Empty, Margin = Padding.Empty };
        actions.Controls.Add(back); actions.Controls.Add(save);
        var layout = new OrbitDialogLayout(this, fields, state, actions);
        Controls.Add(layout);
        UpdateSelection();
        var deliveryOrder = original.Channel == "Giao hang";
        SalesUi.Fit(this, new Size(deliveryOrder ? 860 : 640, 640));
        Shown += (_, _) => layout.FitToContent(this, deliveryOrder);
        typing.Tick += async (_, _) => { typing.Stop(); await Search(); };
        search.TextChanged += (_, _) => { if (!ready || suppressSearch) return; typing.Stop(); typing.Start(); };
        search.KeyDown += async (_, e) => { if (e.KeyCode != Keys.Enter) return; e.SuppressKeyPress = true; typing.Stop(); await SubmitSearch(); };
        Shown += async (_, _) =>
        {
            state.Text = L.T("Đang tải khách…");
            try
            {
                await Search(initializing: true);
                if (IsDisposed) return;
                if (original.Customer is not null)
                {
                    var current = await customers.GetAsync(session, original.Customer, lifetime.Token);
                    if (IsDisposed) return;
                    if (current is not null) SelectCustomer(current);
                }
                if (original.Channel == "Giao hang")
                {
                    var partners = await sales.PartnersAsync(session, lifetime.Token); if (IsDisposed) return;
                    partner.DataSource = new[] { new SalesLookup("", L.T("Tự giao")) }.Concat(partners).ToList();
                    partner.SelectedValue = original.Partner ?? ""; if (partner.SelectedIndex < 0) partner.SelectedIndex = 0;
                }
                save.Enabled = true; fields.Enabled = true; ready = true; dirty = false; state.Text = "";
                UpdateSelection(); search.Focus();
            }
            catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException or ArgumentException or InvalidOperationException)
            { if (!IsDisposed) state.Text = L.T("Không tải được thông tin đơn. Chọn Quay lại rồi mở lại. ") + ex.Message; }
        };
        partner.SelectedValueChanged += (_, _) => { if (ready) dirty = true; };
        foreach (var input in new[] { recipient, phone, address, ship }) input.TextChanged += (_, _) => { if (ready) dirty = true; };
    }
    private async Task SubmitSearch()
    {
        if (!ready) return;
        await Search();
    }
    private async Task Search(bool initializing = false)
    {
        var request = ++generation;
        try
        {
            rows = await customers.ListAsync(session, search.Text.Trim(), 0, lifetime.Token);
            if (IsDisposed || request != generation) return;
            binding = true;
            try { results.DataSource = null; results.DataSource = rows; results.ClearSelection(); results.CurrentCell = null; }
            finally { binding = false; }
            HighlightSelected();
            if (!initializing) state.Text = rows.Count == 0 ? L.T("Không tìm thấy khách phù hợp. Thử tên hoặc số điện thoại khác.") : "";
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException)
        { if (initializing) throw; if (!IsDisposed) state.Text = ex.Message; }
    }
    private void SelectCustomer(CustomerRow? row)
    {
        selected = row; HighlightSelected(); UpdateSelection();
        if (ready) dirty = true;
    }
    private void HighlightSelected()
    {
        binding = true;
        try
        {
            results.ClearSelection();
            if (selected is null) return;
            foreach (DataGridViewRow item in results.Rows)
                if (item.DataBoundItem is CustomerRow candidate && candidate.Code == selected.Code)
                { item.Selected = true; results.CurrentCell = item.Cells[0]; break; }
        }
        finally { binding = false; }
    }
    private void UpdateSelection()
    {
        walkIn.Enabled = ready && selected is not null;
        selection.ForeColor = selected is null ? OrbitManagementTheme.Muted : OrbitManagementTheme.Text;
        selection.Text = selected is null
            ? L.T("Khách vãng lai — không tích điểm cho đơn này.")
            : L.F("Đã chọn {0} · {1} · hạng {2} · {3} điểm", selected.Name, string.IsNullOrWhiteSpace(selected.Phone) ? "—" : selected.Phone, selected.Tier, selected.Points.ToString("N0", SalesUi.Culture));
    }
    private async Task CreateCustomer()
    {
        using var editor = new OperationEditorDialog(L.T("Thêm khách"), id => customers.ReconcileAsync(session, id));
        var name = editor.AddText(L.T("Tên khách")); var number = editor.AddText(L.T("Số điện thoại (có thể trống)"));
        editor.CaptureWrite = () => { var request = CustomerService.Normalize(new(name.Text, number.Text)); return id => customers.SaveAsync(session, request, null, id); };
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(editor, this) != DialogResult.OK || editor.ResultCode is null) return;
        suppressSearch = true; search.Text = number.Text.Length > 0 ? number.Text : name.Text; suppressSearch = false;
        typing.Stop(); await Search();
        if (IsDisposed) return;
        var created = rows.FirstOrDefault(x => x.Code == editor.ResultCode) ?? await customers.GetAsync(session, editor.ResultCode, lifetime.Token);
        if (created is not null && !IsDisposed) SelectCustomer(created);
    }
    private void Save()
    {
        if (!ready) return;
        errors.Clear();
        decimal shipping;
        try { shipping = SalesUi.ParseMoney(ship.Text); }
        catch (ArgumentException ex) { state.Text = ex.Message; errors.SetError(ship, ex.Message); ship.Focus(); return; }
        if (original.Channel == "Giao hang")
        {
            TextBox? firstInvalid = null;
            foreach (var (input, invalid, message) in new[]
            {
                (recipient, string.IsNullOrWhiteSpace(recipient.Text), L.T("Cần nhập tên người nhận.")),
                (phone, string.IsNullOrWhiteSpace(phone.Text) || phone.Text.Trim().Any(c => c is < '0' or > '9'), L.T("Số điện thoại người nhận chỉ gồm các chữ số 0–9 và không được trống.")),
                (address, string.IsNullOrWhiteSpace(address.Text), L.T("Cần nhập địa chỉ giao hàng."))
            })
            {
                if (!invalid) continue;
                errors.SetError(input, message); firstInvalid ??= input;
            }
            if (firstInvalid is not null) { state.Text = errors.GetError(firstInvalid); firstInvalid.Focus(); return; }
        }
        try
        {
            Result = SalesService.Normalize(original with { Customer = selected?.Code,
                Partner = original.Channel == "Giao hang" ? partner.SelectedValue as string : null, Recipient = recipient.Text,
                Phone = phone.Text, Address = address.Text, Ship = shipping });
            DialogResult = DialogResult.OK; Close();
        }
        catch (ArgumentException ex) { state.Text = ex.Message; var target = original.Channel == "Giao hang" ? (Control)recipient : search; errors.SetError(target, ex.Message); target.Focus(); }
    }
    protected override async Task<bool> CanCloseAsync()
    {
        return !dirty || Result is not null || session.IsRevoked || await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.T("Bỏ thông tin chưa áp dụng?"), L.T("Nháp chưa gửi"), MessageBoxButtons.OKCancel) == DialogResult.OK;
    }
    protected override void Dispose(bool disposing) { if (disposing && !resourcesDisposed) { resourcesDisposed = true; typing.Stop(); typing.Dispose(); lifetime.Cancel(); lifetime.Dispose(); errors.Dispose(); } base.Dispose(disposing); }
}
