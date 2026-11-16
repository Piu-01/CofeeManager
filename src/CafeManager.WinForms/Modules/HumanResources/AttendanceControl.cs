using System.Data;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.HumanResources;

internal sealed class AttendanceControl : OrbitRecordListControl
{
    private readonly AttendanceService service; private readonly SessionContext session; private readonly IPendingOperationStore pending;
    public AttendanceControl(AttendanceService service, SessionContext session, IPendingOperationStore pending)
    {
        this.service = service; this.session = session; this.pending = pending;
        var month = EmployeesControl.Choice(Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray()); month.SelectedIndex = DateTime.Today.Month - 1;
        var year = new NumericUpDown { Minimum = 2000, Maximum = 9999, Value = DateTime.Today.Year };
        var employee = new TextBox { PlaceholderText = L.T("Mã NV (trống=tất cả)") };
        var status = EmployeesControl.Choice(L.T("Tất cả"), L.T("Đi làm hoàn tất"), L.T("Chưa hoàn tất"), L.T("Nghỉ phép"), L.T("Vắng"));
        string? Status() => status.SelectedIndex switch { 1 => "Complete", 2 => "Incomplete", 3 => "Nghi phep", 4 => "Vang", _ => null };
        Configure("Chấm công", session, async (q, skip, t) => await service.ListAsync(session, new(month.SelectedIndex + 1, (int)year.Value, string.IsNullOrWhiteSpace(employee.Text) ? null : employee.Text.Trim(), Status(), q, skip), t),
            ("Employee", L.T("Mã NV")), ("EmployeeName", L.T("Nhân viên")), ("WorkDate", L.T("Ngày làm")), ("ShiftName", L.T("Ca")), ("ClockIn", L.T("Vào")), ("ClockOut", L.T("Ra")), ("Hours", L.T("Giờ")), ("StatusText", L.T("Trạng thái")), ("PeriodText", L.T("Kỳ lương")));
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((AttendanceRow)rows[0]!).TotalRows;
        UseStackedSearch(L.T("dòng công")); AddFilter(month, 90, true); AddFilter(year, 110); AddFilter(employee, 190); AddFilter(status, 160, true);
        month.SelectedValueChanged += async (_, _) => await ResetAndReload(); year.ValueChanged += async (_, _) => await ResetAndReload(); status.SelectedValueChanged += async (_, _) => await ResetAndReload(); employee.TextChanged += async (_, _) => await ResetAndReload();
        Grid.Columns["WorkDate"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        foreach (var name in new[] { "ClockIn", "ClockOut" }) Grid.Columns[name]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
        AddAction("Thêm", () => Edit(null)); AddAction("Sửa", () => Edit(Selected as AttendanceRow));
    }
    private async Task Edit(AttendanceRow? old)
    {
        if (old?.PeriodLocked == true) throw new ArgumentException(L.T("Kỳ đã chốt lương, chỉ đọc."));
        var options = await service.LookupsAsync(session);
        using var d = new HrEditorDialog(L.T("Chấm công — batch nguyên tử"), "AttendanceBatch", session, pending, id => service.ReceiptAsync(session, id));
        d.ClientSize = new(980, 640); d.Note(L.T("Mỗi dòng có mã nhân viên/ca và ngày làm. Giờ thực có đầy đủ ngày (dd/MM/yyyy HH:mm:ss), để trống khi chưa có. Một dòng lỗi sẽ rollback toàn batch. Dòng cũ giữ nguyên key; chọn Delete để bỏ công sai khi kỳ chưa chốt."));
        var table = new DataTable(); table.Columns.Add("Action", typeof(string)); table.Columns.Add("Employee", typeof(string)); table.Columns.Add("WorkDate", typeof(DateTime)); table.Columns.Add("Shift", typeof(string)); table.Columns.Add("ClockIn", typeof(DateTime)); table.Columns.Add("ClockOut", typeof(DateTime)); table.Columns.Add("Status", typeof(string)); table.Columns.Add("Revision", typeof(byte[]));
        if (old is not null) table.Rows.Add("Update", old.Employee, old.WorkDate, old.Shift, (object?)old.ClockIn ?? DBNull.Value, (object?)old.ClockOut ?? DBNull.Value, old.Status, old.Revision);
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = false, EditMode = DataGridViewEditMode.EditOnEnter, DataSource = table, AllowUserToAddRows = true, AllowUserToDeleteRows = true, Height = 290 };
        void Combo(string property, string header, object source, string display, string value) => grid.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = property, HeaderText = header, Name = property, DataSource = source, DisplayMember = display, ValueMember = value, Width = 160 });
        Combo("Action", L.T("Thao tác"), new[] { new { Code = "Insert", Name = L.T("Thêm") }, new { Code = "Update", Name = L.T("Sửa") }, new { Code = "Delete", Name = L.T("Xóa") } }, "Name", "Code");
        Combo("Employee", L.T("Nhân viên"), options.Where(x => x.Kind == "Employee").ToList(), "Name", "Code");
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WorkDate", Name = "WorkDate", HeaderText = L.T("Ngày làm"), Width = 120, DefaultCellStyle = new() { Format = "dd/MM/yyyy" } });
        Combo("Shift", L.T("Ca"), options.Where(x => x.Kind == "Shift").ToList(), "Name", "Code");
        foreach (var field in new[] { "ClockIn", "ClockOut" }) grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = field, HeaderText = field == "ClockIn" ? L.T("Giờ vào (đủ ngày)") : L.T("Giờ ra (đủ ngày)"), Width = 190, DefaultCellStyle = new() { Format = "dd/MM/yyyy HH:mm:ss", NullValue = "" } });
        Combo("Status", L.T("Trạng thái"), new[] { new { Code = "Di lam", Name = L.T("Đi làm") }, new { Code = "Nghi phep", Name = L.T("Nghỉ phép") }, new { Code = "Vang", Name = L.T("Vắng") } }, "Name", "Code");
        grid.DefaultValuesNeeded += (_, e) => { e.Row.Cells["Action"].Value = "Insert"; e.Row.Cells["WorkDate"].Value = DateTime.Today; e.Row.Cells["Status"].Value = "Di lam"; };
        grid.CellBeginEdit += (_, e) => { if (old is not null && e.RowIndex == 0 && grid.Columns[e.ColumnIndex].DataPropertyName is "Employee" or "WorkDate" or "Shift") e.Cancel = true; };
        grid.UserDeletingRow += (_, e) => { if(e.Row is { DataBoundItem: DataRowView view } gridRow && !view.Row.IsNull("Revision")) { e.Cancel=true; view.Row["Action"]="Delete"; gridRow.ErrorText=L.T("Dòng cũ được đánh dấu Delete; chỉ xóa ở DB khi lưu batch thành công."); } };
        grid.CurrentCellDirtyStateChanged += (_, _) => { if(grid.IsCurrentCellDirty && grid.CurrentCell?.OwningColumn?.DataPropertyName=="Status") grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        bool changingStatus=false;
        grid.CellValueChanged += async (_, e) =>
        {
            if(changingStatus || e.RowIndex<0 || e.ColumnIndex<0 || grid.Columns[e.ColumnIndex].DataPropertyName!="Status" || grid.Rows[e.RowIndex].DataBoundItem is not DataRowView view) return;
            var r=view.Row; if(Convert.ToString(r["Status"])=="Di lam" || r.IsNull("ClockIn") && r.IsNull("ClockOut"))return;
            changingStatus=true;
            try { var confirmed=await OrbitConfirmationDialog.ShowAsync(d,L.T("Nghỉ/Vắng không có giờ thực. Xóa giờ đang nhập của dòng này?"),L.T("Đổi trạng thái công"),MessageBoxButtons.OKCancel); if(d.IsDisposed)return; if(confirmed==DialogResult.OK){r["ClockIn"]=DBNull.Value;r["ClockOut"]=DBNull.Value;}else r["Status"]="Di lam"; }
            finally{changingStatus=false;}
        };
        grid.DataError += (_, e) => { e.ThrowException = false; grid.Rows[e.RowIndex].ErrorText = L.T("Giá trị/ngày giờ không hợp lệ; sửa ô trước khi lưu."); };
        d.AddField(L.T("Các dòng công"), grid);
        d.CaptureCommand = () =>
        {
            if (!grid.EndEdit()) throw new ArgumentException(L.T("Sửa ô nhập sai trước khi lưu.")); d.BindingContext?[table]?.EndCurrentEdit();
            var lines = table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).Select((r, i) => new AttendanceLine(i + 1,
                Convert.ToString(r["Action"])!, Convert.ToString(r["Employee"])!, r.IsNull("WorkDate") ? DateTime.MinValue : (DateTime)r["WorkDate"], Convert.ToString(r["Shift"])!,
                r.IsNull("ClockIn") ? null : (DateTime)r["ClockIn"], r.IsNull("ClockOut") ? null : (DateTime)r["ClockOut"], Convert.ToString(r["Status"])!, r.IsNull("Revision") ? null : (byte[])r["Revision"])).ToArray();
            AttendanceService.Validate(lines); d.Confirmation = L.F("Lưu batch {0} dòng công? Một dòng lỗi sẽ không ghi toàn batch.", lines.Length);
            return (JsonSerializer.Serialize(lines), id => service.SaveAsync(session, lines, id));
        };
        await OrbitDialogHost.ShowAsync(d, this); if (d.Changed) await Reload();
    }
}
