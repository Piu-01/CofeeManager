using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
static class Probe {
 static readonly Type Helper=Assembly.Load("G02AcceptanceProbe").GetType("Program")!;
 static object? Call(string name,params object?[] a)=>Helper.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)!.Invoke(null,a);
 static Task<string> NewDb(bool demo=false,string? collation=null,int versions=3)=>(Task<string>)Call("NewDb",demo,collation,versions)!;
 static Task<SqlConnection> Owner(string db)=>(Task<SqlConnection>)Call("Owner",db)!;
 static ConnectionFactory Factory(string db,bool setup=false)=>(ConnectionFactory)Call("Factory",db,setup)!;
 static async Task<object?> Sql(SqlConnection c,string sql,params SqlParameter[] p){await using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddRange(p);return await cmd.ExecuteScalarAsync();}
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 static readonly List<object> Results=[];static readonly string Password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));static string Output="";
 static async Task Check(string id,Func<Task<string>> a){try{Results.Add(new{Id=id,Status="PASS",Actual=await a()});}catch(Exception e){Results.Add(new{Id=id,Status="FAIL",Actual=e is SqlException s?"SQL"+s.Number:e is DataAccessException d?"DAL"+d.Error.Number:e.Message});}File.WriteAllText(Path.Combine(Output,"results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));}
 static async Task Main(string[] args){Output=args[1];Directory.CreateDirectory(Output);Helper.GetField("Root",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)!.SetValue(null,args[0]);
  var invalidDb=await NewDb();await using(var owner=await Owner(invalidDb)){
   foreach(var code in Enumerable.Range(0,128).Concat(new[]{0x00A0,0x00E9,0x200B,0xFF21,0xFF3F,0xFF0D}).Where(code=>!(code is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-')))
    await Check("B07.Bootstrap.invalid.U"+code.ToString("X4"),async()=>{await using var c=await Owner(invalidDb);await Sql(c,"EXECUTE AS USER=N'G02_Setup'");await using var cmd=c.CreateCommand();cmd.CommandType=CommandType.StoredProcedure;cmd.CommandText="dbo.sp_SetupBootstrap";cmd.Parameters.AddWithValue("@Mode","NewEmployee");cmd.Parameters.Add(new("@Username",SqlDbType.NVarChar,-1){Value="b07"+(char)code+"key"});cmd.Parameters.AddWithValue("@NewHash",new PasswordService().Hash(Password));cmd.Parameters.AddWithValue("@TenNV","B07 validation");try{await cmd.ExecuteNonQueryAsync();throw new Exception("Invalid input accepted");}catch(SqlException e)when(e.Number==51404){}Need((string?)await Sql(owner,"SELECT CONCAT(State,':',(SELECT COUNT(*) FROM dbo.TAIKHOAN),':',(SELECT COUNT(*) FROM dbo.NHANVIEN)) FROM dbo.AppSetupState")=="Pending:0:0","Partial bootstrap write");Need((string?)await Sql(c,"DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)")=="0:0","Transaction left open");return "SQL51404; Pending/account0/NV0;transaction0:0; DB="+invalidDb;});
  }
  foreach(var collation in new[]{"Latin1_General_100_CI_AS","Latin1_General_100_CS_AS"}){
   foreach(var name in new[]{"_","-",".","Ab._-19",new string('_',50),new string('-',50)})await Check("B07.Bootstrap.valid."+collation+"."+(name.Length==50?name[0]+"50":name),async()=>{var db=await NewDb(collation:collation);var f=Factory(db,true);await new BootstrapService(new SetupRepository(f),new AuthRepository(f),new PasswordService()).CompleteAsync(new BootstrapRequest{Mode="NewEmployee",Username=name,Password=Password,Confirmation=Password,EmployeeName="B07 valid"});using var session=await new AuthService(new AuthRepository(Factory(db)),new PasswordService()).LoginAsync(new("  "+name.ToLowerInvariant()+"  ",Password));Need(session.Identity.Role=="ADMIN","No ADMIN login");await using var c=await Owner(db);Need((string?)await Sql(c,"SELECT TenDangNhap FROM dbo.TAIKHOAN")==name,"Username not exact");Need((string?)await Sql(c,"SELECT State FROM dbo.AppSetupState")=="Completed","Not Completed");return "Committed exact username and1ADMIN; case/outerspace login succeeds;DB="+db;});
   await Check("B07.Legacy.preflight."+collation,async()=>{var db=await NewDb(true,collation,1);await using var c=await Owner(db);await Sql(c,"UPDATE dbo.TAIKHOAN SET TenDangNhap='  Ab._-19  ',MatKhauHash=@hash,HoatDong=1 WHERE TenDangNhap='admin'",new SqlParameter("@hash",new PasswordService().Hash(Password)));await(Task)Call("Apply",db,2)!;await Sql(c,File.ReadAllText(Path.Combine(args[0],"database/tests/g02/security-fixtures.sql")));Need((string?)await Sql(c,"SELECT TenDangNhap FROM dbo.TAIKHOAN WHERE MaVT='ADMIN'")=="Ab._-19","Preflight failed trim/display preservation");var f=Factory(db,true);var before=(string?)await Sql(c,"SELECT MatKhauHash FROM dbo.TAIKHOAN WHERE MaVT='ADMIN'");await new BootstrapService(new SetupRepository(f),new AuthRepository(f),new PasswordService()).ConfirmLegacyAsync(new(" aB._-19 ",Password));Need((string?)await Sql(c,"SELECT MatKhauHash FROM dbo.TAIKHOAN WHERE MaVT='ADMIN'")==before,"Legacy changed credential");using var s=await new AuthService(new AuthRepository(Factory(db)),new PasswordService()).LoginAsync(new("aB._-19",Password));return "Preflight accepted/trimmed existing symbols; legacy confirmation kept hash; runtime login succeeded;DB="+db;});
  }
  Environment.ExitCode=Results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
 }
}
