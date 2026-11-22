using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Assets;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Assets;

public sealed class AssetsControl : OrbitRecordListControl
{
    private readonly AssetService service;
    private readonly SessionContext session;
    public AssetsControl(AssetService service,SessionContext session,DocumentLinks links)
    {
        this.service=service;this.session=session;

        Configure("Tài sản",session,async(q,skip,t)=>{var page=await service.ListAsync(session,new(q,skip),t);return new OrbitPageRows<AssetRow>(page.Rows,page.TotalCount);},("Code",L.T("Mã")),("Name",L.T("Tài sản")),("PurchasedOn",L.T("Ngày mua")),("Value",L.T("Nguyên giá")),("Months",L.T("Tháng KH")));
        TotalCountSelector=rows=>((OrbitPageRows<AssetRow>)rows).TotalCount;UseStackedSearch(L.T("tài sản"));Workspace.Description.Text=L.T("Hồ sơ tài sản và khấu hao tham khảo · Lưu hồ sơ không tự ghi chi tiền.");
        Grid.Columns["Value"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Value"]!.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");Grid.Columns["Value"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;Grid.Columns["PurchasedOn"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        async Task Edit(bool create){var context=await service.ContextAsync(session);using var dialog=new AssetEditorDialog(service,session,new PendingOperationFile(context.Scope),null,false);await OrbitDialogHost.ShowAsync(dialog,this);if(dialog.Changed)await Reload();}
        var admin=session.Identity.Role is "ADMIN" or "STORE";
        AddAction("Thêm",()=>Edit(true));AddAction("Yêu cầu chờ",()=>Edit(true));AddAction("Chi tiết",async()=>{if(Selected is AssetRow row)await links.ShowAssetAsync(this,row.Code);},needsRow:true);
        AddRowDeleteColumn();
        inline=CreateInlineEditor();inline.CanWrite=_=>admin;inline.RowKey=item=>((AssetRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column((AssetRow)item,cell.OwningColumn?.Name ?? "");
        inline.Save=SaveInline;inline.Delete=DeleteInline;inline.Reconcile=ReconcileInline;inline.Reload=()=>Reload();
    }
    private OrbitRowEditor inline=null!;
    private static InlineColumn? Column(AssetRow row,string property)=>property switch
    {
        "Name"=>new("Name",InlineCellKind.Text,item=>((AssetRow)item).Name,(_,text)=>text.Trim()),
        "PurchasedOn"=>new("PurchasedOn",InlineCellKind.Date,item=>((AssetRow)item).PurchasedOn,(_,text)=>Date(text)),
        "Value"=>new("Value",InlineCellKind.Money,item=>((AssetRow)item).Value,(_,text)=>Money(text),RightAligned:true),
        "Months"=>new("Months",InlineCellKind.Integer,item=>((AssetRow)item).Months,(_,text)=>Months(text),RightAligned:true),
        _=>null
    };
    private static DateOnly Date(string text)
    {
        if(!DateOnly.TryParseExact(text.Trim(),"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))throw new ArgumentException(L.T("Ngày phải theo dạng dd/MM/yyyy."));
        return date;
    }
    private static decimal Money(string text)
    {
        var cleaned=text.Replace("₫","").Replace(" ","").Replace(".","").Replace(',','.');
        if(!decimal.TryParse(cleaned,NumberStyles.Number,CultureInfo.InvariantCulture,out var value))throw new ArgumentException(L.T("Nguyên giá không hợp lệ."));
        return value;
    }
    private static int Months(string text)
    {
        if(!int.TryParse(text.Trim(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var months))throw new ArgumentException(L.T("Số tháng khấu hao phải là số nguyên dương."));
        return months;
    }
    private async Task SaveInline(object item,string property,object? value,Guid id,CancellationToken token)
    {
        var row=(AssetRow)item;
        var request=AssetService.Normalize(new(property=="Name"?(string)value!:row.Name,property=="PurchasedOn"?(DateOnly)value!:row.PurchasedOn,property=="Value"?(decimal)value!:row.Value,property=="Months"?(int)value!:row.Months));
        await service.WriteAsync(session,new(id,"Update",row.Code,row.Revision,request),token);
    }
    // Tài sản thuộc nhóm tài chính: xác nhận trước khi xóa.
    private async Task<bool> DeleteInline(object item,Guid id,CancellationToken token)
    {
        var row=(AssetRow)item;
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Xóa tài sản {0} · {1}? Hồ sơ có chi phí/lịch sử sẽ bị chặn.",row.Code,row.Name),L.T("Xóa tài sản"),MessageBoxButtons.YesNo,L.T("Xóa"),L.T("Hủy"))!=DialogResult.Yes)return false;
        await service.WriteAsync(session,new(id,"Delete",row.Code,row.Revision,null),token);return true;
    }
    // Không ghi file chờ theo dòng; đối chiếu bằng receipt SQL rồi tải lại.
    private async Task<bool?> ReconcileInline(object item,Guid id,CancellationToken token)=>await service.ReceiptAsync(session,id,token) is not null;
}

