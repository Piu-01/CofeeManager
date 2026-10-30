using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using CafeManager.Business.Security;
using Microsoft.Data.SqlClient;

if(args.Length!=4||args[0]!="setup"||!args[1].StartsWith("CoffeeManager_Test_G05_",StringComparison.Ordinal))throw new ArgumentException("setup database marker outputDirectory");
var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[1],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
await using var c=new SqlConnection(cs);await c.OpenAsync();await using var guard=c.CreateCommand();guard.CommandText="SELECT RunId FROM dbo.G05TestMarker";
if((string?)await guard.ExecuteScalarAsync()!=args[2])throw new InvalidOperationException("Wrong marker");
var privateDir=Path.Combine(Path.GetTempPath(),"CoffeeManager_G05_"+args[2]);if(Directory.Exists(privateDir))throw new IOException("Private directory already exists");Directory.CreateDirectory(privateDir);File.WriteAllText(Path.Combine(privateDir,"marker.txt"),args[2]);
var credentials=new Dictionary<string,object>();var accounts=new Dictionary<string,int>();var passwords=new PasswordService();
await using(var reseed=c.CreateCommand()){reseed.CommandText="DBCC CHECKIDENT('dbo.TAIKHOAN',RESEED,9001000) WITH NO_INFOMSGS";await reseed.ExecuteNonQueryAsync();}
var i=991;
foreach(var role in new[]{"ADMIN","STORE","CASH"})
{
    var employee="NV"+i++;var user="g05_"+role.ToLowerInvariant();var password=Convert.ToHexString(RandomNumberGenerator.GetBytes(16))+"a!";
    await using var cmd=c.CreateCommand();cmd.CommandText="INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,LuongCung,MaCV) VALUES(@employee,@name,'Full-time',1000,(SELECT TOP(1) MaCV FROM dbo.CHUCVU ORDER BY MaCV)); INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES(@employee,@user,@hash,@role,1);SELECT CONVERT(int,SCOPE_IDENTITY());";
    cmd.Parameters.AddWithValue("@employee",employee);cmd.Parameters.AddWithValue("@name","G05 fixture "+role);cmd.Parameters.AddWithValue("@user",user);cmd.Parameters.AddWithValue("@hash",passwords.Hash(password));cmd.Parameters.AddWithValue("@role",role);accounts[role]=Convert.ToInt32(await cmd.ExecuteScalarAsync());credentials[role]=new{Username=user,Password=password};
}
var credentialPath=Path.Combine(privateDir,"credentials.bin");File.WriteAllBytes(credentialPath,ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(credentials),null,DataProtectionScope.CurrentUser));
var metadata=new{Database=args[1],Marker=args[2],PrivateDirectory=privateDir,CredentialPath=credentialPath,ActorIds=accounts,WindowsOwner=WindowsIdentity.GetCurrent().Name,Note="DPAPI encrypted fixture credentials; never print or commit password/hash. Delete private directory after test."};
Directory.CreateDirectory(args[3]);File.WriteAllText(Path.Combine(args[3],"gui-fixture.json"),JsonSerializer.Serialize(metadata,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Prepared GUI fixture accounts; credentials DPAPI only.");
