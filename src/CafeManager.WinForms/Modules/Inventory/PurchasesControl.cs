using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class PurchasesControl : UserControl
{
    private readonly PurchaseService service;private readonly DocumentLinks links;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false};private readonly OrbitSearchBox search=new();private readonly OrbitListWorkspace workspace;
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;private CancellationTokenSource? loading;private int page,generation;
    public PurchasesControl(PurchaseService service,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        this.service=service;this.session=session;this.pending=pending;this.links=links;Dock=DockStyle.Fill;workspace=new(search){StackedTools=true};workspace.Title.Text=L.T("Nhập hàng");workspace.Description.Text=L.T("Nháp không ghi kho; chốt tạo kho/chi/nợ nguyên tử.");Controls.Add(workspace);
        foreach(var col in new[]{("Code",L.T("MÃ")),("Date",L.T("NGÀY NHẬP")),("Supplier",L.T("NHÀ CUNG CẤP")),("Total",L.T("TỔNG")),("Immediate",L.T("TRẢ NGAY")),("Debt",L.T("NỢ GỐC"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});workspace.Body.Controls.Add(grid);
        grid.Columns["Date"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";foreach(var name in new[]{"Total","Immediate","Debt"}){grid.Columns[name]!.DefaultCellStyle.Format="N0";grid.Columns[name]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}
        var add=new OrbitButton{Text=L.T("Lập phiếu nhập")};workspace.PrimaryActions.Controls.Add(add);var refresh=new OrbitButton{Text=L.T("Tìm / Tải lại")};var detail=new OrbitButton{Text=L.T("Xem chi tiết")};workspace.RowActions.Controls.AddRange([refresh,detail]);
        add.Click+=async(_,_)=>{using var editor=new PurchaseEditorDialog(service,session,pending);if(await OrbitDialogHost.ShowAsync(editor,this)==DialogResult.OK)await LoadRows();};
        refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};detail.Click+=async(_,_)=>await Detail();grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;page=0;await LoadRows();}};
        Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
    }
    private async Task Detail()
    {
        if(grid.CurrentRow?.DataBoundItem is not PurchaseRow selected)return;
        try{var row=await service.GetAsync(session,selected.Code);if(IsDisposed)return;if(row is null){workspace.Status.Text=L.T("Phiếu không tồn tại.");return;}using var dialog=new PurchaseDetailDialog(row,links);await OrbitDialogHost.ShowAsync(dialog,this);}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=e.Message;}
    }
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token);var current=++generation;
        try{var rows=await service.ListAsync(session,search.Input.Text.Trim(),page*50,loading.Token);if(IsDisposed||generation!=current)return;grid.DataSource=rows;workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,L.T("phiếu"));workspace.Status.Text=rows.Count==0?L.T("Chưa có phiếu trong bộ lọc."):L.F("{0} phiếu — Trang {1}", rows[0].TotalRows, page+1);}
        catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&generation==current)workspace.Status.Text=L.T("Chưa cập nhật: ")+e.Message;}
    }
    private void StopLoading()
    {
        generation++;
        var previous=loading;loading=null;
        previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing)StopLoading();base.Dispose(disposing);}
}
public sealed class PurchaseDetailDialog : OrbitDialogForm
{
    public PurchaseDetailDialog(PurchaseRow row,DocumentLinks? links=null)
    {
        Text=L.T("Phiếu nhập — ")+row.Code;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);
        var text=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,Text=L.F("{0} — {1} — NCC {2}\r\nTổng {3} ₫\r\n", row.Code, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm}", row.Date), row.Supplier, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Total))+(row.Immediate.HasValue?L.F("Trả ngay {0} ₫ — Chi {1}\r\nNợ {2} ₫ — {3}", string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Immediate), row.FinanceCode, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Debt), row.DebtCode):L.T("Dữ liệu chuyển đổi: phần thanh toán ban đầu chưa đủ nguồn."))+"\r\n\r\n"+string.Join("\r\n",row.Lines.Select(l=>L.F("{0} — {1}: {2} {3} × {4} ₫", l.Code, l.Name, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N4}", l.Quantity), l.Unit, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", l.Price))))+L.T("\r\n\r\nĐã chốt — chỉ đọc.")};
        var close=new OrbitButton{Text=L.T("Đóng")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.Add(close);if(links is not null){if(row.FinanceCode is not null&&links.CanFinance){var finance=new OrbitButton{Text=L.T("Phiếu chi trả ngay")};finance.Click+=async(_,_)=>await links.ShowFinanceAsync(this,row.FinanceCode);actions.Controls.Add(finance);}if(row.DebtCode is not null){var debt=new OrbitButton{Text=L.T("Công nợ nguồn")};debt.Click+=async(_,_)=>await links.ShowDebtAsync(this,row.DebtCode);actions.Controls.Add(debt);}}var dialogLayout=new OrbitDialogLayout(this,text,new Label(),actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);
    }
}



