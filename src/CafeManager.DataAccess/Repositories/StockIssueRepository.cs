using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
namespace CafeManager.DataAccess.Repositories;
public sealed class StockIssueRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<StockIssueRow>> ListAsync(AccountIdentity a,string search,int skip,CancellationToken t)=>executor.ReadAsync("sp_StockIssueList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],Map,t);
    public async Task<StockIssueRow?> GetAsync(AccountIdentity a,string code,CancellationToken t)=>(await executor.ReadAsync("sp_StockIssueGet",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public async Task<string> CreateAsync(AccountIdentity a,StockIssueRequest r,CancellationToken t)
    {
        using var lines=StockIssueTableBuilder.Build(r.Lines);
        return (await executor.ReadAsync("sp_LapPhieuXuat",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,r.RequestId),AuthRepository.Parameter("@Date",SqlDbType.DateTime2,r.Date),AuthRepository.Parameter("@Kind",SqlDbType.NVarChar,r.Kind,-1),AuthRepository.Parameter("@Reason",SqlDbType.NVarChar,r.Reason,-1),new SqlParameter("@Lines",SqlDbType.Structured){TypeName="dbo.StockIssueLinesInput",Value=lines}],row=>row.GetString(0).TrimEnd(),t,true)).Single();
    }
    public async Task<string?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_StockIssueReceipt",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>r.GetString(0).TrimEnd(),t)).SingleOrDefault();
    private static StockIssueRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetDateTime(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetString(4),(byte[])r[5],JsonSerializer.Deserialize<List<StockIssueDetailLine>>(r.GetString(6))??[],r.GetInt32(7));
}
