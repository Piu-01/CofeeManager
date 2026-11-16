using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class StockIssuesControl : UserControl
{
    private readonly StockIssueService service;private readonly PurchaseService lookups;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false};private readonly OrbitSearchBox search=new();private readonly OrbitListWorkspace workspace;
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;private CancellationTokenSource? loading;private int page,generation;
    public StockIssuesControl(StockIssueService service,PurchaseService lookups,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.lookups=lookups;this.session=session;this.pending=pending;Dock=DockStyle.Fill;workspace=new(search){StackedTools=true};workspace.Title.Text=L.T("Xuất nguyên liệu");workspace.Description.Text=L.T("Hủy do hỏng / Trả hàng; chưa tự hoàn tiền hoặc cấn nợ.");Controls.Add(workspace);
        foreach(var col in new[]{("Code",L.T("MÃ")),("Date",L.T("NGÀY XUẤT")),("Kind",L.T("LOẠI")),("Reason",L.T("LÝ DO"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});workspace.Body.Controls.Add(grid);
        grid.Columns["Date"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";        var add=new OrbitButton{Text=L.T("Lập phiếu xuất")};workspace.PrimaryActions.Controls.Add(add);var refresh=new OrbitButton{Text=L.T("Tìm / Tải lại")};var detail=new OrbitButton{Text=L.T("Xem chi tiết")};workspace.RowActions.Controls.AddRange([refresh,detail]);
        add.Click+=async(_,_)=>{using var editor=new StockIssueEditorDialog(service,lookups,session,pending);if(await OrbitDialogHost.ShowAsync(editor,this)==DialogResult.OK)await LoadRows();};
        refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};detail.Click+=async(_,_)=>await Detail();grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;page=0;await LoadRows();}};
        Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
    }
    private async Task Detail()
    {
        if(grid.CurrentRow?.DataBoundItem is not StockIssueRow selected)return;
        try{var row=await service.GetAsync(session,selected.Code);if(IsDisposed)return;if(row is null){workspace.Status.Text=L.T("Phiếu không tồn tại.");return;}using var dialog=new StockIssueDetailDialog(row);await OrbitDialogHost.ShowAsync(dialog,this);}
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
public sealed class StockIssueDetailDialog : OrbitDialogForm
{
    public StockIssueDetailDialog(StockIssueRow row)
    {
        Text=L.T("Phiếu xuất — ")+row.Code;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(900,600);
        var text=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,Text=$"{row.Code} — {row.Date:dd/MM/yyyy HH:mm}\r\n{Models.States.FinanceStates.IssueLabel(row.Kind)} — {row.Reason??L.T("Lý do cũ chưa lưu")}\r\n\r\n"+string.Join("\r\n",row.Lines.Select(l=>G05Display.Text($"{l.Code} — {l.Name}: {l.Quantity:N4} {l.Unit}")))+L.T("\r\n\r\nĐã chốt — chỉ đọc. Trả hàng chưa tự hoàn tiền/cấn nợ.")};
        var close=new OrbitButton{Text=L.T("Đóng")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.Add(close);var dialogLayout=new OrbitDialogLayout(this,text,new Label(),actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,true);
    }
}


