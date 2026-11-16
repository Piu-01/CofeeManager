using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Requests.Assets;
using CafeManager.Models.Dtos.Assets;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Assets;

// Yêu cầu giữ nguyên qua timeout/restart; chỉ gửi sau khi lưu file chờ scoped DB.
internal sealed class AssetEditorDialog : OrbitDialogForm
{
    private readonly AssetService service;
    private readonly SessionContext session;
    private readonly IPendingOperationStore pending;
    private readonly TableLayoutPanel fields = new() { Dock=DockStyle.Fill, ColumnCount=1, AutoScroll=true };
    private readonly Label status = new() { AutoSize=true };
    private readonly TextBox name=new(),date=new(),amount=new(),months=new();
    private readonly OrbitButton save=new(){Text=L.T("Lưu"),IsPrimary=true},check=new(){Text=L.T("Kiểm tra kết quả")},close=new(){Text=L.T("Đóng")};
    private readonly ErrorProvider errors=new(){BlinkStyle=ErrorBlinkStyle.NeverBlink};
    private AssetWriteRequest request;
    private bool busy,unknown,saved,dirty,corrupt;
    public bool Changed => saved;
    public AssetEditorDialog(AssetService service,SessionContext session,IPendingOperationStore pending,AssetRow? row=null,bool delete=false)
    {
        this.service=service;this.session=session;this.pending=pending;
        request=new(Guid.NewGuid(),delete?"Delete":row is null?"Create":"Update",row?.Code,row?.Revision,row is null?null:new(row.Name,row.PurchasedOn,row.Value,row.Months));
        Text=delete?L.T("Xóa tài sản"):L.T("Hồ sơ tài sản");AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(620,440);errors.ContainerControl=this;AcceptButton=null;
        void Add(string label,Control input){input.Dock=DockStyle.Top;fields.RowStyles.Add(new(SizeType.AutoSize));fields.Controls.Add(new OrbitDialogField(label,input),0,fields.RowCount++);}
        Add(L.T("Tên tài sản"),name);Add(L.T("Ngày mua (dd/MM/yyyy)"),date);Add(L.T("Nguyên giá (nguyên VND)"),amount);Add(L.T("Số tháng khấu hao tham khảo"),months);
        foreach(var box in new[]{name,date,amount,months})box.TextChanged+=(_,_)=>dirty=true;
        var actions=new FlowLayoutPanel();actions.Controls.AddRange([save,check,close]);var layout=new OrbitDialogLayout(this,fields,status,actions);Controls.Add(layout);layout.EnableResponsiveActions(this);
        save.Click+=async(_,_)=>await Save();check.Click+=async(_,_)=>await Reconcile();close.Click+=(_,_)=>Close();
        Shown+=(_,_)=>{
            try{var stored=pending.Read(session.Identity.AccountId,"AssetWrite");if(stored is not null){request=JsonSerializer.Deserialize<AssetWriteRequest>(stored.Payload)??throw new InvalidDataException(L.T("File chờ rỗng."));if(request.RequestId!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu chờ không khớp."));unknown=true;}}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or global::System.Security.Cryptography.CryptographicException){unknown=corrupt=true;status.Text=e.Message;}
            Fill();dirty=false;State();
        };
        OrbitField.ApplyInputBehavior(this);
    }
    private void Fill(){var value=request.Value;name.Text=value?.Name??"";date.Text=(value?.PurchasedOn??DateOnly.FromDateTime(DateTime.Today)).ToString("dd/MM/yyyy");amount.Text=(value?.Value??0).ToString("0",CultureInfo.InvariantCulture);months.Text=(value?.Months??12).ToString(CultureInfo.InvariantCulture);if(unknown)status.Text=L.F("Yêu cầu {0}: {1} {2}. Đối chiếu trước khi tiếp tục.", request.RequestId, request.Action, request.Code);else if(request.Action=="Delete")status.Text=L.F("Xóa {0}; hồ sơ có phiếu thu chi sẽ bị chặn.", request.Code);}
    private void State(){fields.Enabled=!busy&&!unknown&&!saved&&request.Action!="Delete";save.Enabled=!busy&&!unknown&&!saved;save.Text=request.Action=="Delete"?L.T("Xóa tài sản"):L.T("Lưu tài sản");check.Enabled=!busy&&unknown&&!corrupt;check.Visible=unknown;close.Enabled=!busy&&(!unknown||saved||corrupt);}
    private async Task Save()
    {
        if(busy||unknown||saved)return;errors.Clear();bool sent=false;
        try{
            if(request.Action!="Delete"){
                if(!DateOnly.TryParseExact(date.Text.Trim(),"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)){errors.SetError(date,L.T("Nhập dd/MM/yyyy hợp lệ."));date.Focus();return;}
                if(!decimal.TryParse(amount.Text.Trim(),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var money)){errors.SetError(amount,L.T("Nhập nguyên VND không dấu phân cách."));amount.Focus();return;}
                if(!int.TryParse(months.Text.Trim(),out var count)){errors.SetError(months,L.T("Nhập số tháng nguyên dương."));months.Focus();return;}
                request=request with {Value=AssetService.Normalize(new(name.Text,day,money,count))};
            }
            if(await OrbitConfirmationDialog.ShowAsync(this,request.Action=="Delete"?L.F("Xóa hồ sơ {0}?", request.Code):L.T("Lưu hồ sơ tài sản? Thao tác này không tự ghi phiếu chi."),Text,MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
            busy=true;State();pending.Save(new(session.Identity.AccountId,request.RequestId,"AssetWrite",JsonSerializer.Serialize(request),DateTime.UtcNow));sent=true;
            var result=await service.WriteAsync(session,request);if(!IsDisposed)Complete(result);
        }
        catch(ArgumentException e){if(!IsDisposed){status.Text=e.Message;var target=e.ParamName switch{"Name"=>name,"Months"=>months,_=>amount};errors.SetError(target,e.Message);target.Focus();}}
        catch(UserOperationException e){if(!IsDisposed){unknown=sent&&e.Error.RequiresReconciliation;status.Text=e.Message;if(sent&&!unknown)Clear();}}
        catch(OperationCanceledException){if(!IsDisposed){unknown=sent;status.Text=L.T("Đã dừng chờ; kiểm tra kết quả trước khi tiếp tục.");}}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){if(!IsDisposed){unknown=sent;status.Text=e.Message;}}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private bool Clear(){try{pending.Clear(session.Identity.AccountId,"AssetWrite");return true;}catch(Exception e)when(e is IOException or UnauthorizedAccessException){unknown=true;status.Text=L.T("Chưa dọn được file chờ: ")+e.Message;return false;}}
    private void Complete(AssetReceipt receipt){saved=true;dirty=false;unknown=false;status.Text=L.T("Đã ghi ")+receipt.Operation+": "+receipt.Code+".";if(Clear()){DialogResult=DialogResult.OK;Close();}}
    private async Task Reconcile(){if(busy||corrupt)return;busy=true;State();try{var receipt=await service.ReceiptAsync(session,request.RequestId);if(IsDisposed)return;if(receipt is not null)Complete(receipt);else if(await OrbitConfirmationDialog.ShowAsync(this,L.T("SQL xác minh yêu cầu chưa ghi. Bỏ yêu cầu chờ và nhập lại?"),Text,MessageBoxButtons.OKCancel)==DialogResult.OK&&Clear()){unknown=false;request=request with {RequestId=Guid.NewGuid()};status.Text=L.T("Yêu cầu chưa ghi đã được bỏ; kiểm tra nội dung trước khi lưu.");}}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync(){if(busy||unknown&&!saved&&!corrupt)return false;return saved||!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ nháp và đóng?"),Text,MessageBoxButtons.OKCancel)==DialogResult.OK;}
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
