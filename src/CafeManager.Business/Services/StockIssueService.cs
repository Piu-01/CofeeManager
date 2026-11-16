using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.Business.Services;
public sealed class StockIssueService(StockIssueRepository repository,PermissionService permissions)
{
    public static StockIssueRequest Normalize(StockIssueRequest r)
    {
        if(r.RequestId==Guid.Empty||r.Kind is not("Huy do hong" or "Tra hang")||r.Lines.Count==0||r.Lines.Select(l=>l.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=r.Lines.Count)throw new ArgumentException(L.T("Loại xuất hoặc dòng xuất không hợp lệ."));
        foreach(var line in r.Lines){MaterialService.Quantity(line.Quantity);if(line.Quantity==0)throw new ArgumentException(L.T("Lượng xuất phải dương."));MaterialService.Required(line.Code,5,L.T("Nguyên liệu"));}
        return r with{Reason=MaterialService.Required(r.Reason,200,L.T("Lý do"))};
    }
    private async Task<T> Run<T>(SessionContext s,Func<CancellationToken,Task<T>> action,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);try { await permissions.DemandAsync(s,AppPermission.Inventory,linked.Token);var result=await action(linked.Token);s.EnsureCurrent();return result;}catch(UnauthorizedAccessException e){throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Permission,s.IsRevoked?51505:51605,e.Message));} catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<StockIssueRow>> ListAsync(SessionContext s,string search,int skip,CancellationToken t=default)=>Run(s,ct=>repository.ListAsync(s.Identity,search,skip,ct),t);
    public Task<StockIssueRow?> GetAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<string> CreateAsync(SessionContext s,StockIssueRequest r,CancellationToken t=default)=>Run(s,ct=>repository.CreateAsync(s.Identity,Normalize(r),ct),t);
    public Task<string?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

