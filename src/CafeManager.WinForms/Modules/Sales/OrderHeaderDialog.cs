using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OrderHeaderDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly ComboBox customer = SalesUi.Combo(), promotion = SalesUi.Combo(), partner = SalesUi.Combo();
    private readonly TextBox search = new() { PlaceholderText = "Tên/số điện thoại khách", Width = 200 };
    private readonly TextBox recipient = new() { MaxLength = 100 }, phone = new() { MaxLength = 15 }, address = new() { MaxLength = 250 }, ship = new() { Text = "0" };
    private readonly Label state = new() { Dock = DockStyle.Top, AutoSize = true };
    private readonly OrderHeaderInput original;
    private readonly CustomerService customers;
    private readonly SessionContext session;
    private readonly ErrorProvider errors;
    private readonly CancellationTokenSource lifetime = new();
    private bool resourcesDisposed;
    private int generation;
    private bool ready, dirty;
    public OrderHeaderInput? Result { get; private set; }
    public OrderHeaderDialog(OrderHeaderInput original, DateTime businessDate, SalesService sales, CustomerService customers, LoyaltyService loyalty, SessionContext session)
    {
        Text = "Khách / Ưu đãi / Giao hàng"; this.original = original; this.customers = customers; this.session = session;
        errors = new ErrorProvider { ContainerControl = this };
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 1, Padding = new Padding(16) };
        fields.Enabled = false;
        void Field(string label, Control control) { fields.RowStyles.Add(new RowStyle(SizeType.AutoSize)); fields.Controls.Add(new Label { Text = label, AutoSize = true }); control.Dock = DockStyle.Top; fields.Controls.Add(control); }
        Field("Tìm khách (bỏ chọn = vãng lai)", SalesUi.Bar(search, SalesUi.Button("Tìm", async (_, _) => await Search()), SalesUi.Button("Thêm khách", async (_, _) => await CreateCustomer())));
        Field("Khách / hạng / điểm hiện tại", customer); Field("Không chọn chương trình vẫn áp dụng hạng", promotion);
        if (original.Channel == "Giao hang")
        {
            Field("Đối tác (Tự giao nếu bỏ chọn)", partner); Field("Tên người nhận", recipient); Field("Số điện thoại", phone); Field("Địa chỉ", address); Field("Phí ship (VND)", ship);
        }
        recipient.Text = original.Recipient; phone.Text = original.Phone; address.Text = original.Address; ship.Text = original.Ship.ToString("0", SalesUi.Culture);
        var save = SalesUi.Button("Áp dụng", (_, _) => Save()); save.Enabled = false;
        var back = SalesUi.Button("Quay lại", (_, _) => Close()); CancelButton = back;
        Controls.Add(fields); Controls.Add(state); Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 60, Controls = { SalesUi.Bar(save, back) } });
        AppTheme.Apply(this); SalesUi.Fit(this, new Size(560, 600));
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await Search(); } };
        Shown += async (_, _) =>
        {
            state.Text = "Đang tải khách và ưu đãi…";
            try
            {
                await Search(initializing: true);
                if (IsDisposed) return;
                if (original.Customer is not null)
                {
                    var current = await customers.GetAsync(session, original.Customer, lifetime.Token);
                    if (IsDisposed) return;
                    if (current is not null) { var options = (List<SalesLookup>)customer.DataSource!; options = options.Where(x => x.Code != current.Code).Append(CustomerOption(current)).ToList(); customer.DataSource = options; customer.SelectedValue = current.Code; }
                }
                var offers = new List<PromotionRow>();
                for (var skip = 0; ; skip += 50)
                {
                    var page = await loyalty.PromotionsAsync(session, "", skip, DateOnly.FromDateTime(businessDate), lifetime.Token); offers.AddRange(page); if (page.Count < 50) break;
                }
                if (IsDisposed) return;
                promotion.DataSource = new[] { new SalesLookup("", "Không chọn chương trình") }.Concat(offers.Select(x => new SalesLookup(x.Code, $"{x.Name} · {x.Discount}%"))).ToList();
                promotion.SelectedValue = original.Promotion ?? ""; if (promotion.SelectedIndex < 0) promotion.SelectedIndex = 0;
                // Combo đối tác chỉ có BindingContext khi được gắn vào form giao hàng.
                if (original.Channel == "Giao hang")
                {
                    var partners = await sales.PartnersAsync(session, lifetime.Token); if (IsDisposed) return;
                    partner.DataSource = new[] { new SalesLookup("", "Tự giao") }.Concat(partners).ToList();
                    partner.SelectedValue = original.Partner ?? ""; if (partner.SelectedIndex < 0) partner.SelectedIndex = 0;
                }
                save.Enabled = true; fields.Enabled = true; ready = true; dirty = false; state.Text = "Loại đơn và bàn giữ nguyên sau tạo.";
            }
            catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException or ArgumentException or InvalidOperationException)
            { if (!IsDisposed) state.Text = "Không tải được thông tin đơn. Chọn Quay lại rồi mở lại. " + ex.Message; }
        };
        foreach (var combo in new[] { customer, promotion, partner }) combo.SelectedValueChanged += (_, _) => { if (ready) dirty = true; };
        foreach (var input in new[] { recipient, phone, address, ship }) input.TextChanged += (_, _) => { if (ready) dirty = true; };
    }
    private static SalesLookup CustomerOption(CustomerRow row) => new(row.Code, $"{row.Name} · {row.Phone} · {row.Tier} · {row.Points} điểm");
    private async Task Search(bool initializing = false)
    {
        var request = ++generation;
        try
        {
            var rows = await customers.ListAsync(session, search.Text, 0, lifetime.Token);
            if (IsDisposed || request != generation) return;
            customer.DataSource = new[] { new SalesLookup("", "Khách vãng lai") }.Concat(rows.Select(CustomerOption)).ToList();
            state.Text = "Tối đa50 kết quả; nhập số điện thoại cụ thể để chọn đúng khách.";
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException)
        { if (initializing) throw; if (!IsDisposed) state.Text = ex.Message; }
    }
    private async Task CreateCustomer()
    {
        using var editor = new OperationEditorDialog("Thêm khách", id => customers.ReconcileAsync(session, id));
        var name = editor.AddText("Tên khách"); var number = editor.AddText("Số điện thoại (có thể trống)");
        editor.CaptureWrite = () => { var request = CustomerService.Normalize(new(name.Text, number.Text)); return id => customers.SaveAsync(session, request, null, id); };
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(editor, this) != DialogResult.OK || editor.ResultCode is null) return;
        search.Text = number.Text.Length > 0 ? number.Text : name.Text; await Search();
        if (!IsDisposed) customer.SelectedValue = editor.ResultCode;
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
                (recipient, string.IsNullOrWhiteSpace(recipient.Text), "Cần nhập tên người nhận."),
                (phone, string.IsNullOrWhiteSpace(phone.Text) || phone.Text.Trim().Any(c => c is < '0' or > '9'), "Số điện thoại người nhận chỉ gồm các chữ số 0–9 và không được trống."),
                (address, string.IsNullOrWhiteSpace(address.Text), "Cần nhập địa chỉ giao hàng.")
            })
            {
                if (!invalid) continue;
                errors.SetError(input, message); firstInvalid ??= input;
            }
            if (firstInvalid is not null) { state.Text = errors.GetError(firstInvalid); firstInvalid.Focus(); return; }
        }
        try
        {
            Result = SalesService.Normalize(original with { Customer = customer.SelectedValue as string, Promotion = promotion.SelectedValue as string,
                Partner = original.Channel == "Giao hang" ? partner.SelectedValue as string : null, Recipient = recipient.Text,
                Phone = phone.Text, Address = address.Text, Ship = shipping });
            DialogResult = DialogResult.OK; Close();
        }
        catch (ArgumentException ex) { state.Text = ex.Message; var target = original.Channel == "Giao hang" ? recipient : (Control)customer; errors.SetError(target, ex.Message); target.Focus(); }
    }
    protected override async Task<bool> CanCloseAsync()
    {
        return !dirty || Result is not null || session.IsRevoked || await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, "Bỏ thông tin chưa áp dụng?", "Nháp chưa gửi", MessageBoxButtons.OKCancel) == DialogResult.OK;
    }
    protected override void Dispose(bool disposing) { if (disposing && !resourcesDisposed) { resourcesDisposed = true; lifetime.Cancel(); lifetime.Dispose(); errors.Dispose(); } base.Dispose(disposing); }
}



