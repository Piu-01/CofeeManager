using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Tables;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;
public sealed class TableRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<AreaRow>> AreasAsync(AccountIdentity a,string search,int skip,CancellationToken t)=>executor.ReadAsync("sp_AreaList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],r=>new AreaRow(r.GetString(0).TrimEnd(),r.GetString(1),(byte[])r[2]),t);
    public Task<List<TableRow>> ListAsync(AccountIdentity a,string search,int skip,string? area,CancellationToken t)=>executor.ReadAsync("sp_TableList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip),AuthRepository.Parameter("@Area",SqlDbType.VarChar,area,-1)],Map,t);
    public async Task<TableRow?> SelectAsync(AccountIdentity a,string code,CancellationToken t)=>(await executor.ReadAsync("sp_TableSelectCurrent",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public async Task<OperationResult> WriteAsync(AccountIdentity a,bool area,string action,TableRequest request,byte[]? revision,Guid id,CancellationToken t)
    {
        var procedure=(area,action) switch {(true,"Create")=>"sp_AreaCreate",(true,"Update")=>"sp_AreaUpdate",(true,"Delete")=>"sp_AreaDelete",(false,"Create")=>"sp_TableCreate",(false,"Update")=>"sp_TableUpdate",(false,"Delete")=>"sp_TableDelete",_=>throw new ArgumentException("Thao tác không hợp lệ.")};
        return (await executor.ReadAsync(procedure,[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,request.Code,-1),AuthRepository.Parameter("@Name",SqlDbType.NVarChar,request.Name,-1),AuthRepository.Parameter("@Area",SqlDbType.VarChar,request.Area,-1),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,revision,8)],r=>new OperationResult(r.GetString(0).TrimEnd(),r.IsDBNull(1)?null:(byte[])r[1]),t,true)).Single();
    }
    public Task<OperationReceipt?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>new MaterialRepository(factory).ReceiptAsync(a,id,t);
    private static TableRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetString(1),r.GetString(2).TrimEnd(),r.GetString(3),r.IsDBNull(4)?null:r.GetString(4).TrimEnd(),(byte[])r[5],r.GetDateTime(6));
}
