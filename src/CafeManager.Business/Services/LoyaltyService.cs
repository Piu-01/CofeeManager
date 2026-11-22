using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Requests.Loyalty;
namespace CafeManager.Business.Services;
public sealed class LoyaltyService(LoyaltyRepository repository,PermissionService permissions)
{
    public static PromotionRequest Normalize(PromotionRequest r,bool creating=true)
    {
        var code=creating&&string.IsNullOrWhiteSpace(r.Code)?"":MaterialService.Required(r.Code,10,L.T("Mã")).ToUpperInvariant();if(creating&&code.Length>0&&!Regex.IsMatch(code,"^KM[0-9]{8}$"))throw new ArgumentException(L.T("Mã KM và8chữ số."));
        if(r.EndDate is not null&&r.EndDate<r.StartDate||r.Discount<0||r.Discount>100)throw new ArgumentException(L.T("Ngày/khoảng giảm không hợp lệ."));
        var audience=string.IsNullOrWhiteSpace(r.Audience)?"ALL":r.Audience.Trim().ToUpperInvariant();
        var status=string.IsNullOrWhiteSpace(r.Status)?"Nhap":r.Status.Trim();
        if(audience is not ("ALL" or "FIRST" or "MEMBER" or "TIER")||status is not ("Nhap" or "DangChay" or "TamDung"))
            throw new ArgumentException(L.T("Đối tượng/trạng thái ưu đãi không hợp lệ."));
        var tiers=(r.Tiers??[]).Select(x=>MaterialService.Required(x,5,L.T("Mã hạng")).ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        var items=(r.Items??[]).Select(x=>MaterialService.Required(x,5,L.T("Mã món")).ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if(audience=="TIER"&&tiers.Length==0)throw new ArgumentException(L.T("Ưu đãi theo hạng cần chọn hạng."));
        if(audience!="TIER"&&tiers.Length>0)throw new ArgumentException(L.T("Chỉ ưu đãi theo hạng mới chọn hạng."));
        if(items.Length>500||tiers.Any(x=>!Regex.IsMatch(x,"^[A-Z0-9]{2,5}$"))||items.Any(x=>!Regex.IsMatch(x,"^[A-Z0-9]{5}$")))
            throw new ArgumentException(L.T("Hạng/món của ưu đãi không hợp lệ."));
        return r with{Code=code,Name=MaterialService.Required(r.Name,100,L.T("Tên chương trình")),Audience=audience,Status=status,Tiers=tiers,Items=items};
    }
    public static IReadOnlyList<TierInput> NormalizeTiers(IReadOnlyList<TierInput> tiers)
    {
        var rows=tiers.Select(t=>t with{Code=MaterialService.Required(t.Code,5,L.T("Mã hạng")).ToUpperInvariant()}).ToArray();
        if(rows.Length==0||rows.Min(t=>t.MinimumPoints)!=0||rows.Select(t=>t.Code).Distinct().Count()!=rows.Length||rows.Select(t=>t.MinimumPoints).Distinct().Count()!=rows.Length||rows.Any(t=>!Regex.IsMatch(t.Code,"^[A-Z0-9]{1,5}$")||t.MinimumPoints<0||t.Discount<0||t.Discount>100))throw new ArgumentException(L.T("Hạng cần mã/ngưỡng duy nhất, có ngưỡng0 và tỷ lệ0..100."));return rows;
    }
    public static void Money(decimal amount){if(amount<0||amount>999999999999999999m||decimal.Round(amount)!=amount)throw new ArgumentException(L.T("Tiền nguyên VND không âm, tối đa18chữ số."));}
    private async Task<T> Run<T>(SessionContext s,bool admin,Func<CancellationToken,Task<T>> f,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);await permissions.DemandAsync(s,admin?AppPermission.CatalogAdmin:AppPermission.Sales,linked.Token);
        try{var result=await f(linked.Token);s.EnsureCurrent();return result;}catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<PromotionRow>> PromotionsAsync(SessionContext s,string q,int skip,DateOnly? onDate=null,CancellationToken t=default)=>Run(s,false,ct=>repository.PromotionsAsync(s.Identity,q,skip,onDate,ct),t);
    public Task<OperationResult> SavePromotionAsync(SessionContext s,PromotionRequest r,byte[]? revision,Guid id,CancellationToken t=default)
    {
        var normalized=Normalize(r,revision is null);
        if(normalized.Code.Length>0)return Run(s,true,ct=>repository.SavePromotionAsync(s.Identity,normalized,revision,id,false,ct),t);
        return Run(s,true,async ct=>await repository.SavePromotionAsync(s.Identity,normalized with{Code=await repository.AllocateCodeAsync(ct)},revision,id,false,ct),t);
    }
    public Task<OperationResult> DeletePromotionAsync(SessionContext s,PromotionRow r,Guid id,CancellationToken t=default)=>Run(s,true,ct=>repository.SavePromotionAsync(s.Identity,new(r.Code,r.Name,r.StartDate,r.EndDate,r.Discount,r.Status=="HetHan"?"DangChay":r.Status,r.Audience,[],[]),r.Revision,id,true,ct),t);
    public Task<OperationResult> SetStateAsync(SessionContext s,PromotionRow r,bool resume,Guid id,CancellationToken t=default)=>Run(s,true,ct=>repository.SetPromotionStateAsync(s.Identity,r.Code,r.Revision,id,resume,ct),t);
    public Task<PromotionDetail?> PromotionDetailAsync(SessionContext s,string code,CancellationToken t=default)=>Run(s,true,ct=>repository.PromotionDetailAsync(s.Identity,code,ct),t);
    public Task<List<TierRow>> PolicyAsync(SessionContext s,CancellationToken t=default)=>Run(s,true,ct=>repository.PolicyAsync(s.Identity,ct),t);
    public Task<PolicyPreview> PolicyPreviewAsync(SessionContext s,IReadOnlyList<TierInput> tiers,CancellationToken t=default){var rows=NormalizeTiers(tiers);return Run(s,true,ct=>repository.PolicyPreviewAsync(s.Identity,rows,ct),t);}
    public Task<OperationResult> PolicySaveAsync(SessionContext s,IReadOnlyList<TierInput> tiers,PolicyPreview preview,Guid id,CancellationToken t=default){var rows=NormalizeTiers(tiers);return Run(s,true,ct=>repository.PolicySaveAsync(s.Identity,rows,preview,id,ct),t);}
    public Task<DiscountPreview> DiscountAsync(SessionContext s,DiscountPreviewRequest r,CancellationToken t=default)
    {
        Money(r.Money);Money(r.Ship);
        var lines=(r.Lines??[]).Select(x=>new PreviewLine(MaterialService.Required(x.Product,5,L.T("Mã món")).ToUpperInvariant(),x.Quantity)).ToArray();
        if(lines.Any(x=>x.Quantity<=0))throw new ArgumentException(L.T("Số lượng tạm tính phải dương."));
        var normalized=r with{Customer=string.IsNullOrWhiteSpace(r.Customer)?null:r.Customer.Trim(),Lines=lines};
        return Run(s,false,ct=>repository.DiscountAsync(s.Identity,normalized,ct),t);
    }
    public Task<List<PointHistory>> HistoryAsync(SessionContext s,string code,int skip,CancellationToken t=default)=>Run(s,true,ct=>repository.HistoryAsync(s.Identity,code,skip,ct),t);
    public Task<OperationReceipt?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,false,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

