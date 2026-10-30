using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Finance;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Finance;
public sealed class SupplierDebtsControl : UserControl
{
    private readonly SupplierDebtService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;private readonly DocumentLinks links;
    private readonly OrbitSearchBox search=new();private readonly OrbitListWorkspace workspace;private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false};
    private readonly DateTimePicker asOf=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Checked=false};private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;private readonly OrbitButton pay=new(){Text="Trả nợ"};private CancellationTokenSource? loading;private int page,generation;
    public SupplierDebtsControl(SupplierDebtService service,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        this.service=service;this.session=session;this.pending=pending;this.links=links;Dock=DockStyle.Fill;workspace=new(search){StackedTools=true};workspace.Title.Text="Công nợ nhà cung cấp";workspace.Description.Text="Không chọn ngày: Hiện tại. Chọn ngày: số dư tại ngày; trả nợ luôn dùng số dư hiện tại.";workspace.Eyebrow.Text="CÔNG NỢ";Controls.Add(workspace);
        workspace.Filters.Controls.AddRange([new Label{Text="Tại ngày (tích để chọn)",AutoSize=true},asOf]);
        foreach(var c in new[]{("Code","MÃ NỢ"),("Supplier","NCC"),("PurchaseCode","PHIẾU NHẬP"),("Original","NỢ GỐC"),("Paid","ĐÃ TRẢ"),("Remaining","CÒN NỢ"),("Due","HẠN"),("Status","TRẠNG THÁI")})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=c.Item1,DataPropertyName=c.Item1,HeaderText=c.Item2,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});workspace.Body.Controls.Add(grid);
        foreach(var c in new[]{"Original","Paid","Remaining"}){grid.Columns[c]!.DefaultCellStyle.Format="N0";grid.Columns[c]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}grid.Columns["Due"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        var refresh=new OrbitButton{Text="Tìm / Tải lại"};var detail=new OrbitButton{Text="Xem lịch sử"};workspace.RowActions.Controls.AddRange([refresh,detail]);pay.Visible=session.Identity.Role=="ADMIN";workspace.PrimaryActions.Controls.Add(pay);
        refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};detail.Click+=async(_,_)=>await Detail();grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};
        pay.Click+=async(_,_)=>{if(grid.CurrentRow?.DataBoundItem is not SupplierDebtRow row)return;using var dialog=new DebtPaymentDialog(service,session,pending,row.Code);if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await LoadRows();};
        search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;page=0;await LoadRows();}};Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
    }
    private async Task Detail(){if(grid.CurrentRow?.DataBoundItem is SupplierDebtRow row)await links.ShowDebtAsync(this,row.Code);}
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token);var current=++generation;workspace.Pagination.SetPending("Đang tải…");workspace.Status.Text="Đang tải…";
        try{var rows=await service.ListAsync(session,search.Input.Text.Trim(),asOf.Checked?asOf.Value.Date:null,page*50,loading.Token);if(IsDisposed||Disposing||session.IsRevoked||current!=generation)return;grid.DataSource=rows;workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,"nguồn nợ");workspace.Status.Text=(asOf.Checked?$"Tại ngày {asOf.Value:dd/MM/yyyy}":"Hiện tại")+(rows.Count==0?" — Không có nợ phù hợp.":$" — {rows[0].TotalRows} nguồn — Trang {page+1}");}
        catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&!Disposing&&!session.IsRevoked&&current==generation)workspace.Status.Text="Chưa cập nhật: "+e.Message;}
    }
    private void StopLoading()
    {
        generation++;
        var previous=loading;loading=null;
        previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing)StopLoading();base.Dispose(disposing);}
}
public sealed class SupplierDebtDetailDialog : OrbitDialogForm
{
    public SupplierDebtDetailDialog(SupplierDebtRow row,DocumentLinks links)
    {
        Text="Nợ hiện tại — "+row.Code;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};body.RowStyles.Add(new RowStyle(SizeType.AutoSize));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        body.Controls.Add(new Label{Text=G05Display.Text($"{row.Code} / {row.PurchaseCode} / {row.Supplier}\r\nNợ gốc {row.Original:N0} ₫ — Đã trả {row.Paid:N0} ₫ — Còn {row.Remaining:N0} ₫"),AutoSize=true},0,0);
        var history=new OrbitDataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AutoGenerateColumns=true,DataSource=row.History};body.Controls.Add(history,0,1);history.DataBindingComplete+=(_,_)=>G05Display.Grid(history);G05Display.Grid(history);
        var purchase=new OrbitButton{Text="Phiếu nhập nguồn"};purchase.Click+=async(_,_)=>await links.ShowPurchaseAsync(this,row.PurchaseCode);
        var finance=new OrbitButton{Text="Phiếu chi",Visible=links.CanFinance};finance.Click+=async(_,_)=>{if(history.CurrentRow?.DataBoundItem is SupplierDebtPayment payment)await links.ShowFinanceAsync(this,payment.Code);};
        var close=new OrbitButton{Text="Đóng"};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([purchase,finance,close]);var dialogLayout=new OrbitDialogLayout(this,body,new Label(),actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);
    }
}


