using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.Inventory;
namespace CafeManager.WinForms.UI;

// Điều hướng chứng từ nguồn qua cùng owner chain, không truy cập SQL ở UI.
public sealed class DocumentLinks(PurchaseService purchases,FinanceService finance,SupplierDebtService debts,SessionContext session)
{
    public bool CanFinance=>session.Identity.Role=="ADMIN";
    private static async Task Open(Control owner,Func<Task<OrbitDialogForm?>> create)
    {
        try{using var dialog=await create();if(owner.IsDisposed)return;if(dialog is null){await OrbitConfirmationDialog.ShowAsync(owner,"Chứng từ không tồn tại.","Tra cứu");return;}await OrbitDialogHost.ShowAsync(dialog,owner);}
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!owner.IsDisposed&&!owner.Disposing)await OrbitConfirmationDialog.ShowAsync(owner,e.Message,"Chưa tải được chứng từ");}
    }
    public Task ShowPurchaseAsync(Control owner,string code)=>Open(owner,async()=>{var row=await purchases.GetAsync(session,code);return row is null?null:new PurchaseDetailDialog(row,this);});
    public Task ShowFinanceAsync(Control owner,string code)=>Open(owner,async()=>{var row=await finance.GetAsync(session,code);return row is null?null:new FinanceDetailDialog(row,this);});
    public Task ShowDebtAsync(Control owner,string code)=>Open(owner,async()=>{var row=await debts.GetAsync(session,code);return row is null?null:new SupplierDebtDetailDialog(row,this);});
}
