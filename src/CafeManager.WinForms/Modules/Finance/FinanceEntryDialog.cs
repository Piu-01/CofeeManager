using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Requests.Finance;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;
namespace CafeManager.WinForms.Modules.Finance;

public sealed class FinanceEntryDialog : OrbitDialogForm
{
    private readonly FinanceService service;
    private readonly SessionContext session;
    private readonly IPendingOperationStore pending;
    private readonly ComboBox category = new() { DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly DateTimePicker date = new() { Format=DateTimePickerFormat.Custom, CustomFormat="dd/MM/yyyy HH:mm" };
    private readonly TextBox amount = new(), asset = new(), note = new() { Multiline=true, Height=72 };
    private readonly Label status = new() { AutoSize=true };
    private readonly OrbitButton save = new() { Text=L.T("Ghi phiếu") }, check = new() { Text=L.T("Kiểm tra kết quả"), Visible=false }, close = new() { Text=L.T("Quay lại") };
    private readonly ErrorProvider errors = new();
    private readonly TableLayoutPanel fields = new() { AutoScroll=true, ColumnCount=2, RowCount=5, Dock=DockStyle.Fill };
    private FinanceRequest? request;
    private bool busy, unknown, dirty, loadFailed;
    public string? ResultCode { get; private set; }
    public FinanceEntryDialog(FinanceService service,SessionContext session,IPendingOperationStore pending,AssetService? assets=null)
    {
        this.service=service; this.session=session; this.pending=pending;
        Text=L.T("Ghi phiếu thu chi"); AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=AutoScaleMode.Dpi;
        ClientSize=new Size(620,440); MinimumSize=new Size(480,360); errors.ContainerControl=this;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        AddField(L.T("Danh mục"),category,0); AddField(L.T("Ngày lập"),date,1); AddField(L.T("Số tiền (₫)"),amount,2); AddField(L.T("Mã tài sản (tùy chọn)"),asset,3); AddField(L.T("Ghi chú"),note,4);
        if(assets is not null){var pick=new OrbitButton{Text=L.T("Chọn tài sản")};var tools=new FlowLayoutPanel{AutoSize=true};tools.Controls.Add(pick);AddField(L.T("Tra tài sản"),tools,5);pick.Click+=async(_,_)=>{if(busy||unknown)return;using var dialog=new Modules.Assets.AssetPickerDialog(assets,session);if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK&&dialog.SelectedAsset is { } selected)asset.Text=selected.Code;};}
        var actions=new FlowLayoutPanel { AutoSize=true }; actions.Controls.AddRange([check,save,close]);
        var dialogLayout=new OrbitDialogLayout(this,fields,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,false); OrbitField.ApplyInputBehavior(this);
        foreach(var c in new Control[]{amount,asset,note}) c.TextChanged+=(_,_)=>dirty=true;
        category.SelectedIndexChanged+=(_,_)=>dirty=true; date.ValueChanged+=(_,_)=>dirty=true;
        save.Click+=async(_,_)=>await Save(); check.Click+=async(_,_)=>await Reconcile(); close.Click+=(_,_)=>Close();
        Shown+=async(_,_)=>await LoadCategories(); AcceptButton=null;
    }
    private void AddField(string label,Control input,int row)
    {
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.Controls.Add(new Label { Text=label,AutoSize=true,Margin=new Padding(4,12,12,8) },0,row);
        input.Dock=DockStyle.Top; input.Margin=new Padding(4,8,4,8); fields.Controls.Add(input,1,row); OrbitManagementTheme.DialogInput(input);
    }
    private void State()
    {
        fields.Enabled=!busy&&!unknown; save.Enabled=!busy&&!loadFailed; check.Enabled=!busy; close.Enabled=!busy&&!unknown;
        check.Visible=unknown; save.Text=unknown?L.T("Gửi lại cùng yêu cầu"):L.T("Ghi phiếu");
    }
    private async Task LoadCategories()
    {
        busy=true; State();
        try
        {
            category.DisplayMember=nameof(FinanceCategory.Name);category.ValueMember=nameof(FinanceCategory.Code);category.DataSource=await service.CategoriesAsync(session);
            var stored=pending.Read(session.Identity.AccountId,"FinanceCreate");
            if(stored is not null)
            {
                request=JsonSerializer.Deserialize<FinanceRequest>(stored.Payload)??throw new InvalidDataException(L.T("Yêu cầu lưu chưa hợp lệ."));
                if(request.RequestId!=stored.RequestId) throw new InvalidDataException(L.T("Mã yêu cầu lưu không khớp."));
                category.SelectedItem=((List<FinanceCategory>)category.DataSource).FirstOrDefault(c=>c.Code==request.Category);
                date.Value=request.Date; amount.Text=request.Amount.ToString("0",CultureInfo.InvariantCulture); asset.Text=request.Asset; note.Text=request.Note;
                unknown=true; status.Text=L.F("Yêu cầu {0}: cần kiểm tra kết quả trước khi tiếp tục.", request.RequestId);
            }
            dirty=false;
        }
        catch(Exception e) when(e is UserOperationException or IOException or global::System.Security.Cryptography.CryptographicException or JsonException or ArgumentException)
        { loadFailed=true; status.Text=e.Message; save.Enabled=false; }
        finally { busy=false; State(); if(category.Items.Count==0) save.Enabled=false; }
    }
    private async Task Save()
    {
        if(busy) return;
        errors.Clear();
        if(!unknown)
        {
            if(!decimal.TryParse(amount.Text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value)) { errors.SetError(amount,L.T("Nhập số tiền nguyên VND, không dấu phân cách.")); amount.Focus(); return; }
            if(category.SelectedItem is not FinanceCategory selected) { errors.SetError(category,L.T("Chọn danh mục.")); category.Focus(); return; }
            try { request=FinanceService.Normalize(new(Guid.NewGuid(),date.Value,selected.Code,value,note.Text,asset.Text)); }
            catch(ArgumentException e)
            {
                var target=e.ParamName switch { "Note"=>note,"Asset"=>asset,"Category"=>(Control)category,_=>amount };
                errors.SetError(target,e.Message); target.Focus(); return;
            }
        }
        if(request is null) return;
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Ghi phiếu {0} ₫? Sau khi ghi không thể sửa hoặc xóa.", string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", request.Amount)),L.T("Xác nhận ghi phiếu"),MessageBoxButtons.YesNo)!=DialogResult.Yes) return;
        busy=true; State();
        try
        {
            // Lưu nhận dạng trước gửi để vẫn đối chiếu được sau crash/timeout.
            pending.Save(new(session.Identity.AccountId,request.RequestId,"FinanceCreate",JsonSerializer.Serialize(request),DateTime.UtcNow));
            var code=await service.CreateAsync(session,request);
            Complete(code);
        }
        catch(UserOperationException e)
        {
            unknown=unknown||e.Error.RequiresReconciliation; status.Text=e.Message;
            if(!unknown){try{pending.Clear(session.Identity.AccountId,"FinanceCreate");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}
        }
        catch(OperationCanceledException) { unknown=true; status.Text=L.T("Đã dừng chờ; cần kiểm tra kết quả."); }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException)
        { unknown=request is not null; status.Text=L.T("Không cập nhật được hồ sơ yêu cầu. Giữ mã và kiểm tra kết quả: ")+e.Message; }
        finally { busy=false; if(!IsDisposed) State(); }
    }
    private void Complete(string code)
    {
        pending.Clear(session.Identity.AccountId,"FinanceCreate"); ResultCode=code; unknown=false; dirty=false;
        status.Text=L.T("Đã ghi ")+code; DialogResult=DialogResult.OK; Close();
    }
    private async Task Reconcile()
    {
        if(busy||request is null) return; busy=true; State();
        try
        {
            var code=await service.ReconcileAsync(session,request.RequestId);
            if(code is not null) Complete(code);
            else status.Text=L.T("Chưa có receipt. Yêu cầu trước có thể còn chạy; chỉ gửi lại đúng yêu cầu đã lưu, không tạo phiếu mới.");
        }
        catch(Exception e) when(e is UserOperationException or OperationCanceledException or IOException) { status.Text=e.Message; }
        finally { busy=false; if(!IsDisposed) State(); }
    }
    protected override async Task<bool> CanCloseAsync()
    {
        if(busy||unknown) return false;
        return !dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ nội dung phiếu chưa ghi?"),L.T("Bỏ nháp"),MessageBoxButtons.YesNo)==DialogResult.Yes;
    }
    protected override void Dispose(bool disposing) { if(disposing) errors.Dispose(); base.Dispose(disposing); }
}
