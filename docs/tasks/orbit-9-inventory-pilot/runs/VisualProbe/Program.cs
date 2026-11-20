using System.Reflection;
using System.Text.Json;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;

static class Program
{
    [STAThread] static void Main(string[] args)
    {
        if(args.Contains("--dpi96"))Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,TrustServerCertificate=true};
        using(var c=new SqlConnection(guard.ConnectionString)){c.Open();using var command=c.CreateCommand();command.CommandText="SELECT RunId FROM dbo.G03TestMarker";if(command.ExecuteScalar() as string!="20261019_215457_29e552a9")throw new Exception("Marker mismatch");}
        var cs=new SqlConnectionStringBuilder(DevelopmentConnection.Read()){InitialCatalog=db};var factory=new ConnectionFactory(cs.ConnectionString);
        var auth=new AuthRepository(factory);var permission=new PermissionService(auth);
        // Identity fixture cho UI test, không phải kiểm thử hoặc tự thao tác đăng nhập.
        var identity=auth.ReadCredentialAsync("admin").GetAwaiter().GetResult()!.Identity;using var session=new SessionContext(identity);
        using var form=new MainForm(session,permission,_=>throw new NotSupportedException("Read-only visual fixture"),null,new Dictionary<NavigationTarget,Func<UserControl>>{[NavigationTarget.Materials]=()=>new MaterialsControl(new MaterialService(new MaterialRepository(factory),permission),session)});
        form.Text=args.Contains("--dpi96")?"CoffeeManager · UI fixture 96DPI (desktop virtualized)":"CoffeeManager · UI fixture 125%";
        form.Shown+=async(_,_)=>{
            try{
                await Task.Delay(300);form.ClientSize=new Size((int)Math.Round(1366*form.DeviceDpi/96F),(int)Math.Round(768*form.DeviceDpi/96F));
                await (Task)typeof(MainForm).GetMethod("Navigate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,new object[]{NavigationTarget.Materials})!;
                await Task.Delay(500);
                File.WriteAllText(args[0],JsonSerializer.Serialize(new{DeviceDpi=form.DeviceDpi,Client=form.ClientSize,Awareness=args.Contains("--dpi96")?"DpiUnawareGdiScaled; desktop125%; not a Windows100% acceptance":"PerMonitorV2 on current desktop",Database=db,IdentitySource="Test fixture metadata, not login test",NativeForm="CafeManager.WinForms.Forms.MainForm"},new JsonSerializerOptions{WriteIndented=true}));
            }catch(Exception ex){File.WriteAllText(args[0],ex.ToString());}
        };
        Application.ThreadException += (_, e) => File.WriteAllText(args[0]+".error",e.Exception.ToString());
        Application.Run(form);
    }
}

