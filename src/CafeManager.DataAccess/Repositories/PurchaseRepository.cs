using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess.Repositories;
public sealed class PurchaseRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<InventoryLookup>> LookupsAsync(AccountIdentity actor,string kind,CancellationToken t)=>executor.ReadAsync("sp_PurchaseLookups",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@Kind",SqlDbType.VarChar,kind,10)],r=>new InventoryLookup(r.GetString(0).TrimEnd(),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2)),t);
    public Task<List<MaterialPurchaseLookup>> MaterialsAsync(AccountIdentity actor,CancellationToken t)=>executor.ReadAsync("sp_PurchaseMaterialLookup",CatalogRepository.Actor(actor),r=>new MaterialPurchaseLookup(r.GetString(0).TrimEnd(),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),r.GetDecimal(3),r.IsDBNull(4)?null:r.GetDecimal(4)),t);
    public Task<List<PurchaseRow>> ListAsync(AccountIdentity actor,string search,int skip,string? status,CancellationToken t)=>executor.ReadAsync("sp_PurchaseList",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip),AuthRepository.Parameter("@Status",SqlDbType.VarChar,status,10)],Map,t);
    public async Task<PurchaseRow?> GetAsync(AccountIdentity actor,string code,CancellationToken t)=>(await executor.ReadAsync("sp_PurchaseGet",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public async Task<decimal> PreviewAsync(AccountIdentity actor,IReadOnlyList<PurchaseLine> lines,CancellationToken t)
    {
        using var table=PurchaseTableBuilder.Build(lines);
        return (await executor.ReadAsync("sp_PurchasePreview",[..CatalogRepository.Actor(actor),new SqlParameter("@Lines",SqlDbType.Structured){TypeName="dbo.PurchaseLinesInput",Value=table}],r=>r.GetDecimal(0),t)).Single();
    }
    public async Task<string> CreateAsync(AccountIdentity actor,PurchaseRequest request,CancellationToken t)
    {
        using var table=PurchaseTableBuilder.Build(request.Lines);
        return (await executor.ReadAsync("sp_TaoPhieuNhap",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,request.RequestId),AuthRepository.Parameter("@Supplier",SqlDbType.VarChar,request.Supplier,-1),AuthRepository.Parameter("@Date",SqlDbType.DateTime2,request.Date),AuthRepository.Parameter("@Due",SqlDbType.Date,request.Due),Money("@Immediate",request.Immediate),Money("@ExpectedTotal",request.ExpectedTotal),new SqlParameter("@Lines",SqlDbType.Structured){TypeName="dbo.PurchaseLinesInput",Value=table}],r=>r.GetString(0).TrimEnd(),t,true)).Single();
    }
    public async Task<string> SaveCountAsync(AccountIdentity actor,PurchaseCountRequest request,CancellationToken t)
    {
        using var table=PurchaseTableBuilder.Receive(request.Lines);
        return (await executor.ReadAsync("sp_LuuKiemNhan",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,request.RequestId),AuthRepository.Parameter("@Code",SqlDbType.VarChar,request.Code,-1),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,request.Revision,8),new SqlParameter("@Lines",SqlDbType.Structured){TypeName="dbo.PurchaseReceiveInput",Value=table}],r=>r.GetString(0).TrimEnd(),t,true)).Single();
    }
    public async Task<string> PostAsync(AccountIdentity actor,PurchasePostRequest request,CancellationToken t)
        =>(await executor.ReadAsync("sp_ChotNhanHang",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,request.RequestId),AuthRepository.Parameter("@Code",SqlDbType.VarChar,request.Code,-1),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,request.Revision,8),Money("@Immediate",request.Immediate)],r=>r.GetString(0).TrimEnd(),t,true)).Single();
    public async Task<string> CancelAsync(AccountIdentity actor,PurchaseCancelRequest request,CancellationToken t)
        =>(await executor.ReadAsync("sp_HuyPhieuNhap",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,request.RequestId),AuthRepository.Parameter("@Code",SqlDbType.VarChar,request.Code,-1),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,request.Revision,8),AuthRepository.Parameter("@Reason",SqlDbType.NVarChar,request.Reason,-1)],r=>r.GetString(0).TrimEnd(),t,true)).Single();
    public async Task<string?> ReceiptAsync(AccountIdentity actor,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_PurchaseReceipt",[..CatalogRepository.Actor(actor),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>r.GetString(0).TrimEnd(),t)).SingleOrDefault();
    private static SqlParameter Money(string name,decimal value)=>new(name,SqlDbType.Decimal){Precision=38,Scale=18,Value=value};
    private static string? Text(SqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetString(i).TrimEnd();
    private static PurchaseRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetDateTime(1),r.GetString(2).TrimEnd(),r.GetDecimal(3),r.GetString(4),r.IsDBNull(5)?null:r.GetDecimal(5),Text(r,6),Text(r,7),r.IsDBNull(8)?null:r.GetDecimal(8),(byte[])r[9],
        r.GetString(10),r.GetDecimal(11),r.IsDBNull(12)?null:r.GetDateTime(12),r.IsDBNull(13)?null:r.GetDateTime(13),Text(r,14),Text(r,15),
        JsonSerializer.Deserialize<List<PurchaseDetailLine>>(r.GetString(16))??[],r.GetInt32(17));
}
