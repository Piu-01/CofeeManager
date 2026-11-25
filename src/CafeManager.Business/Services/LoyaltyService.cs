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
        var kind=string.IsNullOrWhiteSpace(r.Kind)?"Giam":r.Kind.Trim();
        if(kind is not ("Giam" or "Tang"))throw new ArgumentException(L.T("Loại ưu đãi không hợp lệ."));
        if(r.Discount<0||r.Discount>100)throw new ArgumentException(L.T("Tỷ lệ giảm không hợp lệ."));
        var audience=string.IsNullOrWhiteSpace(r.Audience)?"ALL":r.Audience.Trim().ToUpperInvariant();
        var status=string.IsNullOrWhiteSpace(r.Status)?"DangChay":r.Status.Trim();
        if(status=="Nhap")status="DangChay";
        if(audience is not ("ALL" or "TIER" or "WALKIN")||status is not ("DangChay" or "TamDung"))
            throw new ArgumentException(L.T("Đối tượng/trạng thái ưu đãi không hợp lệ."));
        var tiers=(r.Tiers??[]).Select(x=>MaterialService.Required(x,5,L.T("Mã hạng")).ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        var items=(r.Items??[]).Select(x=>MaterialService.Required(x,5,L.T("Mã món")).ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if(audience!="TIER"&&tiers.Length>0)throw new ArgumentException(L.T("Chỉ ưu đãi theo hạng mới chọn hạng."));
        if(items.Length>500||tiers.Any(x=>!Regex.IsMatch(x,"^[A-Z0-9]{2,5}$"))||items.Any(x=>!Regex.IsMatch(x,"^[A-Z0-9]{5}$")))
            throw new ArgumentException(L.T("Hạng/món của ưu đãi không hợp lệ."));
        var gifts=(r.Gifts??[]).Select(g=>new PromotionGift(MaterialService.Required(g.Product,5,L.T("Mã món")).ToUpperInvariant(),g.Quantity))
            .GroupBy(g=>g.Product,StringComparer.Ordinal).Select(g=>new PromotionGift(g.Key,g.Sum(x=>x.Quantity))).ToArray();
        if(kind=="Giam")
        {
            if(gifts.Length>0)throw new ArgumentException(L.T("Ưu đãi giảm không kèm món tặng."));
        }
        else
        {
            if(items.Length>0)throw new ArgumentException(L.T("Ưu đãi tặng món không có phạm vi món giảm."));
            if(gifts.Length is <1 or >50||gifts.Any(g=>!Regex.IsMatch(g.Product,"^[A-Z0-9]{5}$")||g.Quantity is <1 or >999))
                throw new ArgumentException(L.T("Món tặng cần 1–50 dòng, số lượng 1–999."));
        }
        var start=NormalizeDay(r.StartDate)??throw new ArgumentException(L.T("Cần ngày bắt đầu."));
        DateOnly? end=null;
        if(r.EndDate is DateOnly endDay)
            end=new DateOnly((endDay.Month*100+endDay.Day)<(start.Month*100+start.Day)?2001:2000,endDay.Month,endDay.Day);
        return r with{Code=code,Name=MaterialService.Required(r.Name,100,L.T("Tên chương trình")),Audience=audience,Status=status,
            Kind=kind,Tiers=tiers,Items=items,Gifts=gifts,StartDate=start,EndDate=end,Discount=kind=="Tang"?0:r.Discount};
    }
    // Ngày/tháng lặp hằng năm: BD luôn năm 2000; KT năm 2001 khi vắt năm (xử lý ở trên).
    private static DateOnly? NormalizeDay(DateOnly? day) =>
        day is DateOnly value ? new DateOnly(2000,value.Month,value.Day) : null;
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
    // Lọc trạng thái hiệu lực (047): NULL/rỗng = tất cả; mã do SQL tính theo ngày server.
    public Task<List<PromotionRow>> PromotionsAsync(SessionContext s,string q,int skip,DateOnly? onDate=null,string? status=null,CancellationToken t=default)
    {
        var filter=string.IsNullOrWhiteSpace(status)?null:status.Trim();
        if(filter is not null&&filter is not ("DangChay" or "TamDung" or "ChuaToiKy" or "HetHan"))
            throw new ArgumentException(L.T("Trạng thái lọc không hợp lệ."));
        return Run(s,false,ct=>repository.PromotionsAsync(s.Identity,q,skip,onDate,filter,ct),t);
    }
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

