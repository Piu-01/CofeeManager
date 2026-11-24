using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;

// Danh sách phiếu nhập: lọc trạng thái, in phiếu, mở kiểm nhận; chi tiết chỉ đọc sau chốt.
public sealed class PurchasesControl : UserControl
{
    private sealed record StatusOption(string? Code,string Label){public override string ToString()=>Label;}
    private readonly PurchaseService service;private readonly DocumentLinks links;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,Profile=OrbitGridProfile.Purchases};
    private readonly OrbitSearchBox search=new();private readonly OrbitEmptyState empty=new(){Visible=false};private readonly OrbitListWorkspace workspace;private readonly ToolTip tips=new();
    private readonly ComboBox statusBox=new(){DropDownStyle=ComboBoxStyle.DropDownList};private readonly OrbitFilterBox statusField;
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;
    private OrbitButton receive=new(),print=new();private CancellationTokenSource? loading;private int page,generation;private string applied="";private string? appliedStatus;
    public PurchasesControl(PurchaseService service,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        this.service=service;this.session=session;this.pending=pending;this.links=links;Dock=DockStyle.Fill;
        // Chuẩn danh sách: ô tìm giãn hết hàng 1, lọc trạng thái dưới ô tìm, thao tác dạng icon.
        workspace=new(search){StackedTools=true,ShowMeta=false};workspace.Title.Text=L.T("Nhập hàng");workspace.Description.Text=L.T("Lập phiếu Chờ nhận → kiểm thực nhận → chốt kho. Tồn chỉ tăng khi chốt.");
        Controls.Add(workspace);
        foreach(var col in new[]{("Code",L.T("MÃ")),("Date",L.T("NGÀY NHẬP")),("Supplier",L.T("NHÀ CUNG CẤP")),("StatusText",L.T("TRẠNG THÁI")),("Total",L.T("TỔNG")),("Immediate",L.T("TRẢ NGAY")),("Debt",L.T("NỢ GỐC")),("CountedAt",L.T("NGÀY KIỂM"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2});
        workspace.Body.Controls.AddRange([grid,empty]);workspace.Body.SizeChanged+=(_,_)=>PositionEmpty();grid.ColumnHeadersHeightChanged+=(_,_)=>PositionEmpty();
        statusBox.DataSource=new List<StatusOption>{new(null,L.T("Tất cả trạng thái")),new("ChoNhan",L.T("Chờ nhận")),new("DaKiem",L.T("Đã kiểm")),new("DaChot",L.T("Đã chốt")),new("DaHuy",L.T("Đã huỷ"))};
        statusField=new(statusBox){LogicalWidth=170};workspace.Filters.Controls.Add(statusField);
        if(session.Identity.Role=="ADMIN"){var add=new OrbitButton{Text=L.T("Lập phiếu nhập")};workspace.PrimaryActions.Controls.Add(add);OrbitManagementTheme.Button(add,ManagementButtonStyle.Primary,"plus");add.Click+=async(_,_)=>{using var editor=new PurchaseEditorDialog(service,session,pending);if(await OrbitDialogHost.ShowAsync(editor,this)==DialogResult.OK)await LoadRows();};}
        var submit=new OrbitButton{AccessibleName=L.T("Tìm kiếm")};OrbitManagementTheme.Button(submit,ManagementButtonStyle.Icon,"search");
        receive=new OrbitButton{AccessibleName=L.T("Kiểm nhận")};OrbitManagementTheme.Button(receive,ManagementButtonStyle.Icon,"receipt");
        print=new OrbitButton{AccessibleName=L.T("In phiếu")};OrbitManagementTheme.Button(print,ManagementButtonStyle.Icon,"printer");
        var refresh=new OrbitButton{AccessibleName=L.T("Tải lại danh sách")};OrbitManagementTheme.Button(refresh,ManagementButtonStyle.Icon,"refresh");
        workspace.SearchActions.Controls.AddRange([submit,receive,print,refresh]);tips.SetToolTip(submit,L.T("Tìm kiếm (Enter)"));tips.SetToolTip(receive,L.T("Mở phiếu đang chọn để kiểm nhận"));tips.SetToolTip(print,L.T("In phiếu đang chọn"));tips.SetToolTip(refresh,L.T("Làm mới kết quả theo bộ lọc đang áp dụng"));
        search.Input.PlaceholderText=L.T("Tìm mã phiếu hoặc nhà cung cấp…");tips.SetToolTip(search.Input,L.T("Nhấn Enter hoặc kính lúp để tìm"));
        submit.Click+=async(_,_)=>await Submit();refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};
        receive.Click+=async(_,_)=>await Receive();print.Click+=async(_,_)=>await Print();
        statusBox.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated&&!IsDisposed){page=0;await LoadRows();}};
        search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Submit();}};
        grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Detail();}};
        grid.SelectionChanged+=(_,_)=>State();grid.CellFormatting+=(_,e)=>FormatStatus(e);
        grid.Columns["CountedAt"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        State();
    }
    private PurchaseRow? Current=>grid.CurrentRow?.DataBoundItem as PurchaseRow;
    private void State()
    {
        var row=Current;var canReceive=row is{Status:"ChoNhan" or "DaKiem"};
        receive.Enabled=canReceive&&!loadingActive;print.Enabled=row is not null;receive.Visible=session.Identity.Role is "ADMIN" or "STORE";
    }
    private bool loadingActive=>loading is not null;
    private void FormatStatus(DataGridViewCellFormattingEventArgs e)
    {
        if(e.ColumnIndex<0||e.RowIndex<0||grid.Columns[e.ColumnIndex].DataPropertyName!="StatusText")return;
        if(grid.Rows[e.RowIndex].DataBoundItem is not PurchaseRow row)return;
        e.CellStyle.ForeColor=row.Status switch{"ChoNhan"=>OrbitManagementTheme.Caramel,"DaKiem"=>OrbitManagementTheme.Lilac,"DaChot"=>Color.FromArgb(154,205,150),"DaHuy"=>Color.FromArgb(241,170,167),_=>OrbitManagementTheme.Muted};
    }
    private void PositionEmpty()=>empty.SetBounds(0,grid.ColumnHeadersHeight,workspace.Body.Width,Math.Max(0,workspace.Body.Height-grid.ColumnHeadersHeight));
    private async Task Submit(){page=0;applied=search.Input.Text.Trim();await LoadRows();}
    private async Task Receive()
    {
        if(Current is not PurchaseRow selected)return;
        if(selected.Status is not("ChoNhan" or "DaKiem")){workspace.Status.Text=L.T("Chỉ kiểm nhận phiếu Chờ nhận hoặc Đã kiểm.");return;}
        using var dialog=new PurchaseReceiveDialog(service,session,pending,selected.Code);await OrbitDialogHost.ShowAsync(dialog,this);await LoadRows();
    }
    private async Task Print()
    {
        if(Current is not PurchaseRow selected)return;
        try{await PurchasePrinting.PreviewAsync(this,selected);}
        catch(Exception e)when(e is not OperationCanceledException){workspace.Status.Text=L.T("Chưa in được phiếu: ")+e.Message;}
    }
    private async Task Detail()
    {
        if(Current is not PurchaseRow selected)return;
        try{var row=await service.GetAsync(session,selected.Code);if(IsDisposed)return;if(row is null){workspace.Status.Text=L.T("Phiếu không tồn tại.");return;}using var dialog=new PurchaseDetailDialog(row,links,service,session);await OrbitDialogHost.ShowAsync(dialog,this);if(dialog.Changed)await LoadRows();}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=e.Message;}
    }
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token);var current=++generation;
        try{var status=(statusBox.SelectedItem as StatusOption)?.Code;appliedStatus=status;var rows=await service.ListAsync(session,applied,page*50,status,loading.Token);if(IsDisposed||generation!=current)return;grid.DataSource=rows;empty.Text=L.T("Chưa có phiếu trong bộ lọc.");empty.Visible=rows.Count==0;if(empty.Visible)empty.BringToFront();workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,L.T("phiếu"));workspace.Status.Text=rows.Count==0?L.T("Chưa có phiếu trong bộ lọc."):L.F("{0} phiếu — Trang {1} · Nhấn đúp dòng để xem chi tiết", rows[0].TotalRows, page+1);}
        catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&generation==current)workspace.Status.Text=L.T("Chưa cập nhật: ")+e.Message;}
        finally{if(generation==current)loading=null;State();}
    }
    private void StopLoading()
    {
        generation++;var previous=loading;loading=null;previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing){StopLoading();tips.Dispose();}base.Dispose(disposing);}
}
public sealed class PurchaseDetailDialog : OrbitDialogForm
{
    private sealed record LineView(string Code,string Name,string Unit,decimal Quantity,decimal? Received,decimal? Diff,string? Reason,decimal Price,decimal Amount);
    private readonly Label status=new(){AutoSize=true};
    public bool Changed{get;private set;}
    public PurchaseDetailDialog(PurchaseRow row,DocumentLinks? links=null,PurchaseService? purchases=null,SessionContext? session=null)
    {
        Text=L.T("Phiếu nhập — ")+row.Code;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1020,640);
        var culture=G05Display.Culture;string Money(decimal value)=>value.ToString("#,##0' ₫'",culture);
        var summary=new OrbitSummaryPanel(L.T("Thông tin phiếu"));summary.Dock=DockStyle.Top;summary.Margin=new Padding(0,0,0,12);
        summary.AddValue(L.T("Nhà cung cấp")).Text=row.Supplier;
        summary.AddValue(L.T("Ngày nhập")).Text=row.Date.ToString("dd/MM/yyyy HH:mm",culture);
        summary.AddValue(L.T("Trạng thái"),prominent:true).Text=row.StatusText;
        var ordered=row.Lines.Sum(l=>l.Quantity*l.Price);var actual=row.Lines.Sum(l=>(l.Received??l.Quantity)*l.Price);
        summary.AddValue(L.T("Tổng theo số đặt")).Text=Money(ordered);
        if(row.Status is "DaKiem" or "DaChot") summary.AddValue(L.T("Tổng theo thực nhận"),prominent:true).Text=Money(actual);
        if(row.Immediate is decimal immediate)
        {
            summary.AddValue(row.Status=="DaChot"?L.T("Đã trả ngay"):L.T("Dự kiến trả ngay")).Text=row.FinanceCode is null?Money(immediate):Money(immediate)+" · "+row.FinanceCode;
            var remaining=row.Debt??Math.Max(0,actual-immediate);
            summary.AddValue(L.T("Nợ còn")).Text=Money(remaining)+(row.DebtCode is null?"":" · "+row.DebtCode);
        }
        else if(row.Status=="ChoNhan")summary.AddValue(L.T("Dự kiến trả ngay")).Text=Money(row.PlannedImmediate);
        else summary.AddNote().Text=L.T("Dữ liệu chuyển đổi: phần thanh toán ban đầu chưa đủ nguồn.");
        if(row.DueDate is DateTime due)summary.AddValue(L.T("Hạn trả dự kiến")).Text=due.ToString("dd/MM/yyyy",culture);
        if(row.CountedAt is DateTime counted)summary.AddValue(L.T("Người kiểm")).Text=(row.CountedBy??"—")+" · "+counted.ToString("dd/MM/yyyy HH:mm",culture);
        switch(row.Status)
        {
            case "ChoNhan":summary.AddDivider();summary.AddNote().Text=L.T("Chờ nhận — chưa cộng kho, chưa tạo phiếu chi/công nợ.");break;
            case "DaKiem":summary.AddDivider();summary.AddNote().Text=L.T("Đã kiểm — tải lại trước khi chốt; tồn/tiền sẽ tính theo thực nhận.");break;
            case "DaChot":summary.AddDivider();summary.AddNote().Text=L.T("Đã chốt — chỉ đọc.");break;
            case "DaHuy":summary.AddDivider();summary.AddNote().Text=L.T("Đã huỷ: ")+(row.CancelReason??L.T("Không ghi lý do"));break;
        }
        var lines=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AutoGenerateColumns=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false};
        foreach(var col in new[]{("Code",L.T("MÃ NVL"),60),("Name",L.T("NGUYÊN LIỆU"),170),("Unit",L.T("ĐVT"),50),("Quantity",L.T("SỐ ĐẶT"),60),("Received",L.T("THỰC NHẬN"),65),("Diff",L.T("LỆCH"),55),("Reason",L.T("LÝ DO LỆCH"),120),("Price",L.T("ĐƠN GIÁ"),85),("Amount",L.T("THÀNH TIỀN"),85)})
            lines.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,FillWeight=col.Item3});
        lines.DataSource=row.Lines.Select(l=>new LineView(l.Code,l.Name,l.Unit,l.Quantity,l.Received,l.Diff,l.Reason,l.Price,(l.Received??l.Quantity)*l.Price)).ToList();
        foreach(var name in new[]{"Code","Unit"}){lines.Columns[name]!.DefaultCellStyle.ForeColor=OrbitManagementTheme.Muted;}
        lines.Columns["Code"]!.DefaultCellStyle.Font=OrbitManagementTheme.Code;
        foreach(var name in new[]{"Quantity","Received","Diff","Price","Amount"})
        {
            var style=lines.Columns[name]!.DefaultCellStyle;style.Format=name=="Quantity"||name is "Received"||name=="Diff"?"N0":"#,##0' ₫'";style.FormatProvider=culture;style.Alignment=DataGridViewContentAlignment.MiddleRight;
            lines.Columns[name]!.HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleRight;
        }
        OrbitManagementTheme.DialogGrid(lines);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.Controls.Add(summary,0,0);body.Controls.Add(lines,0,1);
        var close=new OrbitButton{Text=L.T("Đóng")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.Add(close);
        var print=new OrbitButton{Text=L.T("In phiếu")};print.Click+=async(_,_)=>await Print(row);actions.Controls.Add(print);
        if(links is not null)
        {
            if(row.FinanceCode is not null&&links.CanFinance){var finance=new OrbitButton{Text=L.T("Phiếu chi trả ngay")};finance.Click+=async(_,_)=>await links.ShowFinanceAsync(this,row.FinanceCode);actions.Controls.Add(finance);}
            if(row.DebtCode is not null){var debt=new OrbitButton{Text=L.T("Công nợ nguồn")};debt.Click+=async(_,_)=>await links.ShowDebtAsync(this,row.DebtCode);actions.Controls.Add(debt);}
        }
        if(purchases is not null&&session?.Identity.Role=="ADMIN"&&row.Status is "ChoNhan" or "DaKiem")
        {
            var cancel=new OrbitButton{Text=L.T("Huỷ phiếu")};cancel.Click+=async(_,_)=>await Cancel(row,purchases,session);actions.Controls.Add(cancel);
        }
        var dialogLayout=new OrbitDialogLayout(this,body,status,actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);
    }
    private async Task Print(PurchaseRow row)
    {
        try{await PurchasePrinting.PreviewAsync(this,row);}catch(Exception e)when(e is not OperationCanceledException){status.Text=L.T("Chưa in được phiếu: ")+e.Message;}
    }
    private async Task Cancel(PurchaseRow row,PurchaseService purchases,SessionContext session)
    {
        using var prompt=new PurchaseCancelDialog();if(await OrbitDialogHost.ShowAsync(prompt,this)!=DialogResult.OK||prompt.Reason is null)return;
        try{await purchases.CancelAsync(session,new(Guid.NewGuid(),row.Code,row.Revision,prompt.Reason));Changed=true;Close();}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){status.Text=e.Message;}
    }
}
public sealed class PurchaseCancelDialog : OrbitDialogForm
{
    private readonly TextBox reason=new(){Multiline=true,Width=420,Height=70,MaxLength=200,ScrollBars=ScrollBars.Vertical};
    private readonly Label status=new(){AutoSize=true};
    public string? Reason{get;private set;}
    public PurchaseCancelDialog()
    {
        Text=L.T("Huỷ phiếu nhập");AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(520,300);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        body.Controls.Add(new Label{Text=L.T("Lý do huỷ (bắt buộc, tối đa 200 ký tự). Phiếu chưa chốt nên không ảnh hưởng tồn/tiền."),AutoSize=true,Dock=DockStyle.Top},0,0);
        body.Controls.Add(reason,0,1);
        var back=new OrbitButton{Text=L.T("Quay lại")};back.Click+=(_,_)=>Close();
        var confirm=new OrbitButton{Text=L.T("Huỷ phiếu")};confirm.Click+=(_,_)=>{var text=reason.Text.Trim();if(text.Length==0){status.Text=L.T("Cần nhập lý do huỷ.");reason.Focus();return;}Reason=text;DialogResult=DialogResult.OK;Close();};
        var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([confirm,back]);
        Controls.Add(new OrbitDialogLayout(this,body,status,actions));OrbitField.ApplyInputBehavior(this);Shown+=(_,_)=>reason.Focus();AcceptButton=null;
    }
}
