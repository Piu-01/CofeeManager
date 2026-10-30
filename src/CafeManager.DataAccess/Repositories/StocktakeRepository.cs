using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess.Repositories;
public sealed class StocktakeRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<StocktakeRow>> ListAsync(AccountIdentity a,string search,int skip,CancellationToken t)=>executor.ReadAsync("sp_StocktakeList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],Map,t);
    public async Task<StocktakeRow?> GetAsync(AccountIdentity a,string code,CancellationToken t)=>(await executor.ReadAsync("sp_StocktakeGet",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public async Task<string> WriteAsync(AccountIdentity a,StocktakeRequest r,CancellationToken t)
    {
        using var scope=StocktakeTableBuilder.Scope(r.Scope);using var counts=StocktakeTableBuilder.Counts(r.Counts);
        List<SqlParameter> p=[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,r.RequestId)];
        if(r.Operation=="StocktakeCreate")p.Add(new("@Scope",SqlDbType.Structured){TypeName="dbo.StocktakeScope",Value=scope});
        else{p.Add(AuthRepository.Parameter("@Code",SqlDbType.VarChar,r.Code,-1));p.Add(AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,r.Revision,8));if(r.Operation=="StocktakeSave")p.Add(new("@Counts",SqlDbType.Structured){TypeName="dbo.StocktakeCounts",Value=counts});}
        var procedure=r.Operation switch{"StocktakeCreate"=>"sp_StocktakeCreate","StocktakeSave"=>"sp_StocktakeSaveCounts","StocktakeCommit"=>"sp_ChotKiemKe",_=>throw new ArgumentException("Thao tác kiểm kê sai.")};
        return(await executor.ReadAsync(procedure,p.ToArray(),row=>row.GetString(0).TrimEnd(),t,true)).Single();
    }
    public async Task<string?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_StocktakeReceipt",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>r.GetString(0).TrimEnd(),t)).SingleOrDefault();
    private static StocktakeRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetDateTime(1),r.GetBoolean(2),r.IsDBNull(3)?null:r.GetDateTime(3),r.GetString(4),(byte[])r[5],r.IsDBNull(6)?[]:JsonSerializer.Deserialize<List<StocktakeLine>>(r.GetString(6))??[],r.GetInt32(7));
}
