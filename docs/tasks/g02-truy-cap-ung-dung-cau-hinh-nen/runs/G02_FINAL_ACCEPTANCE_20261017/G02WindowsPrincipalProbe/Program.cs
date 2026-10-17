using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using Microsoft.Data.SqlClient;

static class Probe
{
 static void Need(bool b,string message){if(!b)throw new InvalidOperationException(message);}
 static async Task Expect(SqlConnection c,string sql,int number,params SqlParameter[] ps){await using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddRange(ps);try{await cmd.ExecuteNonQueryAsync();}catch(SqlException e)when(e.Errors.Cast<SqlError>().Any(x=>x.Number==number)){return;}throw new InvalidOperationException("Expected SQL rejection absent: "+number);}
 static async Task<int> Main(string[] args)
 {
  var checks=new List<object>();var db=args[0];var role=args[1];var output=args[2];
  try{
   if(db=="--assembly-only"){
    var assembly=typeof(SignedCms).Assembly;var information=assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    Need(information?.Split('+')[0]=="10.0.13","Runtime did not load patched PKCS10.0.13");
    await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{Status="PASS",Id="ActualPatchedPkcsLoaded.WindowsForms",Path=assembly.Location,InformationalVersion=information,Runtime=Environment.Version.ToString(),FileSHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))},new JsonSerializerOptions{WriteIndented=true}));return 0;
   }
   Need(Regex.IsMatch(db,@"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"),"Isolated target required");Need(role is "runtime" or "setup","Expected role required");
   using var identity=WindowsIdentity.GetCurrent();Need(!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator),"Test identity must not be administrator");
   var pkcs=typeof(SignedCms).Assembly;var version=FileVersionInfo.GetVersionInfo(pkcs.Location).FileVersion??"";var packageVersion=pkcs.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;checks.Add(new{Id="PkcsLoadObservation",Path=pkcs.Location,FileVersion=version,InformationalVersion=packageVersion});Need(packageVersion?.Split('+')[0]=="10.0.13","Runtime did not load patched PKCS10.0.13");
   await using var c=new SqlConnection(new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false}.ConnectionString);await c.OpenAsync();
   await using(var cmd=c.CreateCommand()){cmd.CommandText="SELECT ORIGINAL_LOGIN(),SUSER_SNAME(),USER_NAME(),ISNULL(IS_ROLEMEMBER('CafeManagerSetup'),0),ISNULL(IS_ROLEMEMBER('CafeManagerApp'),0),ISNULL(IS_SRVROLEMEMBER('sysadmin'),0)";await using var r=await cmd.ExecuteReaderAsync();Need(await r.ReadAsync(),"Principal row absent");Need(string.Equals(r.GetString(0),identity.Name,StringComparison.OrdinalIgnoreCase)&&string.Equals(r.GetString(1),identity.Name,StringComparison.OrdinalIgnoreCase),"SQL identity is not the real Windows principal");Need(r.GetInt32(5)==0,"SQL test login is sysadmin");Need(r.GetInt32(3)==(role=="setup"?1:0)&&r.GetInt32(4)==(role=="runtime"?1:0),"Separated DB roles incorrect");checks.Add(new{Id="RealWindowsSqlIdentity",Status="PASS",WindowsIdentity=identity.Name,SqlLogin=r.GetString(0),DatabaseUser=r.GetString(2),SetupRole=r.GetInt32(3),RuntimeRole=r.GetInt32(4),SqlSysadmin=0});}
   if(role=="runtime"){
    await Expect(c,"EXEC dbo.sp_SetupCheckAccess",229);checks.Add(new{Id="RuntimeSetupCheckDenied",Status="PASS",SqlError=229});
    await Expect(c,"SELECT MatKhauHash FROM dbo.TAIKHOAN",229);checks.Add(new{Id="RuntimeRawCredentialsDenied",Status="PASS",SqlError=229});
    await Expect(c,"EXEC dbo.sp_SetupBootstrap @Mode='NewEmployee',@Username='must.not.commit',@NewHash=@hash,@TenNV=N'Forbidden'",229,new SqlParameter("@hash",new PasswordService().Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)))));checks.Add(new{Id="RuntimeBootstrapDenied",Status="PASS",SqlError=229});
   }else{
    await using(var cmd=c.CreateCommand()){cmd.CommandText="EXEC dbo.sp_SetupCheckAccess";Need(Convert.ToBoolean(await cmd.ExecuteScalarAsync()),"Setup access check failed");}
    await using(var cmd=c.CreateCommand()){cmd.CommandText="EXEC dbo.sp_AppReadState";await using var reader=await cmd.ExecuteReaderAsync();Need(await reader.ReadAsync()&&reader.GetString(0)=="Completed","Refusing write probe before Completed");}
    await Expect(c,"EXEC dbo.sp_SetupBootstrap @Mode='NewEmployee',@Username='must.not.repeat',@NewHash=@hash,@TenNV=N'Blocked second admin'",51401,new SqlParameter("@hash",new PasswordService().Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)))));checks.Add(new{Id="CompletedBootstrapBlockedForRealSetupIdentity",Status="PASS",SqlError=51401});
   }
   checks.Add(new{Id="ActualPatchedPkcsLoaded",Status="PASS",Path=pkcs.Location,FileVersion=version,InformationalVersion=pkcs.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion});
   await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{Status="PASS",Database=db,Role=role,Checks=checks},new JsonSerializerOptions{WriteIndented=true}));return 0;
  }catch(Exception e){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{Status="FAIL",Database=db,Role=role,ErrorType=e.GetType().Name,Diagnostic=e is InvalidOperationException?e.Message:null,SqlError=(e as SqlException)?.Number,Checks=checks},new JsonSerializerOptions{WriteIndented=true}));return 1;}
 }
}
