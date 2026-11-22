using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Requests.Customers;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Customers;
public sealed class CustomersControl : OrbitRecordListControl
{
    private readonly CustomerService? service;private readonly SessionContext? session;private readonly LoyaltyService? loyalty;
    private OrbitRowEditor? inline;
    public event Action<CustomerRow?>? CustomerSelected;
    public CustomersControl(){}
    public CustomersControl(CustomerService service,SessionContext session,LoyaltyService loyalty)
    {
        this.service=service;this.session=session;this.loyalty=loyalty;
        Configure("Khách hàng",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,t),("Code",L.T("Mã")),("Name",L.T("Khách hàng")),("Phone",L.T("SDT")),("Points",L.T("Điểm")),("Tier",L.T("Hạng")),("Discount",L.T("Ưu đãi %")));
        Search.PlaceholderText=L.T("Tìm SDT hoặc tên khách");
        var admin=session.Identity.Role=="ADMIN";
        AddAction("Thêm",()=>Edit(true));AddAction("Mở sổ điểm",Open,admin);AddAction("Lịch sử điểm",History,admin);
        AddRowDeleteColumn();
        inline=CreateInlineEditor();inline.CanWrite=_=>admin;inline.RowKey=item=>((CustomerRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column((CustomerRow)item,cell.OwningColumn?.Name ?? "");
        inline.Save=SaveInline;inline.Delete=DeleteInline;
        inline.Reconcile=async(_,id,t)=>await service.ReconcileAsync(session,id,t) is not null;inline.Reload=()=>Reload();
    }
    public CustomersControl(CustomerService service,SessionContext session,bool picker=false,bool allowGuest=false)
    {
        this.service=service;this.session=session;
        Configure("Khách hàng",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,t),("Code",L.T("Mã")),("Name",L.T("Khách hàng")),("Phone",L.T("SDT")),("Points",L.T("Điểm")),("Tier",L.T("Hạng")),("Discount",L.T("Ưu đãi %")));
        Search.PlaceholderText=L.T("Tìm SDT hoặc tên khách");
        if(picker)
        {
            Workspace.Title.Text=L.T("Chọn khách hàng");
            Workspace.Description.Text=L.T("Tìm và chọn khách hàng trong danh sách.");
            AddAction("Chọn khách",async()=>{if(Selected is CustomerRow row){var current=await service.GetAsync(session,row.Code)??throw new ArgumentException(L.T("Khách đã bị xóa; tải lại danh sách trước khi chọn."));CustomerSelected?.Invoke(current);}});
            if(allowGuest)AddAction("Khách vãng lai",()=>{CustomerSelected?.Invoke(null);return Task.CompletedTask;});
            AddAction("Đóng",()=>{FindForm()?.Close();return Task.CompletedTask;});
            return;
        }
        var admin=session.Identity.Role=="ADMIN";
        AddAction("Thêm",()=>Edit(true));AddAction("Mở sổ điểm",Open,admin);
        AddRowDeleteColumn();
        inline=CreateInlineEditor();inline.CanWrite=_=>admin;inline.RowKey=item=>((CustomerRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column((CustomerRow)item,cell.OwningColumn?.Name ?? "");
        inline.Save=SaveInline;inline.Delete=DeleteInline;
        inline.Reconcile=async(_,id,t)=>await service.ReconcileAsync(session,id,t) is not null;inline.Reload=()=>Reload();
    }
    private async Task Edit(bool create)
    {
        if(service is null||session is null||(!create&&Selected is not CustomerRow))return;var row=create?null:(CustomerRow)Selected!;
        using var dialog=new OperationEditorDialog(create?L.T("Thêm khách hàng"):L.T("Sửa khách hàng"),id=>service.ReconcileAsync(session,id));
        if(row is not null)dialog.AddNote(L.F("{0} · Điểm {1} · Hạng {2} (chỉ đọc).", row.Code, row.Points, row.Tier));
        var name=dialog.AddText(L.T("Tên khách"),row?.Name??"");var phone=dialog.AddText(L.T("Số điện thoại (có thể trống)"),row?.Phone??"");
        dialog.CaptureWrite=()=>{var request=CustomerService.Normalize(new(name.Text,phone.Text));return id=>service.SaveAsync(session,request,row,id);};
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK){await Reload();if(dialog.ResultCode is { } code){var created=await service.GetAsync(session,code);foreach(DataGridViewRow item in Grid.Rows)if(item.DataBoundItem is CustomerRow customer&&customer.Code==code){item.Selected=true;Grid.CurrentCell=item.Cells[0];break;}State.Text=L.F("Đã lưu {0}. Điểm/hạng được đọc lại từ DB.", created?.Code??code);}}
    }
    private async Task Open()
    {
        if(service is null||session is null||Selected is not CustomerRow row)return;
        using var dialog=new OperationEditorDialog(L.T("Mở sổ điểm khách"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1}; chỉ một lần trước giao dịch.", row.Code, row.Name));var points=dialog.AddText(L.T("Điểm mở sổ"),"0");var source=dialog.AddText(L.T("Nguồn / lý do"));
        dialog.CaptureWrite=()=>{if(!int.TryParse(points.Text,out var value)||value<0)throw new ArgumentException(L.T("Điểm phải là số nguyên không âm."));var request=new CustomerOpeningRequest(row.Code,value,MaterialService.Required(source.Text,100,L.T("Nguồn")));dialog.Confirmation=L.F("Ghi {0} điểm mở sổ cho {1}? Nguồn: {2}. Không thể mở lại.", value, row.Code, request.Source);return id=>service.OpenAsync(session,request,row.Revision,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private static InlineColumn? Column(CustomerRow row,string property)=>property switch
    {
        "Name"=>new("Name",InlineCellKind.Text,item=>((CustomerRow)item).Name,(_,text)=>text.Trim()),
        "Phone"=>new("Phone",InlineCellKind.Text,item=>((CustomerRow)item).Phone??"",(_,text)=>text.Trim()),
        _=>null
    };
    private async Task SaveInline(object item,string property,object? value,Guid id,CancellationToken token)
    {
        if(service is null||session is null)return;
        var row=(CustomerRow)item;
        var request=CustomerService.Normalize(new(property=="Name"?(string)value!:row.Name,property=="Phone"?(string)value!:row.Phone));
        await service.SaveAsync(session,request,row,id,token);
    }
    // Khách hàng thuộc dữ liệu nghiệp vụ: xác nhận trước khi xóa; SQL chặn khi còn nguồn/lịch sử.
    private async Task<bool> DeleteInline(object item,Guid id,CancellationToken token)
    {
        if(service is null||session is null)return false;
        var row=(CustomerRow)item;
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Xóa khách {0} · {1}? Khách còn lịch sử sẽ bị chặn.",row.Code,row.Name),L.T("Xóa khách"),MessageBoxButtons.YesNo,L.T("Xóa"),L.T("Hủy"))!=DialogResult.Yes)return false;
        await service.DeleteAsync(session,row,id,token);return true;
    }
    private async Task History(){if(service is null||session is null||loyalty is null||Selected is not CustomerRow row)return;using var dialog=new OperationEditorDialog(L.T("Lịch sử điểm · ")+row.Name,id=>service.ReconcileAsync(session,id));dialog.SetReadOnly();dialog.AddNote(L.F("{0} · {1} điểm · hạng {2}. Chỉ đọc; lịch sử điểm không sửa/xóa.", row.Code, row.Points, row.Tier));dialog.AddField(L.T("Lịch sử điểm"),new CustomerPointHistoryList(loyalty,session,row.Code));await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this);await Task.CompletedTask;}
}
internal sealed class CustomerPointHistoryList : OrbitRecordListControl
{
    public CustomerPointHistoryList(LoyaltyService service,SessionContext session,string code)
    {
        EmptyHint=()=>L.T("Khách chưa có giao dịch điểm.");
        Configure("Lịch sử điểm (chỉ đọc)",session,async(_,skip,t)=>await service.HistoryAsync(session,code,skip,t),("Id",L.T("Mã GD")),("OccurredAt",L.T("Thời điểm")),("Direction",L.T("Loại")),("Points",L.T("Điểm")),("Reason",L.T("Lý do")),("OrderCode",L.T("Hóa đơn")));
        Workspace.Compact=true;Workspace.ShowTools=false;Workspace.ShowMeta=false;Search.Visible=false;
        Grid.Columns["OccurredAt"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        void FillDateColumn()=>Grid.Columns["OccurredAt"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        Grid.HandleCreated+=(_,_)=>FillDateColumn();Grid.DpiChangedAfterParent+=(_,_)=>FillDateColumn();Grid.DataBindingComplete+=(_,_)=>FillDateColumn();FillDateColumn();
    }
}


