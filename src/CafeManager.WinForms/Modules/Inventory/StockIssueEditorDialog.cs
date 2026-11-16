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

public sealed class StockIssueEditorDialog : OrbitDialogForm
{
    private sealed class Draft{public string Code{get;set;}="";public string Name{get;set;}="";public string Unit{get;set;}="";public decimal Quantity{get;set;}}
    private readonly StockIssueService service;private readonly PurchaseService lookups;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly ComboBox kind=new(){DropDownStyle=ComboBoxStyle.DropDownList},material=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=310};private readonly TextBox reason=new(){Width=280};private readonly DateTimePicker date=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy"};
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=false,EditMode=DataGridViewEditMode.EditOnEnter};private readonly BindingList<Draft> lines=[];
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};private readonly Label status=new(){AutoSize=true};private readonly OrbitButton save=new(){Text=L.T("Chốt xuất")},check=new(){Text=L.T("Kiểm tra kết quả"),Visible=false};
    private StockIssueRequest? request;private bool busy,unknown,dirty,loadFailed;
    public StockIssueEditorDialog(StockIssueService service,PurchaseService lookups,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.lookups=lookups;this.session=session;this.pending=pending;
        Text=L.T("Xuất nguyên liệu — Chưa ghi kho");AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);MinimumSize=new Size(640,420);
        kind.Items.AddRange([L.T("Hủy do hỏng"),L.T("Trả hàng")]);kind.SelectedIndex=0;
        var header=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};header.Controls.AddRange([new Label{Text=L.T("Loại xuất"),AutoSize=true},kind,date,new Label{Text=L.T("Lý do"),AutoSize=true},reason,new Label{Text=L.T("Trả hàng chưa tự hoàn tiền/cấn nợ. Đồ đã pha bị hủy: chỉ xuất lượng thực dùng, tránh ghi kép tiêu hao bán."),AutoSize=true}]);
        foreach(var c in new[]{("Code",L.T("MÃ")),("Name",L.T("NGUYÊN LIỆU")),("Unit",L.T("ĐƠN VỊ")),("Quantity",L.T("LƯỢNG XUẤT"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=c.Item1,DataPropertyName=c.Item1,HeaderText=c.Item2,ReadOnly=c.Item1!="Quantity",AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});grid.DataSource=lines;grid.CellValueChanged+=(_,_)=>dirty=true;grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text=L.T("Lượng xuất phải là số hợp lệ.");};
        var tools=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};var add=new OrbitButton{Text=L.T("Thêm dòng")};var remove=new OrbitButton{Text=L.T("Bỏ dòng")};tools.Controls.AddRange([material,add,remove]);
        add.Click+=(_,_)=>{if(material.SelectedItem is not InventoryLookup l)return;var old=lines.FirstOrDefault(x=>x.Code==l.Code);if(old is not null){grid.CurrentCell=grid.Rows[lines.IndexOf(old)].Cells["Quantity"];grid.BeginEdit(true);return;}lines.Add(new(){Code=l.Code,Name=l.Name,Unit=l.Unit??""});dirty=true;};remove.Click+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is Draft d){lines.Remove(d);dirty=true;}};
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.Controls.Add(header,0,0);body.Controls.Add(grid,0,1);body.Controls.Add(tools,0,2);
        var close=new OrbitButton{Text=L.T("Quay lại")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,save,close]);var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        reason.TextChanged+=(_,_)=>dirty=true;date.ValueChanged+=(_,_)=>dirty=true;kind.SelectedIndexChanged+=(_,_)=>dirty=true;save.Click+=async(_,_)=>await Save();check.Click+=async(_,_)=>await Reconcile();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private void State(){body.Enabled=!busy&&!unknown;save.Enabled=!busy&&!loadFailed;save.Text=unknown?L.T("Gửi lại cùng yêu cầu"):L.T("Chốt xuất");check.Visible=unknown;check.Enabled=!busy;}
    private async Task Initialize()
    {
        busy=true;State();try
        {
            var materials=await lookups.LookupsAsync(session,"Material");material.DisplayMember=nameof(InventoryLookup.Name);material.ValueMember=nameof(InventoryLookup.Code);material.DataSource=materials;var stored=pending.Read(session.Identity.AccountId,"StockIssueCreate");
            if(stored is not null)
            {
                request=JsonSerializer.Deserialize<StockIssueRequest>(stored.Payload)??throw new InvalidDataException(L.T("Yêu cầu xuất chưa hợp lệ."));if(request.RequestId!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu không khớp."));date.Value=request.Date;reason.Text=request.Reason;kind.SelectedIndex=request.Kind=="Tra hang"?1:0;
                foreach(var line in request.Lines){var m=materials.FirstOrDefault(x=>x.Code==line.Code);lines.Add(new(){Code=line.Code,Name=m?.Name??line.Code,Unit=m?.Unit??"",Quantity=line.Quantity});}unknown=true;status.Text=L.T("Yêu cầu đang chờ đối chiếu: ")+request.RequestId;
            }dirty=false;
        }catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException){loadFailed=true;status.Text=e.Message;}finally{busy=false;State();}
    }
    private async Task Save()
    {
        if(busy)return;
        if(!unknown)
        {
            if(!grid.EndEdit()){status.Text=L.T("Sửa ô đang nhập trước khi chốt.");return;}
            try{request=StockIssueService.Normalize(new(Guid.NewGuid(),date.Value,kind.SelectedIndex==0?"Huy do hong":"Tra hang",reason.Text,lines.Select(l=>new StockIssueLine(l.Code,l.Quantity)).ToList()));}
            catch(ArgumentException e){status.Text=e.Message;if(string.IsNullOrWhiteSpace(reason.Text))reason.Focus();return;}
        }
        if(request is null)return;
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Chốt xuất {0} nguyên liệu? Toàn phiếu sẽ giảm kho một lần và không sửa được. Trả hàng chưa tự hoàn tiền/cấn nợ.", request.Lines.Count),L.T("Chốt xuất kho"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        busy=true;State();try{pending.Save(new(session.Identity.AccountId,request.RequestId,"StockIssueCreate",JsonSerializer.Serialize(request),DateTime.UtcNow));Complete(await service.CreateAsync(session,request));}
        catch(UserOperationException e){unknown=unknown||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,"StockIssueCreate");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text=L.T("Đã dừng chờ; kiểm tra kết quả.");}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private void Complete(string code){pending.Clear(session.Identity.AccountId,"StockIssueCreate");dirty=false;unknown=false;status.Text=L.T("Đã chốt ")+code;DialogResult=DialogResult.OK;Close();}
    private async Task Reconcile(){if(busy||request is null)return;busy=true;State();try{var code=await service.ReconcileAsync(session,request.RequestId);if(code is not null)Complete(code);else status.Text=L.T("Chưa có receipt; yêu cầu trước có thể còn chạy. Chỉ gửi lại cùng yêu cầu.");}catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync()=>!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ nháp xuất chưa ghi kho?"),L.T("Bỏ nháp"),MessageBoxButtons.YesNo)==DialogResult.Yes);
}



