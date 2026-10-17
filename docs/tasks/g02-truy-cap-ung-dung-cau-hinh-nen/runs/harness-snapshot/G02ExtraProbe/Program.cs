using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using CafeManager.Business.Services;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

static class ExtraProbe
{
 static Type Helper=Assembly.Load("G02AcceptanceProbe").GetType("Program")!;
 static object? Call(string method,params object?[] args)=>Helper.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
 static Task<string> NewDb(bool demo=false,int version=3)=>(Task<string>)Call("NewDb",demo,null,version)!;
 static Task<SqlConnection> Owner(string db)=>(Task<SqlConnection>)Call("Owner",db)!;
 static Task Apply(string db,int v)=>(Task)Call("Apply",db,v)!;
 static List<object> Results=[];
 static void Need(bool value,string message){if(!value)throw new Exception(message);}
 static async Task Check(string id,string expected,Func<Task<string>> action){try{Results.Add(new{Id=id,Expected=expected,Actual=await action(),Status="PASS"});}catch(Exception e){Results.Add(new{Id=id,Expected=expected,Actual=e is SqlException s?$"SQL{s.Number}":e.Message,Status="FAIL"});}}
 static async Task<object?> Sql(SqlConnection c,string sql,params SqlParameter[] ps){await using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddRange(ps);return await cmd.ExecuteScalarAsync();}
 static async Task Reject(SqlConnection c,string sql,SqlParameter[] ps,int[]? numbers=null){try{await Sql(c,sql,ps);}catch(SqlException e)when(numbers==null||e.Errors.Cast<SqlError>().Any(x=>numbers.Contains(x.Number))){var state=await Sql(c,"DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)");Need((string?)state=="0:0","Transaction not idle after error");return;}throw new Exception("Expected rejection absent");}
 static async Task<int> Main(string[] args)
 {
  var root=args[0];var output=args[1];var db=args[2];Directory.CreateDirectory(output);Helper.GetField("Root",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,root);
  await using var owner=await Owner(db);await using var app=await Owner(db);await Sql(app,"EXECUTE AS USER=N'G02_Runtime'");
  var actor=Convert.ToInt32(await Sql(owner,"SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin'"));var stamp=(byte[])(await Sql(owner,"SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@id",new SqlParameter("@id",actor)))!;
  SqlParameter[] Actor()=>[new("@Actor",actor),new("@Stamp",SqlDbType.Binary,8){Value=stamp}];
  await Check("T08-K02.Supplier.maxplus1.SQL.confirm","Name101 rejected without truncation",async()=>{var p=Actor().Concat(new[]{new SqlParameter("@Name",SqlDbType.NVarChar,101){Value=new string('Z',101)}}).ToArray();await Reject(app,"EXEC dbo.sp_CatalogSupplierCreate @Actor=@Actor,@Stamp=@Stamp,@Code='NCC84',@Name=@Name,@Phone='01840000001'",p);return "Rejected";});
  await Check("T08-K02.ncc.length16.SQL.confirm","Phone16 rejected without truncation",async()=>{var p=Actor().Concat(new[]{new SqlParameter("@Phone",SqlDbType.VarChar,16){Value="0185000000000000"}}).ToArray();await Reject(app,"EXEC dbo.sp_CatalogSupplierCreate @Actor=@Actor,@Stamp=@Stamp,@Code='NCC85',@Name=N'Phone16',@Phone=@Phone",p);return "Rejected";});
  foreach(var kind in new[]{"ProductCategory","Supplier","PaymentMethod","DeliveryPartner","ExpenseCategory","Position","Shift"})
  {
   var extra=kind switch{"ProductCategory"=>",@RequestId='12345678-1111-1111-1111-123456789012'","Supplier"=>",@Phone='01860000001'","DeliveryPartner"=>",@Ratio=0","ExpenseCategory"=>",@ExpenseType=N'Chi'","Position"=>",@Allowance=0,@Factor=1","Shift"=>",@StartTime='22:00',@EndTime='06:00'",_=>""};
   foreach(var invalid in new[]{"empty","space","tab"})await Check($"T08-K02.{kind}.{invalid}.SQL.confirm","Invalid required name rejected, idle transaction",async()=>{var name=invalid switch{"empty"=>"","space"=>" ",_=>"\t"};await Reject(app,$"EXEC dbo.sp_Catalog{kind}Create @Actor=@Actor,@Stamp=@Stamp,@Code='XX886',@Name=@Name"+extra,[..Actor(),new("@Name",name)],[51501]);return "SQL51501; @@TRANCOUNT/XACT_STATE captured before SELECT are0:0";});
  }
  foreach(var code in new[]{"TC005","TC011","TC012"})foreach(var action in new[]{"type","code","delete"})await Check($"T08-K07.a.{code}.{action}.raw.confirm","Unused system row protected",async()=>{var sql=action switch{"type"=>$"UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Thu' WHERE MaDMTC='{code}'","code"=>$"UPDATE dbo.DANHMUC_THUCHI SET MaDMTC='TC876' WHERE MaDMTC='{code}'",_=>$"DELETE dbo.DANHMUC_THUCHI WHERE MaDMTC='{code}'"};await Reject(owner,sql,[]);return "Rejected, mapping remains and transaction idle";});
  foreach(var role in new[]{"ADMIN","CASH","STORE"})foreach(var kind in new[]{"ProductCategory","Supplier","DeliveryPartner","ExpenseCategory","Position","Shift"})await Check($"T08-K09.lookup.{role}.{kind}.confirm","Approved role and exact nonfinancial columns",async()=>{var id=Convert.ToInt32(await Sql(owner,"SELECT MaTK FROM dbo.TAIKHOAN WHERE MaVT=@role",new SqlParameter("@role",role)));var s=(byte[])(await Sql(owner,"SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@id",new SqlParameter("@id",id)))!;SqlParameter[] ps=[new("@Actor",id),new("@Stamp",SqlDbType.Binary,8){Value=s},new("@Kind",kind)];var allowed=role=="ADMIN"||kind=="ProductCategory"||(role=="CASH"&&kind=="DeliveryPartner")||(role=="STORE"&&kind=="Supplier");if(!allowed){await Reject(app,"EXEC dbo.sp_CatalogLookup @Actor,@Stamp,@Kind",ps,[51505]);return "Denied51505; transaction idle";}await using var cmd=app.CreateCommand();cmd.CommandText="EXEC dbo.sp_CatalogLookup @Actor,@Stamp,@Kind";cmd.Parameters.AddRange(ps);await using var reader=await cmd.ExecuteReaderAsync();var fields=Enumerable.Range(0,reader.FieldCount).Select(reader.GetName).ToArray();var expected=kind switch{"Supplier"=>new[]{"Code","Name","Phone"},"ExpenseCategory"=>["Code","Name","ExpenseType"],"Shift"=>["Code","Name","StartTime","EndTime"],_=>["Code","Name"]};Need(fields.SequenceEqual(expected),"Projection mismatch");return "Allowed fields="+string.Join(',',fields);});
  foreach(var variant in new[]{"TC011Thu","TC012Existing","PaymentNameCollision","UsernameCaseCollision"})
  {
   var target=await NewDb(variant=="UsernameCaseCollision",1);await using var c=await Owner(target);var v=3;
   if(variant=="UsernameCaseCollision"){await Sql(c,"ALTER TABLE dbo.TAIKHOAN DROP CONSTRAINT UQ_TK_Login; ALTER TABLE dbo.TAIKHOAN ALTER COLUMN TenDangNhap varchar(50) COLLATE Latin1_General_100_CS_AS NOT NULL; ALTER TABLE dbo.TAIKHOAN ADD CONSTRAINT UQ_TK_Login UNIQUE(TenDangNhap);UPDATE dbo.TAIKHOAN SET TenDangNhap='ADMIN' WHERE TenDangNhap='thungan'");v=2;}
   else{await Apply(target,2);await Sql(c,variant switch{"TC011Thu"=>"INSERT dbo.DANHMUC_THUCHI VALUES('TC011',N'Wrong type',N'Thu')","TC012Existing"=>"INSERT dbo.DANHMUC_THUCHI VALUES('TC012',N'Existing meaning',N'Chi')",_=>"INSERT dbo.PHUONGTHUCTT VALUES('PT003',N'Chuyển khoản ngân hàng')"});}
   await Check((v==2?"T06-K10.b":"T08-K06.c")+"."+variant,"Preflight rejects and leaves version/DDL absent",async()=>{var rejected=false;try{await Apply(target,v);}catch(SqlException){rejected=true;}Need(rejected,"Preflight accepted");Need(Convert.ToInt32(await Sql(c,"SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version=@v",new SqlParameter("@v",v)))==0,"Version committed");Need((await Sql(c,v==2?"SELECT OBJECT_ID('dbo.AppSetupState')":"SELECT OBJECT_ID('dbo.SystemExpenseCategory')"))is DBNull,"DDL committed");return $"Rejected; version/DDL absent; fixture {target}";});
  }
  var demo=await NewDb(true);await using var demoOwner=await Owner(demo);await Sql(demoOwner,"UPDATE dbo.DANHMUC_THUCHI SET TenDMTC=N'Custom salary name' WHERE MaDMTC='TC005'");
  await Check("T08-K06.b.rerun","Real runner returns AlreadyApplied without reset",async()=>{var info=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var a in new[]{"-NoProfile","-File",Path.Combine(root,"tools","database","Apply-Migration.ps1"),"-Database",demo,"-Version","3","-File",Path.Combine(root,"database","migrations","003_reference_catalogs.sql")})info.ArgumentList.Add(a);using var p=Process.Start(info)!;var stdout=await p.StandardOutput.ReadToEndAsync();var stderr=await p.StandardError.ReadToEndAsync();await p.WaitForExitAsync();File.WriteAllText(Path.Combine(output,"migration-rerun.txt"),stdout+stderr);Need(p.ExitCode==0&&stdout.Contains("AlreadyApplied"),"Rerun failed");Need((string?)await Sql(demoOwner,"SELECT TenDMTC FROM dbo.DANHMUC_THUCHI WHERE MaDMTC='TC005'")=="Custom salary name","Configuration reset");return "AlreadyApplied; custom system-category name retained";});
  await Check("T06-K09.a.DB-error","Connection failure distinct from invalid credentials",async()=>{var auth=new AuthService(new CafeManager.DataAccess.Repositories.AuthRepository(new ConnectionFactory($"Server=localhost,1;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1;ConnectRetryCount=0")),new PasswordService());try{await auth.LoginAsync(new LoginRequest("g02.admin","irrelevant"));}catch(DataAccessException e){Need(e.Error.Kind==CafeManager.Models.Errors.DataErrorKind.Connection,"Not connection error");return "DataAccessException Connection, no valid session";}throw new Exception("Failed connection unexpectedly authenticated");});
  await Check("T08-K02.ncc.truncation.actual","Observe persisted widths without exposing data",async()=>{var name=Convert.ToInt32(await Sql(owner,"SELECT LEN(TenNCC) FROM dbo.NHACUNGCAP WHERE MaNCC='NCC84'"));var phone=Convert.ToInt32(await Sql(owner,"SELECT LEN(SDT) FROM dbo.NHACUNGCAP WHERE MaNCC='NCC85'"));Need(name==100&&phone==15,"Expected truncation not reproduced");return "InputName101 persisted100; inputPhone16 persisted15 (evidence of preceding FAIL, not valid contract)";});
  File.WriteAllText(Path.Combine(output,"extra-results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));return Results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
 }
}
