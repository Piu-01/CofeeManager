using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Dtos.Inventory;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;
public sealed class CustomerRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<CustomerRow>> ListAsync(AccountIdentity a,string q,int skip,CancellationToken t)=>executor.ReadAsync("sp_CustomerList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,q,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],Map,t);
    public async Task<CustomerRow?> GetAsync(AccountIdentity a,string code,CancellationToken t)=>(await executor.ReadAsync("sp_CustomerGet",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public Task<List<CustomerOrder>> OrdersAsync(AccountIdentity a,string code,int skip,CancellationToken t)=>executor.ReadAsync("sp_CustomerOrders",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],r=>new CustomerOrder(r.GetString(0).TrimEnd(),r.GetDateTime(1),r.GetString(2)),t);
    public async Task<OperationResult> WriteAsync(AccountIdentity a,string action,Guid id,string? code,byte[]? revision,string? name,string? phone,int? points,string? source,CancellationToken t)
    {
        var procedure=action switch{"Create"=>"sp_CustomerCreate","Update"=>"sp_CustomerUpdate","Delete"=>"sp_CustomerDelete","Open"=>"sp_CustomerOpen",_=>throw new ArgumentException("Thao tác không hợp lệ.")};
        return(await executor.ReadAsync(procedure,[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,revision,8),AuthRepository.Parameter("@Name",SqlDbType.NVarChar,name,-1),AuthRepository.Parameter("@Phone",SqlDbType.VarChar,phone,-1),AuthRepository.Parameter("@Points",SqlDbType.BigInt,points),AuthRepository.Parameter("@Source",SqlDbType.NVarChar,source,-1)],r=>new OperationResult(r.GetString(0).TrimEnd(),r.IsDBNull(1)?null:(byte[])r[1]),t,true)).Single();
    }
    public Task<OperationReceipt?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>new MaterialRepository(factory).ReceiptAsync(a,id,t);
    private static CustomerRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),r.GetInt32(3),r.GetString(4).TrimEnd(),r.GetInt32(5),(byte[])r[6],r.GetDateTime(7));
}
