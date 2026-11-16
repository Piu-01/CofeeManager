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
        tabs.AddPage(L.T("Khuyến mãi"),new PromotionList(service,customers,session));
        if(session.Identity.Role=="ADMIN"){tabs.AddPage(L.T("Hạng thành viên"),new PolicyList(service,session));tabs.AddPage(L.T("Điểm khách hàng"),new PointList(service,customers,session));}
        Controls.Add(tabs);
    }
    internal static decimal ReadMoney(TextBox input){if(!decimal.TryParse(input.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException(L.T("Tiền không hợp lệ; dùng dấu phẩy cho phần thập phân."));LoyaltyService.Money(amount);return amount;}
}
internal sealed class PromotionList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly CustomerService customers;private readonly SessionContext session;
    public PromotionList(LoyaltyService service,CustomerService customers,SessionContext session)
    {
        this.service=service;this.customers=customers;this.session=session;
        Configure("Khuyến mãi và preview SQL",session,async(q,skip,t)=>await service.PromotionsAsync(session,q,skip,null,t),("Code",L.T("Mã")),("Name",L.T("Chương trình")),("StartDate",L.T("Từ ngày")),("EndDate",L.T("Đến ngày")),("Discount",L.T("Giảm %")));
        Grid.Columns["StartDate"]!.DefaultCellStyle.Format=Grid.Columns["EndDate"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        AddAction("Thêm",()=>Edit(true),session.Identity.Role=="ADMIN");AddAction("Sửa",()=>Edit(false),session.Identity.Role=="ADMIN");AddAction("Xóa",Delete,session.Identity.Role=="ADMIN");AddAction("Preview ưu đãi",Preview);
    }
    private async Task Edit(bool create)
    {
        if(!create&&Selected is not PromotionRow)return;var row=create?null:(PromotionRow)Selected!;
        using var dialog=new OperationEditorDialog(create?L.T("Thêm khuyến mãi"):L.T("Sửa khuyến mãi"),id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText(L.T("Mã KM và8chữ số"),row?.Code??"",!create);var name=dialog.AddText(L.T("Tên chương trình"),row?.Name??"");
        var start=new OrbitDateField{Value=(row?.StartDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        var end=new OrbitDateField{Value=(row?.EndDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        dialog.AddField(L.T("Ngày bắt đầu"),start);dialog.AddField(L.T("Ngày kết thúc (bao gồm ngày này)"),end);var discount=dialog.AddText(L.T("Tỷ lệ giảm0–100%"),row?.Discount.ToString()??"0");
        dialog.CaptureWrite=()=>{if(!int.TryParse(discount.Text,out var percent))throw new ArgumentException(L.T("Tỷ lệ phải nguyên0–100."));var request=LoyaltyService.Normalize(new PromotionRequest(code.Text,name.Text,DateOnly.FromDateTime(start.Value),DateOnly.FromDateTime(end.Value),percent),create);return id=>service.SavePromotionAsync(session,request,row?.Revision,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(){if(Selected is not PromotionRow row)return;using var dialog=new OperationEditorDialog(L.T("Xóa khuyến mãi"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1}; có hóa đơn tham chiếu thì không xóa được.", row.Code, row.Name));dialog.Confirmation=L.F("Xóa {0}?", row.Code);dialog.CaptureWrite=()=>id=>service.DeletePromotionAsync(session,row,id);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
    private async Task Preview()
    {
        using var dialog=new OperationEditorDialog(L.T("Preview ưu đãi từ SQL"),id=>service.ReconcileAsync(session,id));dialog.SetReadOnly();
        var money=dialog.AddText(L.T("Tiền món nguyên VND"),"0");var ship=dialog.AddText(L.T("Phí ship nguyên VND"),"0");
        var date=new OrbitDateField();dialog.AddField(L.T("Ngày lập cần preview"),date);
        CustomerRow? customer=null;var customerButton=new OrbitButton{Text=L.T("Khách vãng lai · Chọn khách"),AutoSize=true};dialog.AddField(L.T("Khách hàng"),customerButton);
        customerButton.Click+=async(_,_)=>{using var picker=new CustomerPickerDialog(customers,session,true);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(picker, dialog)==DialogResult.OK){customer=picker.SelectedCustomer;customerButton.Text=customer is null?L.T("Khách vãng lai"):customer.Name+" · "+customer.Code;}};
        var usePromotion=new CheckBox{Text=L.T("Dùng chương trình đang chọn: ")+((Selected as PromotionRow)?.Code??L.T("không có")),AutoSize=true,Checked=Selected is PromotionRow};dialog.AddField(L.T("Nguồn ưu đãi"),usePromotion);
        var preview=new OrbitButton{Text=L.T("Xem preview SQL"),AutoSize=true};dialog.AddField(L.T("Kiểm tra số tiền"),preview);
        var result=new Label{AutoSize=true,MaximumSize=new Size(520,0)};dialog.AddField(L.T("Kết quả"),result);
        var promotion=(Selected as PromotionRow)?.Code;var version=0;
        void InvalidatePreview(){version++;result.Text=L.T("Cần tải preview mới.");}
        money.TextChanged+=(_,_)=>InvalidatePreview();ship.TextChanged+=(_,_)=>InvalidatePreview();date.ValueChanged+=(_,_)=>InvalidatePreview();usePromotion.CheckedChanged+=(_,_)=>InvalidatePreview();customerButton.Click+=(_,_)=>InvalidatePreview();
        preview.Click+=async(_,_)=>{if(!preview.Enabled)return;preview.Enabled=false;var current=++version;result.Text=L.T("Đang tải…");try{var response=await service.DiscountAsync(session,new(LoyaltyControl.ReadMoney(money),LoyaltyControl.ReadMoney(ship),DateOnly.FromDateTime(date.Value),customer?.Code,usePromotion.Checked?promotion:null));if(!dialog.IsDisposed&&current==version)result.Text=L.F("Nguồn {0} · {1}%\nGiảm {2} ₫\nPhải trả {3} ₫\nĐọc SQL {4}; chưa là kết quả checkout.", response.Source, response.PercentDiscount, response.Discount.ToString("N0",CultureInfo.GetCultureInfo("vi-VN")), response.Payable.ToString("N0",CultureInfo.GetCultureInfo("vi-VN")), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:HH:mm:ss}", response.ReadAt));}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed&&current==version)result.Text=L.T("Chưa có preview hợp lệ · ")+ex.Message;}finally{if(!preview.IsDisposed)preview.Enabled=true;}};
        await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this);version++;
    }
}
internal sealed class PolicyList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly SessionContext session;
    public PolicyList(LoyaltyService service,SessionContext session){this.service=service;this.session=session;Configure("Chính sách hạng",session,async(_,skip,t)=>skip==0?await service.PolicyAsync(session,t):new List<TierRow>(),("Code",L.T("Hạng")),("MinimumPoints",L.T("Ngưỡng điểm")),("Discount",L.T("Giảm %")));Search.Visible=false;Paginated=false;AddAction("Thay chính sách",Edit);}
    private async Task Edit()
    {
        var old=await service.PolicyAsync(session);using var dialog=new OperationEditorDialog(L.T("Thay chính sách hạng"),id=>service.ReconcileAsync(session,id)){Width=900,Height=620};
        var data=new global::System.Data.DataTable();data.Columns.Add(L.T("Mã hạng"),typeof(string));data.Columns.Add(L.T("Ngưỡng"),typeof(string));data.Columns.Add(L.T("Giảm %"),typeof(string));foreach(var row in old)data.Rows.Add(row.Code,row.MinimumPoints.ToString(),row.Discount.ToString());
        var grid=new DataGridView{DataSource=data,Height=260,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};dialog.AddField(L.T("Toàn bộ chính sách mới"),grid);
        PolicyPreview? preview=null;IReadOnlyList<TierInput>? captured=null;
        IReadOnlyList<TierInput> ReadRows(){if(!grid.EndEdit())throw new ArgumentException(L.T("Hoàn tất ô đang sửa."));var rows=data.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted).Select(x=>new TierInput(Convert.ToString(x[0])??"",int.TryParse(Convert.ToString(x[1]),out var threshold)?threshold:-1,int.TryParse(Convert.ToString(x[2]),out var percent)?percent:-1)).ToArray();return LoyaltyService.NormalizeTiers(rows);}
        var view=new OrbitButton{Text=L.T("Xem lại từ SQL"),AutoSize=true};dialog.AddField(L.T("Preview trước khi xác nhận"),view);var info=new Label{AutoSize=true,MaximumSize=new Size(800,0)};dialog.AddField(L.T("Ảnh hưởng"),info);
        var changed=0;grid.CellValueChanged+=(_,_)=>{changed++;preview=null;info.Text=L.T("Cần preview lại.");};grid.UserDeletedRow+=(_,_)=>{changed++;preview=null;};
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;try{captured=ReadRows();var current=changed;var response=await service.PolicyPreviewAsync(session,captured);if(!dialog.IsDisposed&&current==changed){preview=response;info.Text=L.F("Hạng khách sẽ được tính lại theo chính sách mới. {0} khách đổi hạng tại thời điểm preview.\nCũ: {1}\nMới: {2}", response.AffectedCustomers, string.Join("; ",old.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%")), string.Join("; ",captured.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%")));}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed)info.Text=ex.Message;}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var rows=ReadRows();if(preview is null||captured is null||!rows.SequenceEqual(captured))throw new ArgumentException(L.T("Cần xem preview SQL cho nội dung hiện tại."));var snapshot=preview;var payload=rows.ToArray();dialog.Confirmation=L.T("Thay chính sách và tính lại hạng toàn bộ khách trong một transaction? Điểm và lịch sử hóa đơn giữ nguyên.");return id=>service.PolicySaveAsync(session,payload,snapshot,id);};
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
}
internal sealed class PointList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly CustomerService customers;private readonly SessionContext session;private CustomerRow? customer;
    public PointList(LoyaltyService service,CustomerService customers,SessionContext session)
    {
        this.service=service;this.customers=customers;this.session=session;
        EmptyHint=()=>customer is null?L.T("Chọn khách hàng để xem lịch sử điểm."):null;
        Configure("Lịch sử điểm (chỉ đọc)",session,async(_,skip,t)=>customer is null?new List<PointHistory>():await service.HistoryAsync(session,customer.Code,skip,t),("Id",L.T("Mã GD")),("OccurredAt",L.T("Thời điểm")),("Direction",L.T("Loại")),("Points",L.T("Điểm")),("Reason",L.T("Lý do")),("OrderCode",L.T("Hóa đơn")));Search.Visible=false;Grid.Columns["OccurredAt"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";
        AddAction("Chọn khách",async()=>{using var picker=new CustomerPickerDialog(customers,session);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(picker, this)==DialogResult.OK){customer=picker.SelectedCustomer;SetSelectionCaption(customer is null?null:L.F("{0} · {1} · {2} điểm · {3}", customer.Code, customer.Name, customer.Points, customer.Tier));await ResetAndReload();State.Text=customer is null?L.T("Chọn khách để xem điểm."):L.F("{0} · {1} · điểm {2} · hạng {3}", customer.Code, customer.Name, customer.Points, customer.Tier);}});AddAction("Điều chỉnh điểm",Adjust);
    }
    private async Task Adjust()
    {
        if(customer is null)throw new ArgumentException(L.T("Chọn khách hàng trước."));var current=await customers.GetAsync(session,customer.Code)??throw new ArgumentException(L.T("Khách không còn tồn tại."));
        using var dialog=new OperationEditorDialog(L.T("Điều chỉnh điểm"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1} · hiện tại {2} điểm / {3}", current.Code, current.Name, current.Points, current.Tier));
        var direction=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};direction.Items.AddRange([L.T("Cộng"),L.T("Trừ")]);direction.SelectedIndex=0;dialog.AddField(L.T("Loại điều chỉnh"),direction);var points=dialog.AddText(L.T("Số điểm dương"),"");var reason=dialog.AddText(L.T("Lý do bắt buộc"));
        PointPreview? preview=null;PointAdjustmentRequest? captured=null;var changed=0;
        void Invalidate(){preview=null;changed++;}
        direction.SelectedIndexChanged+=(_,_)=>Invalidate();points.TextChanged+=(_,_)=>Invalidate();reason.TextChanged+=(_,_)=>Invalidate();
        PointAdjustmentRequest Read(){if(!int.TryParse(points.Text,out var value))throw new ArgumentException(L.T("Điểm phải nguyên dương."));return LoyaltyService.Normalize(new PointAdjustmentRequest(current.Code,direction.SelectedIndex==0?"Cong":"Tru",value,reason.Text));}
        var view=new OrbitButton{Text=L.T("Xem lại số dư từ SQL"),AutoSize=true};dialog.AddField(L.T("Preview trước khi ghi"),view);var info=new Label{AutoSize=true};dialog.AddField(L.T("Kết quả dự kiến"),info);
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;try{captured=Read();var version=changed;var response=await service.PointPreviewAsync(session,captured);if(!dialog.IsDisposed&&version==changed){preview=response;info.Text=L.F("Sau điều chỉnh: {0} điểm · hạng {1}.", response.Balance, response.Tier);}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed)info.Text=ex.Message;}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var request=Read();if(preview is null||captured!=request)throw new ArgumentException(L.T("Cần preview SQL cho nội dung hiện tại."));var snapshot=preview;dialog.Confirmation=L.F("{0} {1} điểm cho {2}? Lý do: {3}. Lịch sử không thể sửa/xóa.", direction.Text, request.Points, current.Code, request.Reason);return id=>service.AdjustAsync(session,request,snapshot,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK){customer=await customers.GetAsync(session,current.Code);await Reload();}
    }
}








