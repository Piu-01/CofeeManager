using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Products;
namespace CafeManager.Business.Services;
public sealed class ProductService(ProductRepository repository,PermissionService permissions)
{
    public static ProductRequest Normalize(ProductRequest r,bool creating=false)
    {
        var code=creating&&string.IsNullOrWhiteSpace(r.Code)?"":MaterialService.Required(r.Code,5,L.T("Mã")).ToUpperInvariant();
        if(code.Length>0&&(!Regex.IsMatch(code,"^SP[0-9]{3}$")||code=="SP000")) throw new ArgumentException(L.T("Mã SP001–SP999."));
        if(r.Price<=0||r.Price>999999999999999999m||decimal.Round(r.Price)!=r.Price) throw new ArgumentException(L.T("Giá phải là số nguyên VND dương."));
        if(r.Status is not ("Dang ban" or "Ngung ban")) throw new ArgumentException(L.T("Trạng thái không hợp lệ."));
        var lines=r.Recipe.Select(x=>new RecipeInput(MaterialService.Required(x.Code,5,L.T("Nguyên liệu")).ToUpperInvariant(),x.Quantity)).ToArray();
        foreach(var line in lines) { MaterialService.Quantity(line.Quantity); if(line.Quantity==0) throw new ArgumentException(L.T("Định mức phải dương.")); }
        if(lines.Select(x=>x.Code).Distinct().Count()!=lines.Length) throw new ArgumentException(L.T("Nguyên liệu công thức bị trùng."));
        return r with {Code=code,Name=MaterialService.Required(r.Name,50,L.T("Tên")),Category=MaterialService.Required(r.Category,5,L.T("Danh mục")),Recipe=lines};
    }
    private async Task<T> Run<T>(SessionContext s,AppPermission p,Func<CancellationToken,Task<T>> work,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t); await permissions.DemandAsync(s,p,linked.Token);
        try { var result=await work(linked.Token); s.EnsureCurrent(); return result; } catch(DataAccessException e){ if(e.Error.Number==51505)s.Revoke(); throw new UserOperationException(e.Error); }
    }
    public Task<List<ProductRow>> ListAsync(SessionContext s,string search,int skip,string? category=null,bool selling=false,CancellationToken t=default)=>Run(s,AppPermission.MenuRead,ct=>repository.ListAsync(s.Identity,search,skip,category,selling,ct),t);
    public Task<List<RecipeLine>> RecipeAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,AppPermission.RecipeRead,ct=>repository.RecipeAsync(s.Identity,code,ct),t);
    public Task<List<MaterialRow>> MaterialsAsync(SessionContext s,CancellationToken t=default)=>Run(s,AppPermission.RecipeRead,ct=>repository.MaterialsAsync(s.Identity,ct),t);
    public Task<List<NameLookup>> CategoriesAsync(SessionContext s,CancellationToken t=default)=>Run(s,AppPermission.MenuRead,ct=>repository.CategoriesAsync(s.Identity,ct),t);
    public Task<OperationResult> SaveAsync(SessionContext s,ProductRequest request,byte[]? revision,Guid id,CancellationToken t=default)
    {
        var r=Normalize(request,revision is null);
        if(r.Code.Length>0)return Run(s,AppPermission.CatalogAdmin,ct=>repository.WriteAsync(s.Identity,revision is null?"Create":"Update",r,revision,id,ct),t);
        return Run(s,AppPermission.CatalogAdmin,async ct=>await repository.WriteAsync(s.Identity,"Create",r with{Code=await repository.AllocateCodeAsync(ct)},revision,id,ct),t);
    }
    public Task<OperationResult> RecipeSaveAsync(SessionContext s,ProductRow row,IReadOnlyList<RecipeInput> lines,Guid id,CancellationToken t=default){var r=Normalize(new(row.Code,row.Name,row.Category,row.Price,row.Status,lines));return Run(s,AppPermission.CatalogAdmin,ct=>repository.WriteAsync(s.Identity,"ReplaceRecipe",r,row.Revision,id,ct),t);}
    public Task<OperationResult> DeleteAsync(SessionContext s,ProductRow row,Guid id,CancellationToken t=default)=>Run(s,AppPermission.CatalogAdmin,ct=>repository.WriteAsync(s.Identity,"Delete",new(row.Code,row.Name,row.Category,row.Price,row.Status,[]),row.Revision,id,ct),t);
    public Task<OperationReceipt?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,AppPermission.MenuRead,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

