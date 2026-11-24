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

// Lập phiếu đặt hàng: chỉ ghi phiếu Chờ nhận, không cộng tồn và không tạo chi/nợ.
public sealed class PurchaseEditorDialog : OrbitDialogForm
{
    private sealed class Draft
    {
        public string Code{get;set;}=""; public string Name{get;set;}="";public string Unit{get;set;}="";
        public decimal Quantity{get;set;} public decimal Price{get;set;}
        public decimal Amount=>Quantity*Price;
    }
    private static readonly CultureInfo Vi=G05Display.Culture;
    private readonly PurchaseService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly ComboBox supplier=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=260},material=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=320};
    private readonly DateTimePicker date=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy"},due=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Checked=false};
    private readonly TextBox immediate=new(){Text="0",Width=140,TextAlign=HorizontalAlignment.Right};
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=false,EditMode=DataGridViewEditMode.EditOnEnter};
    private readonly BindingList<Draft> lines=[]; private readonly Label status=new(){AutoSize=true};
    private readonly Label totals=new(){AutoSize=true,Font=OrbitManagementTheme.Body,ForeColor=OrbitManagementTheme.Text};
    private readonly Label materialInfo=new(){AutoSize=true,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Small,Text=L.T("Chọn nguyên liệu để xem tồn và giá nhập gần nhất.")};
    private readonly OrbitButton save=new(){Text=L.T("Lưu phiếu chờ nhận")},check=new(){Text=L.T("Kiểm tra kết quả"),Visible=false},preview=new(){Text=L.T("Kiểm tra tổng")},reprint=new(){Text=L.T("In phiếu"),Visible=false};
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
    private PurchaseRequest? request;private PurchaseRow? savedRow;private bool busy,unknown,dirty,loadFailed;private readonly ErrorProvider errors=new();
    internal string? ResultCode{get;private set;}
    public PurchaseEditorDialog(PurchaseService service,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.session=session;this.pending=pending;
        Text=L.T("Lập phiếu nhập — Chờ nhận");AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(980,640);MinimumSize=new Size(700,460);errors.ContainerControl=this;
        var header=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};
        header.Controls.AddRange([new Label{Text=L.T("Nhà cung cấp"),AutoSize=true},supplier,new Label{Text=L.T("Ngày nhập"),AutoSize=true},date,new Label{Text=L.T("Hạn trả dự kiến"),AutoSize=true},due,new Label{Text=L.T("Trả ngay (₫)"),AutoSize=true},immediate]);
        immediate.ReadOnly=session.Identity.Role!="ADMIN";
        if(immediate.ReadOnly)header.Controls.Add(new Label{Text=L.T("Thanh toán do quản lý xử lý"),AutoSize=true});
        foreach(var col in new[]{("Code",L.T("MÃ NVL")),("Name",L.T("NGUYÊN LIỆU")),("Unit",L.T("ĐVT")),("Quantity",L.T("SỐ LƯỢNG")),("Price",L.T("ĐƠN GIÁ (₫)")),("Amount",L.T("THÀNH TIỀN"))})
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,ReadOnly=col.Item1 is "Code" or "Name" or "Unit" or "Amount",AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});
        grid.Columns["Name"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.DataSource=lines;
        grid.CellValidating+=(_,e)=>ValidateCell(e);
        grid.CellValidated+=(_,e)=>{if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText="";};
        grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text=L.T("Số liệu trong ô chưa hợp lệ; sửa trước khi lưu.");};
        grid.CellValueChanged+=(_,e)=>{if(e.RowIndex>=0){dirty=true;grid.InvalidateRow(e.RowIndex);}UpdateTotals();};
        var tools=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};var add=new OrbitButton{Text=L.T("Thêm dòng")};var remove=new OrbitButton{Text=L.T("Bỏ dòng")};tools.Controls.AddRange([material,add,remove]);
        // Mỗi dòng một nguyên liệu; chọn lại mã đã có thì nhảy vào ô số lượng thay vì tạo dòng trùng.
        add.Click+=(_,_)=>{if(material.SelectedItem is not MaterialPurchaseLookup row)return;var old=lines.FirstOrDefault(l=>l.Code==row.Code);if(old is not null){grid.CurrentCell=grid.Rows[lines.IndexOf(old)].Cells["Quantity"];grid.BeginEdit(true);return;}lines.Add(new(){Code=row.Code,Name=row.Name,Unit=row.Unit??""});dirty=true;UpdateTotals();};
        remove.Click+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is Draft row){lines.Remove(row);dirty=true;UpdateTotals();}};
        material.SelectedIndexChanged+=(_,_)=>{if(material.SelectedItem is MaterialPurchaseLookup selected)materialInfo.Text=L.F("Tồn hiện tại: {0} {1} · Giá nhập gần nhất: {2}", selected.Stock.ToString("N0",Vi), selected.Unit??"", selected.LastPrice is decimal price?price.ToString("N0",Vi)+" ₫":L.T("chưa có"));};
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=1};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var right=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Dock=DockStyle.Fill};right.Controls.AddRange([totals,materialInfo]);footer.Controls.Add(tools,0,0);footer.Controls.Add(right,1,0);
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.Controls.Add(header,0,0);body.Controls.Add(grid,0,1);body.Controls.Add(footer,0,2);
        var close=new OrbitButton{Text=L.T("Quay lại")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,preview,reprint,save,close]);
        var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        foreach(Control c in new Control[]{supplier,date,due,immediate}){if(c is TextBox txt)txt.TextChanged+=(_,_)=>{dirty=true;UpdateTotals();};else if(c is DateTimePicker dt)dt.ValueChanged+=(_,_)=>dirty=true;else if(c is ComboBox cb)cb.SelectedIndexChanged+=(_,_)=>dirty=true;}
        immediate.Leave+=(_,_)=>{if(decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash))immediate.Text=cash.ToString("N0",Vi);UpdateTotals();};
        save.Click+=async(_,_)=>await Save();preview.Click+=async(_,_)=>await Preview();check.Click+=async(_,_)=>await Reconcile();reprint.Click+=async(_,_)=>await Print();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private bool Editable=>!busy&&!unknown&&ResultCode is null;
    private void State(){body.Enabled=!busy&&!unknown&&ResultCode is null;save.Enabled=Editable&&!loadFailed;save.Text=unknown?L.T("Gửi lại cùng yêu cầu"):L.T("Lưu phiếu chờ nhận");preview.Enabled=Editable&&!loadFailed;check.Visible=unknown;check.Enabled=!busy;reprint.Visible=ResultCode is not null&&savedRow is not null;reprint.Enabled=!busy;closeText();}
    private void closeText(){if(ResultCode is not null)Text=L.T("Đã lưu phiếu ")+ResultCode;}
    private void ValidateCell(DataGridViewCellValidatingEventArgs e)
    {
        if(e.RowIndex<0||e.ColumnIndex<0)return;
        var name=grid.Columns[e.ColumnIndex].DataPropertyName;
        if(name is not("Quantity" or "Price"))return;
        var text=Convert.ToString(e.FormattedValue)??"";
        if(!decimal.TryParse(text,NumberStyles.Number,Vi,out var value)||value<=0||decimal.Truncate(value)!=value)
        {
            e.Cancel=true;grid.Rows[e.RowIndex].ErrorText=name=="Quantity"?L.T("Số lượng phải là số nguyên dương."):L.T("Đơn giá phải là số nguyên VND dương.");status.Text=grid.Rows[e.RowIndex].ErrorText;
        }
    }
    private void UpdateTotals()
    {
        var total=lines.Sum(l=>l.Quantity*l.Price);
        var cash=decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var parsed)&&parsed>=0?decimal.Truncate(parsed):0;
        var debt=Math.Max(0,total-cash);
        totals.Text=L.F("Tổng: {0} ₫ · Trả ngay: {1} ₫ · Còn nợ: {2} ₫",total.ToString("N0",Vi),cash.ToString("N0",Vi),debt.ToString("N0",Vi));
        due.Enabled=Editable&&debt>0;if(!due.Enabled)due.Checked=false;
        totals.ForeColor=total>0?OrbitManagementTheme.Text:OrbitManagementTheme.Muted;
    }
    private async Task Initialize()
    {
        busy=true;State();
        try
        {
            supplier.DisplayMember=nameof(InventoryLookup.Name);supplier.ValueMember=nameof(InventoryLookup.Code);supplier.DataSource=await service.LookupsAsync(session,"Supplier");
            var materials=await service.MaterialsAsync(session);material.DisplayMember=nameof(MaterialPurchaseLookup.Name);material.ValueMember=nameof(MaterialPurchaseLookup.Code);material.DataSource=materials;
            var stored=pending.Read(session.Identity.AccountId,"PurchaseCreate");
            if(stored is not null)
            {
                request=JsonSerializer.Deserialize<PurchaseRequest>(stored.Payload)??throw new InvalidDataException(L.T("Hồ sơ nhập hàng chưa hợp lệ."));
                if(request.RequestId!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu không khớp."));
                supplier.SelectedItem=((List<InventoryLookup>)supplier.DataSource).FirstOrDefault(s=>s.Code==request.Supplier);date.Value=request.Date;due.Checked=request.Due.HasValue;if(request.Due.HasValue)due.Value=request.Due.Value;immediate.Text=request.Immediate.ToString("N0",Vi);
                foreach(var line in request.Lines){var lookup=materials.FirstOrDefault(m=>m.Code==line.Code);lines.Add(new(){Code=line.Code,Name=lookup?.Name??line.Code,Unit=lookup?.Unit??"",Quantity=line.Quantity,Price=line.Price});}
                unknown=true;status.Text=L.F("Yêu cầu {0}: cần đối chiếu trước khi gửi lại.", request.RequestId);
            }
            dirty=false;UpdateTotals();
        }
        catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or ArgumentException){loadFailed=true;status.Text=e.Message;}
        finally{busy=false;State();}
    }
    private List<PurchaseLine> ReadLines(){if(!grid.EndEdit())throw new ArgumentException(L.T("Sửa ô đang nhập trước khi lưu."));var result=lines.Select(l=>new PurchaseLine(l.Code,l.Quantity,l.Price)).ToList();PurchaseService.ValidateLines(result);return result;}
    private async Task Preview()
    {
        if(busy)return;busy=true;State();
        try{var total=await service.PreviewAsync(session,ReadLines());status.Text=L.F("Tổng theo SQL: {0} ₫. Phiếu chưa ghi kho.", total.ToString("N0",Vi));}
        catch(Exception e)when(e is UserOperationException or ArgumentException){status.Text=e.Message;}
        finally{busy=false;State();}
    }
    private async Task Save()
    {
        if(busy||!Editable)return;errors.Clear();
        if(!unknown)
        {
            if(session.Identity.Role!="ADMIN"){status.Text=L.T("Chỉ quản lý được lập phiếu nhập.");return;}
            if(supplier.SelectedItem is not InventoryLookup selected){errors.SetError(supplier,L.T("Chọn nhà cung cấp."));supplier.Focus();return;}
            if(!decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash)||cash<0||decimal.Truncate(cash)!=cash){errors.SetError(immediate,L.T("Nhập số tiền nguyên VND không âm."));immediate.Focus();return;}
            if(date.Value.Date>DateTime.Today){errors.SetError(date,L.T("Ngày nhập không được ở tương lai."));date.Focus();return;}
            busy=true;State();
            try{var input=ReadLines();var total=await service.PreviewAsync(session,input);if(cash>total)throw new ArgumentException(L.T("Trả ngay vượt tổng nhập."));request=new(Guid.NewGuid(),selected.Code,date.Value,due.Checked?due.Value.Date:null,cash,total,input);}
            catch(Exception e)when(e is UserOperationException or ArgumentException){status.Text=e.Message;return;}
            finally{busy=false;State();}
        }
        if(request is null)return;
        var totalText=request.ExpectedTotal.ToString("N0",Vi);var cashText=request.Immediate.ToString("N0",Vi);var debtText=(request.ExpectedTotal-request.Immediate).ToString("N0",Vi);
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Lưu phiếu {0} dòng, tổng {1} ₫, trả ngay {2} ₫, nợ {3} ₫? Phiếu ở trạng thái Chờ nhận, chưa cộng kho.", request.Lines.Count, totalText, cashText, debtText),L.T("Lưu phiếu chờ nhận"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await SendRequest();
    }
    private async Task SendRequest()
    {
        if(request is null)return;
        busy=true;State();
        try{pending.Save(new(session.Identity.AccountId,request.RequestId,"PurchaseCreate",JsonSerializer.Serialize(request),DateTime.UtcNow));var code=await service.CreateDraftAsync(session,request);await Complete(code);}
        catch(UserOperationException e){unknown=unknown||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,"PurchaseCreate");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text=L.T("Đã dừng chờ; kiểm tra kết quả trước khi thử lại.");}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        catch(Exception e){status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Complete(string code)
    {
        pending.Clear(session.Identity.AccountId,"PurchaseCreate");ResultCode=code;dirty=false;unknown=false;
        try
        {
            savedRow=await service.GetAsync(session,code);
            status.Text=L.F("Đã lưu phiếu {0} — Chờ nhận. Có thể in phiếu để nhân viên đối chiếu khi giao hàng.", code);
            State();
            if(savedRow is not null&&await OrbitConfirmationDialog.ShowAsync(this,L.F("In phiếu {0} ngay?", code),L.T("In phiếu nhập"),MessageBoxButtons.YesNo)==DialogResult.Yes)await Print();
        }
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){status.Text=e.Message;}
        catch(Exception e){status.Text=e.Message;}
        finally{if(!IsDisposed){DialogResult=DialogResult.OK;Close();}}
    }
    private async Task Print()
    {
        if(savedRow is null||busy)return;busy=true;State();
        try{await PurchasePrinting.PreviewAsync(this,savedRow);}
        catch(Exception e)when(e is not OperationCanceledException){status.Text=L.T("Chưa in được phiếu: ")+e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Reconcile(){if(busy||request is null)return;busy=true;State();try{var code=await service.ReconcileAsync(session,request.RequestId);if(code is not null)await Complete(code);else{busy=false;State();status.Text=L.T("Chưa có receipt; phiên trước có thể còn chạy. Chỉ gửi lại cùng yêu cầu đã lưu.");if(await OrbitConfirmationDialog.ShowAsync(this,L.T("Gửi lại đúng yêu cầu đang lưu, giữ nguyên mã và nội dung?"),L.T("Đối chiếu yêu cầu"),MessageBoxButtons.YesNo)==DialogResult.Yes){await SendRequest();return;}}}catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync()=>ResultCode is not null||(!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ nháp phiếu nhập chưa lưu?"),L.T("Bỏ nháp"),MessageBoxButtons.YesNo)==DialogResult.Yes));
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
