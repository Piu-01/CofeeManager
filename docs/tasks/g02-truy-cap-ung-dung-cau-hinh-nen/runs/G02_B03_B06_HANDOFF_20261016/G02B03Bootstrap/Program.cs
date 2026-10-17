using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Services;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
static class Probe
{
 static readonly Type Helper=Assembly.Load("G02AcceptanceProbe").GetType("Program")!;
 static object? Call(string name,params object?[] args)=>Helper.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
 static Task<string> NewDb(string? collation=null)=>(Task<string>)Call("NewDb",false,collation,3)!;
 static Task<SqlConnection> Owner(string db)=>(Task<SqlConnection>)Call("Owner",db)!;
 static ConnectionFactory Factory(string db,bool setup=false)=>(ConnectionFactory)Call("Factory",db,setup)!;
 static async Task<SqlConnection> Setup(string db){var c=await Owner(db);await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER=N'G02_Setup'";await cmd.ExecuteNonQueryAsync();return c;}
 static string Password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));static List<object> Results=[];
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 static async Task Check(string id,Func<Task<string>> action){try{Results.Add(new{Id=id,Expected="Validate full original input before writes; durable atomic Completed only for valid canonical ASCII",Actual=await action(),Status="PASS"});}catch(Exception e){Results.Add(new{Id=id,Expected="Reject invalid input / persist exact valid input",Actual=e is SqlException s?$"SQL{s.Number}":e.Message,Status="FAIL"});}}
 static async Task<object?> Scalar(SqlConnection c,string sql){await using var cmd=c.CreateCommand();cmd.CommandText=sql;return await cmd.ExecuteScalarAsync();}
 static SqlCommand Bootstrap(SqlConnection c,string? username){var cmd=c.CreateCommand();cmd.CommandText="dbo.sp_SetupBootstrap";cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.AddWithValue("@Mode","NewEmployee");cmd.Parameters.Add(new("@Username",SqlDbType.NVarChar,-1){Value=(object?)username??DBNull.Value});cmd.Parameters.AddWithValue("@NewHash",new PasswordService().Hash(Password));cmd.Parameters.AddWithValue("@TenNV","Boundary fixture");return cmd;}
 static async Task Main(string[] args){Directory.CreateDirectory(args[1]);Helper.GetField("Root",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,args[0]);var db=await NewDb();await using var owner=await Owner(db);await using var setup=await Setup(db);const string snapshot="SELECT CONCAT(State,':',(SELECT COUNT(*) FROM dbo.TAIKHOAN),':',(SELECT COUNT(*) FROM dbo.NHANVIEN),':',(SELECT COUNT(*) FROM dbo.CHUCVU)) FROM dbo.AppSetupState";var before=await Scalar(owner,snapshot);
  foreach(var (label,value) in new(string,string?)[]{("NULL",null),("Empty",""),("Space"," "),("OuterLeft"," a"),("OuterRight","a "),("InnerSpace","a b"),("Tab","a\t"),("Slash","a/b"),("Vietnamese","tên"),("FullWidth","Ａ"),("Emoji","a🙂"),("Length51",new string('a',51)),("Length1001",new string('a',1001))})await Check("B03.Bootstrap.SQL."+label,async()=>{await using var cmd=Bootstrap(setup,value);try{await cmd.ExecuteNonQueryAsync();throw new Exception("Invalid bootstrap accepted");}catch(SqlException e)when(e.Number==51404){}Need(Equals(before,await Scalar(owner,snapshot)),"Invalid input changed state/NV/account/CV");Need((string?)await Scalar(setup,"DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)")=="0:0","Transaction remained");return "SQL51404, unchanged Pending/NV/account/CV, transaction0:0; fixture="+db;});
  foreach(var (n,collation) in new[]{(1,(string?)null),(50,(string?)null),(50,"Latin1_General_100_CI_AS"),(50,"Latin1_General_100_CS_AS")})await Check($"B03.Bootstrap.Valid{n}.{collation??"default"}",async()=>{var target=await NewDb(collation);await using var c=await Setup(target);await using(var cmd=Bootstrap(c,new string('A',n)))await cmd.ExecuteNonQueryAsync();await using var read=await Owner(target);Need(Convert.ToInt32(await Scalar(read,"SELECT LEN(TenDangNhap) FROM dbo.TAIKHOAN"))==n,"Username truncated");using var session=await new AuthService(new AuthRepository(Factory(target)),new PasswordService()).LoginAsync(new LoginRequest("  "+new string('a',n)+"  ",Password));Need(session.Identity.Role=="ADMIN","Login failed after valid bootstrap");Need((string?)await Scalar(read,"SELECT State FROM dbo.AppSetupState")=="Completed","Not Completed");return "Exact ASCII length, Completed, login case/outerSpace normalization valid; fixture="+target;});
  File.WriteAllText(Path.Combine(args[1],"results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));Environment.ExitCode=Results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
 }
}
