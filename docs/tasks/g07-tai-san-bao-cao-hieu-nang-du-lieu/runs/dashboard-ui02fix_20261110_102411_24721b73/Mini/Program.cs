using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;
if(args.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Test DB/marker");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using var c=new SqlConnection(b.ConnectionString);await c.OpenAsync();await using(var cmd=c.CreateCommand()){
 cmd.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker mismatch");
 cmd.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s;CREATE USER ExtendedRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER ExtendedRuntime;";cmd.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await cmd.ExecuteNonQueryAsync();
}
var f=new ConnectionFactory(b.ConnectionString);var auth=new AuthRepository(f);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var sales=new SalesService(new SalesRepository(f),new PermissionService(auth));
var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Mang di"),"SP001",1,null));var order=(await sales.GetAsync(session,code))!;var preview=await sales.PreviewAsync(session,order.Header);await sales.CheckoutAsync(session,new(code,order.Header.Revision,Guid.NewGuid(),preview.Token,[new("PT001",preview.Payable)]));Console.WriteLine("One current paid invoice prepared");
