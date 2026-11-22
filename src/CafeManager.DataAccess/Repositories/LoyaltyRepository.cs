using System.Data;
using System.Text.Json;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Requests.Loyalty;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;
public sealed class LoyaltyRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    private readonly SalesRepository reader=new(factory);
    private static SqlParameter P(string name,SqlDbType type,object? value,int size=0)=>AuthRepository.Parameter(name,type,value,size);
    private static SqlParameter Number(string name,decimal value)=>new(name,SqlDbType.Decimal){Precision=38,Scale=18,Value=value};
    private static SqlParameter Tiers(IReadOnlyList<TierInput> tiers)
    {
        var table=new DataTable();table.Columns.Add("Code",typeof(string));table.Columns.Add("MinimumPoints",typeof(long));table.Columns.Add("DiscountPercent",typeof(long));
        foreach(var tier in tiers.OrderBy(x=>x.Code,StringComparer.Ordinal))table.Rows.Add(tier.Code,tier.MinimumPoints,tier.Discount);
        return new("@Tiers",SqlDbType.Structured){TypeName="dbo.LoyaltyTierInput",Value=table};
    }
    private static SqlParameter PreviewLines(IReadOnlyList<PreviewLine> lines)
    {
        var table=new DataTable();table.Columns.Add("MaSP",typeof(string));table.Columns.Add("SoLuong",typeof(int));
        foreach(var line in lines)table.Rows.Add(line.Product,line.Quantity);
        return new("@Lines",SqlDbType.Structured){TypeName="dbo.PreviewLines",Value=table};
    }
    private static object? Day(DateOnly? date)=>date?.ToDateTime(TimeOnly.MinValue);
    private static AppliedPromotion Applied(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetString(1),r.GetInt32(2),r.GetDecimal(3),r.GetDecimal(4));
    private static PromotionRow Promotion(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetString(1),
        DateOnly.FromDateTime(r.GetDateTime(2)),r.IsDBNull(3)?null:DateOnly.FromDateTime(r.GetDateTime(3)),r.GetInt32(4),
        r.GetString(5),r.GetString(6),r.GetString(7),r.GetInt32(8),(byte[])r[9],
        r.GetString(10),r.GetInt32(11),r.GetInt32(12));
    public Task<List<PromotionRow>> PromotionsAsync(AccountIdentity a,string q,int skip,DateOnly? onDate,CancellationToken t)=>executor.ReadAsync("sp_PromotionList",[..CatalogRepository.Actor(a),P("@Search",SqlDbType.NVarChar,q,-1),P("@Skip",SqlDbType.Int,skip),P("@OnDate",SqlDbType.Date,Day(onDate))],Promotion,t);
    public Task<string> AllocateCodeAsync(CancellationToken t)=>CatalogRepository.AllocateAsync(executor,"KHUYENMAI",t);
    public async Task<OperationResult> SavePromotionAsync(AccountIdentity a,PromotionRequest r,byte[]? revision,Guid id,bool delete,CancellationToken t)
    {
        var proc=delete?"sp_PromotionDelete":revision is null?"sp_PromotionCreate":"sp_PromotionUpdate";
        var parameters=new List<SqlParameter>([..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,r.Code,-1),
            P("@RequestId",SqlDbType.UniqueIdentifier,id),P("@ExpectedRevision",SqlDbType.Binary,revision,8)]);
        if(!delete)parameters.AddRange([P("@Name",SqlDbType.NVarChar,r.Name,-1),
            P("@StartDate",SqlDbType.Date,Day(r.StartDate)),P("@EndDate",SqlDbType.Date,Day(r.EndDate)),
            P("@Discount",SqlDbType.BigInt,r.Discount),P("@Audience",SqlDbType.VarChar,r.Audience,10),
            P("@Status",SqlDbType.VarChar,r.Status,10),
            P("@TiersJson",SqlDbType.NVarChar,JsonSerializer.Serialize(r.Tiers),-1),
            P("@ItemsJson",SqlDbType.NVarChar,JsonSerializer.Serialize(r.Items),-1),
            P("@Kind",SqlDbType.VarChar,r.Kind,10),
            P("@GiftJson",SqlDbType.NVarChar,JsonSerializer.Serialize(r.Gifts??[]),-1)]);
        return(await executor.ReadAsync(proc,parameters.ToArray(),Result,t,true)).Single();
    }
    public async Task<OperationResult> SetPromotionStateAsync(AccountIdentity a,string code,byte[] revision,Guid id,bool resume,CancellationToken t)=>
        (await executor.ReadAsync("sp_PromotionWrite",[..CatalogRepository.Actor(a),P("@Action",SqlDbType.VarChar,resume?"Resume":"Pause",10),
            P("@Code",SqlDbType.VarChar,code,-1),P("@RequestId",SqlDbType.UniqueIdentifier,id),
            P("@ExpectedRevision",SqlDbType.Binary,revision,8)],Result,t,true)).Single();
    public async Task<PromotionDetail?> PromotionDetailAsync(AccountIdentity a,string code,CancellationToken t)
    {
        return await reader.Call<PromotionDetail?>("sp_PromotionGet",[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,code,-1)],
            async(r,ct)=>
            {
                if(!await r.ReadAsync(ct))return null;
                var promotion=Promotion(r);
                await r.NextResultAsync(ct);
                var tiers=new List<string>();while(await r.ReadAsync(ct))tiers.Add(r.GetString(0).TrimEnd());
                await r.NextResultAsync(ct);
                var items=new List<PromotionItem>();while(await r.ReadAsync(ct))items.Add(new(r.GetString(0).TrimEnd(),r.GetString(1),r.GetDecimal(2),r.GetString(3)));
                var gifts=new List<PromotionGiftRow>();
                if(await r.NextResultAsync(ct))while(await r.ReadAsync(ct))gifts.Add(new(r.GetString(0).TrimEnd(),r.GetString(1),r.GetDecimal(2),r.GetString(3),r.GetInt32(4)));
                return new PromotionDetail(promotion,tiers,items,gifts);
            },t);
    }
    public Task<List<TierRow>> PolicyAsync(AccountIdentity a,CancellationToken t)=>executor.ReadAsync("sp_LoyaltyPolicyGet",CatalogRepository.Actor(a),r=>new TierRow(r.GetString(0).TrimEnd(),r.GetInt32(1),r.GetInt32(2),(byte[])r[3]),t);
    public async Task<PolicyPreview> PolicyPreviewAsync(AccountIdentity a,IReadOnlyList<TierInput> tiers,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyPolicyPreview",[..CatalogRepository.Actor(a),Tiers(tiers)],r=>new PolicyPreview((byte[])r[0],(byte[])r[1],r.GetInt32(2)),t)).Single();
    public async Task<OperationResult> PolicySaveAsync(AccountIdentity a,IReadOnlyList<TierInput> tiers,PolicyPreview preview,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyPolicyReplace",[..CatalogRepository.Actor(a),Tiers(tiers),P("@ExpectedRevision",SqlDbType.Binary,preview.Revision,8),P("@PayloadHash",SqlDbType.Binary,preview.PayloadHash,32),P("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>new OperationResult(r.GetString(0).TrimEnd(),(byte[])r[1],r.GetInt32(2)),t,true)).Single();
    public async Task<DiscountPreview> DiscountAsync(AccountIdentity a,DiscountPreviewRequest r,CancellationToken t)
    {
        return await reader.Call("sp_DiscountPreview",[..CatalogRepository.Actor(a),Number("@Money",r.Money),Number("@Ship",r.Ship),
            P("@BusinessDate",SqlDbType.Date,r.BusinessDate.ToDateTime(TimeOnly.MinValue)),P("@Customer",SqlDbType.VarChar,r.Customer,-1),
            PreviewLines(r.Lines)],async(store,ct)=>
            {
                if(!await store.ReadAsync(ct))throw new InvalidDataException(L.T("SQL không trả tạm tính."));
                var source=store.GetString(0);var tier=store.IsDBNull(1)?null:store.GetString(1).TrimEnd();
                var percent=store.GetInt32(3);var tierPercent=store.GetInt32(4);var promotionPercent=store.GetInt32(5);
                var tierDiscount=store.GetDecimal(6);var promotionDiscount=store.GetDecimal(7);var count=store.GetInt32(8);
                var discount=store.GetDecimal(9);var payable=store.GetDecimal(10);var readAt=store.GetDateTime(11);
                var revision=(byte[])store[12];var giftCount=store.IsDBNull(13)?0:store.GetInt32(13);
                var promotions=new List<AppliedPromotion>();
                if(await store.NextResultAsync(ct))while(await store.ReadAsync(ct))promotions.Add(Applied(store));
                var gifts=new List<PreviewGift>();
                if(await store.NextResultAsync(ct))while(await store.ReadAsync(ct))gifts.Add(new(store.GetString(0).TrimEnd(),store.GetString(1),store.GetString(2).TrimEnd(),store.GetString(3),store.GetInt32(4)));
                return new DiscountPreview(source,tier,percent,tierPercent,promotionPercent,tierDiscount,promotionDiscount,
                    count,discount,payable,readAt,revision,promotions,giftCount,gifts);
            },t);
    }
    public Task<List<PointHistory>> HistoryAsync(AccountIdentity a,string code,int skip,CancellationToken t)=>executor.ReadAsync("sp_LoyaltyHistory",[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,code,-1),P("@Skip",SqlDbType.Int,skip)],r=>new PointHistory(r.GetInt32(0),r.GetDateTime(1),r.GetString(2),r.GetInt32(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5).TrimEnd()),t);
    public Task<OperationReceipt?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>new MaterialRepository(factory).ReceiptAsync(a,id,t);
    private static OperationResult Result(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.IsDBNull(1)?null:(byte[])r[1]);
}
