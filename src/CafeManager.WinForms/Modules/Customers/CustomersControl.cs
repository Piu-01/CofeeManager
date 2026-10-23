using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Requests.Customers;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Customers;
public sealed class CustomersControl : OrbitRecordListControl
{
    private readonly CustomerService? service;private readonly SessionContext? session;
    public event Action<CustomerRow?>? CustomerSelected;
    public CustomersControl(){}
    public CustomersControl(CustomerService service,SessionContext session,bool picker=false,bool allowGuest=false)
    {
        this.service=service;this.session=session;
        Configure("Khách hàng",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,t),("Code","Mã"),("Name","Khách hàng"),("Phone","SDT"),("Points","Điểm"),("Tier","Hạng"),("Discount","Ưu đãi %"));
        Search.PlaceholderText="Tìm SDT hoặc tên khách";
        if(picker)
        {
            Workspace.Title.Text="Chọn khách hàng";
            Workspace.Description.Text="Tìm và chọn khách hàng trong danh sách.";
            AddAction("Chọn khách",async()=>{if(Selected is CustomerRow row){var current=await service.GetAsync(session,row.Code)??throw new ArgumentException("Khách đã bị xóa; tải lại danh sách trước khi chọn.");CustomerSelected?.Invoke(current);}});
            if(allowGuest)AddAction("Khách vãng lai",()=>{CustomerSelected?.Invoke(null);return Task.CompletedTask;});
            AddAction("Đóng",()=>{FindForm()?.Close();return Task.CompletedTask;});
            return;
        }
        AddAction("Thêm",()=>Edit(true));AddAction("Sửa",()=>Edit(false),session.Identity.Role=="ADMIN");AddAction("Xóa",Delete,session.Identity.Role=="ADMIN");AddAction("Mở sổ điểm",Open,session.Identity.Role=="ADMIN");AddAction("Hóa đơn",Orders);
    }
    private async Task Edit(bool create)
    {
        if(service is null||session is null||(!create&&Selected is not CustomerRow))return;var row=create?null:(CustomerRow)Selected!;
        using var dialog=new OperationEditorDialog(create?"Thêm khách hàng":"Sửa khách hàng",id=>service.ReconcileAsync(session,id));
        if(row is not null)dialog.AddNote($"{row.Code} · Điểm {row.Points} · Hạng {row.Tier} (chỉ đọc).");
        var name=dialog.AddText("Tên khách",row?.Name??"");var phone=dialog.AddText("Số điện thoại (có thể trống)",row?.Phone??"");
        dialog.CaptureWrite=()=>{var request=CustomerService.Normalize(new(name.Text,phone.Text));return id=>service.SaveAsync(session,request,row,id);};
        if(dialog.ShowDialog(this)==DialogResult.OK){await Reload();if(dialog.ResultCode is { } code){var created=await service.GetAsync(session,code);foreach(DataGridViewRow item in Grid.Rows)if(item.DataBoundItem is CustomerRow customer&&customer.Code==code){item.Selected=true;Grid.CurrentCell=item.Cells[0];break;}State.Text=$"Đã lưu {created?.Code??code}. Điểm/hạng được đọc lại từ DB.";}}
    }
    private async Task Open()
    {
        if(service is null||session is null||Selected is not CustomerRow row)return;
        using var dialog=new OperationEditorDialog("Mở sổ điểm khách",id=>service.ReconcileAsync(session,id));dialog.AddNote($"{row.Code} · {row.Name}; chỉ một lần trước giao dịch.");var points=dialog.AddText("Điểm mở sổ","0");var source=dialog.AddText("Nguồn / lý do");
        dialog.CaptureWrite=()=>{if(!int.TryParse(points.Text,out var value)||value<0)throw new ArgumentException("Điểm phải là số nguyên không âm.");var request=new CustomerOpeningRequest(row.Code,value,MaterialService.Required(source.Text,100,"Nguồn"));dialog.Confirmation=$"Ghi {value} điểm mở sổ cho {row.Code}? Nguồn: {request.Source}. Không thể mở lại.";return id=>service.OpenAsync(session,request,row.Revision,id);};if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(){if(service is null||session is null||Selected is not CustomerRow row)return;using var dialog=new OperationEditorDialog("Xóa khách",id=>service.ReconcileAsync(session,id));dialog.AddNote($"{row.Code} · {row.Name}; chỉ xóa khi chưa có nguồn/lịch sử.");dialog.Confirmation=$"Xóa khách {row.Code}?";dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,row,id);if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();}
    private async Task Orders(){if(service is null||session is null||Selected is not CustomerRow row)return;using var dialog=new OperationEditorDialog("Hóa đơn · "+row.Name,id=>service.ReconcileAsync(session,id));dialog.SetReadOnly();var history=new CustomerOrderList(service,session,row.Code);dialog.AddField("Hóa đơn",history);dialog.ShowDialog(this);await Task.CompletedTask;}
}
internal sealed class CustomerOrderList : OrbitRecordListControl
{
    public CustomerOrderList(CustomerService service,SessionContext session,string code)
    {
        Configure("Hóa đơn của khách",session,async(_,skip,t)=>await service.OrdersAsync(session,code,skip,t),("Code","Mã hóa đơn"),("CreatedAt","Ngày lập"),("Status","Trạng thái"));
        Workspace.Compact=true;Workspace.ShowTools=false;Workspace.ShowMeta=false;
        Grid.Columns["CreatedAt"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        void FillDateColumn()=>Grid.Columns["CreatedAt"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        Grid.HandleCreated+=(_,_)=>FillDateColumn();Grid.DpiChangedAfterParent+=(_,_)=>FillDateColumn();Grid.DataBindingComplete+=(_,_)=>FillDateColumn();FillDateColumn();
    }
}

