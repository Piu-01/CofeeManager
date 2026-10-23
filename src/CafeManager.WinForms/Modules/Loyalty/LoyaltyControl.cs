using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Requests.Loyalty;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Loyalty;
public sealed class LoyaltyControl : UserControl
{
    public LoyaltyControl(){}
    public LoyaltyControl(LoyaltyService service,CustomerService customers,SessionContext session)
    {
        var tabs=new OrbitManagementTabs();
        tabs.AddPage("Khuyến mãi",new PromotionList(service,customers,session));
        if(session.Identity.Role=="ADMIN"){tabs.AddPage("Hạng thành viên",new PolicyList(service,session));tabs.AddPage("Điểm khách hàng",new PointList(service,customers,session));}
        Controls.Add(tabs);
    }
    internal static decimal ReadMoney(TextBox input){if(!decimal.TryParse(input.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException("Tiền không hợp lệ; dùng dấu phẩy cho phần thập phân.");LoyaltyService.Money(amount);return amount;}
}
internal sealed class PromotionList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly CustomerService customers;private readonly SessionContext session;
    public PromotionList(LoyaltyService service,CustomerService customers,SessionContext session)
    {
        this.service=service;this.customers=customers;this.session=session;
        Configure("Khuyến mãi và preview SQL",session,async(q,skip,t)=>await service.PromotionsAsync(session,q,skip,null,t),("Code","Mã"),("Name","Chương trình"),("StartDate","Từ ngày"),("EndDate","Đến ngày"),("Discount","Giảm %"));
        Grid.Columns["StartDate"]!.DefaultCellStyle.Format=Grid.Columns["EndDate"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        AddAction("Thêm",()=>Edit(true),session.Identity.Role=="ADMIN");AddAction("Sửa",()=>Edit(false),session.Identity.Role=="ADMIN");AddAction("Xóa",Delete,session.Identity.Role=="ADMIN");AddAction("Preview ưu đãi",Preview);
    }
    private async Task Edit(bool create)
    {
        if(!create&&Selected is not PromotionRow)return;var row=create?null:(PromotionRow)Selected!;
        using var dialog=new OperationEditorDialog(create?"Thêm khuyến mãi":"Sửa khuyến mãi",id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText("Mã KM và8chữ số",row?.Code??"",!create);var name=dialog.AddText("Tên chương trình",row?.Name??"");
        var start=new OrbitDateField{Value=(row?.StartDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        var end=new OrbitDateField{Value=(row?.EndDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        dialog.AddField("Ngày bắt đầu",start);dialog.AddField("Ngày kết thúc (bao gồm ngày này)",end);var discount=dialog.AddText("Tỷ lệ giảm0–100%",row?.Discount.ToString()??"0");
        dialog.CaptureWrite=()=>{if(!int.TryParse(discount.Text,out var percent))throw new ArgumentException("Tỷ lệ phải nguyên0–100.");var request=LoyaltyService.Normalize(new PromotionRequest(code.Text,name.Text,DateOnly.FromDateTime(start.Value),DateOnly.FromDateTime(end.Value),percent),create);return id=>service.SavePromotionAsync(session,request,row?.Revision,id);};if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(){if(Selected is not PromotionRow row)return;using var dialog=new OperationEditorDialog("Xóa khuyến mãi",id=>service.ReconcileAsync(session,id));dialog.AddNote($"{row.Code} · {row.Name}; có hóa đơn tham chiếu thì không xóa được.");dialog.Confirmation=$"Xóa {row.Code}?";dialog.CaptureWrite=()=>id=>service.DeletePromotionAsync(session,row,id);if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();}
    private Task Preview()
    {
        using var dialog=new OperationEditorDialog("Preview ưu đãi từ SQL",id=>service.ReconcileAsync(session,id));dialog.SetReadOnly();
        var money=dialog.AddText("Tiền món nguyên VND","0");var ship=dialog.AddText("Phí ship nguyên VND","0");
        var date=new OrbitDateField();dialog.AddField("Ngày lập cần preview",date);
        CustomerRow? customer=null;var customerButton=new OrbitButton{Text="Khách vãng lai · Chọn khách",AutoSize=true};dialog.AddField("Khách hàng",customerButton);
        customerButton.Click+=(_,_)=>{using var picker=new CustomerPickerDialog(customers,session,true);if(picker.ShowDialog(dialog)==DialogResult.OK){customer=picker.SelectedCustomer;customerButton.Text=customer is null?"Khách vãng lai":customer.Name+" · "+customer.Code;}};
        var usePromotion=new CheckBox{Text="Dùng chương trình đang chọn: "+((Selected as PromotionRow)?.Code??"không có"),AutoSize=true,Checked=Selected is PromotionRow};dialog.AddField("Nguồn ưu đãi",usePromotion);
        var preview=new OrbitButton{Text="Xem preview SQL",AutoSize=true};dialog.AddField("Kiểm tra số tiền",preview);
        var result=new Label{AutoSize=true,MaximumSize=new Size(520,0)};dialog.AddField("Kết quả",result);
        var promotion=(Selected as PromotionRow)?.Code;var version=0;
        void InvalidatePreview(){version++;result.Text="Cần tải preview mới.";}
        money.TextChanged+=(_,_)=>InvalidatePreview();ship.TextChanged+=(_,_)=>InvalidatePreview();date.ValueChanged+=(_,_)=>InvalidatePreview();usePromotion.CheckedChanged+=(_,_)=>InvalidatePreview();customerButton.Click+=(_,_)=>InvalidatePreview();
        preview.Click+=async(_,_)=>{if(!preview.Enabled)return;preview.Enabled=false;var current=++version;result.Text="Đang tải…";try{var response=await service.DiscountAsync(session,new(LoyaltyControl.ReadMoney(money),LoyaltyControl.ReadMoney(ship),DateOnly.FromDateTime(date.Value),customer?.Code,usePromotion.Checked?promotion:null));if(!dialog.IsDisposed&&current==version)result.Text=$"Nguồn {response.Source} · {response.PercentDiscount}%\nGiảm {response.Discount.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))} ₫\nPhải trả {response.Payable.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))} ₫\nĐọc SQL {response.ReadAt:HH:mm:ss}; chưa là kết quả checkout.";}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed&&current==version)result.Text="Chưa có preview hợp lệ · "+ex.Message;}finally{if(!preview.IsDisposed)preview.Enabled=true;}};
        dialog.ShowDialog(this);version++;return Task.CompletedTask;
    }
}
internal sealed class PolicyList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly SessionContext session;
    public PolicyList(LoyaltyService service,SessionContext session){this.service=service;this.session=session;Configure("Chính sách hạng",session,async(_,skip,t)=>skip==0?await service.PolicyAsync(session,t):new List<TierRow>(),("Code","Hạng"),("MinimumPoints","Ngưỡng điểm"),("Discount","Giảm %"));Search.Visible=false;Paginated=false;AddAction("Thay chính sách",Edit);}
    private async Task Edit()
    {
        var old=await service.PolicyAsync(session);using var dialog=new OperationEditorDialog("Thay chính sách hạng",id=>service.ReconcileAsync(session,id)){Width=900,Height=620};
        var data=new global::System.Data.DataTable();data.Columns.Add("Mã hạng",typeof(string));data.Columns.Add("Ngưỡng",typeof(string));data.Columns.Add("Giảm %",typeof(string));foreach(var row in old)data.Rows.Add(row.Code,row.MinimumPoints.ToString(),row.Discount.ToString());
        var grid=new DataGridView{DataSource=data,Height=260,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};dialog.AddField("Toàn bộ chính sách mới",grid);
        PolicyPreview? preview=null;IReadOnlyList<TierInput>? captured=null;
        IReadOnlyList<TierInput> ReadRows(){if(!grid.EndEdit())throw new ArgumentException("Hoàn tất ô đang sửa.");var rows=data.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted).Select(x=>new TierInput(Convert.ToString(x[0])??"",int.TryParse(Convert.ToString(x[1]),out var threshold)?threshold:-1,int.TryParse(Convert.ToString(x[2]),out var percent)?percent:-1)).ToArray();return LoyaltyService.NormalizeTiers(rows);}
        var view=new OrbitButton{Text="Xem lại từ SQL",AutoSize=true};dialog.AddField("Preview trước khi xác nhận",view);var info=new Label{AutoSize=true,MaximumSize=new Size(800,0)};dialog.AddField("Ảnh hưởng",info);
        var changed=0;grid.CellValueChanged+=(_,_)=>{changed++;preview=null;info.Text="Cần preview lại.";};grid.UserDeletedRow+=(_,_)=>{changed++;preview=null;};
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;try{captured=ReadRows();var current=changed;var response=await service.PolicyPreviewAsync(session,captured);if(!dialog.IsDisposed&&current==changed){preview=response;info.Text=$"Hạng khách sẽ được tính lại theo chính sách mới. {response.AffectedCustomers} khách đổi hạng tại thời điểm preview.\nCũ: {string.Join("; ",old.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%"))}\nMới: {string.Join("; ",captured.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%"))}";}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed)info.Text=ex.Message;}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var rows=ReadRows();if(preview is null||captured is null||!rows.SequenceEqual(captured))throw new ArgumentException("Cần xem preview SQL cho nội dung hiện tại.");var snapshot=preview;var payload=rows.ToArray();dialog.Confirmation="Thay chính sách và tính lại hạng toàn bộ khách trong một transaction? Điểm và lịch sử hóa đơn giữ nguyên.";return id=>service.PolicySaveAsync(session,payload,snapshot,id);};
        if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
}
internal sealed class PointList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly CustomerService customers;private readonly SessionContext session;private CustomerRow? customer;
    public PointList(LoyaltyService service,CustomerService customers,SessionContext session)
    {
        this.service=service;this.customers=customers;this.session=session;
        EmptyHint=()=>customer is null?"Chọn khách hàng để xem lịch sử điểm.":null;
        Configure("Lịch sử điểm (chỉ đọc)",session,async(_,skip,t)=>customer is null?new List<PointHistory>():await service.HistoryAsync(session,customer.Code,skip,t),("Id","Mã GD"),("OccurredAt","Thời điểm"),("Direction","Loại"),("Points","Điểm"),("Reason","Lý do"),("OrderCode","Hóa đơn"));Search.Visible=false;Grid.Columns["OccurredAt"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        AddAction("Chọn khách",async()=>{using var picker=new CustomerPickerDialog(customers,session);if(picker.ShowDialog(this)==DialogResult.OK){customer=picker.SelectedCustomer;SetSelectionCaption(customer is null?null:$"{customer.Code} · {customer.Name} · {customer.Points} điểm · {customer.Tier}");await ResetAndReload();State.Text=customer is null?"Chọn khách để xem điểm.":$"{customer.Code} · {customer.Name} · điểm {customer.Points} · hạng {customer.Tier}";}});AddAction("Điều chỉnh điểm",Adjust);
    }
    private async Task Adjust()
    {
        if(customer is null)throw new ArgumentException("Chọn khách hàng trước.");var current=await customers.GetAsync(session,customer.Code)??throw new ArgumentException("Khách không còn tồn tại.");
        using var dialog=new OperationEditorDialog("Điều chỉnh điểm",id=>service.ReconcileAsync(session,id));dialog.AddNote($"{current.Code} · {current.Name} · hiện tại {current.Points} điểm / {current.Tier}");
        var direction=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};direction.Items.AddRange(["Cộng","Trừ"]);direction.SelectedIndex=0;dialog.AddField("Loại điều chỉnh",direction);var points=dialog.AddText("Số điểm dương","");var reason=dialog.AddText("Lý do bắt buộc");
        PointPreview? preview=null;PointAdjustmentRequest? captured=null;var changed=0;
        void Invalidate(){preview=null;changed++;}
        direction.SelectedIndexChanged+=(_,_)=>Invalidate();points.TextChanged+=(_,_)=>Invalidate();reason.TextChanged+=(_,_)=>Invalidate();
        PointAdjustmentRequest Read(){if(!int.TryParse(points.Text,out var value))throw new ArgumentException("Điểm phải nguyên dương.");return LoyaltyService.Normalize(new PointAdjustmentRequest(current.Code,direction.SelectedIndex==0?"Cong":"Tru",value,reason.Text));}
        var view=new OrbitButton{Text="Xem lại số dư từ SQL",AutoSize=true};dialog.AddField("Preview trước khi ghi",view);var info=new Label{AutoSize=true};dialog.AddField("Kết quả dự kiến",info);
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;try{captured=Read();var version=changed;var response=await service.PointPreviewAsync(session,captured);if(!dialog.IsDisposed&&version==changed){preview=response;info.Text=$"Sau điều chỉnh: {response.Balance} điểm · hạng {response.Tier}.";}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed)info.Text=ex.Message;}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var request=Read();if(preview is null||captured!=request)throw new ArgumentException("Cần preview SQL cho nội dung hiện tại.");var snapshot=preview;dialog.Confirmation=$"{direction.Text} {request.Points} điểm cho {current.Code}? Lý do: {request.Reason}. Lịch sử không thể sửa/xóa.";return id=>service.AdjustAsync(session,request,snapshot,id);};if(dialog.ShowDialog(this)==DialogResult.OK){customer=await customers.GetAsync(session,current.Code);await Reload();}
    }
}





