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
    public AssetsControl(AssetService service,SessionContext session,DocumentLinks links)
    {

        Configure("Tài sản",session,async(q,skip,t)=>{var page=await service.ListAsync(session,new(q,skip),t);return new OrbitPageRows<AssetRow>(page.Rows,page.TotalCount);},("Code",L.T("Mã")),("Name",L.T("Tài sản")),("PurchasedOn",L.T("Ngày mua")),("Value",L.T("Nguyên giá")),("Months",L.T("Tháng KH")));
        TotalCountSelector=rows=>((OrbitPageRows<AssetRow>)rows).TotalCount;UseStackedSearch(L.T("tài sản"));Workspace.Description.Text=L.T("Hồ sơ tài sản và khấu hao tham khảo · Lưu hồ sơ không tự ghi chi tiền.");
        Grid.Columns["Value"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Value"]!.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");Grid.Columns["Value"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;Grid.Columns["PurchasedOn"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        async Task Edit(bool create,bool delete=false){var row=create?null:Selected is AssetRow selected?await service.GetAsync(session,selected.Code):throw new ArgumentException(L.T("Chọn tài sản."));if(!create&&row is null)throw new ArgumentException(L.T("Tài sản không còn tồn tại."));var context=await service.ContextAsync(session);using var dialog=new AssetEditorDialog(service,session,new PendingOperationFile(context.Scope),row,delete);await OrbitDialogHost.ShowAsync(dialog,this);if(dialog.Changed)await Reload();}
        AddAction("Thêm",()=>Edit(true));AddAction("Sửa",()=>Edit(false));AddAction("Xóa",()=>Edit(false,true));AddAction("Yêu cầu chờ",()=>Edit(true));AddAction("Chi tiết",async()=>{if(Selected is AssetRow row)await links.ShowAssetAsync(this,row.Code);},needsRow:true);
    }
}

