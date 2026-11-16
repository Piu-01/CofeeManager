using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.Business.Services;
public sealed class StocktakeService(StocktakeRepository repository,PermissionService permissions)
{
    public static void Validate(StocktakeRequest r)
    {
        if(r.RequestId==Guid.Empty||r.Operation is not("StocktakeCreate" or "StocktakeSave" or "StocktakeCommit"))throw new ArgumentException(L.T("Yêu cầu kiểm kê không hợp lệ."));
        if(r.Operation=="StocktakeCreate"){if(r.Scope.Count==0||r.Scope.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=r.Scope.Count)throw new ArgumentException(L.T("Chọn nguyên liệu, không lặp mã."));}
        else if(string.IsNullOrWhiteSpace(r.Code)||r.Revision?.Length!=8)throw new ArgumentException(L.T("Thiếu mã hoặc phiên bản phiếu."));
        if(r.Counts.Select(l=>l.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=r.Counts.Count)throw new ArgumentException(L.T("Trùng nguyên liệu kiểm kê."));
        foreach(var l in r.Counts){if(l.Quantity.HasValue)MaterialService.Quantity(l.Quantity.Value);if(l.Reason?.Length>100)throw new ArgumentException(L.T("Lý do tối đa100 ký tự."));}
    }
    private async Task<T> Run<T>(SessionContext s,Func<CancellationToken,Task<T>> action,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);try { await permissions.DemandAsync(s,AppPermission.Inventory,linked.Token);var result=await action(linked.Token);s.EnsureCurrent();return result;}catch(UnauthorizedAccessException e){throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Permission,s.IsRevoked?51505:51605,e.Message));} catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<StocktakeRow>> ListAsync(SessionContext s,string search,int skip,CancellationToken t=default)=>Run(s,ct=>repository.ListAsync(s.Identity,search,skip,ct),t);
    public Task<StocktakeRow?> GetAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<string> WriteAsync(SessionContext s,StocktakeRequest r,CancellationToken t=default){Validate(r);return Run(s,ct=>repository.WriteAsync(s.Identity,r,ct),t);}
    public Task<string?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

