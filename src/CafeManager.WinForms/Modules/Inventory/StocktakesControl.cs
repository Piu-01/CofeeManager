using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class StocktakesControl : UserControl
{
    private readonly StocktakeService service;private readonly PurchaseService lookups;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,Profile=OrbitGridProfile.Stocktakes};
    private readonly OrbitSearchBox search=new();private readonly OrbitEmptyState empty=new(){Visible=false};private readonly OrbitListWorkspace workspace;private readonly ToolTip tips=new();
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;private CancellationTokenSource? loading;private int page,generation;private string applied="";
    public StocktakesControl(StocktakeService service,PurchaseService lookups,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.lookups=lookups;this.session=session;this.pending=pending;Dock=DockStyle.Fill;
        workspace=new(search){StackedTools=true,ShowMeta=false};workspace.Title.Text=L.T("Kiểm kê");workspace.Description.Text=L.T("Snapshot và số đếm; chốt hôm nay khi kho còn khớp.");Controls.Add(workspace);
        foreach(var col in new[]{("Code",L.T("MÃ")),("Date",L.T("NGÀY KIỂM")),("Closed",L.T("ĐÃ ĐIỀU CHỈNH")),("SnapshotAt",L.T("THỜI ĐIỂM SNAPSHOT"))})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2});
        // Cột bool hiển thị nhãn nghiệp vụ thay cho True/False.
        grid.CellFormatting+=(_,e)=>{if(e.RowIndex>=0&&e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="Closed"){e.Value=e.Value is true?L.T("Đã điều chỉnh"):L.T("Chưa điều chỉnh");e.FormattingApplied=true;}};
        workspace.Body.Controls.AddRange([grid,empty]);workspace.Body.SizeChanged+=(_,_)=>PositionEmpty();grid.ColumnHeadersHeightChanged+=(_,_)=>PositionEmpty();
        var add=new OrbitButton{Text=L.T("Lấy số liệu tồn")};workspace.PrimaryActions.Controls.Add(add);OrbitManagementTheme.Button(add,ManagementButtonStyle.Primary,"plus");
        var submit=new OrbitButton{AccessibleName=L.T("Tìm kiếm")};OrbitManagementTheme.Button(submit,ManagementButtonStyle.Icon,"search");
        var refresh=new OrbitButton{AccessibleName=L.T("Tải lại danh sách")};OrbitManagementTheme.Button(refresh,ManagementButtonStyle.Icon,"refresh");
        workspace.SearchActions.Controls.AddRange([submit,refresh]);tips.SetToolTip(submit,L.T("Tìm kiếm (Enter)"));tips.SetToolTip(refresh,L.T("Làm mới kết quả theo tìm kiếm đang áp dụng"));
        search.Input.PlaceholderText=L.T("Tìm mã phiếu kiểm kê…");tips.SetToolTip(search.Input,L.T("Nhấn Enter hoặc kính lúp để tìm"));
        add.Click+=async(_,_)=>{using var editor=new StocktakeEditorDialog(service,lookups,session,pending);if(await OrbitDialogHost.ShowAsync(editor,this)==DialogResult.OK)await LoadRows();};
        submit.Click+=async(_,_)=>await Submit();refresh.Click+=async(_,_)=>{page=0;await LoadRows();};previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();};next.Click+=async(_,_)=>{page++;await LoadRows();};
        search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Submit();}};
        grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await Detail();};grid.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Detail();}};
        Load+=async(_,_)=>await LoadRows();VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
    }
    private void PositionEmpty()=>empty.SetBounds(0,grid.ColumnHeadersHeight,workspace.Body.Width,Math.Max(0,workspace.Body.Height-grid.ColumnHeadersHeight));
    private async Task Submit(){page=0;applied=search.Input.Text.Trim();await LoadRows();}
    private async Task Detail()
    {
        if(grid.CurrentRow?.DataBoundItem is not StocktakeRow selected)return;
        try{var row=await service.GetAsync(session,selected.Code);if(IsDisposed)return;if(row is null){workspace.Status.Text=L.T("Phiếu không tồn tại.");return;}using var dialog=new StocktakeEditorDialog(service,lookups,session,pending,row.Code);await OrbitDialogHost.ShowAsync(dialog,this);await LoadRows();}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=e.Message;}
    }
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token);var current=++generation;
        try{var rows=await service.ListAsync(session,applied,page*50,loading.Token);if(IsDisposed||generation!=current)return;grid.DataSource=rows;empty.Text=L.T("Chưa có phiếu trong bộ lọc.");empty.Visible=rows.Count==0;if(empty.Visible)empty.BringToFront();workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,L.T("phiếu"));workspace.Status.Text=rows.Count==0?L.T("Chưa có phiếu trong bộ lọc."):L.F("{0} phiếu — Trang {1} · Nhấn đúp dòng để xem chi tiết", rows[0].TotalRows, page+1);}
        catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&generation==current)workspace.Status.Text=L.T("Chưa cập nhật: ")+e.Message;}
    }
    private void StopLoading()
    {
        generation++;
        var previous=loading;loading=null;
        previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing){StopLoading();tips.Dispose();}base.Dispose(disposing);}
}
