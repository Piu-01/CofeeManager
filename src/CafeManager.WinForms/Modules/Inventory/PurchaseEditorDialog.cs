using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;
namespace CafeManager.WinForms.Modules.Inventory;

public sealed class PurchaseEditorDialog : OrbitDialogForm
{
    private sealed class Draft
    {
        public string Code{get;set;}=""; public string Name{get;set;}="";public string Unit{get;set;}="";
        public decimal Quantity{get;set;} public decimal Price{get;set;}
    }
    private readonly PurchaseService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly ComboBox supplier=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=260},material=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=300};
    private readonly DateTimePicker date=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy"},due=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Checked=false};
    private readonly TextBox immediate=new(){Text="0",Width=140};
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=false,EditMode=DataGridViewEditMode.EditOnEnter};
    private readonly BindingList<Draft> lines=[]; private readonly Label status=new(){AutoSize=true};
    private readonly OrbitButton save=new(){Text="Chốt nhập kho"},check=new(){Text="Kiểm tra kết quả",Visible=false},preview=new(){Text="Tính tổng"};
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
    private PurchaseRequest? request;private bool busy,unknown,dirty,loadFailed;private readonly ErrorProvider errors=new();
    internal string? ResultCode{get;private set;}
    public PurchaseEditorDialog(PurchaseService service,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.session=session;this.pending=pending;
        Text="Nhập hàng — Chưa ghi kho";AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);MinimumSize=new Size(640,420);errors.ContainerControl=this;
        var header=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};
        header.Controls.AddRange([new Label{Text="Nhà cung cấp",AutoSize=true},supplier,new Label{Text="Ngày nhập",AutoSize=true},date,new Label{Text="Hạn nợ",AutoSize=true},due,new Label{Text="Trả ngay (₫)",AutoSize=true},immediate]);
        immediate.ReadOnly=session.Identity.Role!="ADMIN";
        if(immediate.ReadOnly)header.Controls.Add(new Label{Text="Thanh toán do quản lý xử lý",AutoSize=true});
        foreach(var col in new[]{("Code","MÃ"),("Name","NGUYÊN LIỆU"),("Unit","ĐƠN VỊ"),("Quantity","LƯỢNG"),("Price","GIÁ / ĐƠN VỊ")})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,ReadOnly=col.Item1 is "Code" or "Name" or "Unit",AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});
        grid.DataSource=lines;grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text="Lượng/giá phải là số hợp lệ; sửa ô trước khi chốt.";};
        grid.CellValueChanged+=(_,_)=>dirty=true;
        var tools=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};var add=new OrbitButton{Text="Thêm dòng"};var remove=new OrbitButton{Text="Bỏ dòng"};tools.Controls.AddRange([material,add,remove]);
        add.Click+=(_,_)=>{if(material.SelectedItem is not InventoryLookup row)return;var old=lines.FirstOrDefault(l=>l.Code==row.Code);if(old is not null){grid.CurrentCell=grid.Rows[lines.IndexOf(old)].Cells["Quantity"];grid.BeginEdit(true);return;}lines.Add(new(){Code=row.Code,Name=row.Name,Unit=row.Unit??""});dirty=true;};
        remove.Click+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is Draft row){lines.Remove(row);dirty=true;}};
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.Controls.Add(header,0,0);body.Controls.Add(grid,0,1);body.Controls.Add(tools,0,2);
        var close=new OrbitButton{Text="Quay lại"};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,preview,save,close]);var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        foreach(Control c in new Control[]{supplier,date,due,immediate}){if(c is TextBox txt)txt.TextChanged+=(_,_)=>dirty=true;else if(c is DateTimePicker dt)dt.ValueChanged+=(_,_)=>dirty=true;else if(c is ComboBox cb)cb.SelectedIndexChanged+=(_,_)=>dirty=true;}
        save.Click+=async(_,_)=>await Save();preview.Click+=async(_,_)=>await Preview();check.Click+=async(_,_)=>await Reconcile();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private void State(){body.Enabled=!busy&&!unknown;save.Enabled=!busy&&!loadFailed;preview.Enabled=!busy&&!unknown&&!loadFailed;check.Visible=unknown;check.Enabled=!busy;save.Text=unknown?"Gửi lại cùng yêu cầu":"Chốt nhập kho";}
    private async Task Initialize()
    {
        busy=true;State();
        try
        {
            supplier.DataSource=await service.LookupsAsync(session,"Supplier");var materials=await service.LookupsAsync(session,"Material");material.DataSource=materials;
            var stored=pending.Read(session.Identity.AccountId,"PurchaseCreate");
            if(stored is not null)
            {
                request=JsonSerializer.Deserialize<PurchaseRequest>(stored.Payload)??throw new InvalidDataException("Hồ sơ nhập hàng chưa hợp lệ.");
                if(request.RequestId!=stored.RequestId)throw new InvalidDataException("Mã yêu cầu không khớp.");
                supplier.SelectedItem=((List<InventoryLookup>)supplier.DataSource).FirstOrDefault(s=>s.Code==request.Supplier);date.Value=request.Date;due.Checked=request.Due.HasValue;if(request.Due.HasValue)due.Value=request.Due.Value;immediate.Text=request.Immediate.ToString("0",CultureInfo.InvariantCulture);
                foreach(var line in request.Lines){var lookup=materials.FirstOrDefault(m=>m.Code==line.Code);lines.Add(new(){Code=line.Code,Name=lookup?.Name??line.Code,Unit=lookup?.Unit??"",Quantity=line.Quantity,Price=line.Price});}
                unknown=true;status.Text=$"Yêu cầu {request.RequestId}: cần đối chiếu.";
            }
            dirty=false;
        }
        catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or ArgumentException){loadFailed=true;status.Text=e.Message;}
        finally{busy=false;State();}
    }
    private List<PurchaseLine> ReadLines(){if(!grid.EndEdit())throw new ArgumentException("Sửa ô đang nhập trước khi chốt.");var result=lines.Select(l=>new PurchaseLine(l.Code,l.Quantity,l.Price)).ToList();PurchaseService.ValidateLines(result);return result;}
    private async Task Preview()
    {
        if(busy)return;busy=true;State();
        try{var total=await service.PreviewAsync(session,ReadLines());status.Text=G05Display.Text($"Tổng nhập SQL: {total:N0} ₫ — Chưa ghi kho");}
        catch(Exception e)when(e is UserOperationException or ArgumentException){status.Text=e.Message;}
        finally{busy=false;State();}
    }
    private async Task Save()
    {
        if(busy)return;errors.Clear();
        if(!unknown)
        {
            if(supplier.SelectedItem is not InventoryLookup selected){errors.SetError(supplier,"Chọn nhà cung cấp.");supplier.Focus();return;}
            if(!decimal.TryParse(immediate.Text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var cash)||cash<0||decimal.Truncate(cash)!=cash){errors.SetError(immediate,"Nhập số tiền nguyên VND không âm.");immediate.Focus();return;}
            busy=true;State();
            try{var input=ReadLines();var total=await service.PreviewAsync(session,input);if(cash>total)throw new ArgumentException("Trả ngay vượt tổng nhập.");request=new(Guid.NewGuid(),selected.Code,date.Value,due.Checked?due.Value.Date:null,cash,total,input);}
            catch(Exception e)when(e is UserOperationException or ArgumentException){status.Text=e.Message;return;}
            finally{busy=false;State();}
        }
        if(request is null)return;
        if(await OrbitConfirmationDialog.ShowAsync(this,G05Display.Text($"Chốt nhập {request.Lines.Count} dòng, tổng {request.ExpectedTotal:N0} ₫, trả ngay {request.Immediate:N0} ₫, nợ {request.ExpectedTotal-request.Immediate:N0} ₫? Chứng từ sau chốt chỉ đọc."),"Chốt nhập kho",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        busy=true;State();
        try{pending.Save(new(session.Identity.AccountId,request.RequestId,"PurchaseCreate",JsonSerializer.Serialize(request),DateTime.UtcNow));var code=await service.CreateAsync(session,request);Complete(code);}
        catch(UserOperationException e){unknown=unknown||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,"PurchaseCreate");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text="Đã dừng chờ; kiểm tra kết quả trước khi thử lại.";}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private void Complete(string code){pending.Clear(session.Identity.AccountId,"PurchaseCreate");ResultCode=code;dirty=false;unknown=false;DialogResult=DialogResult.OK;Close();}
    private async Task Reconcile(){if(busy||request is null)return;busy=true;State();try{var code=await service.ReconcileAsync(session,request.RequestId);if(code is not null)Complete(code);else status.Text="Chưa có receipt; phiên trước có thể còn chạy. Chỉ gửi lại cùng yêu cầu đã lưu.";}catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync()=>!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,"Bỏ nháp nhập hàng chưa ghi kho?","Bỏ nháp",MessageBoxButtons.YesNo)==DialogResult.Yes);
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}



