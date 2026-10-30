using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Requests.Finance;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;
namespace CafeManager.WinForms.Modules.Finance;

public sealed class FinanceControl : UserControl
{
    private readonly FinanceService service;private readonly DocumentLinks links;
    private readonly SessionContext session;
    private readonly IPendingOperationStore pending;
    private readonly OrbitDataGridView grid=new() { Dock=DockStyle.Fill,AutoGenerateColumns=false,ReadOnly=true,AllowUserToAddRows=false };
    private readonly DateTimePicker from=new() { Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=DateTime.Today.AddDays(-30),Width=130 },to=new() { Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=130 };
    private readonly ComboBox type=new() { DropDownStyle=ComboBoxStyle.DropDownList,Width=85 },origin=new() { DropDownStyle=ComboBoxStyle.DropDownList,Width=180 };
    private readonly OrbitSearchBox search=new();
    private readonly OrbitListWorkspace workspace;
    private OrbitButton previous=>workspace.Pagination.Previous;private OrbitButton next=>workspace.Pagination.Next;
    private readonly OrbitButton add=new() { Text="Ghi phiếu" },detail=new() { Text="Xem chi tiết" };
    private CancellationTokenSource? loading;
    private int page,generation;
    public FinanceControl(FinanceService service,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        this.service=service; this.session=session; this.pending=pending;this.links=links;
        AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=AutoScaleMode.Dpi; Dock=DockStyle.Fill;
        workspace=new(search){StackedTools=true}; workspace.Title.Text="Thu chi"; workspace.Description.Text="Ghi tay và nguồn chi từ nghiệp vụ; phiếu đã ghi chỉ đọc."; workspace.Eyebrow.Text="TÀI CHÍNH";
        type.Items.AddRange(["Tất cả","Thu","Chi"]); type.SelectedIndex=0;
        origin.Items.AddRange(["Tất cả nguồn","Ghi tay","Trả ngay nhập hàng","Trả nợ nhà cung cấp","Chi lương","Dữ liệu cũ chưa đủ nguồn"]); origin.SelectedIndex=0;
        workspace.Filters.Controls.AddRange([from,to,type,origin]);
        foreach(var c in new[]{("Code","MÃ"),("Date","NGÀY"),("Type","LOẠI"),("Category","DANH MỤC"),("Amount","SỐ TIỀN"),("Note","GHI CHÚ")}) grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=c.Item1,Name=c.Item1,HeaderText=c.Item2,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells });
        grid.Columns["Amount"]!.DefaultCellStyle.Format="N0"; grid.Columns["Amount"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight; grid.Columns["Date"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        workspace.Body.Controls.Add(grid);
        var refresh=new OrbitButton { Text="Tìm / Tải lại" }; workspace.RowActions.Controls.AddRange([refresh,detail]);
        workspace.PrimaryActions.Controls.Add(add); Controls.Add(workspace); OrbitField.ApplyInputBehavior(this);grid.DataBindingComplete+=(_,_)=>G05Display.Grid(grid);G05Display.Grid(grid);
        OrbitManagementTheme.Button(add,ManagementButtonStyle.Primary,"plus");
        refresh.Click+=async(_,_)=>{page=0;await LoadRows();}; previous.Click+=async(_,_)=>{if(page>0)page--;await LoadRows();}; next.Click+=async(_,_)=>{page++;await LoadRows();};
        search.Input.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;page=0;await LoadRows();}};
        add.Click+=async(_,_)=>{using var dialog=new FinanceEntryDialog(service,session,pending);if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await LoadRows();};
        detail.Click+=async(_,_)=>await ShowDetail(); grid.CellDoubleClick+=async(_,e)=>{if(e.RowIndex>=0)await ShowDetail();};
        Load+=async(_,_)=>await LoadRows(); VisibleChanged+=async(_,_)=>{if(Visible&&IsHandleCreated)await LoadRows();else if(!Visible)StopLoading();};
    }
    private async Task ShowDetail()
    {
        if(grid.CurrentRow?.DataBoundItem is not FinanceRow selected)return;
        try { var row=await service.GetAsync(session,selected.Code); if(IsDisposed)return; if(row is null){workspace.Status.Text="Phiếu không tồn tại.";return;} using var dialog=new FinanceDetailDialog(row,links);await OrbitDialogHost.ShowAsync(dialog,this); }
        catch(Exception e) when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=e.Message;}
    }
    private async Task LoadRows()
    {
        if(IsDisposed||Disposing||session.IsRevoked)return; StopLoading(); loading=CancellationTokenSource.CreateLinkedTokenSource(session.Token); var current=++generation;
        workspace.Status.Text="Đang tải…"; workspace.Pagination.SetPending("Đang tải…");detail.Enabled=false;
        try
        {
            string? originValue=origin.SelectedIndex switch{1=>"Manual",2=>"PurchaseImmediate",3=>"SupplierDebt",4=>"Payroll",5=>"LegacyUnknown",_=>null};
            var rows=await service.ListAsync(session,new FinanceFilter(from.Value,to.Value,search.Input.Text.Trim(),type.SelectedIndex==0?null:type.Text,originValue,page*50),loading.Token);
            if(IsDisposed||Disposing||session.IsRevoked||current!=generation)return;
            grid.DataSource=rows; workspace.Pagination.SetPage(page,50,rows.Count==0?0:rows[0].TotalRows,"phiếu");detail.Enabled=rows.Count>0;
            workspace.Status.Text=rows.Count==0?"Không có phiếu trong bộ lọc.":G05Display.Text($"{rows[0].TotalRows} phiếu · Thu {rows[0].TotalIncome:N0} ₫ · Chi {rows[0].TotalExpense:N0} ₫ · Trang {page+1}");
        }
        catch(OperationCanceledException){}
        catch(Exception e) when(e is UserOperationException or ArgumentException){if(!IsDisposed&&!Disposing&&!session.IsRevoked&&current==generation){workspace.Status.Text="Chưa cập nhật: "+e.Message;workspace.Pagination.SetPending("Chưa cập nhật");}}
    }
    private void StopLoading()
    {
        generation++;
        var previous=loading;loading=null;
        previous?.Cancel();previous?.Dispose();
    }
    protected override void Dispose(bool disposing){if(disposing)StopLoading();base.Dispose(disposing);}
}



