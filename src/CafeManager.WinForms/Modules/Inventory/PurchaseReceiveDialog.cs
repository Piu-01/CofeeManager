using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;

// Nhận hàng: STORE đếm thực nhận, ADMIN chốt mới cộng kho/chi/nợ. Không giữ khóa khi đếm.
public sealed class PurchaseReceiveDialog : OrbitDialogForm
{
    private sealed class ReceiveRow
    {
        public string Code{get;set;}="";public string Name{get;set;}="";public string Unit{get;set;}="";
        public decimal Ordered{get;set;}public decimal? Actual{get;set;}public string? Reason{get;set;}
    }
    private static readonly CultureInfo Vi=G05Display.Culture;
    private readonly PurchaseService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;private readonly string code;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,EditMode=DataGridViewEditMode.EditOnEnter};
    private readonly BindingList<ReceiveRow> lines=[];
    private readonly TextBox immediate=new(){Width=150,TextAlign=HorizontalAlignment.Right};
    private readonly Label info=new(){AutoSize=true,Dock=DockStyle.Top,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Small};
    private readonly Label status=new(){AutoSize=true};
    private readonly OrbitButton save=new(){Text=L.T("Lưu số kiểm")},post=new(){Text=L.T("Chốt nhập kho")},check=new(){Text=L.T("Kiểm tra kết quả"),Visible=false},print=new(){Text=L.T("In phiếu")};
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
    private PurchaseRow? row;private PurchaseCountRequest? countRequest;private PurchasePostRequest? postRequest;private string? pendingOperation;private bool busy,unknown,dirty,blocked;
    public PurchaseReceiveDialog(PurchaseService service,SessionContext session,IPendingOperationStore pending,string code)
    {
        this.service=service;this.session=session;this.pending=pending;this.code=code;
        Text=L.T("Nhận hàng — ")+code;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(980,620);MinimumSize=new Size(700,440);
        foreach(var col in new[]{("Code",L.T("MÃ NVL")),("Name",L.T("NGUYÊN LIỆU")),("Unit",L.T("ĐVT")),("Ordered",L.T("SỐ ĐẶT")),("Actual",L.T("THỰC NHẬN")),("Reason",L.T("LÝ DO LỆCH"))})
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,ReadOnly=col.Item1 is "Code" or "Name" or "Unit" or "Ordered",AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});
        grid.Columns["Name"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;grid.Columns["Reason"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.DataSource=lines;
        grid.CellValidating+=(_,e)=>ValidateCell(e);
        grid.CellValidated+=(_,e)=>{if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText="";};
        grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text=L.T("Số đếm chưa hợp lệ; sửa ô trước khi lưu.");};
        grid.CellValueChanged+=(_,_)=>dirty=true;
        var immediateRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=true};
        immediateRow.Controls.AddRange([new Label{Text=L.T("Trả ngay khi chốt (₫)"),AutoSize=true},immediate]);
        immediate.ReadOnly=session.Identity.Role!="ADMIN";if(immediate.ReadOnly)immediateRow.Controls.Add(new Label{Text=L.T("Do quản lý chốt"),AutoSize=true});
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        body.Controls.Add(info,0,0);body.Controls.Add(immediateRow,0,1);body.Controls.Add(grid,0,2);
        var close=new OrbitButton{Text=L.T("Quay lại")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,print,save,post,close]);
        var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        grid.Columns["Actual"]!.DefaultCellStyle.Format="N0";grid.Columns["Ordered"]!.DefaultCellStyle.Format="N0";grid.Columns["Ordered"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;grid.Columns["Actual"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
        immediate.Leave+=(_,_)=>{if(decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash))immediate.Text=cash.ToString("N0",Vi);};
        save.Click+=async(_,_)=>await SaveCount();post.Click+=async(_,_)=>await Post();check.Click+=async(_,_)=>await Reconcile();print.Click+=async(_,_)=>await Print();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private bool Editable=>!blocked&&row is{Status:"ChoNhan" or "DaKiem"}&&!busy&&!unknown;
    private bool IsAdmin=>session.Identity.Role=="ADMIN";
    private void State()
    {
        grid.ReadOnly=!Editable;body.Enabled=!busy&&!unknown;immediate.ReadOnly=!IsAdmin||row is null||row.Status=="DaChot";
        save.Enabled=Editable;post.Enabled=Editable&&row?.Status=="DaKiem"&&IsAdmin;
        print.Enabled=!busy&&row is not null;check.Visible=unknown&&!blocked;check.Enabled=!busy;
        if(row is not null)Text=L.T("Nhận hàng — ")+row.Code+" · "+row.StatusText;
    }
    private async Task Initialize()
    {
        busy=true;State();
        try
        {
            foreach(var operation in new[]{"PurchaseCount","PurchaseReceive"})
            {
                var stored=pending.Read(session.Identity.AccountId,operation);
                if(stored is null)continue;
                pendingOperation=operation;
                if(operation=="PurchaseCount")countRequest=JsonSerializer.Deserialize<PurchaseCountRequest>(stored.Payload)??throw new InvalidDataException(L.T("Yêu cầu kiểm nhận chưa hợp lệ."));
                else postRequest=JsonSerializer.Deserialize<PurchasePostRequest>(stored.Payload)??throw new InvalidDataException(L.T("Yêu cầu chốt nhận chưa hợp lệ."));
                var id=countRequest?.RequestId??postRequest?.RequestId;
                if(id!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu không khớp."));
                var owner=countRequest?.Code??postRequest!.Code;
                if(owner!=code){blocked=true;status.Text=L.F("Còn yêu cầu {0} chưa đối chiếu cho phiếu {1}; mở phiếu đó xử lý trước.",operation,owner);countRequest=null;postRequest=null;pendingOperation=null;continue;}
                unknown=true;status.Text=L.F("Cần đối chiếu {0}: {1}", operation, stored.RequestId);
                break;
            }
            await LoadRow();
        }
        catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or ArgumentException){status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task LoadRow()
    {
        var loaded=await service.GetAsync(session,code);if(IsDisposed)return;
        row=loaded??throw new ArgumentException(L.T("Phiếu không tồn tại."));
        lines.Clear();foreach(var line in row.Lines)lines.Add(new(){Code=line.Code,Name=line.Name,Unit=line.Unit,Ordered=line.Quantity,Actual=line.Received,Reason=line.Reason});
        dirty=false;
        info.Text=L.F("Nhà cung cấp {0} · Ngày nhập {1} · {2}", row.Supplier, row.Date.ToString("dd/MM/yyyy",Vi), row.StatusText)+(row.CountedAt is DateTime counted?L.F(" · Kiểm {0} bởi {1}",counted.ToString("dd/MM/yyyy HH:mm",Vi),row.CountedBy??"—"):string.Empty);
        immediate.Text=(row.Immediate??row.PlannedImmediate).ToString("N0",Vi);
        State();
    }
    private void ValidateCell(DataGridViewCellValidatingEventArgs e)
    {
        if(e.RowIndex<0||e.ColumnIndex<0)return;
        var name=grid.Columns[e.ColumnIndex].DataPropertyName;
        var text=Convert.ToString(e.FormattedValue)??"";
        if(name=="Actual"&&text.Trim().Length>0)
        {
            if(!decimal.TryParse(text,NumberStyles.Number,Vi,out var value)||value<0||decimal.Truncate(value)!=value)
            {e.Cancel=true;grid.Rows[e.RowIndex].ErrorText=L.T("Thực nhận phải là số nguyên không âm.");status.Text=grid.Rows[e.RowIndex].ErrorText;}
            else if(grid.Rows[e.RowIndex].DataBoundItem is ReceiveRow line&&value>line.Ordered)
            {e.Cancel=true;grid.Rows[e.RowIndex].ErrorText=L.T("Thực nhận không vượt số đặt; giao bổ sung lập phiếu mới.");status.Text=grid.Rows[e.RowIndex].ErrorText;}
        }
        if(name=="Reason"&&text.Length>200){e.Cancel=true;grid.Rows[e.RowIndex].ErrorText=L.T("Lý do lệch tối đa 200 ký tự.");status.Text=grid.Rows[e.RowIndex].ErrorText;}
    }
    private bool ValidateCounts()
    {
        if(row is null)return false;
        foreach(DataGridViewRow item in grid.Rows)
        {
            item.ErrorText="";
            if(item.DataBoundItem is not ReceiveRow line)continue;
            if(line.Actual is null){item.ErrorText=L.T("Cần đếm dòng này.");grid.CurrentCell=item.Cells["Actual"];status.Text=item.ErrorText;return false;}
            if(line.Actual!=line.Ordered&&string.IsNullOrWhiteSpace(line.Reason)){item.ErrorText=L.T("Lệch số đặt phải ghi lý do.");grid.CurrentCell=item.Cells["Reason"];status.Text=item.ErrorText;return false;}
        }
        return true;
    }
    private PurchaseCountRequest BuildCount()
    {
        var result=lines.Select(l=>new PurchaseReceiveLine(l.Code,l.Actual!.Value,string.IsNullOrWhiteSpace(l.Reason)?null:l.Reason.Trim())).ToList();
        return new(Guid.NewGuid(),row!.Code,row.Revision,result);
    }
    private async Task SaveCount()
    {
        if(!Editable||row is null)return;
        if(!grid.EndEdit()){status.Text=L.T("Sửa ô đang nhập trước khi lưu.");return;}
        if(row.Status=="DaChot"||row.Status=="DaHuy"){status.Text=L.T("Phiếu đã chốt hoặc đã huỷ.");return;}
        if(!ValidateCounts())return;
        countRequest=BuildCount();pendingOperation="PurchaseCount";
        await Send(()=>service.SaveCountAsync(session,countRequest));
    }
    private async Task Post()
    {
        if(!Editable||row is null)return;
        if(row.Status!="DaKiem"){status.Text=L.T("Lưu số kiểm trước khi chốt nhập kho.");return;}
        if(!decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash)||cash<0||decimal.Truncate(cash)!=cash){status.Text=L.T("Trả ngay phải là số nguyên VND không âm.");immediate.Focus();return;}
        var prices=row.Lines.ToDictionary(x=>x.Code,x=>x.Price,StringComparer.OrdinalIgnoreCase);
        var actual=ActualTotal(prices);
        if(cash>actual){status.Text=L.T("Trả ngay vượt tổng thực nhận.");return;}
        var debt=actual-cash;
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Chốt nhập kho phiếu {0}? Tồn tăng theo thực nhận {1} ₫; trả ngay {2} ₫; nợ {3} ₫. Sau chốt phiếu chỉ đọc.", row.Code, actual.ToString("N0",Vi), cash.ToString("N0",Vi), debt.ToString("N0",Vi)),L.T("Chốt nhập kho"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        postRequest=new(Guid.NewGuid(),row.Code,row.Revision,cash);pendingOperation="PurchaseReceive";
        await Send(()=>service.PostAsync(session,postRequest));
    }
    private decimal ActualTotal(Dictionary<string,decimal> prices)=>lines.Sum(l=>(l.Actual??l.Ordered)*(prices.TryGetValue(l.Code,out var price)?price:0));
    private async Task Send(Func<Task<string>> action)
    {
        if(busy)return;busy=true;State();var posted=false;
        string payload;Guid requestId;
        if(pendingOperation=="PurchaseCount"&&countRequest is not null){payload=JsonSerializer.Serialize(countRequest);requestId=countRequest.RequestId;}
        else if(postRequest is not null){payload=JsonSerializer.Serialize(postRequest);requestId=postRequest.RequestId;}
        else{busy=false;State();return;}
        try
        {
            pending.Save(new(session.Identity.AccountId,requestId,pendingOperation!,payload,DateTime.UtcNow));
            var saved=await action();posted=true;await LoadRow();pending.Clear(session.Identity.AccountId,pendingOperation!);unknown=false;pendingOperation=null;countRequest=null;postRequest=null;status.Text=L.T("Đã lưu phiếu ")+saved;
        }
        catch(UserOperationException e){unknown=unknown||posted||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,pendingOperation!);pendingOperation=null;countRequest=null;postRequest=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text=L.T("Đã dừng chờ; cần đối chiếu.");}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        catch(Exception e){status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Reconcile()
    {
        if(busy)return;
        var id=countRequest?.RequestId??postRequest?.RequestId;
        if(id is null||pendingOperation is null){status.Text=L.T("Không có yêu cầu đang chờ.");return;}
        busy=true;State();
        try
        {
            var found=await service.ReconcileAsync(session,id.Value);
            if(found is not null){await LoadRow();pending.Clear(session.Identity.AccountId,pendingOperation);unknown=false;pendingOperation=null;countRequest=null;postRequest=null;status.Text=L.T("Đã đối chiếu phiếu ")+found;}
            else
            {
                status.Text=L.T("Chưa có receipt; yêu cầu trước có thể còn chạy. Chỉ gửi lại đúng yêu cầu đang lưu.");
                busy=false;State();
                if(await OrbitConfirmationDialog.ShowAsync(this,L.T("Gửi lại đúng yêu cầu đang lưu, giữ nguyên mã và nội dung?"),L.T("Đối chiếu yêu cầu"),MessageBoxButtons.YesNo)==DialogResult.Yes)
                {
                    if(pendingOperation=="PurchaseCount"&&countRequest is not null)await Send(()=>service.SaveCountAsync(session,countRequest));
                    else if(postRequest is not null)await Send(()=>service.PostAsync(session,postRequest));
                }
                return;
            }
        }
        catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Print()
    {
        if(row is null||busy)return;busy=true;State();
        try{await PurchasePrinting.PreviewAsync(this,row);}
        catch(Exception e)when(e is not OperationCanceledException){status.Text=L.T("Chưa in được phiếu: ")+e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    protected override async Task<bool> CanCloseAsync()=>!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ số đếm chưa lưu? Phiếu vẫn ở trạng thái trước đó."),L.T("Bỏ số đếm"),MessageBoxButtons.YesNo)==DialogResult.Yes);
}
