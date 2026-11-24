using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.HumanResources;

internal sealed class EmployeesControl : OrbitRecordListControl
{
    private readonly EmployeeService service;
    private readonly EmployeeAccountService accounts;
    private readonly SessionContext session;
    private readonly IPendingOperationStore pending;
    private readonly List<HumanResourcesLookup> positionLookups = [];
    private OrbitRowEditor? inline;
    private bool positionBuilding;
    public EmployeesControl(EmployeeService service, EmployeeAccountService accounts, SessionContext session, IPendingOperationStore pending, bool accountTab)
    {
        this.service = service; this.accounts = accounts; this.session = session; this.pending = pending;
        var employed = Choice(L.T("Tất cả"), L.T("Đang làm"), L.T("Đã nghỉ")); var contract = Choice(L.T("Mọi hợp đồng"), "Full-time", "Part-time");
        var role = Choice(L.T("Mọi vai trò"), "ADMIN", "CASH", "STORE"); var active = Choice(L.T("Mọi tài khoản"), L.T("Hoạt động"), L.T("Đã khóa"));
        var position = Choice(L.T("Mọi chức vụ"));
        Configure(accountTab ? L.T("Tài khoản nhân viên") : L.T("Nhân viên"), session, async (q, skip, t) => await service.ListAsync(session,
            new(q, skip, position.SelectedIndex > 0 && position.SelectedIndex - 1 < positionLookups.Count ? positionLookups[position.SelectedIndex - 1].Code : null, contract.SelectedIndex > 0 ? contract.Text : null,
            employed.SelectedIndex == 0 ? null : employed.SelectedIndex == 1, role.SelectedIndex > 0 ? role.Text : null, active.SelectedIndex == 0 ? null : active.SelectedIndex == 1), t),
            accountTab ? [("Code", L.T("Mã NV")), ("Name", L.T("Nhân viên")), ("Username", L.T("Đăng nhập")), ("Role", L.T("Vai trò")), ("AccountText", L.T("Tài khoản")), ("EmploymentText", L.T("Làm việc"))]
            : [("Code", L.T("Mã NV")), ("Name", L.T("Nhân viên")), ("Phone", L.T("SDT")), ("Contract", L.T("Hợp đồng")), ("MonthlyRate", L.T("Lương tháng")), ("HourlyRate", L.T("Lương giờ")), ("PositionName", L.T("Chức vụ")), ("EmploymentText", L.T("Làm việc"))]);
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((EmployeeRow)rows[0]!).TotalRows;
        UseStackedSearch(L.T("nhân viên"));
        foreach (var filter in accountTab ? new[] { role, active, employed } : new[] { employed, contract, position })
        { AddFilter(filter, 160, true); filter.SelectedValueChanged += async (_, _) => { if (!positionBuilding) await ResetAndReload(); }; }
        foreach (DataGridViewColumn col in Grid.Columns) if (col.DataPropertyName is "MonthlyRate" or "HourlyRate") { col.DefaultCellStyle.Format = "#,##0' ₫'"; col.DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("vi-VN"); col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
        if (accountTab) { AddAction("Cấp tài khoản", () => Account("Create"), needsRow: true); AddAction("Đổi quyền / khóa", () => Account("Change"), needsRow: true); AddAction("Reset mật khẩu", () => Account("Reset"), needsRow: true); }
        else
        {
            var admin = session.Identity.Role == "ADMIN";
            AddAction("Thêm", () => Edit(true)); AddAction("Nghỉ / Đi làm lại", Employment, needsRow: true);
            AddRowDeleteColumn();
            inline = CreateInlineEditor(); inline.CanWrite = _ => admin; inline.RowKey = item => ((EmployeeRow)item).Code;
            inline.ColumnFor = (item, cell) => Column(cell.OwningColumn?.Name ?? "");
            inline.Edit = item => Edit(false, (EmployeeRow)item); inline.Delete = DeleteInline; inline.Reconcile = ReconcileInline; inline.Reload = () => Reload();
        }
        AddAction("Yêu cầu chờ", Recover);
        Load += async (_, _) =>
        {
            if (accountTab) return;
            try
            {
                var list = await service.LookupsAsync(session); if (IsDisposed) return;
                positionBuilding = true;
                try
                {
                    var keep = position.SelectedIndex; positionLookups.Clear(); positionLookups.AddRange(list);
                    position.Items.Clear(); position.Items.Add(L.T("Mọi chức vụ")); foreach (var p in list) position.Items.Add(p.Name);
                    position.SelectedIndex = keep >= 0 && keep < position.Items.Count ? keep : 0;
                }
                finally { positionBuilding = false; }
            }
            catch (Exception e) when(e is UserOperationException or OperationCanceledException) { if (!IsDisposed) State.Text = e.Message; }
        };
    }
    internal static ComboBox Choice(params string[] values) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top }; c.Items.AddRange(values); c.SelectedIndex = 0; return c; }
    internal static decimal Amount(TextBox box) { if (!decimal.TryParse(box.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var value)) throw new ArgumentException(L.T("Nhập số tiền/giờ hợp lệ.")); return value; }
    private HrEditorDialog Dialog(string title, string operation) => new(title, operation, session, pending, id => service.ReceiptAsync(session, id, operation));
    private async Task Recover()
    {
        using var selector = Dialog(L.T("Đối chiếu yêu cầu nhân sự"), "EmployeeRecovery"); selector.SetWriteAvailable(false);
        var operation = Choice("EmployeeCreate", "EmployeeUpdate", "EmployeeDelete", "EmployeeEmployment", "AccountCreate", "AccountChange", "AccountReset"); selector.AddField(L.T("Loại thao tác"), operation);
        selector.Note(L.T("Mở yêu cầu đã lưu trên tài khoản/DB này, hoặc nhập request ID để ADMIN tra metadata khi tài khoản tạo yêu cầu đã bị đổi quyền/reset."));
        var open = new OrbitButton { Text = L.T("Mở yêu cầu đang chờ") }; selector.AddField(L.T("File chờ"), open);
        open.Click += async (_, _) => { using var d = Dialog(L.T("Đối chiếu ") + operation.Text, operation.Text); d.SetWriteAvailable(false); d.Note(L.T("Chỉ đối chiếu; không gửi lại mật khẩu hoặc nội dung cũ.")); await OrbitDialogHost.ShowAsync(d, selector); if(d.Changed) await Reload(); };
        var id = selector.TextField(L.T("Request ID cần tra (có thể của ADMIN khác)")); var result = new TextBox { ReadOnly = true, Dock = DockStyle.Top };
        var read = new OrbitButton { Text = L.T("Tra request ID") }; selector.AddField(L.T("Tra cứu"), read); selector.AddField(L.T("Kết quả"), result);
        read.Click += async (_, _) => { read.Enabled=false; try { if(!Guid.TryParse(id.Text,out var request))throw new ArgumentException(L.T("Request ID không hợp lệ.")); var receipt=await service.ReceiptAsync(session,request,operation.Text);if(!selector.IsDisposed)result.Text=receipt is null?L.T("SQL xác minh chưa ghi."):L.F("Đã ghi {0}: {1}", receipt.Operation, receipt.Code); } catch(Exception e)when(e is ArgumentException or UserOperationException or OperationCanceledException){if(!selector.IsDisposed)result.Text=e.Message;} finally{if(!selector.IsDisposed)read.Enabled=true;} };
        await OrbitDialogHost.ShowAsync(selector,this);
    }
    private async Task<EmployeeRow> Current() => Selected is EmployeeRow r ? await service.GetAsync(session, r.Code) ?? throw new ArgumentException(L.T("Hồ sơ không còn tồn tại.")) : throw new ArgumentException(L.T("Chọn nhân viên."));
    private async Task Edit(bool create, EmployeeRow? row = null)
    {
        EmployeeRow? old = null;
        if (!create) old = row is null ? await Current() : await service.GetAsync(session, row.Code) ?? throw new ArgumentException(L.T("Hồ sơ không còn tồn tại."));
        var options = await service.LookupsAsync(session);
        if (options.Count == 0) throw new ArgumentException(L.T("Tạo chức vụ trong Danh mục trước."));
        using var d = Dialog(create ? L.T("Thêm nhân viên") : L.T("Sửa nhân viên"), create ? "EmployeeCreate" : "EmployeeUpdate");
        var name = d.TextField(L.T("Tên nhân viên"), old?.Name ?? ""); var phone = d.TextField(L.T("Số điện thoại"), old?.Phone ?? "");
        var contract = Choice("Full-time", "Part-time"); contract.SelectedItem = old?.Contract ?? "Full-time"; d.AddField(L.T("Hợp đồng"), contract);
        var monthly = d.TextField(L.T("Lương tháng (VND)"), (old?.MonthlyRate ?? 0).ToString("0", CultureInfo.GetCultureInfo("vi-VN")));
        var hourly = d.TextField(L.T("Lương giờ (VND)"), (old?.HourlyRate ?? 0).ToString("0", CultureInfo.GetCultureInfo("vi-VN")));
        var position = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = options, DisplayMember = "Name", ValueMember = "Code", Dock = DockStyle.Top };
        if (old is not null) position.SelectedValue = old.Position; d.AddField(L.T("Chức vụ"), position);
        d.CaptureCommand = () => { var r = EmployeeService.Normalize(new(name.Text, phone.Text, contract.Text, Amount(monthly), Amount(hourly), (string)position.SelectedValue!)); return (JsonSerializer.Serialize(r), id => service.SaveAsync(session, r, old, id)); };
        await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) await Reload();
    }
    private async Task Employment()
    {
        var row = await Current(); using var d = Dialog(L.T("Thay trạng thái làm việc"), "EmployeeEmployment");
        d.Note($"{row.Code} · {row.Name}. " + (row.Employed ? L.T("Nghỉ việc sẽ khóa tài khoản, giữ lịch sử.") : L.T("Đi làm lại vẫn giữ tài khoản khóa đến khi quản lý mở.")));
        d.Confirmation = L.T("Xác nhận thay đổi trạng thái làm việc?");
        d.CaptureCommand = () => (JsonSerializer.Serialize(new { row.Code, Employed = !row.Employed }), id => service.EmploymentAsync(session, row, !row.Employed, id));
        await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) { if (row.AccountId == session.Identity.AccountId) session.Revoke(); else await Reload(); }
    }
    private static InlineColumn? Column(string property) => property switch
    {
        "Name" => new("Name", InlineCellKind.Text),
        "Phone" => new("Phone", InlineCellKind.Text),
        "Contract" => new("Contract", InlineCellKind.Dropdown),
        "MonthlyRate" => new("MonthlyRate", InlineCellKind.Money),
        "HourlyRate" => new("HourlyRate", InlineCellKind.Money),
        "PositionName" => new("PositionName", InlineCellKind.Dropdown),
        _ => null
    };
    private async Task<bool> DeleteInline(object item, Guid id, CancellationToken token)
    {
        var row = (EmployeeRow)item;
        if (await OrbitConfirmationDialog.ShowAsync(this, L.F("Xóa hồ sơ {0} · {1}? Hồ sơ có tham chiếu sẽ bị SQL chặn.", row.Code, row.Name), L.T("Xóa hồ sơ chưa có lịch sử"), MessageBoxButtons.YesNo, L.T("Xóa"), L.T("Hủy")) != DialogResult.Yes) return false;
        await service.DeleteAsync(session, row, id, token); return true;
    }
    // Không ghi file chờ cho từng dòng; SQL xác minh receipt theo request ID rồi tải lại.
    private async Task<bool?> ReconcileInline(object item, Guid id, CancellationToken token)
    {
        var update = await service.ReceiptAsync(session, id, "EmployeeUpdate", token);
        var receipt = update ?? await service.ReceiptAsync(session, id, "EmployeeDelete", token);
        return receipt is null ? false : true;
    }
    private async Task Account(string action)
    {
        var row = await Current(); if (action == "Create" && row.AccountId is not null || action != "Create" && row.AccountId is null) throw new ArgumentException(L.T("Trạng thái tài khoản không phù hợp; tải lại hồ sơ."));
        using var d = Dialog(action == "Reset" ? L.T("Reset mật khẩu") : L.T("Quản trị tài khoản"), "Account" + action);
        d.Note(L.F("{0} · {1}. Đổi quyền/khóa/reset làm phiên cũ hết hiệu lực. Không thay chức vụ hoặc lịch sử.", row.Code, row.Name));
        var username = action == "Create" ? d.TextField(L.T("Tên đăng nhập")) : null;
        var role = Choice("ADMIN", "CASH", "STORE"); role.SelectedItem = row.Role ?? "CASH";
        var active = Choice(L.T("Hoạt động"), L.T("Đã khóa")); active.SelectedIndex = row.Active == false ? 1 : 0;
        if (action != "Reset") { d.AddField(L.T("Vai trò"), role); d.AddField(L.T("Trạng thái"), active); }
        var password = action != "Change" ? d.TextField(L.T("Mật khẩu mới (8–128 ký tự)"), secret: true) : null;
        var confirm = action != "Change" ? d.TextField(L.T("Nhập lại mật khẩu"), secret: true) : null;
        d.Confirmation = L.T("Xác nhận thay đổi tài khoản? Nếu là tài khoản đang dùng, phiên sẽ kết thúc sau khi ghi.");
        d.CaptureCommand = () =>
        {
            var r = new AccountRequest(row.Code, row.AccountStamp, username?.Text, role.Text, active.SelectedIndex == 0);
            var pw = password?.Text; var confirmation = confirm?.Text;
            if (action != "Change" && (pw is null || pw.Length is < 8 or > 128 || pw != confirmation)) throw new ArgumentException(L.T("Mật khẩu cần8–128 ký tự và xác nhận khớp."));
            return (JsonSerializer.Serialize(new { row.Code, Action = action }), id => accounts.WriteAsync(session, action, r, pw, confirmation, id));
        };
        await OrbitDialogHost.ShowAsync(d, this);
        if (password is not null) password.Clear(); if (confirm is not null) confirm.Clear();
        if (d.Changed) { if (row.AccountId == session.Identity.AccountId) session.Revoke(); else await Reload(); }
    }
}

