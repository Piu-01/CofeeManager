using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.Assets;
using Microsoft.Data.SqlClient;

// Smoke kỹ thuật các tầng, không thay nghiệm thu G07 hoặc kiểm hình thức.
if(args.Length==4&&args[3]=="benchmark"){await PerformanceProbe.Run(args[0],args[1],args[2]);return;}
if((args.Length!=4&&args.Length!=5)||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Cần DBtest G07 demo, marker, output, version.");
var evidence=new Dictionary<string,object?>{["Kind"]="Technical dependency smoke, not acceptance"};
var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
try{
    await using var owner=new SqlConnection(builder.ConnectionString);await owner.OpenAsync();
    await using var marker=owner.CreateCommand();marker.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await marker.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker mismatch");
    var passwords=new PasswordService();await using var setup=owner.CreateCommand();setup.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin');EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@Hash,@MaNV='NV001',@ExpectedStamp=@s;CREATE USER G07Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G07Runtime;";setup.Parameters.AddWithValue("@Hash",passwords.Hash(Guid.NewGuid().ToString("N")));await setup.ExecuteNonQueryAsync();
    var factory=new ConnectionFactory(builder.ConnectionString);typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='G07Runtime'";await cmd.ExecuteNonQueryAsync(t);}));
    var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext(await auth.ReadIdentityAsync(1,default)??throw new InvalidOperationException("Missing actor"));
    var assets=new AssetService(new AssetRepository(factory),permissions);var context=await assets.ContextAsync(session);var request=new AssetWriteRequest(Guid.NewGuid(),"Create",null,null,new("Tài sản smoke",new(2026,1,15),100,3));
    var result=await assets.WriteAsync(session,request);var row=await assets.GetAsync(session,result.Code)??throw new InvalidOperationException("Asset mapping failed");
    if(row.Value!=100||(await assets.ReceiptAsync(session,request.RequestId))?.Code!=row.Code)throw new InvalidOperationException("Asset receipt failed");
    var list=await assets.ListAsync(session,new(row.Code));if(list.TotalCount!=1||list.Rows.Count!=1)throw new InvalidOperationException("Asset page failed");
    var expense=await assets.ExpensesAsync(session,row.Code,new(1,1,1),new(9999,12,31));if(expense.TotalAmount!=0)throw new InvalidOperationException("Asset created expense");
    await using var calc=owner.CreateCommand();calc.CommandText="SELECT Accumulated FROM dbo.fn_AssetDepreciation(100,3,'20260115','20260301')";if(Convert.ToDecimal(await calc.ExecuteScalarAsync())!=100)throw new InvalidOperationException("SQL depreciation mismatch");
    evidence["Asset"]=row.Code;evidence["DatabaseScope"]=context.Scope;evidence["AssetPageCount"]=list.TotalCount;

    if(int.Parse(args[3])>=26){await ReportSmoke.Run(factory,permissions,session,evidence,owner);evidence["UI"]=UiTechnicalProbe.Run(factory,permissions,session,assets,row);}
    if(args.Length==5&&args[4]=="acceptance")evidence["AcceptanceSummary"]=await new AcceptanceProbe(factory,owner,permissions,session,Path.Combine(Path.GetDirectoryName(args[2])!,"extended-results.json")).Run();
    if(args.Length==5&&args[4]=="acceptance"&&int.Parse(args[3])>=27)evidence["AssetLockRegression"]=await new AssetLockRegression(factory,owner,permissions,session,Path.Combine(Path.GetDirectoryName(args[2])!,"asset-lock-regression.json")).Run();
    evidence["Status"]="Succeeded";
}catch(Exception e){evidence["Status"]="Failed";evidence["Error"]=e.ToString();Environment.ExitCode=1;}
finally{File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));}






