using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.HumanResources;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class NativeProgram
{
 [STAThread]
 static void Main(string[] args)
 {
  Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
  if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G06_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Test DB/marker/output required");
  var evidence=new Dictionary<string,object?>();
  try {
   var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false,ApplicationName="G06NativeTest"};
   using var owner=new SqlConnection(builder.ConnectionString);owner.Open();
   using var mark=owner.CreateCommand();mark.CommandText="SELECT RunId FROM dbo.G06TestMarker";if((string?)mark.ExecuteScalar()!=args[1])throw new Exception("Marker mismatch");
   var passwords=new PasswordService();var password=Guid.NewGuid().ToString("N");
   using var setup=owner.CreateCommand();setup.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@h,@MaNV='NV001',@ExpectedStamp=@s;CREATE USER G06Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G06Runtime;";setup.Parameters.AddWithValue("@h",passwords.Hash(password));setup.ExecuteNonQuery();
   var factory=new ConnectionFactory(builder.ConnectionString);
   typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{using var q=c.CreateCommand();q.CommandText="EXECUTE AS USER='G06Runtime';";await q.ExecuteNonQueryAsync(t);}));
   var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);var login=new AuthService(auth,passwords);
   using var session=login.LoginAsync(new("admin",password)).GetAwaiter().GetResult();
   using(var c=new SqlConnection(builder.ConnectionString)){
    c.Open();using var asUser=c.CreateCommand();asUser.CommandText="EXECUTE AS USER='G06Runtime';";asUser.ExecuteNonQuery();
    using var invalid=c.CreateCommand();invalid.CommandText="EXEC dbo.sp_EmployeeWrite @Actor=1,@Stamp=@s,@Action='Create',@RequestId=@r,@Name=N'',@Contract=N'Full-time',@MonthlyRate=0,@HourlyRate=0,@Position='CV001'";invalid.Parameters.AddWithValue("@s",session.Identity.SecurityStamp);invalid.Parameters.AddWithValue("@r",Guid.NewGuid());
    try{invalid.ExecuteNonQuery();evidence["InvalidError"]="Unexpected success";}catch(SqlException e){evidence["InvalidError"]=e.Number;}
    using var state=c.CreateCommand();state.CommandText="SELECT @@TRANCOUNT a,XACT_STATE() b,CONCAT(@@TRANCOUNT,':',XACT_STATE()) c;";using var r=state.ExecuteReader();r.Read();evidence["TransactionDiagnostic"]=new{TranCount=r.GetInt32(0),XactState=r.GetInt32(1),Concat=r.GetString(2)};
   }
   var employees=new EmployeeService(new EmployeeRepository(factory),permissions);var accounts=new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,passwords);
   var attendance=new AttendanceService(new AttendanceRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
   var finance=new FinanceService(new FinanceRepository(factory),permissions);
   var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),finance,new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll);
   var context=employees.ContextAsync(session).GetAwaiter().GetResult();
   evidence["Database"]=args[0];evidence["Marker"]=args[1];evidence["Kind"]="Native UI against real MainForm/modules; session authenticated via production AuthService, not LoginForm UI";evidence["DeviceDpi"]=120;
   using var main=new MainForm(session,permissions,auth.ReadStateAsync,null,new Dictionary<NavigationTarget,Func<UserControl>>{
    [NavigationTarget.HumanResources]=()=>new HumanResourcesControl(employees,accounts,session,attendance,payroll,links),
    [NavigationTarget.Finance]=()=>new FinanceControl(finance,session,new PendingOperationFile(context.Scope),links)
   });
   main.Text="G06 — Nghiệm thu DB riêng";main.ClientSize=new(1366,768);
   main.Shown+=(_,_)=>{evidence["DeviceDpi"]=main.DeviceDpi;evidence["Status"]="Ready";File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));};
   Application.Run(main);evidence["Status"]="Closed";
  }catch(Exception e){evidence["Status"]="Failed";evidence["Error"]=e.ToString();}
  finally{File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));}
 }
}
