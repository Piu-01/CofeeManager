using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Requests.Loyalty;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;
public sealed class LoyaltyRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    private static SqlParameter P(string name,SqlDbType type,object? value,int size=0)=>AuthRepository.Parameter(name,type,value,size);
    private static SqlParameter Number(string name,decimal value)=>new(name,SqlDbType.Decimal){Precision=38,Scale=18,Value=value};
    private static SqlParameter Tiers(IReadOnlyList<TierInput> tiers)
    {
        var table=new DataTable();table.Columns.Add("Code",typeof(string));table.Columns.Add("MinimumPoints",typeof(long));table.Columns.Add("DiscountPercent",typeof(long));
        foreach(var tier in tiers.OrderBy(x=>x.Code,StringComparer.Ordinal))table.Rows.Add(tier.Code,tier.MinimumPoints,tier.Discount);
        return new("@Tiers",SqlDbType.Structured){TypeName="dbo.LoyaltyTierInput",Value=table};
    }
    public Task<List<PromotionRow>> PromotionsAsync(AccountIdentity a,string q,int skip,DateOnly? onDate,CancellationToken t)=>executor.ReadAsync("sp_PromotionList",[..CatalogRepository.Actor(a),P("@Search",SqlDbType.NVarChar,q,-1),P("@Skip",SqlDbType.Int,skip),P("@OnDate",SqlDbType.Date,onDate?.ToDateTime(TimeOnly.MinValue))],r=>new PromotionRow(r.GetString(0).TrimEnd(),r.GetString(1),DateOnly.FromDateTime(r.GetDateTime(2)),DateOnly.FromDateTime(r.GetDateTime(3)),r.GetInt32(4),(byte[])r[5]),t);
    public async Task<OperationResult> SavePromotionAsync(AccountIdentity a,PromotionRequest r,byte[]? revision,Guid id,bool delete,CancellationToken t)
    {
        var proc=delete?"sp_PromotionDelete":revision is null?"sp_PromotionCreate":"sp_PromotionUpdate";
        return(await executor.ReadAsync(proc,[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,r.Code,-1),P("@RequestId",SqlDbType.UniqueIdentifier,id),P("@ExpectedRevision",SqlDbType.Binary,revision,8),P("@Name",SqlDbType.NVarChar,r.Name,-1),P("@StartDate",SqlDbType.Date,r.StartDate.ToDateTime(TimeOnly.MinValue)),P("@EndDate",SqlDbType.Date,r.EndDate.ToDateTime(TimeOnly.MinValue)),P("@Discount",SqlDbType.BigInt,r.Discount)],Result,t,true)).Single();
    }
    public Task<List<TierRow>> PolicyAsync(AccountIdentity a,CancellationToken t)=>executor.ReadAsync("sp_LoyaltyPolicyGet",CatalogRepository.Actor(a),r=>new TierRow(r.GetString(0).TrimEnd(),r.GetInt32(1),r.GetInt32(2),(byte[])r[3]),t);
    public async Task<PolicyPreview> PolicyPreviewAsync(AccountIdentity a,IReadOnlyList<TierInput> tiers,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyPolicyPreview",[..CatalogRepository.Actor(a),Tiers(tiers)],r=>new PolicyPreview((byte[])r[0],(byte[])r[1],r.GetInt32(2)),t)).Single();
    public async Task<OperationResult> PolicySaveAsync(AccountIdentity a,IReadOnlyList<TierInput> tiers,PolicyPreview preview,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyPolicyReplace",[..CatalogRepository.Actor(a),Tiers(tiers),P("@ExpectedRevision",SqlDbType.Binary,preview.Revision,8),P("@PayloadHash",SqlDbType.Binary,preview.PayloadHash,32),P("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>new OperationResult(r.GetString(0).TrimEnd(),(byte[])r[1],r.GetInt32(2)),t,true)).Single();
    public async Task<PointPreview> PointPreviewAsync(AccountIdentity a,PointAdjustmentRequest r,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyAdjustPreview",[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,r.Code,-1),P("@Direction",SqlDbType.NVarChar,r.Direction,10),P("@Points",SqlDbType.BigInt,r.Points)],reader=>new PointPreview(reader.GetInt64(0),(byte[])reader[1],(byte[])reader[2],reader.GetString(3).TrimEnd()),t)).Single();
    public async Task<OperationResult> AdjustAsync(AccountIdentity a,PointAdjustmentRequest r,PointPreview preview,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_LoyaltyAdjust",[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,r.Code,-1),P("@Direction",SqlDbType.NVarChar,r.Direction,10),P("@Points",SqlDbType.BigInt,r.Points),P("@Reason",SqlDbType.NVarChar,r.Reason,-1),P("@ExpectedCustomerRevision",SqlDbType.Binary,preview.CustomerRevision,8),P("@ExpectedPolicyRevision",SqlDbType.Binary,preview.PolicyRevision,8),P("@RequestId",SqlDbType.UniqueIdentifier,id)],Result,t,true)).Single();
    public async Task<DiscountPreview> DiscountAsync(AccountIdentity a,DiscountPreviewRequest r,CancellationToken t)=>(await executor.ReadAsync("sp_DiscountPreview",[..CatalogRepository.Actor(a),Number("@Money",r.Money),Number("@Ship",r.Ship),P("@BusinessDate",SqlDbType.Date,r.BusinessDate.ToDateTime(TimeOnly.MinValue)),P("@Customer",SqlDbType.VarChar,r.Customer,-1),P("@Promotion",SqlDbType.VarChar,r.Promotion,-1)],reader=>new DiscountPreview(reader.GetString(0),reader.IsDBNull(1)?null:reader.GetString(1).TrimEnd(),reader.IsDBNull(2)?null:reader.GetString(2).TrimEnd(),reader.GetInt32(3),reader.GetDecimal(4),reader.GetDecimal(5),reader.GetDateTime(6),(byte[])reader[7]),t)).Single();
    public Task<List<PointHistory>> HistoryAsync(AccountIdentity a,string code,int skip,CancellationToken t)=>executor.ReadAsync("sp_LoyaltyHistory",[..CatalogRepository.Actor(a),P("@Code",SqlDbType.VarChar,code,-1),P("@Skip",SqlDbType.Int,skip)],r=>new PointHistory(r.GetInt32(0),r.GetDateTime(1),r.GetString(2),r.GetInt32(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5).TrimEnd()),t);
    public Task<OperationReceipt?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>new MaterialRepository(factory).ReceiptAsync(a,id,t);
    private static OperationResult Result(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.IsDBNull(1)?null:(byte[])r[1]);
}

