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
        var tabs=new OrbitManagementTabs(true);tabs.AddPage("Bàn / Sơ đồ",new TableList(service,session));
        if(session.Identity.Role=="ADMIN")tabs.AddPage("Khu vực",new AreaList(service,session));
        Controls.Add(tabs);
    }
}
internal sealed class TableList : OrbitRecordListControl
{
    private readonly TableService service;
    private readonly SessionContext session;
    private readonly FlowLayoutPanel map=new(){Dock=DockStyle.Fill,Padding=new Padding(10),AutoScroll=true,WrapContents=true,BackColor=Theme.OrbitManagementTheme.Surface};
    private readonly ComboBox filter=new(){Width=160,DropDownStyle=ComboBoxStyle.DropDownList};
    public TableList(TableService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Bàn và trạng thái hiện tại",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,string.IsNullOrEmpty(filter.SelectedValue as string)?null:filter.SelectedValue as string,t),("Code","Mã"),("Name","Bàn"),("Area","Khu vực"),("Status","Trạng thái"),("OrderCode","Đơn mở"));
        AddFilter(filter,135);Workspace.Body.Controls.Add(map);Grid.Visible=false;
        var listView=new OrbitButton{Text="Danh sách"};var mapView=new OrbitButton{Text="Sơ đồ"};
        Theme.OrbitManagementTheme.Button(listView,Theme.ManagementButtonStyle.Segment,"list");Theme.OrbitManagementTheme.Button(mapView,Theme.ManagementButtonStyle.Segment,"grid");
        AddFilter(new OrbitViewSwitch(listView,mapView),180);mapView.NavigationSelected=true;
        listView.Click+=(_,_)=>{map.Visible=false;Grid.Visible=true;Grid.BringToFront();listView.NavigationSelected=true;mapView.NavigationSelected=false;listView.Invalidate();mapView.Invalidate();};mapView.Click+=(_,_)=>{Grid.Visible=false;map.Visible=true;map.BringToFront();listView.NavigationSelected=false;mapView.NavigationSelected=true;listView.Invalidate();mapView.Invalidate();};
        filter.SelectedValueChanged+=async(_,_)=>{if(filter.DataSource is not null)await ResetAndReload();};
        map.SizeChanged+=(_,_)=>ArrangeCards();
        AddAction("Chọn bàn",SelectCurrent);AddAction("Thêm",()=>Edit(true),session.Identity.Role=="ADMIN");AddAction("Sửa",()=>Edit(false),session.Identity.Role=="ADMIN");AddAction("Xóa",Delete,session.Identity.Role=="ADMIN");
        Grid.DataBindingComplete+=(_,_)=>
        {
            foreach(Control control in map.Controls.Cast<Control>().ToArray())control.Dispose();map.Controls.Clear();
            if(Grid.DataSource is not List<TableRow> rows)return;
            foreach(var row in rows){var button=new OrbitTableCard{Tag=row.Code,TableCode=row.Code,Caption=row.Name,Status=row.Status,OrderCode=row.OrderCode??"",Text=$"{row.Code} · {row.Name} · {row.Status}",AccessibleName=$"{row.Name} · {(row.Status=="Trong"?"Trống":"Đang phục vụ")}"};map.Controls.Add(button);button.Click+=async(_,_)=>{button.Enabled=false;try{foreach(DataGridViewRow item in Grid.Rows)if(item.DataBoundItem is TableRow selected&&selected.Code==row.Code){Grid.CurrentCell=item.Cells[0];break;}await SelectCode(row.Code);}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){State.Text=ex.Message;}finally{if(!button.IsDisposed)button.Enabled=true;}};}
        };
        Grid.CurrentCellChanged+=(_,_)=>HighlightCards();
        Grid.DataBindingComplete+=(_,_)=>{ArrangeCards();HighlightCards();};
        Load+=async(_,_)=>{try{var areas=await AllAreas();filter.DisplayMember="Name";filter.ValueMember="Code";filter.DataSource=new[]{new AreaRow("","Tất cả khu vực",[])}.Concat(areas).ToList();}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){State.Text=ex.Message;}};
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
    private Task SelectCurrent()=>Selected is TableRow row?SelectCode(row.Code):Task.CompletedTask;
    private async Task SelectCode(string code)
    {
        var row=await service.SelectAsync(session,code);if(IsDisposed)return;
        State.Text=row is null?"Bàn đã được xóa; tải lại.":$"{row.Name} · {row.Status} · đơn hiện tại: {row.OrderCode??"không có"}. Mở POS sẽ được tích hợp ở G04.";
    }
    private async Task Edit(bool create)
    {
        if(!create&&Selected is not TableRow)return;var row=create?null:(TableRow)Selected!;
        var areas=await AllAreas();using var dialog=new OperationEditorDialog(create?"Thêm bàn":"Sửa bàn",id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText("Mã B0001–B9999",row?.Code??"",!create);var name=dialog.AddText("Tên bàn",row?.Name??"");var area=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Code",DataSource=areas};if(row is not null)area.SelectedValue=row.Area;dialog.AddField("Khu vực",area);
        dialog.CaptureWrite=()=>{var request=TableService.Normalize(new(code.Text,name.Text,area.SelectedValue as string),false);return id=>service.SaveAsync(session,false,request,row?.Revision,id);};if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(){if(Selected is not TableRow row)return;using var dialog=new OperationEditorDialog("Xóa bàn",id=>service.ReconcileAsync(session,id));dialog.AddNote($"{row.Code} · {row.Name}. Có lịch sử hóa đơn thì không xóa được.");dialog.Confirmation=$"Xóa bàn {row.Code}?";dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,false,new(row.Code,row.Name,row.Area),row.Revision,id);if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();}
}
internal sealed class AreaList : OrbitRecordListControl
{
    private readonly TableService service;private readonly SessionContext session;
    public AreaList(TableService service,SessionContext session){this.service=service;this.session=session;Configure("Khu vực",session,async(q,skip,t)=>await service.AreasAsync(session,q,skip,t),("Code","Mã"),("Name","Khu vực"));AddAction("Thêm",()=>Edit(true));AddAction("Sửa",()=>Edit(false));AddAction("Xóa",Delete);}
    private async Task Edit(bool create){if(!create&&Selected is not AreaRow)return;var row=create?null:(AreaRow)Selected!;using var dialog=new OperationEditorDialog(create?"Thêm khu vực":"Sửa khu vực",id=>service.ReconcileAsync(session,id));var code=dialog.AddText("Mã KV001–KV999",row?.Code??"",!create);var name=dialog.AddText("Tên khu vực",row?.Name??"");dialog.CaptureWrite=()=>{var request=TableService.Normalize(new(code.Text,name.Text,null),true);return id=>service.SaveAsync(session,true,request,row?.Revision,id);};if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();}
    private async Task Delete(){if(Selected is not AreaRow row)return;using var dialog=new OperationEditorDialog("Xóa khu vực",id=>service.ReconcileAsync(session,id));dialog.AddNote($"Xóa {row.Code} · {row.Name}; khu vực có bàn sẽ bị chặn.");dialog.Confirmation=$"Xóa {row.Code}?";dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,true,new(row.Code,row.Name,null),row.Revision,id);if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();}
}







