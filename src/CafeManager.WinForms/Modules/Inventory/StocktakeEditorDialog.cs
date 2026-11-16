using System.ComponentModel;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;

public sealed class StocktakeEditorDialog : OrbitDialogForm
{
    private sealed class CountRow
    {
        public string Code{get;set;}="";public string Name{get;set;}="";public string Unit{get;set;}="";public decimal Expected{get;set;}public decimal? Actual{get;set;}public string? Reason{get;set;}
    }
    private readonly StocktakeService service;private readonly PurchaseService lookups;private readonly SessionContext session;private readonly IPendingOperationStore pending;private readonly string? initialCode;
    private readonly CheckedListBox scope=new(){Dock=DockStyle.Top,Height=130,CheckOnClick=true};private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false};private readonly BindingList<CountRow> counts=[];
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};private readonly Label status=new(){AutoSize=true};private readonly OrbitButton create=new(){Text=L.T("Lấy snapshot")},save=new(){Text=L.T("Lưu số đếm")},commit=new(){Text=L.T("Chốt kiểm kê")},check=new(){Text=L.T("Kiểm tra kết quả"),Visible=false},fill=new(){Text=L.T("Điền bằng hệ thống")},refresh=new(){Text=L.T("Tải lại phiếu")};
    private StocktakeRow? row;private StocktakeRequest? request;private bool busy,unknown,dirty,loadFailed;
    public StocktakeEditorDialog(StocktakeService service,PurchaseService lookups,SessionContext session,IPendingOperationStore pending,string? code=null)
    {
        this.service=service;this.lookups=lookups;this.session=session;this.pending=pending;initialCode=code;
        Text=L.T("Kiểm kê");AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);MinimumSize=new Size(640,420);
        var info=new Label{Text=L.T("Chưa đếm để trống. Lệch tồn cần lý do. Tạm dừng phát sinh kho trước khi chốt; không giữ khóa khi đếm."),AutoSize=true,Dock=DockStyle.Top};
        foreach(var c in new[]{("Code",L.T("MÃ")),("Name",L.T("NGUYÊN LIỆU")),("Unit",L.T("ĐƠN VỊ")),("Expected",L.T("TỒN SNAPSHOT")),("Actual",L.T("THỰC ĐẾM")),("Reason",L.T("LÝ DO LỆCH"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=c.Item1,DataPropertyName=c.Item1,HeaderText=c.Item2,ReadOnly=c.Item1 is not("Actual" or "Reason"),AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});grid.DataSource=counts;
        grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text=L.T("Số đếm không hợp lệ; sửa ô trước khi lưu.");};grid.CellValueChanged+=(_,_)=>dirty=true;
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.Controls.Add(info,0,0);body.Controls.Add(scope,0,1);body.Controls.Add(grid,0,2);
        var close=new OrbitButton{Text=L.T("Quay lại")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([create,fill,refresh,save,commit,check,close]);var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        create.Click+=async(_,_)=>await NewSnapshot();save.Click+=async(_,_)=>await Write("StocktakeSave");commit.Click+=async(_,_)=>await Write("StocktakeCommit");check.Click+=async(_,_)=>await Reconcile();
        fill.Click+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is CountRow selected){selected.Actual=selected.Expected;grid.Refresh();dirty=true;}};
        refresh.Click+=async(_,_)=>{if(row is null)return;if(dirty&&await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ số đếm chưa lưu và tải lại phiếu?"),L.T("Tải lại"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{await LoadRow(row.Code);}catch(Exception e)when(e is UserOperationException or OperationCanceledException){status.Text=e.Message;}};
        Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private bool Editable=>row is{Closed:false,CutoverKind:"New"};
    private void State()
    {
        body.Enabled=!busy&&!unknown;grid.ReadOnly=!Editable;scope.Visible=row is null;
        create.Enabled=!busy&&!unknown&&!loadFailed;create.Text=row is null?L.T("Lấy snapshot"):L.T("Snapshot mới");
        save.Enabled=fill.Enabled=!busy&&!unknown&&Editable;commit.Enabled=!busy&&!unknown&&Editable;
        refresh.Enabled=!busy&&!unknown&&row is not null;check.Visible=unknown;check.Enabled=!busy;
    }
    private async Task Initialize()
    {
        busy=true;State();try
        {
            var materials=await lookups.LookupsAsync(session,"Material");foreach(var material in materials)scope.Items.Add(material,true);
            var stored=pending.Read(session.Identity.AccountId,"StocktakeWorkflow");
            if(stored is not null){request=JsonSerializer.Deserialize<StocktakeRequest>(stored.Payload)??throw new InvalidDataException(L.T("Yêu cầu kiểm kê chưa hợp lệ."));if(request.RequestId!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu không khớp."));unknown=true;status.Text=L.F("Cần đối chiếu {0}: {1}", request.Operation, request.RequestId);}
            else if(initialCode is not null)await LoadRow(initialCode);
        }catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or OperationCanceledException){loadFailed=true;status.Text=e.Message;}finally{busy=false;State();}
    }
    private async Task LoadRow(string code)
    {
        var loaded=await service.GetAsync(session,code);if(IsDisposed)return;row=loaded??throw new ArgumentException(L.T("Phiếu không tồn tại."));counts.Clear();foreach(var line in row.Lines)counts.Add(new(){Code=line.Code,Name=line.Name,Unit=line.Unit,Expected=line.Expected,Actual=line.Actual,Reason=line.Reason});dirty=false;
        Text=L.T("Kiểm kê — ")+row.Code;status.Text=$"{row.Code} · Snapshot {row.SnapshotAt:dd/MM/yyyy HH:mm:ss} · "+(row.Closed?L.T("Đã điều chỉnh — chỉ đọc"):row.CutoverKind=="Legacy"?L.T("Dữ liệu cũ thiếu metadata snapshot; chỉ tham khảo"):L.T("Chưa điều chỉnh"));State();
    }
    private async Task NewSnapshot()
    {
        if(busy||unknown)return;
        if(row is not null)
        {
            if(await OrbitConfirmationDialog.ShowAsync(this,L.T("Tạo snapshot mới? Số đếm cũ không được chuyển sang phiếu mới; cần đếm và xác nhận lại."),L.T("Lấy lại số liệu"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            row=null;counts.Clear();dirty=false;State();status.Text=L.T("Chọn phạm vi và bấm Lấy snapshot.");return;
        }
        await Write("StocktakeCreate");
    }
    private async Task Write(string operation)
    {
        if(busy||unknown)return;
        if(!grid.EndEdit()){status.Text=L.T("Sửa ô đang nhập trước khi tiếp tục.");return;}
        if(operation=="StocktakeCommit")
        {
            if(dirty){status.Text=L.T("Lưu số đếm trước khi chốt.");return;}
            foreach(DataGridViewRow item in grid.Rows){item.ErrorText="";if(item.DataBoundItem is CountRow c&&(c.Actual is null||(c.Actual!=c.Expected&&string.IsNullOrWhiteSpace(c.Reason)))){item.ErrorText=L.T("Cần số đếm và lý do khi lệch.");grid.CurrentCell=item.Cells[c.Actual is null?"Actual":"Reason"];status.Text=item.ErrorText;return;}}
            if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Chốt kiểm kê {0}? Tồn sẽ điều chỉnh một lần theo số thực đếm và phiếu bị khóa.", row?.Code),L.T("Chốt kiểm kê"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        }
        request=new(Guid.NewGuid(),operation,row?.Code,row?.Revision,scope.CheckedItems.Cast<InventoryLookup>().Select(m=>m.Code).ToList(),counts.Select(c=>new StocktakeCount(c.Code,c.Actual,c.Reason)).ToList());
        try{StocktakeService.Validate(request);}catch(ArgumentException e){status.Text=e.Message;return;}
        await Send();
    }
    private async Task Send()
    {
        if(request is null)return;busy=true;State();var posted=false;
        try{pending.Save(new(session.Identity.AccountId,request.RequestId,"StocktakeWorkflow",JsonSerializer.Serialize(request),DateTime.UtcNow));var code=await service.WriteAsync(session,request);posted=true;await LoadRow(code);pending.Clear(session.Identity.AccountId,"StocktakeWorkflow");unknown=false;request=null;}
        catch(UserOperationException e){unknown=unknown||posted||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,"StocktakeWorkflow");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text=L.T("Đã dừng chờ; cần đối chiếu.");}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Reconcile()
    {
        if(busy||request is null)return;busy=true;State();
        try{var code=await service.ReconcileAsync(session,request.RequestId);if(code is not null){await LoadRow(code);pending.Clear(session.Identity.AccountId,"StocktakeWorkflow");unknown=false;request=null;}else{status.Text=L.T("Chưa có receipt. Yêu cầu trước có thể còn chạy.");busy=false;State();if(await OrbitConfirmationDialog.ShowAsync(this,L.T("Gửi lại đúng yêu cầu đang lưu, giữ nguyên mã và nội dung?"),L.T("Đối chiếu yêu cầu"),MessageBoxButtons.YesNo)==DialogResult.Yes){await Send();return;}}}
        catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}
    }
    protected override async Task<bool> CanCloseAsync()=>!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ số đếm chưa lưu? Snapshot đã lưu vẫn còn để mở lại."),L.T("Bỏ nháp"),MessageBoxButtons.YesNo)==DialogResult.Yes);
}



