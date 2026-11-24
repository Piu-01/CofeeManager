using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.Inventory;
namespace CafeManager.WinForms.UI;

// Điều hướng chứng từ nguồn qua cùng owner chain, không truy cập SQL ở UI.
public sealed class DocumentLinks(PurchaseService purchases,FinanceService finance,SupplierDebtService debts,SessionContext session,PayrollService? payroll=null,AssetService? assets=null)
{
    public Task ShowAssetAsync(Control owner,string code)
    {
        for(Form? form=owner.FindForm();form is not null;form=form.Owner) if(Equals(form.Tag,"Asset:"+code)) return Task.CompletedTask;
        return Open(owner,async()=>{if(assets is null)throw new ArgumentException(L.T("Chưa cấu hình dịch vụ tài sản."));var row=await assets.GetAsync(session,code);return row is null?null:new Modules.Assets.AssetDetailDialog(row,assets,session,this);});
    }
    public Task ShowPayrollAsync(Control owner,int id)
    {
        for(Form? form=owner.FindForm();form is not null;form=form.Owner) if(Equals(form.Tag,"Payroll:"+id)) return Task.CompletedTask;
        return Open(owner,async()=>{if(payroll is null)return null;var row=await payroll.GetAsync(session,id);return row is null?null:new Modules.HumanResources.PayrollDetailDialog(row,this);});
    }
    public bool CanFinance=>session.Identity.Role=="ADMIN";
    public bool CanAssets=>CanFinance&&assets is not null;
    private static async Task Open(Control owner,Func<Task<OrbitDialogForm?>> create)
    {
        try{using var dialog=await create();if(owner.IsDisposed)return;if(dialog is null){await OrbitConfirmationDialog.ShowAsync(owner,L.T("Chứng từ không tồn tại."),L.T("Tra cứu"));return;}await OrbitDialogHost.ShowAsync(dialog,owner);}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException or ArgumentException){if(!owner.IsDisposed&&!owner.Disposing)await OrbitConfirmationDialog.ShowAsync(owner,e.Message,L.T("Chưa tải được chứng từ"));}
    }
    public Task ShowPurchaseAsync(Control owner,string code)=>Open(owner,async()=>{var row=await purchases.GetAsync(session,code);return row is null?null:new PurchaseDetailDialog(row,this,purchases,session);});
    public Task ShowFinanceAsync(Control owner,string code)
    {
        for(Form? form=owner.FindForm();form is not null;form=form.Owner) if(Equals(form.Tag,"Finance:"+code)) return Task.CompletedTask;
        return Open(owner,async()=>{var row=await finance.GetAsync(session,code);return row is null?null:new FinanceDetailDialog(row,this);});
    }
    public Task ShowDebtAsync(Control owner,string code)=>Open(owner,async()=>{var row=await debts.GetAsync(session,code);return row is null?null:new SupplierDebtDetailDialog(row,this);});
}
