using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Products;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;
public sealed class ProductRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    public Task<List<ProductRow>> ListAsync(AccountIdentity a,string search,int skip,string? category,bool selling,CancellationToken t) => executor.ReadAsync("sp_ProductList",[.. CatalogRepository.Actor(a),AuthRepository.Parameter("@Search",SqlDbType.NVarChar,search,-1),AuthRepository.Parameter("@Skip",SqlDbType.Int,skip),AuthRepository.Parameter("@Category",SqlDbType.VarChar,category,-1),AuthRepository.Parameter("@OnlySelling",SqlDbType.Bit,selling)],r=>new ProductRow(r.GetString(0).TrimEnd(),r.GetString(1),r.GetString(2).TrimEnd(),r.GetDecimal(3),r.GetString(4),(byte[])r[5]),t);
    public Task<List<RecipeLine>> RecipeAsync(AccountIdentity a,string code,CancellationToken t) => executor.ReadAsync("sp_RecipeList",[.. CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,code,-1)],r=>new RecipeLine(r.GetString(0).TrimEnd(),r.GetString(1),r.GetString(2),r.GetDecimal(3)),t);
    public async Task<List<MaterialRow>> MaterialsAsync(AccountIdentity actor,CancellationToken token)
    {
        var result=new List<MaterialRow>();var material=new MaterialRepository(factory);
        for(var skip=0;;skip+=50){var page=await material.ListAsync(actor,"",skip,token);result.AddRange(page);if(page.Count<50)return result;}
    }
    public Task<List<NameLookup>> CategoriesAsync(AccountIdentity a,CancellationToken t) => new CatalogRepository(factory).NamesAsync(a,CatalogKind.ProductCategory,t);
    public async Task<OperationResult> WriteAsync(AccountIdentity a,string action,ProductRequest request,byte[]? revision,Guid id,CancellationToken t)
    {
        var procedure=action switch { "Create"=>"sp_ProductCreate","Update"=>"sp_ProductUpdate","Delete"=>"sp_ProductDelete","ReplaceRecipe"=>"sp_ProductReplaceRecipe",_=>throw new ArgumentException("Thao tác không hợp lệ.") };
        var table=new DataTable(); table.Columns.Add("MaNVL",typeof(string)); table.Columns.Add("Quantity",typeof(decimal));
        foreach(var line in request.Recipe.OrderBy(x=>x.Code,StringComparer.Ordinal)) table.Rows.Add(line.Code,line.Quantity);
        return (await executor.ReadAsync(procedure,[.. CatalogRepository.Actor(a),AuthRepository.Parameter("@Code",SqlDbType.VarChar,request.Code,-1),AuthRepository.Parameter("@Name",SqlDbType.NVarChar,request.Name,-1),AuthRepository.Parameter("@Category",SqlDbType.VarChar,request.Category,-1),new SqlParameter("@Price",SqlDbType.Decimal){Precision=38,Scale=18,Value=request.Price},AuthRepository.Parameter("@Status",SqlDbType.NVarChar,request.Status,-1),AuthRepository.Parameter("@ExpectedRevision",SqlDbType.Binary,revision,8),AuthRepository.Parameter("@RequestId",SqlDbType.UniqueIdentifier,id),new SqlParameter("@Recipe",SqlDbType.Structured){TypeName="dbo.RecipeLineInput",Value=table}],r=>new OperationResult(r.GetString(0).TrimEnd(),r.IsDBNull(1)?null:(byte[])r[1]),t,true)).Single();
    }
    public Task<OperationReceipt?> ReceiptAsync(AccountIdentity a,Guid id,CancellationToken t)=>new MaterialRepository(factory).ReceiptAsync(a,id,t);
}

