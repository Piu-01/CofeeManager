using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.States;
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
        if(session.Identity.Role=="ADMIN"){tabs.AddPage(L.T("Hạng thành viên"),new PolicyList(service,session));}
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
        using var dialog=new OperationEditorDialog(create?L.T("Thêm khuyến mãi"):L.F("Sửa khuyến mãi · {0}",row!.Code),id=>service.ReconcileAsync(session,id));
        var name=dialog.AddText(L.T("Tên chương trình"),row?.Name??"");
        var start=new OrbitDateField{Value=(row?.StartDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        var end=new OrbitDateField{Value=(row?.EndDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        dialog.AddField(L.T("Ngày bắt đầu"),start);dialog.AddField(L.T("Ngày kết thúc (bao gồm ngày này)"),end);var discount=dialog.AddText(L.T("Tỷ lệ giảm0–100%"),row?.Discount.ToString()??"0");
        dialog.CaptureWrite=()=>{if(!int.TryParse(discount.Text,out var percent))throw new ArgumentException(L.T("Tỷ lệ phải nguyên0–100."));var request=LoyaltyService.Normalize(new PromotionRequest(row?.Code??"",name.Text,DateOnly.FromDateTime(start.Value),DateOnly.FromDateTime(end.Value),percent),create);return id=>service.SavePromotionAsync(session,request,row?.Revision,id);};if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
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
        preview.Click+=async(_,_)=>{if(!preview.Enabled)return;preview.Enabled=false;var current=++version;result.Text=L.T("Đang tải…");try{var response=await service.DiscountAsync(session,new(LoyaltyControl.ReadMoney(money),LoyaltyControl.ReadMoney(ship),DateOnly.FromDateTime(date.Value),customer?.Code,usePromotion.Checked?promotion:null));if(!dialog.IsDisposed&&current==version){var sourceText=response.Source=="TierAndPromotion"?L.F("{0}: hạng {1}% + khuyến mãi {2}% = {3}%",SalesStates.Label(response.Source),response.TierPercent,response.PromotionPercent,response.PercentDiscount):L.F("{0}: {1}%",SalesStates.Label(response.Source),response.PercentDiscount);result.Text=L.F("Nguồn {0}\nGiảm {1} ₫\nPhải trả {2} ₫\nĐọc SQL {3}; chưa là kết quả checkout.", sourceText, response.Discount.ToString("N0",CultureInfo.GetCultureInfo("vi-VN")), response.Payable.ToString("N0",CultureInfo.GetCultureInfo("vi-VN")), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:HH:mm:ss}", response.ReadAt));}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed&&current==version)result.Text=L.T("Chưa có preview hợp lệ · ")+ex.Message;}finally{if(!preview.IsDisposed)preview.Enabled=true;}};
        await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this);version++;
    }
}
internal sealed class PolicyList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly SessionContext session;private bool rowBusy;
    public PolicyList(LoyaltyService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Chính sách hạng",session,async(_,skip,t)=>skip==0?await service.PolicyAsync(session,t):new List<TierRow>(),("Code",L.T("Hạng")),("MinimumPoints",L.T("Ngưỡng điểm")),("Discount",L.T("Giảm %")));Search.Visible=false;Paginated=false;
        AddAction("Thêm",AddTier);AddRowDeleteColumn();
        RowActivated=item=>{_=RunRowAction(()=>EditTier((TierRow)item));};
        RowDeleteRequested=item=>{_=RunRowAction(()=>DeleteTier((TierRow)item));};
    }
    private async Task RunRowAction(Func<Task> action)
    {
        if(rowBusy)return;rowBusy=true;
        try{await action();}
        catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!IsDisposed)State.Text=ex.Message;}
        finally{rowBusy=false;}
    }
    private static TierInput Input(TierRow row)=>new(row.Code,row.MinimumPoints,row.Discount);
    private Task AddTier()=>ShowEditor(null);
    private Task EditTier(TierRow row)=>ShowEditor(row);
    private async Task ShowEditor(TierRow? row)
    {
        var current=await service.PolicyAsync(session);
        using var dialog=new OperationEditorDialog(row is null?L.T("Thêm hạng thành viên"):L.T("Sửa hạng thành viên"),id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText(L.T("Mã hạng (A–Z, 0–9, tối đa 5 ký tự)"),row?.Code??"");
        var threshold=dialog.AddText(L.T("Ngưỡng điểm"),row?.MinimumPoints.ToString()??"");
        var discount=dialog.AddText(L.T("Giảm % (0–100)"),row?.Discount.ToString()??"0");
        var view=new OrbitButton{Text=L.T("Xem lại từ SQL"),AutoSize=true};dialog.AddField(L.T("Preview trước khi lưu"),view);
        var info=new Label{AutoSize=true,MaximumSize=new Size(520,0)};dialog.AddField(L.T("Ảnh hưởng"),info);
        PolicyPreview? preview=null;IReadOnlyList<TierInput>? captured=null;var version=0;
        void Invalidate(){version++;preview=null;captured=null;info.Text=L.T("Cần xem lại SQL.");}
        code.TextChanged+=(_,_)=>Invalidate();threshold.TextChanged+=(_,_)=>Invalidate();discount.TextChanged+=(_,_)=>Invalidate();
        IReadOnlyList<TierInput> ReadRows()
        {
            if(!int.TryParse(threshold.Text.Trim(),out var points)||points<0)throw new ArgumentException(L.T("Ngưỡng điểm phải là số nguyên không âm."));
            if(!int.TryParse(discount.Text.Trim(),out var percent)||percent<0||percent>100)throw new ArgumentException(L.T("Giảm % phải nguyên 0–100."));
            return current.Where(x=>row is null||x.Code!=row.Code).Select(Input).Append(new TierInput(code.Text.Trim().ToUpperInvariant(),points,percent)).OrderBy(x=>x.MinimumPoints).ToArray();
        }
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;info.Text=L.T("Đang tải…");try{var rows=ReadRows();var currentVersion=version;var response=await service.PolicyPreviewAsync(session,rows);if(!dialog.IsDisposed&&currentVersion==version){preview=response;captured=rows;info.Text=L.F("{0} khách sẽ đổi hạng. Sau thay đổi: {1}", response.AffectedCustomers, string.Join("; ",rows.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%")));}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed){preview=null;captured=null;info.Text=ex.Message;}}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var rows=ReadRows();if(preview is null||captured is null||!rows.SequenceEqual(captured))throw new ArgumentException(L.T("Cần xem lại SQL cho nội dung hiện tại."));var snapshot=preview;var payload=rows.ToArray();dialog.Confirmation=L.F("{0} hạng và tính lại hạng toàn bộ khách trong một transaction?",row is null?L.T("Thêm"):L.T("Sửa"));return async id=>{try{return await service.PolicySaveAsync(session,payload,snapshot,id);}catch{preview=null;captured=null;throw;}};};
        if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await Reload();
    }
    private async Task DeleteTier(TierRow row)
    {
        var current=await service.PolicyAsync(session);
        if(current.Count<=1||row.MinimumPoints==0)throw new ArgumentException(L.T("Phải giữ một hạng có ngưỡng 0 điểm."));
        var rows=current.Where(x=>x.Code!=row.Code).Select(Input).OrderBy(x=>x.MinimumPoints).ToArray();
        var preview=await service.PolicyPreviewAsync(session,rows);
        using var dialog=new OperationEditorDialog(L.T("Xóa hạng thành viên"),id=>service.ReconcileAsync(session,id));
        dialog.AddNote(L.F("{0} · ngưỡng {1} · giảm {2}%. {3} khách sẽ được xếp lại hạng theo ngưỡng còn lại.",row.Code,row.MinimumPoints,row.Discount,preview.AffectedCustomers));
        dialog.Confirmation=L.F("Xóa hạng {0}?",row.Code);
        dialog.CaptureWrite=()=>id=>service.PolicySaveAsync(session,rows,preview,id);
        if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await Reload();
    }
}
