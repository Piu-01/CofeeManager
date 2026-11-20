using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Tables;
namespace CafeManager.Business.Services;
public sealed class TableService(TableRepository repository,PermissionService permissions)
{
    public static TableRequest Normalize(TableRequest r,bool area)
    {
        var code=MaterialService.Required(r.Code,5,"Mã").ToUpperInvariant();
        if(!Regex.IsMatch(code,area?"^KV[0-9]{3}$":"^B[0-9]{4}$")||code==(area?"KV000":"B0000"))throw new ArgumentException("Mã bàn/khu vực không hợp lệ.");
        return new(code,MaterialService.Required(r.Name,50,"Tên"),area?null:MaterialService.Required(r.Area,5,"Khu vực"));
    }
    private async Task<T> Run<T>(SessionContext s,bool write,Func<CancellationToken,Task<T>> f,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);await permissions.DemandAsync(s,write?AppPermission.CatalogAdmin:AppPermission.Sales,linked.Token);
        try{var result=await f(linked.Token);s.EnsureCurrent();return result;}catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<AreaRow>> AreasAsync(SessionContext s,string q="",int skip=0,CancellationToken t=default)=>Run(s,false,ct=>repository.AreasAsync(s.Identity,q,skip,ct),t);
    public Task<List<TableRow>> ListAsync(SessionContext s,string q,int skip,string? area=null,CancellationToken t=default)=>Run(s,false,ct=>repository.ListAsync(s.Identity,q,skip,area,ct),t);
    public Task<TableRow?> SelectAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,false,ct=>repository.SelectAsync(s.Identity,code,ct),t);
    public Task<OperationResult> SaveAsync(SessionContext s,bool area,TableRequest r,byte[]? revision,Guid id,CancellationToken t=default){var request=Normalize(r,area);return Run(s,true,ct=>repository.WriteAsync(s.Identity,area,revision is null?"Create":"Update",request,revision,id,ct),t);}
    public Task<OperationResult> DeleteAsync(SessionContext s,bool area,TableRequest r,byte[] revision,Guid id,CancellationToken t=default)=>Run(s,true,ct=>repository.WriteAsync(s.Identity,area,"Delete",r,revision,id,ct),t);
    public Task<OperationReceipt?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,false,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}
