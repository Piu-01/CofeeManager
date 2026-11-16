using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Assets;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Assets;

internal sealed class AssetPickerDialog : OrbitDialogForm
{
    public AssetRow? SelectedAsset {get;private set;}
    public AssetPickerDialog(AssetService service,SessionContext session)
    {
        Text=L.T("Chọn tài sản");AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(760,500);AcceptButton=null;
        Controls.Add(new Picker(service,session,row=>{SelectedAsset=row;DialogResult=DialogResult.OK;Close();}));
    }
    private sealed class Picker : OrbitRecordListControl
    {
        public Picker(AssetService service,SessionContext session,Action<AssetRow> choose){Configure("Chọn tài sản",session,async(q,s,t)=>{var p=await service.ListAsync(session,new(q,s),t);return new OrbitPageRows<AssetRow>(p.Rows,p.TotalCount);},("Code",L.T("Mã")),("Name",L.T("Tên tài sản")));TotalCountSelector=rows=>((OrbitPageRows<AssetRow>)rows).TotalCount;UseStackedSearch(L.T("tài sản"));AddAction("Chọn",()=>{if(Selected is AssetRow r)choose(r);return Task.CompletedTask;},needsRow:true);}
    }
}

