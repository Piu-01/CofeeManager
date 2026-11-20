using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Errors;
namespace CafeManager.Business.Services;
public sealed record TableStatusResult(IReadOnlyList<TableStatusDto>? Rows,DataError? Error);
public sealed class TableStatusService(TableStatusRepository repository,PermissionService permissions)
{
    public async Task<TableStatusResult> LoadAsync(SessionContext session,string? areaCode,CancellationToken cancellationToken)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(session.Token,cancellationToken);
        await permissions.DemandAsync(session,AppPermission.Sales,linked.Token);
        try{var rows=await repository.GetAsync(areaCode,linked.Token);session.EnsureCurrent();return new(rows,null);}
        catch(DataAccessException e){return new(null,e.Error);}
    }
}
