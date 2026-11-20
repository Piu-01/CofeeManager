using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Customers;
namespace CafeManager.Business.Services;
public sealed class CustomerService(CustomerRepository repository,PermissionService permissions)
{
    public static CustomerRequest Normalize(CustomerRequest r)
    {
        var phone=r.Phone?.Trim();if(string.IsNullOrEmpty(phone))phone=null;
        if(phone is not null&&!Regex.IsMatch(phone,"^[0-9]{1,15}$"))throw new ArgumentException("SDT tối đa15chữ số, giữ số0đầu; có thể để trống.");
        return new(MaterialService.Required(r.Name,50,"Tên khách"),phone);
    }
    private async Task<T> Run<T>(SessionContext s,bool admin,Func<CancellationToken,Task<T>> f,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);await permissions.DemandAsync(s,admin?AppPermission.CatalogAdmin:AppPermission.Customers,linked.Token);
        try{var result=await f(linked.Token);s.EnsureCurrent();return result;}catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<CustomerRow>> ListAsync(SessionContext s,string q,int skip,CancellationToken t=default)=>Run(s,false,ct=>repository.ListAsync(s.Identity,q,skip,ct),t);
    public Task<CustomerRow?> GetAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,false,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<List<CustomerOrder>> OrdersAsync(SessionContext s,string code,int skip,CancellationToken t=default)=>Run(s,false,ct=>repository.OrdersAsync(s.Identity,code,skip,ct),t);
    public Task<OperationResult> SaveAsync(SessionContext s,CustomerRequest request,CustomerRow? row,Guid id,CancellationToken t=default){var r=Normalize(request);return Run(s,row is not null,ct=>repository.WriteAsync(s.Identity,row is null?"Create":"Update",id,row?.Code,row?.Revision,r.Name,r.Phone,null,null,ct),t);}
    public Task<OperationResult> OpenAsync(SessionContext s,CustomerOpeningRequest r,byte[] revision,Guid id,CancellationToken t=default){if(r.Points<0)throw new ArgumentException("Điểm mở sổ không âm.");var source=MaterialService.Required(r.Source,100,"Nguồn");return Run(s,true,ct=>repository.WriteAsync(s.Identity,"Open",id,r.Code,revision,null,null,r.Points,source,ct),t);}
    public Task<OperationResult> DeleteAsync(SessionContext s,CustomerRow row,Guid id,CancellationToken t=default)=>Run(s,true,ct=>repository.WriteAsync(s.Identity,"Delete",id,row.Code,row.Revision,null,null,null,null,ct),t);
    public Task<OperationReceipt?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,false,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}
