using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Requests.Tables;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Tables;
public sealed class TablesControl : UserControl
{
    public TablesControl(){}
    public TablesControl(TableService service,SessionContext session)
    {
        var tabs=new OrbitManagementTabs(true);tabs.AddPage(L.T("Bàn / Sơ đồ"),new TableList(service,session));
        if(session.Identity.Role=="ADMIN")tabs.AddPage(L.T("Khu vực"),new AreaList(service,session));
        Controls.Add(tabs);
    }
}
internal sealed class TableList : OrbitRecordListControl
{
    private readonly TableService service;
    private readonly SessionContext session;
    private readonly FlowLayoutPanel map=new(){Dock=DockStyle.Fill,Padding=new Padding(10),AutoScroll=true,WrapContents=true,BackColor=Theme.OrbitManagementTheme.Surface};
    private readonly ComboBox filter=new(){Width=160,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly OrbitRowEditor inline;
    private List<AreaRow> areaRows=[];
    private bool refreshingAreas;
    public TableList(TableService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Bàn và trạng thái hiện tại",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,string.IsNullOrEmpty(filter.SelectedValue as string)?null:filter.SelectedValue as string,t),("Code",L.T("Mã")),("Name",L.T("Bàn")),("Area",L.T("Khu vực")),("Status",L.T("Trạng thái")),("OrderCode",L.T("Đơn mở")));
        AddFilter(filter,135);Workspace.Body.Controls.Add(map);Grid.Visible=false;
        var listView=new OrbitButton{Text=L.T("Danh sách")};var mapView=new OrbitButton{Text=L.T("Sơ đồ")};
        Theme.OrbitManagementTheme.Button(listView,Theme.ManagementButtonStyle.Segment,"list");Theme.OrbitManagementTheme.Button(mapView,Theme.ManagementButtonStyle.Segment,"grid");
        AddFilter(new OrbitViewSwitch(listView,mapView),180);mapView.NavigationSelected=true;
        listView.Click+=(_,_)=>{map.Visible=false;Grid.Visible=true;Grid.BringToFront();listView.NavigationSelected=true;mapView.NavigationSelected=false;listView.Invalidate();mapView.Invalidate();};mapView.Click+=(_,_)=>{Grid.Visible=false;map.Visible=true;map.BringToFront();listView.NavigationSelected=false;mapView.NavigationSelected=true;listView.Invalidate();mapView.Invalidate();};
        filter.SelectedValueChanged+=async(_,_)=>{if(!refreshingAreas&&filter.DataSource is not null)await ResetAndReload();};
        map.SizeChanged+=(_,_)=>ArrangeCards();
        AddAction("Thêm",()=>Edit(null),session.Identity.Role=="ADMIN");
        // Sửa tại ô, xóa tại dòng (ADMIN); không còn nút Sửa/Xóa hay hộp sửa.
        AddRowDeleteColumn();Workspace.ShowMeta=false;
        inline=CreateInlineEditor();
        inline.CanWrite=_=>session.Identity.Role=="ADMIN";
        inline.RowKey=item=>((TableRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column(cell.OwningColumn?.Name ?? "");
        inline.Edit=item=>Edit((TableRow)item);inline.Delete=DeleteInline;
        inline.Reconcile=async(_,id,t)=>await service.ReconcileAsync(session,id,t) is not null;
        inline.Reload=()=>Reload();
        Grid.DataBindingComplete+=(_,_)=>
        {
            foreach(Control control in map.Controls.Cast<Control>().ToArray())control.Dispose();map.Controls.Clear();
            if(Grid.DataSource is not List<TableRow> rows)return;
            foreach(var row in rows){var label=row.Retained?L.T("Đã trả · giữ bàn"):row.Status=="Trong"?L.T("Trống"):row.Status=="Cho thanh toan"?L.T("Chờ thanh toán"):L.T("Đang dùng");var button=new OrbitTableCard{Tag=row.Code,TableCode=row.Code,Caption=row.Name,Status=row.Status,OrderCode=row.OrderCode??"",Retained=row.Retained,Text=$"{row.Code} · {row.Name} · {label}",AccessibleName=$"{row.Name} · {label}"};map.Controls.Add(button);button.Click+=async(_,_)=>{button.Enabled=false;try{foreach(DataGridViewRow item in Grid.Rows)if(item.DataBoundItem is TableRow selected&&selected.Code==row.Code){Grid.CurrentCell=item.Cells[0];break;}await SelectCode(row.Code);}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){State.Text=ex.Message;}finally{if(!button.IsDisposed)button.Enabled=true;}};}
        };
        Grid.CurrentCellChanged+=(_,_)=>HighlightCards();
        Grid.DataBindingComplete+=(_,_)=>{ArrangeCards();HighlightCards();};
        Load+=async(_,_)=>await RefreshAreasAsync();
        // Quay lại tab Bàn sau khi thêm/xóa khu vực: nạp lại danh sách khu vực cho ô chọn.
        VisibleChanged+=async(_,_)=>{if(Visible)await RefreshAreasAsync();};
    }
    private static InlineColumn? Column(string property)=>property switch
    {
        "Name"=>new("Name",InlineCellKind.Text),
        "Area"=>new("Area",InlineCellKind.Dropdown),
        _=>null
    };
    private async Task<bool> DeleteInline(object item,Guid id,CancellationToken token)
    {
        var row=(TableRow)item;await service.DeleteAsync(session,false,new(row.Code,row.Name,row.Area),row.Revision,id,token);return true;
    }
    private void HighlightCards()
    {
        foreach(OrbitTableCard card in map.Controls)card.Selected=Selected is TableRow selected&&Equals(card.Tag,selected.Code);
    }
    private void ArrangeCards()
    {
        var gap=Theme.OrbitManagementTheme.Px(this,12);var width=Math.Max(1,map.ClientSize.Width-map.Padding.Horizontal);
        var columns=Math.Max(1,width/Math.Max(1,Theme.OrbitManagementTheme.Px(this,200)));
        foreach(Control card in map.Controls){card.Margin=new Padding(gap/2);card.Size=new Size(Math.Max(1,width/columns-gap),Theme.OrbitManagementTheme.Px(this,142));}
    }
    private async Task<List<AreaRow>> AllAreas(){var all=new List<AreaRow>();for(var skip=0;;skip+=50){var page=await service.AreasAsync(session,"",skip);all.AddRange(page);if(page.Count<50)return all;}}
    private async Task RefreshAreasAsync()
    {
        if(refreshingAreas)return;
        refreshingAreas=true;
        try
        {
            var areas=await AllAreas();
            if(IsDisposed)return;
            areaRows=areas;
            var selected=filter.SelectedValue as string??"";
            if(!areas.Any(x=>x.Code==selected))selected="";
            filter.DisplayMember="Name";filter.ValueMember="Code";
            filter.DataSource=new[]{new AreaRow("",L.T("Tất cả khu vực"),[])}.Concat(areas).ToList();
            filter.SelectedValue=selected;
        }
        catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){if(!IsDisposed)State.Text=ex.Message;}
        finally{refreshingAreas=false;}
    }
    private async Task RunRow(Func<Task> action)
    {
        try{await action();}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException){if(!IsDisposed)State.Text=ex.Message;}
    }
    private async Task SelectCode(string code)
    {
        var row=await service.SelectAsync(session,code);if(IsDisposed)return;
        if(row is null){State.Text=L.T("Bàn đã được xóa; tải lại.");return;}
        if(FindForm() is MainForm main)await main.OpenSalesTableAsync(row.Code);
    }
    private async Task Edit(TableRow? row)
    {
        var areas=await AllAreas();using var dialog=new OperationEditorDialog(row is null?L.T("Thêm bàn"):L.F("Sửa bàn · {0}",row.Code),id=>service.ReconcileAsync(session,id));
        var name=dialog.AddText(L.T("Tên bàn"),row?.Name??"");var area=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Code",DataSource=areas};if(row is not null)area.SelectedValue=row.Area;dialog.AddField(L.T("Khu vực"),area);
        dialog.CaptureWrite=()=>{var request=TableService.Normalize(new(row?.Code??"",name.Text,area.SelectedValue as string),false,row is null);return id=>service.SaveAsync(session,false,request,row?.Revision,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(TableRow row){using var dialog=new OperationEditorDialog(L.T("Xóa bàn"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1}. Có lịch sử hóa đơn thì không xóa được.", row.Code, row.Name));dialog.Confirmation=L.F("Xóa bàn {0}?", row.Code);dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,false,new(row.Code,row.Name,row.Area),row.Revision,id);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
}
internal sealed class AreaList : OrbitRecordListControl
{
    private readonly TableService service;private readonly SessionContext session;private readonly OrbitRowEditor inline;
    public AreaList(TableService service,SessionContext session){this.service=service;this.session=session;Configure("Khu vực",session,async(q,skip,t)=>await service.AreasAsync(session,q,skip,t),("Code",L.T("Mã")),("Name",L.T("Khu vực")));AddAction("Thêm",()=>Edit(null));
        AddRowDeleteColumn();Workspace.ShowMeta=false;
        inline=CreateInlineEditor();inline.CanWrite=_=>session.Identity.Role=="ADMIN";
        inline.RowKey=item=>((AreaRow)item).Code;
        inline.ColumnFor=(item,cell)=>(cell.OwningColumn?.Name ?? "")=="Name"?new("Name",InlineCellKind.Text):null;
        inline.Edit=item=>Edit((AreaRow)item);
        inline.Delete=async(item,id,t)=>{var row=(AreaRow)item;await service.DeleteAsync(session,true,new(row.Code,row.Name,null),row.Revision,id,t);return true;};
        inline.Reconcile=async(_,id,t)=>await service.ReconcileAsync(session,id,t) is not null;
        inline.Reload=()=>Reload();}
    private async Task RunRow(Func<Task> action){try{await action();}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException){if(!IsDisposed)State.Text=ex.Message;}}
    private async Task Edit(AreaRow? row){using var dialog=new OperationEditorDialog(row is null?L.T("Thêm khu vực"):L.F("Sửa khu vực · {0}",row.Code),id=>service.ReconcileAsync(session,id));var name=dialog.AddText(L.T("Tên khu vực"),row?.Name??"");dialog.CaptureWrite=()=>{var request=TableService.Normalize(new(row?.Code??"",name.Text,null),true,row is null);return id=>service.SaveAsync(session,true,request,row?.Revision,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
    private async Task Delete(AreaRow row){using var dialog=new OperationEditorDialog(L.T("Xóa khu vực"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("Xóa {0} · {1}; khu vực có bàn sẽ bị chặn.", row.Code, row.Name));dialog.Confirmation=L.F("Xóa {0}?", row.Code);dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,true,new(row.Code,row.Name,null),row.Revision,id);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
}








