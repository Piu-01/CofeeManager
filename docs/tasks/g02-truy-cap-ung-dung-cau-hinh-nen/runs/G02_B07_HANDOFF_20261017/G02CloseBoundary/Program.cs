using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;
using Microsoft.Data.SqlClient;
static class Probe {
 static string Db="",Output="",Secret=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
 static readonly List<object> Results=[];static int Opens;
 static T F<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
 static void Need(bool b,string m){if(!b)throw new Exception(m);}
 static async Task Wait(Func<bool> p){for(int i=0;!p();i++){if(i>600)throw new Exception("UI did not settle");await Task.Delay(25);}}
 static async Task Check(string id,Func<Task<string>> a){try{Results.Add(new{Id=id,Actual=await a(),Status="PASS"});}catch(Exception e){Results.Add(new{Id=id,Status="FAIL",ErrorType=e.GetType().Name,Actual=e is SqlException s?"SQL"+s.Number:e.Message});}File.WriteAllText(Path.Combine(Output,"results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));}
 static string Cs=>$"Server=localhost;Initial Catalog={Db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False";
 static ConnectionFactory Factory(){var f=new ConnectionFactory(Cs);Func<SqlConnection,CancellationToken,Task> init=async(c,ct)=>{Interlocked.Increment(ref Opens);await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER=N'G02_Runtime'";await cmd.ExecuteNonQueryAsync(ct);};typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(f,init);return f;}
 static async Task Sql(string name){await using var c=new SqlConnection(Cs);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="IF NOT EXISTS(SELECT 1 FROM dbo.G02TestMarker) THROW 51000,'Test marker required',1;UPDATE dbo.TAIKHOAN SET TenDangNhap=@name,MatKhauHash=@hash WHERE TenDangNhap=@old";cmd.Parameters.AddWithValue("@name",name);cmd.Parameters.AddWithValue("@old",CurrentName);cmd.Parameters.AddWithValue("@hash",new PasswordService().Hash(Secret));Need(await cmd.ExecuteNonQueryAsync()==1,"Expected one fixture account");CurrentName=name;}
 static string CurrentName="g02.admin";
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,string l);
 [STAThread]static void Main(string[] args){Db=args[0];Output=args[1];Need(System.Text.RegularExpressions.Regex.IsMatch(Db,@"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"),"Test target only");Directory.CreateDirectory(Output);Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();using var host=new Form{Text="G02 boundary acceptance host"};host.Shown+=async(_,_)=>{try{await Cases();}catch(Exception e){Results.Add(new{Id="HARNESS",Status="BLOCKED",ErrorType=e.GetType().Name});}finally{File.WriteAllText(Path.Combine(Output,"results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));host.Close();}};Application.Run(host);}
 static async Task Cases(){await Sql("g02.admin");var auth=new AuthService(new AuthRepository(Factory()),new PasswordService());
  foreach(var (label,input) in new[]{("empty",""),("space"," "),("51",new string('a',51)),("inner","a b"),("tab","a\t"),("newline","a\n"),("slash","a/b"),("vietnamese","tên"),("fullwidth","Ａ"),("emoji","a🙂")})await Check("T06-K10.UI.invalid."+label,async()=>{using var f=new LoginForm(auth,null);int logins=0;f.LoggedIn+=s=>{logins++;s.Dispose();};f.Show();var u=F<TextBox>(f,"usernameInput");u.Text=input;F<TextBox>(f,"passwordInput").Text=Secret;var opens=Opens;F<Button>(f,"loginButton").PerformClick();await Wait(()=>F<Button>(f,"loginButton").Enabled);Need(Opens==opens&&logins==0,"Invalid username reached repository/login");Need(u.Text==input&&F<TextBox>(f,"passwordInput").Text==Secret,"Validation lost input");f.Close();return "Rejected by UI service path before SQL; input retained; no session";});
  foreach(var (label,name,input) in new[]{("1","A","a"),("50",new string('A',50),new string('a',50)),("trim-case","Ab._-19","  aB._-19  ")})await Check("T06-K10.UI.valid."+label,async()=>{await Sql(name);using var f=new LoginForm(auth,null);SessionContext? session=null;f.LoggedIn+=s=>session=s;f.Show();F<TextBox>(f,"usernameInput").Text=input;F<TextBox>(f,"passwordInput").Text=Secret;F<Button>(f,"loginButton").PerformClick();await Wait(()=>F<Button>(f,"loginButton").Enabled);Need(session is not null,"Valid boundary login failed: "+F<Label>(f,"statusLabel").Text);session!.Dispose();Need(F<TextBox>(f,"passwordInput").Text=="","Success did not clear password");f.Close();return "Actual Login_Click, same account authenticated via canonical key; success clears password";});
  await Check("T06-K10.UI.nativeLimit",async()=>{using var f=new LoginForm(auth,null);f.Show();var u=F<TextBox>(f,"usernameInput");Need(u.MaxLength==50,"Limit is not50");u.Clear();u.Focus();SendMessage(u.Handle,0x00C2,new IntPtr(1),new string('a',51));await Task.Delay(100);Need(u.TextLength==50,"Native edit input not limited to50");f.Close();return "WM_EM_REPLACESEL51 inserts50; programmatic51 was independently rejected above";});
  await Sql("g02.admin");
  await Check("T06-K07.session-context-audit",async()=>{using var s=await auth.LoginAsync(new("g02.admin",Secret));var fields=s.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);Need(!fields.Any(x=>x.Name.Contains("password",StringComparison.OrdinalIgnoreCase)||x.Name.Contains("hash",StringComparison.OrdinalIgnoreCase)),"Session stores credential field");Need(!JsonSerializer.Serialize(s.Identity).Contains(Secret,StringComparison.Ordinal),"Credential reached identity");return "Session fields/identity contain identity, stamp, generation/cancellation only; no password/hash fields; generated secret never emitted";});
 }
}
