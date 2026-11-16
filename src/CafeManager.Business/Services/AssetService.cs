using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Assets;
using CafeManager.Models.Requests.Assets;
namespace CafeManager.Business.Services;

public sealed class AssetService(AssetRepository repository, PermissionService permissions)
{
    public static AssetRequest Normalize(AssetRequest r)
    {
        var name = r.Name.Trim();
        if (name.Length is < 1 or > 100 || name.Any(char.IsControl)) throw new ArgumentException(L.T("Tên tài sản cần1–100 ký tự, không ký tự điều khiển."),"Name");
        if (r.Value <= 0 || r.Value > 999999999999999999m || decimal.Truncate(r.Value) != r.Value) throw new ArgumentException(L.T("Nguyên giá phải là nguyên VND dương, tối đa18 chữ số."),"Value");
        if (r.Months <= 0) throw new ArgumentException(L.T("Số tháng khấu hao phải là số nguyên dương."),"Months");
        return r with { Name = name };
    }
    private Task<T> Run<T>(SessionContext s, Func<CancellationToken,Task<T>> action, CancellationToken t) => HumanResourcesAccess.Run(permissions,s,AppPermission.Finance,action,t);
    public Task<AssetContext> ContextAsync(SessionContext s,CancellationToken t=default) => Run(s,ct=>repository.ContextAsync(s.Identity,ct),t);
    public Task<AssetPage> ListAsync(SessionContext s,AssetFilter f,CancellationToken t=default)
    {
        if(f.Search.Length>100||f.Skip<0)throw new ArgumentException(L.T("Bộ lọc tài sản không hợp lệ."));
        return Run(s,ct=>repository.ListAsync(s.Identity,f with {Search=f.Search.Trim()},ct),t);
    }
    public Task<AssetRow?> GetAsync(SessionContext s,string code,CancellationToken t=default) => Run(s,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<AssetReceipt> WriteAsync(SessionContext s,AssetWriteRequest r,CancellationToken t=default)
    {
        if(r.RequestId==Guid.Empty || r.Action is not ("Create" or "Update" or "Delete") || r.Action!="Create" && (r.Code?.Length!=5||r.ExpectedRevision?.Length!=8)) throw new ArgumentException(L.T("Yêu cầu tài sản không hợp lệ."));
        if(r.Action!="Delete")r=r with {Value=Normalize(r.Value??throw new ArgumentException(L.T("Thiếu hồ sơ tài sản.")))};
        return Run(s,ct=>repository.WriteAsync(s.Identity,r,ct),t);
    }
    public Task<AssetReceipt?> ReceiptAsync(SessionContext s,Guid id,CancellationToken t=default) => Run(s,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
    public Task<AssetExpensePage> ExpensesAsync(SessionContext s,string code,DateOnly from,DateOnly to,int skip=0,CancellationToken t=default)
    {
        if(from>to||skip<0)throw new ArgumentException(L.T("Kỳ chi không hợp lệ."));
        return Run(s,ct=>repository.ExpensesAsync(s.Identity,code,from,to,skip,ct),t);
    }
}
