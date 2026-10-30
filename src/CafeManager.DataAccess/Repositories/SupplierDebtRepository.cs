using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Finance;
namespace CafeManager.DataAccess.Repositories;
public sealed class SupplierDebtRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor=new(factory);
    public Task<List<SupplierDebtRow>> ListAsync(AccountIdentity a,string search,DateTime? asOf,int skip,CancellationToken t)=>executor.ReadAsync("sp_DebtList",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@AsOf",SqlDbType.Date,asOf?.Date),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip)],Map,t);
    public async Task<SupplierDebtRow?> GetAsync(AccountIdentity a,string code,CancellationToken t)=>(await executor.ReadAsync("sp_DebtGet",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],Map,t)).SingleOrDefault();
    public Task<List<InventoryLookup>> MethodsAsync(AccountIdentity a,CancellationToken t)=>executor.ReadAsync("sp_DebtMethods",CatalogRepository.Actor(a),r=>new InventoryLookup(r.GetString(0).TrimEnd(),r.GetString(1),null),t);
    public async Task<string> PayAsync(AccountIdentity a,SupplierDebtRequest r,CancellationToken t)=>(await executor.ReadAsync("sp_TraCongNo",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,r.RequestId),AuthRepository.Parameter("@Code",SqlDbType.VarChar,r.Code,-1),AuthRepository.Parameter("@Method",SqlDbType.VarChar,r.Method,-1),new SqlParameter("@Amount",SqlDbType.Decimal){Precision=38,Scale=18,Value=r.Amount},AuthRepository.Parameter("@Date",SqlDbType.DateTime2,r.Date)],row=>row.GetString(0).TrimEnd(),t,true)).Single();
    public async Task<string?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>(await executor.ReadAsync("sp_DebtReceipt",[..CatalogRepository.Actor(a),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id)],r=>r.GetString(0).TrimEnd(),t)).SingleOrDefault();
    private static SupplierDebtRow Map(SqlDataReader r)=>new(r.GetString(0).TrimEnd(),r.GetString(1).TrimEnd(),r.GetString(2).TrimEnd(),r.GetDateTime(3),r.GetDecimal(4),r.GetDecimal(5),r.GetDecimal(6),r.IsDBNull(7)?null:r.GetDateTime(7),r.GetString(8),r.IsDBNull(9)?[]:JsonSerializer.Deserialize<List<SupplierDebtPayment>>(r.GetString(9))??[],r.GetInt32(10));
}
