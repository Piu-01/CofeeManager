using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Finance;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Finance;

public sealed class DebtPaymentDialog : OrbitDialogForm
{
    private readonly SupplierDebtService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;private readonly string code;
    private readonly Label source=new(){AutoSize=true},status=new(){AutoSize=true};private readonly TextBox amount=new();private readonly ComboBox method=new(){DropDownStyle=ComboBoxStyle.DropDownList};private readonly DateTimePicker date=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm"};
    private readonly TableLayoutPanel fields=new(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=4,AutoScroll=true};private readonly OrbitButton pay=new(){Text="Trả nợ"},check=new(){Text="Kiểm tra kết quả",Visible=false};private readonly ErrorProvider errors=new();
    private SupplierDebtRequest? request;private SupplierDebtRow? snapshot;private bool busy,unknown,dirty,loadFailed;
    public DebtPaymentDialog(SupplierDebtService service,SessionContext session,IPendingOperationStore pending,string code)
    {
        this.service=service;this.session=session;this.pending=pending;this.code=code;
        Text="Trả nợ nhà cung cấp";AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(620,440);MinimumSize=new Size(480,360);errors.ContainerControl=this;
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add("Nguồn / Còn nợ",source,0);Add("Số trả (₫)",amount,1);Add("Ngày trả",date,2);Add("Phương thức",method,3);
        var close=new OrbitButton{Text="Quay lại"};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,pay,close]);var dialogLayout=new OrbitDialogLayout(this,fields,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,false);OrbitField.ApplyInputBehavior(this);
        amount.TextChanged+=(_,_)=>dirty=true;date.ValueChanged+=(_,_)=>dirty=true;method.SelectedIndexChanged+=(_,_)=>dirty=true;pay.Click+=async(_,_)=>await Pay();check.Click+=async(_,_)=>await Reconcile();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private void Add(string text,Control control,int row){fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.Controls.Add(new Label{Text=text,AutoSize=true,Margin=new Padding(8)},0,row);control.Dock=DockStyle.Top;control.Margin=new Padding(8);fields.Controls.Add(control,1,row);}
    private void State(){fields.Enabled=!busy&&!unknown;pay.Enabled=!busy&&!loadFailed;pay.Text=unknown?"Gửi lại cùng yêu cầu":"Trả nợ";check.Visible=unknown;check.Enabled=!busy;}
    private async Task Initialize()
    {
        busy=true;State();try
        {
            method.DataSource=await service.MethodsAsync(session);
            var stored=pending.Read(session.Identity.AccountId,"SupplierDebtPay");
            if(stored is not null){request=JsonSerializer.Deserialize<SupplierDebtRequest>(stored.Payload)??throw new InvalidDataException("Yêu cầu trả nợ chưa hợp lệ.");if(request.RequestId!=stored.RequestId)throw new InvalidDataException("Mã yêu cầu không khớp.");amount.Text=request.Amount.ToString("0",CultureInfo.InvariantCulture);date.Value=request.Date;method.SelectedItem=((List<InventoryLookup>)method.DataSource).FirstOrDefault(m=>m.Code==request.Method);unknown=true;status.Text="Cần đối chiếu yêu cầu: "+request.RequestId;}
            await RefreshSource(request?.Code??code);dirty=false;
        }catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or OperationCanceledException){loadFailed=true;status.Text=e.Message;}finally{busy=false;State();}
    }
    private async Task RefreshSource(string debt){snapshot=await service.GetAsync(session,debt);if(snapshot is null)throw new InvalidDataException("Nợ không tồn tại.");source.Text=G05Display.Text($"{snapshot.Code} / {snapshot.PurchaseCode} / {snapshot.Supplier}\r\nCòn nợ hiện tại: {snapshot.Remaining:N0} ₫");}
    private async Task Pay()
    {
        if(busy)return;errors.Clear();
        if(!unknown)
        {
            if(!decimal.TryParse(amount.Text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value)){errors.SetError(amount,"Nhập số tiền nguyên VND.");amount.Focus();return;}
            if(method.SelectedItem is not InventoryLookup selected){errors.SetError(method,"Chọn phương thức.");method.Focus();return;}
            try{request=SupplierDebtService.Normalize(new(Guid.NewGuid(),code,selected.Code,value,date.Value));if(snapshot is null||value>snapshot.Remaining)throw new ArgumentException("Số trả vượt còn nợ; tải lại và sửa số tiền.","Amount");if(date.Value<snapshot.SourceDate)throw new ArgumentException("Ngày trả trước ngày nhập nguồn.","Date");}
            catch(ArgumentException e){var target=e.ParamName=="Date"?(Control)date:amount;errors.SetError(target,e.Message);target.Focus();return;}
        }
        if(request is null)return;
        if(await OrbitConfirmationDialog.ShowAsync(this,G05Display.Text($"Trả {request.Amount:N0} ₫ cho nợ {request.Code}? Sau khi ghi không thể sửa lịch sử."),"Xác nhận trả nợ",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        busy=true;State();try{pending.Save(new(session.Identity.AccountId,request.RequestId,"SupplierDebtPay",JsonSerializer.Serialize(request),DateTime.UtcNow));Complete(await service.PayAsync(session,request));}
        catch(UserOperationException e){unknown=unknown||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){pending.Clear(session.Identity.AccountId,"SupplierDebtPay");request=null;try{await RefreshSource(code);}catch(Exception read)when(read is UserOperationException or IOException){status.Text+=" "+read.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text="Đã dừng chờ; kiểm tra kết quả trước khi gửi lại.";}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private void Complete(string finance){pending.Clear(session.Identity.AccountId,"SupplierDebtPay");unknown=false;dirty=false;status.Text="Đã trả nợ, phiếu chi "+finance;DialogResult=DialogResult.OK;Close();}
    private async Task Reconcile(){if(busy||request is null)return;busy=true;State();try{var finance=await service.ReconcileAsync(session,request.RequestId);if(finance is not null)Complete(finance);else status.Text="Chưa có receipt; phiên trước có thể còn chạy. Chỉ gửi lại cùng yêu cầu.";}catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync()=>!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,"Bỏ thông tin trả nợ chưa ghi?","Bỏ nháp",MessageBoxButtons.YesNo)==DialogResult.Yes);
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}



