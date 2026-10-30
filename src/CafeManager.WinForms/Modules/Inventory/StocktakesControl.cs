using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class StocktakesControl : UserControl
{
    private readonly StocktakeService service;private readonly PurchaseService lookups;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false};private readonly OrbitSearchBox search=new();private readonly OrbitListWorkspace workspace;
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;private CancellationTokenSource? loading;private int page,generation;
    public StocktakesControl(StocktakeService service,PurchaseService lookups,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.lookups=lookups;this.session=session;this.pending=pending;Dock=DockStyle.Fill;workspace=new(search){StackedTools=true};workspace.Title.Text="Kiểm kê";workspace.Description.Text="Snapshot và số đếm; chốt hôm nay khi kho còn khớp.";Controls.Add(workspace);
        foreach(var col in new[]{("Code","MÃ"),("Date","NGÀY KIỂM"),("Closed","ĐÃ ĐIỀU CHỈNH"),("SnapshotAt","THỜI ĐIỂM SNAPSHOT")})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});workspace.Body.Controls.Add(grid);
        grid.Columns["Date"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";        var add=new OrbitButton{Text="Lấy số liệu tồn"};workspace.PrimaryActions.Controls.Add(add);var refresh=new OrbitButton{Text="Tìm / Tải lại"};var detail=new OrbitButton{Text="Xem chi tiết"};workspace.RowActions.Controls.AddRange([refresh,detail]);
        add.Click+=async(_,_)=>{using var editor=new StocktakeEditorDialog(service,lookups,session,pending);if(await OrbitDialogHost.ShowAsync(editor,this)==DialogResult.OK)await LoadRows();};
        refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};detail.Click+=async(_,_)=>await Detail();grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;page=0;await LoadRows();}};
        Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
    }
    private async Task Detail()
    {
        if(grid.CurrentRow?.DataBoundItem is not StocktakeRow selected)return;
        try{var row=await service.GetAsync(session,selected.Code);if(IsDisposed)return;if(row is null){workspace.Status.Text="Phiếu không tồn tại.";return;}using var dialog=new StocktakeEditorDialog(service,lookups,session,pending,row.Code);await OrbitDialogHost.ShowAsync(dialog,this);await LoadRows();}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=e.Message;}
    }
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token);var current=++generation;workspace.Status.Text="Đang tải…";workspace.Pagination.SetPending("Đang tải…");
        try{var rows=await service.ListAsync(session,search.Input.Text.Trim(),page*50,loading.Token);if(IsDisposed||generation!=current)return;grid.DataSource=rows;workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,"phiếu");workspace.Status.Text=rows.Count==0?"Chưa có phiếu trong bộ lọc.":$"{rows[0].TotalRows} phiếu — Trang {page+1}";}
        catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&generation==current)workspace.Status.Text="Chưa cập nhật: "+e.Message;}
    }
    private void StopLoading()
    {
        generation++;
        var previous=loading;loading=null;
        previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing)StopLoading();base.Dispose(disposing);}
}


