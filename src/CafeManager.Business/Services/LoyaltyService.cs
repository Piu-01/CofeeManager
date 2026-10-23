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
        var code=MaterialService.Required(r.Code,10,"Mã").ToUpperInvariant();if(creating&&!Regex.IsMatch(code,"^KM[0-9]{8}$"))throw new ArgumentException("Mã KM và8chữ số.");
        if(r.EndDate<r.StartDate||r.Discount<0||r.Discount>100)throw new ArgumentException("Ngày/khoảng giảm không hợp lệ.");return r with{Code=code,Name=MaterialService.Required(r.Name,100,"Tên chương trình")};
    }
    public static IReadOnlyList<TierInput> NormalizeTiers(IReadOnlyList<TierInput> tiers)
    {
        var rows=tiers.Select(t=>t with{Code=MaterialService.Required(t.Code,5,"Mã hạng").ToUpperInvariant()}).ToArray();
        if(rows.Length==0||rows.Min(t=>t.MinimumPoints)!=0||rows.Select(t=>t.Code).Distinct().Count()!=rows.Length||rows.Select(t=>t.MinimumPoints).Distinct().Count()!=rows.Length||rows.Any(t=>!Regex.IsMatch(t.Code,"^[A-Z0-9]{1,5}$")||t.MinimumPoints<0||t.Discount<0||t.Discount>100))throw new ArgumentException("Hạng cần mã/ngưỡng duy nhất, có ngưỡng0 và tỷ lệ0..100.");return rows;
    }
    public static PointAdjustmentRequest Normalize(PointAdjustmentRequest r)
    {
        if(r.Points<=0||r.Direction is not ("Cong" or "Tru"))throw new ArgumentException("Số điểm dương và loại Cộng/Trừ hợp lệ.");return r with{Code=MaterialService.Required(r.Code,5,"Mã khách"),Reason=MaterialService.Required(r.Reason,100,"Lý do")};
    }
    public static void Money(decimal amount){if(amount<0||amount>999999999999999999m||decimal.Round(amount)!=amount)throw new ArgumentException("Tiền nguyên VND không âm, tối đa18chữ số.");}
    private async Task<T> Run<T>(SessionContext s,bool admin,Func<CancellationToken,Task<T>> f,CancellationToken t)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(s.Token,t);await permissions.DemandAsync(s,admin?AppPermission.CatalogAdmin:AppPermission.Sales,linked.Token);
        try{var result=await f(linked.Token);s.EnsureCurrent();return result;}catch(DataAccessException e){if(e.Error.Number==51505)s.Revoke();throw new UserOperationException(e.Error);}
    }
    public Task<List<PromotionRow>> PromotionsAsync(SessionContext s,string q,int skip,DateOnly? onDate=null,CancellationToken t=default)=>Run(s,false,ct=>repository.PromotionsAsync(s.Identity,q,skip,onDate,ct),t);
    public Task<OperationResult> SavePromotionAsync(SessionContext s,PromotionRequest r,byte[]? revision,Guid id,CancellationToken t=default){var normalized=Normalize(r,revision is null);return Run(s,true,ct=>repository.SavePromotionAsync(s.Identity,normalized,revision,id,false,ct),t);}
    public Task<OperationResult> DeletePromotionAsync(SessionContext s,PromotionRow r,Guid id,CancellationToken t=default)=>Run(s,true,ct=>repository.SavePromotionAsync(s.Identity,new(r.Code,r.Name,r.StartDate,r.EndDate,r.Discount),r.Revision,id,true,ct),t);
    public Task<List<TierRow>> PolicyAsync(SessionContext s,CancellationToken t=default)=>Run(s,true,ct=>repository.PolicyAsync(s.Identity,ct),t);
    public Task<PolicyPreview> PolicyPreviewAsync(SessionContext s,IReadOnlyList<TierInput> tiers,CancellationToken t=default){var rows=NormalizeTiers(tiers);return Run(s,true,ct=>repository.PolicyPreviewAsync(s.Identity,rows,ct),t);}
    public Task<OperationResult> PolicySaveAsync(SessionContext s,IReadOnlyList<TierInput> tiers,PolicyPreview preview,Guid id,CancellationToken t=default){var rows=NormalizeTiers(tiers);return Run(s,true,ct=>repository.PolicySaveAsync(s.Identity,rows,preview,id,ct),t);}
    public Task<PointPreview> PointPreviewAsync(SessionContext s,PointAdjustmentRequest r,CancellationToken t=default){var request=Normalize(r);return Run(s,true,ct=>repository.PointPreviewAsync(s.Identity,request,ct),t);}
    public Task<OperationResult> AdjustAsync(SessionContext s,PointAdjustmentRequest r,PointPreview preview,Guid id,CancellationToken t=default){var request=Normalize(r);return Run(s,true,ct=>repository.AdjustAsync(s.Identity,request,preview,id,ct),t);}
    public Task<DiscountPreview> DiscountAsync(SessionContext s,DiscountPreviewRequest r,CancellationToken t=default){Money(r.Money);Money(r.Ship);var normalized=r with{Customer=string.IsNullOrWhiteSpace(r.Customer)?null:r.Customer.Trim(),Promotion=string.IsNullOrWhiteSpace(r.Promotion)?null:r.Promotion.Trim()};return Run(s,false,ct=>repository.DiscountAsync(s.Identity,normalized,ct),t);}
    public Task<List<PointHistory>> HistoryAsync(SessionContext s,string code,int skip,CancellationToken t=default)=>Run(s,true,ct=>repository.HistoryAsync(s.Identity,code,skip,ct),t);
    public Task<OperationReceipt?> ReconcileAsync(SessionContext s,Guid id,CancellationToken t=default)=>Run(s,false,ct=>repository.ReceiptAsync(s.Identity,id,ct),t);
}

