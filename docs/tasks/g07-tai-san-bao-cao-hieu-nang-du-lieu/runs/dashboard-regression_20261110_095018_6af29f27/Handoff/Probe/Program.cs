using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
if(args.Length!=1)throw new ArgumentException("Evidence output required");
var connection=DevelopmentConnection.Read();var target=new SqlConnectionStringBuilder(connection);if(target.DataSource!="localhost"||target.InitialCatalog!="CoffeeManager_Development")throw new InvalidOperationException("Wrong target");
var repository=new AuthRepository(new ConnectionFactory(connection));if((await repository.ReadStateAsync()).SchemaVersion!=28)throw new InvalidOperationException("Not schema028");
Environment.SetEnvironmentVariable("CAFEMANAGER_USE_DEVELOPMENT","1");Environment.SetEnvironmentVariable("CAFEMANAGER_DEV_QUICK_LOGIN","1");
var helper=typeof(LoginForm).Assembly.GetType("CafeManager.WinForms.Configuration.DevelopmentQuickLogin")!;var config=helper.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[connection]);var credential=config is null?null:(LoginRequest?)helper.GetMethod("Get",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(config,["ADMIN"]);
if(credential is null)throw new InvalidOperationException("Existing ADMIN quick-login credential unavailable");
var passwords=new PasswordService();var account=await repository.ReadCredentialAsync(PasswordService.Username(credential.Username).ToLowerInvariant());if(account is null||passwords.Verify(account.Hash,credential.Password)!=PasswordVerificationResult.Success)throw new InvalidOperationException("Stop before auth write/rehash");
using var session=await new AuthService(repository,passwords).LoginAsync(credential);if(session.Identity.Role!="ADMIN")throw new InvalidOperationException("Not ADMIN");
var service=new ReportService(repository:new ReportRepository(new ConnectionFactory(connection)),permissions:new PermissionService(repository));var results=new List<object>();
await using var c=new SqlConnection(connection);await c.OpenAsync();await using(var cmd=c.CreateCommand()){
 cmd.CommandText="SELECT IS_SRVROLEMEMBER('sysadmin'),IS_ROLEMEMBER('db_owner'),IS_ROLEMEMBER('CafeManagerApp')";await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();if(r.GetInt32(0)!=0||r.GetInt32(1)!=0||r.GetInt32(2)!=1)throw new InvalidOperationException("Runtime privilege too broad");
}
await using var owner=new SqlConnection(new SqlConnectionStringBuilder{DataSource=target.DataSource,InitialCatalog=target.InitialCatalog,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false}.ConnectionString);await owner.OpenAsync();
foreach(var month in new[]{false,true}){
 var d=await service.ReadDashboardAnalyticsAsync(session,month);await using var cmd=owner.CreateCommand();cmd.CommandText="DECLARE @paid decimal(38,0)=(SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=@from AND NgayThanhToan<@until),@expense decimal(38,0)=(SELECT COALESCE(SUM(CONVERT(decimal(38,0),p.SoTien)),0) FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI m ON m.MaDMTC=p.MaDMTC WHERE m.Loai=N'Chi' AND p.NgayLap>=@from AND p.NgayLap<@until);SELECT @paid,@expense";cmd.Parameters.Add("@from",System.Data.SqlDbType.DateTime2).Value=d.Period.From;cmd.Parameters["@from"].Scale=7;cmd.Parameters.Add("@until",System.Data.SqlDbType.DateTime2).Value=d.Period.Until;cmd.Parameters["@until"].Scale=7;await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();if(d.Pages["trend.current"].Totals.Amount!=r.GetDecimal(0)||d.Indicators["expense.current"]!=r.GetDecimal(1))throw new InvalidOperationException("Runtime SQL parity failed");
 results.Add(new{Month=month,Status="PASS",d.Period.From,d.Period.Until,ReadAt=d.Pages["trend.current"].Metadata.ReadAt,Revenue=d.Pages["trend.current"].Totals.Amount,Expense=d.Indicators["expense.current"],OpenOrders=d.Indicators["orders.open"],Debt=d.Indicators["debt.current"],Channels=d.Pages["channels"].Rows.Count,Products=d.Pages["products"].Rows.Count});
}
File.WriteAllText(args[0],JsonSerializer.Serialize(new{Runtime="CafeManagerApp; not sysadmin/db_owner",Schema=28,Results=results},new JsonSerializerOptions{WriteIndented=true}));
