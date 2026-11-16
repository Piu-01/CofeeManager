using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.Business.Services;
public sealed class PurchaseService(PurchaseRepository repository,PermissionService permissions)
{
    public static void ValidateLines(IReadOnlyList<PurchaseLine> lines)
    {
        if(lines.Count==0||lines.Select(l=>l.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=lines.Count)throw new ArgumentException(L.T("Cần ít nhất một nguyên liệu; mỗi mã một dòng."));
        foreach(var line in lines)
        {
            MaterialService.Quantity(line.Quantity);
            if(line.Quantity<=0||line.Price<=0||line.Price>999999999999999999m||decimal.Truncate(line.Price)!=line.Price)throw new ArgumentException(L.T("Lượng và giá phải dương; giá nguyên VND, lượng tối đa4 số lẻ."));
            MaterialService.Required(line.Code,5,L.T("Nguyên liệu"));
        }
    }
    private async Task<T> Run<T>(SessionContext s,Func<CancellationToken,Task<T>> action,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);try { await permissions.DemandAsync(s,AppPermission.Inventory,linked.Token);var result=await action(linked.Token);s.EnsureCurrent();return result;}
        catch(UnauthorizedAccessException e){throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Permission,s.IsRevoked?51505:51605,e.Message));} catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<InventoryLookup>> LookupsAsync(SessionContext s,string kind,CancellationToken t=default)=>Run(s,ct=>repository.LookupsAsync(s.Identity,kind,ct),t);
    public Task<List<PurchaseRow>> ListAsync(SessionContext s,string search,int skip,CancellationToken t=default)=>Run(s,ct=>repository.ListAsync(s.Identity,search,skip,ct),t);
    public Task<PurchaseRow?> GetAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<decimal> PreviewAsync(SessionContext s,IReadOnlyList<PurchaseLine> lines,CancellationToken t=default){ValidateLines(lines);return Run(s,ct=>repository.PreviewAsync(s.Identity,lines,ct),t);}
    public Task<string> CreateAsync(SessionContext s,PurchaseRequest r,CancellationToken t=default)
    {
        ValidateLines(r.Lines);
        if(r.RequestId==Guid.Empty||r.Immediate<0||r.Immediate>r.ExpectedTotal||decimal.Truncate(r.Immediate)!=r.Immediate)throw new ArgumentException(L.T("Phần trả ngay không hợp lệ."));
        return Run(s,async ct=>{if(r.Immediate>0)await permissions.DemandAsync(s,AppPermission.Finance,ct);return await repository.CreateAsync(s.Identity,r,ct);},t);
    }
    public Task<string?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

