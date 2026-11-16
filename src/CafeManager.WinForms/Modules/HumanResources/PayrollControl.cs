using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.HumanResources;

internal sealed class PayrollControl : OrbitRecordListControl
{
    private readonly PayrollService service; private readonly EmployeeService employees; private readonly SessionContext session; private readonly IPendingOperationStore pending; private readonly DocumentLinks links;
    private readonly ComboBox month = EmployeesControl.Choice(Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray());
    private readonly NumericUpDown year = new() { Minimum = 2000, Maximum = 9999, Value = DateTime.Today.Year };
    public PayrollControl(PayrollService service, EmployeeService employees, SessionContext session, IPendingOperationStore pending, DocumentLinks links)
    {
        this.service = service; this.employees = employees; this.session = session; this.pending = pending; this.links = links;
        month.SelectedIndex = DateTime.Today.Month - 1;
        var status = EmployeesControl.Choice(L.T("Mọi trạng thái"), L.T("Chưa chi"), L.T("Đã chi"), L.T("Không phát sinh chi"), L.T("Legacy cần đối soát"));
        Configure("Bảng lương", session, async (q, skip, t) => await service.ListAsync(session, new(month.SelectedIndex + 1, (int)year.Value, q, status.SelectedIndex switch { 1 => "Unpaid", 2 => "Paid", 3 => "NoExpense", 4 => "LegacyUnknown", _ => null }, skip), t),
            ("Id", L.T("Mã BL")), ("Employee", L.T("Mã NV")), ("EmployeeName", L.T("Nhân viên")), ("Month", L.T("Tháng")), ("Year", L.T("Năm")), ("Amount", L.T("Thực lĩnh")), ("StatusText", L.T("Trạng thái")), ("ExpenseCode", L.T("Phiếu chi")));
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((PayrollRow)rows[0]!).TotalRows; UseStackedSearch(L.T("bảng lương"));
        AddFilter(month, 90, true); AddFilter(year, 110); AddFilter(status, 190, true);
        month.SelectedValueChanged += async (_, _) => await ResetAndReload(); year.ValueChanged += async (_, _) => await ResetAndReload(); status.SelectedValueChanged += async (_, _) => await ResetAndReload();
        Grid.Columns["Amount"]!.DefaultCellStyle.Format = "#,##0' ₫'"; Grid.Columns["Amount"]!.DefaultCellStyle.FormatProvider = CultureInfo.GetCultureInfo("vi-VN"); Grid.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        AddAction("Thêm", Preview); AddAction("Chi tiết", Detail, needsRow: true); AddAction("Chi lương", Pay, needsRow: true);
        AddAction("Đối chiếu chốt", () => Recover("PayrollCommit")); AddAction("Đối chiếu chi", () => Recover("PayrollPay"));
    }
    private HrEditorDialog Dialog(string title, string operation) => new(title, operation, session, pending, id => service.ReceiptAsync(session, id, operation));
    private async Task Recover(string operation) { using var d = Dialog(L.T("Đối chiếu yêu cầu lương"), operation); d.SetWriteAvailable(false); d.Note(L.T("Mở yêu cầu chờ của đúng tài khoản và DB. Không gửi lại tự động.")); await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) await Reload(); }
    private async Task Preview()
    {
        var rows = await employees.ListAsync(session, new()); if (rows.Count == 0) throw new ArgumentException(L.T("Chưa có nhân viên."));
        // Selector tải đủ trang, không giới hạn người có thể chốt trong50 hồ sơ đầu.
        for (var skip = rows.Count; skip < rows[0].TotalRows; skip += 50) rows.AddRange(await employees.ListAsync(session, new(Skip: skip)));
        using var d = Dialog(L.T("Preview và chốt bảng lương"), "PayrollCommit"); d.ClientSize = new(900, 640); d.SetWriteAvailable(false);
        var employee = new ComboBox { DataSource = rows, DisplayMember = "Name", ValueMember = "Code", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top }; d.AddField(L.T("Nhân viên"), employee);
        var m = EmployeesControl.Choice(Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray()); m.SelectedIndex = month.SelectedIndex; d.AddField(L.T("Tháng"), m);
        var y = new NumericUpDown { Minimum = 2000, Maximum = 9999, Value = year.Value, Dock = DockStyle.Top }; d.AddField(L.T("Năm"), y);
        d.Note(L.T("Để trống giờ thường để SQL dùng tổng công; phụ cấp trống dùng chức vụ hiện tại. Tăng ca do quản lý duyệt, không tự suy hệ số. Chốt khóa nhập/sửa công còn lại của nhân viên trong kỳ. Kỳ cũ dùng mức lương hồ sơ hiện tại, không suy lịch sử mức lương."));
        var regular = d.TextField(L.T("Giờ thường (trống=tổng giờ)")); var overtime = d.TextField(L.T("Giờ tăng ca"), "0"); var overtimePay = d.TextField(L.T("Tiền tăng ca"), "0"); var allowance = d.TextField(L.T("Phụ cấp (trống=từ chức vụ)")); var bonus = d.TextField(L.T("Thưởng"), "0"); var deduction = d.TextField(L.T("Khấu trừ"), "0");
        var previewButton = new OrbitButton { Text = L.T("Xem preview SQL") }; d.AddField(L.T("Tính chính thức"), previewButton);
        var result = new TextBox { ReadOnly = true, Multiline = true, Height = 150, Dock = DockStyle.Top, ScrollBars = ScrollBars.Vertical }; d.AddField(L.T("Kết quả preview"), result);
        var source = new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = true, Dock = DockStyle.Fill, Height = 160 }; d.AddField(L.T("Công của kỳ"), source);
        PayrollPreview? last = null; PayrollRequest? approved = null; int generation = 0;
        void InvalidatePreview() { generation++; last = null; approved = null; d.SetWriteAvailable(false); result.Text = L.T("Nội dung đã thay đổi; xem preview mới trước khi chốt."); }
        foreach (var box in new[] { regular, overtime, overtimePay, allowance, bonus, deduction }) box.TextChanged += (_, _) => InvalidatePreview();
        employee.SelectedValueChanged += (_, _) => InvalidatePreview(); m.SelectedValueChanged += (_, _) => InvalidatePreview(); y.ValueChanged += (_, _) => InvalidatePreview();
        PayrollRequest Request() => new((string)employee.SelectedValue!, m.SelectedIndex + 1, (int)y.Value, string.IsNullOrWhiteSpace(regular.Text) ? null : EmployeesControl.Amount(regular), EmployeesControl.Amount(overtime), EmployeesControl.Amount(overtimePay), string.IsNullOrWhiteSpace(allowance.Text) ? null : EmployeesControl.Amount(allowance), EmployeesControl.Amount(bonus), EmployeesControl.Amount(deduction));
        previewButton.Click += async (_, _) =>
        {
            var current = ++generation; previewButton.Enabled = false; d.SetWriteAvailable(false);
            try
            {
                var request = Request(); var p = await service.PreviewAsync(session, request); if (d.IsDisposed || current != generation) return;
                last = p; var s = p.Snapshot; approved = request with { Regular = s.RegularHours, Allowance = s.Allowance };
                result.Text = PayrollDetailDialog.Describe(s) + (s.Issues.Count == 0 ? L.T("\r\nCông hợp lệ, có thể xác nhận chốt.") : "\r\n" + string.Join("\r\n", s.Issues.Select(i => i.Message)));
                source.DataSource = s.Attendance; d.SetWriteAvailable(s.Issues.Count == 0);
                foreach (DataGridViewColumn col in source.Columns)
                {
                    col.HeaderText=col.DataPropertyName switch { "Employee"=>L.T("Mã NV"),"WorkDate"=>L.T("Ngày làm"),"Shift"=>L.T("Mã ca"),"ClockIn"=>L.T("Giờ vào"),"ClockOut"=>L.T("Giờ ra"),"Status"=>L.T("Trạng thái"),_=>col.HeaderText };
                    if (col.DataPropertyName is "ClockIn" or "ClockOut") col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                    if(col.DataPropertyName=="WorkDate")col.DefaultCellStyle.Format="dd/MM/yyyy";
                }
                d.Confirmation = L.F("Chốt lương {0} kỳ{1}/{2}, thực lĩnh{3} ₫? Kỳ công sẽ khóa; chi lương là bước riêng.", s.Employee, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:00}", s.Month), s.Year, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", s.Amount));
            }
            catch (Exception e) when (e is UserOperationException or ArgumentException or OperationCanceledException) { if (!d.IsDisposed) result.Text = e.Message; }
            finally { if (!d.IsDisposed) previewButton.Enabled = true; }
        };
        d.CaptureCommand = () =>
        {
            if (last is null || approved is null || last.Snapshot.Issues.Count != 0) throw new ArgumentException(L.T("Xem và xác nhận preview SQL hợp lệ trước."));
            var r = approved; var token = last.Token;
            return (JsonSerializer.Serialize(new { Request = r, Token = token }), id => service.CommitAsync(session, r, token, id));
        };
        await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) await Reload();
    }
    private async Task Detail() { if (Selected is PayrollRow row) await links.ShowPayrollAsync(this, row.Id); }
    private async Task Pay()
    {
        if (Selected is not PayrollRow selected) return;
        var row = await service.GetAsync(session, selected.Id) ?? throw new ArgumentException(L.T("Bảng lương không còn tồn tại."));
        if (row.Status != "Unpaid") throw new ArgumentException(L.T("Bảng lương ") + row.StatusText + L.T(", không thể chi mới."));
        using var d = Dialog(L.T("Chi toàn bộ lương"), "PayrollPay");
        d.Note(L.F("BL{0} · {1} · {2}/{3}\r\nThực lĩnh: {4} ₫. Chi toàn bộ một lần; phiếu chi do SQL tạo.", row.Id, row.EmployeeName, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:00}", row.Month), row.Year, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Amount)));
        var date = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm:ss", Value = DateTime.Now, Dock = DockStyle.Top }; d.AddField(L.T("Ngày chi"), date);
        d.Confirmation = L.F("Chi {0} ₫ cho bảng lương{1}? Không thể chi lần hai.", string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Amount), row.Id);
        d.CaptureCommand = () => { var value = date.Value.AddTicks(-(date.Value.Ticks % TimeSpan.TicksPerSecond)); return (JsonSerializer.Serialize(new { row.Id, Date = value, row.Amount }), id => service.PayAsync(session, row, value, id)); };
        await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) await Reload();
    }
}
