using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Finance;
namespace CafeManager.Business.Services;
public sealed class SupplierDebtService(SupplierDebtRepository repository,PermissionService permissions)
{
    public static SupplierDebtRequest Normalize(SupplierDebtRequest r)
    {
        if(r.RequestId==Guid.Empty||r.Amount<=0||r.Amount>999999999999999999m||decimal.Truncate(r.Amount)!=r.Amount)throw new ArgumentException("Số trả phải là nguyên VND dương, tối đa18 chữ số.","Amount");
        var code=MaterialService.Required(r.Code,10,"Code");var method=MaterialService.Required(r.Method,5,"Method");if(method=="PT010")throw new ArgumentException("Không hỗ trợ đổi điểm.","Method");return r with{Code=code,Method=method};
    }
    private async Task<T> Run<T>(SessionContext s,AppPermission permission,Func<CancellationToken,Task<T>> action,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);try { await permissions.DemandAsync(s,permission,linked.Token);var result=await action(linked.Token);s.EnsureCurrent();return result;}catch(UnauthorizedAccessException e){throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Permission,s.IsRevoked?51505:51605,e.Message));} catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<SupplierDebtRow>> ListAsync(SessionContext s,string search,DateTime? asOf,int skip,CancellationToken t=default)=>Run(s,AppPermission.DebtRead,ct=>repository.ListAsync(s.Identity,search,asOf,skip,ct),t);
    public Task<SupplierDebtRow?> GetAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,AppPermission.DebtRead,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<List<InventoryLookup>> MethodsAsync(SessionContext s,CancellationToken t=default)=>Run(s,AppPermission.DebtPay,ct=>repository.MethodsAsync(s.Identity,ct),t);
    public Task<string> PayAsync(SessionContext s,SupplierDebtRequest r,CancellationToken t=default)=>Run(s,AppPermission.DebtPay,ct=>repository.PayAsync(s.Identity,Normalize(r),ct),t);
    public Task<string?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,AppPermission.DebtPay,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

