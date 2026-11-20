using System.Text.Json;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.UI;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;
internal static class Program
{
    static IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var n in All(c))yield return n;}}
    [STAThread] static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true};
        using(var connection=new SqlConnection(guard.ConnectionString)){connection.Open();using var command=connection.CreateCommand();command.CommandText="SELECT RunId FROM dbo.G03TestMarker";if(command.ExecuteScalar() as string!="20261019_215457_29e552a9")throw new Exception("Marker mismatch");}
        var cs=new SqlConnectionStringBuilder(DevelopmentConnection.Read()){InitialCatalog=db};
        var factory=new ConnectionFactory(cs.ConnectionString);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);
        var results=new List<object>();void Save()=>File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        using var host=new Form{Text="Orbit layout read-only technical probe",ClientSize=new Size(1100,650),ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-10000,-10000)};
        host.Shown+=async(_,_)=>
        {
            try
            {
                foreach(var username in (args.Contains("--catalog-only")?new[]{"admin"}:new[]{"admin","thungan","kho"}))
                {
                    var identity=(await auth.ReadCredentialAsync(username))?.Identity??throw new Exception("Fixture identity missing");using var session=new SessionContext(identity);
                    var customers=new CustomerService(new CustomerRepository(factory),permission);
                    var modules=new Dictionary<string,Func<UserControl>>{
                        ["Materials"]=()=>new CafeManager.WinForms.Modules.Inventory.MaterialsControl(new MaterialService(new MaterialRepository(factory),permission),session),
                        ["Products"]=()=>new CafeManager.WinForms.Modules.Menu.ProductsControl(new ProductService(new ProductRepository(factory),permission),session)};
                    if(identity.Role=="ADMIN")modules["Catalogs"]=()=>new CafeManager.WinForms.Modules.Catalogs.CatalogsControl(new CatalogService(new CatalogRepository(factory),permission),session);
                    if(identity.Role is "ADMIN" or "CASH"){
                        modules["Customers"]=()=>new CafeManager.WinForms.Modules.Customers.CustomersControl(customers,session);
                        modules["Tables"]=()=>new CafeManager.WinForms.Modules.Tables.TablesControl(new TableService(new TableRepository(factory),permission),session);
                        modules["Loyalty"]=()=>new CafeManager.WinForms.Modules.Loyalty.LoyaltyControl(new LoyaltyService(new LoyaltyRepository(factory),permission),customers,session);}
                    foreach(var pair in modules.Where(p=>!args.Contains("--catalog-only")||p.Key=="Catalogs"))
                    {
                        UserControl? module=null;
                        try
                        {
                            module=pair.Value();module.Dock=DockStyle.Fill;host.Controls.Add(module);
                            var tab=All(module).OfType<TabControl>().FirstOrDefault();var count=tab?.TabCount??1;
                            for(var index=0;index<count;index++)
                            {
                                if(tab is not null)tab.SelectedIndex=index;
                                await Task.Delay(450);
                                for(var tick=0;tick<30&&All(module).OfType<Label>().Any(l=>l.Visible&&l.Text.StartsWith("Đang tải"));tick++)await Task.Delay(100);
                                module.PerformLayout();
                                var workspace=All(module).OfType<OrbitManagementLayout>().FirstOrDefault(c=>c.Visible);
                                var grid=All((Control?)tab?.SelectedTab??module).OfType<DataGridView>().FirstOrDefault(g=>g.DataSource is not null && (g.Visible||pair.Key=="Tables"));
                                var buttons=All(module).OfType<Button>().Where(b=>b.Visible).Select(b=>new{b.Text,b.Width,b.Height,b.Enabled,Parent=b.Parent?.GetType().Name}).ToArray();
                                                                if(args.Contains("--catalog-only")&&index==0&&grid is null)
                                {
                                    await Task.Delay(3000);
                                    var initialGrid=All(module).OfType<DataGridView>().Single();
                                    results.Add(new{Stage="Initial-after-3s",Status=initialGrid.DataSource is null?"FAIL":"PASS",HandleCreated=module.IsHandleCreated,Rows=initialGrid.Rows.Count});Save();
                                    await (Task)module.GetType().GetMethod("RefreshRows",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(module,null)!;
                                    results.Add(new{Stage="Explicit-existing-refresh",Status=initialGrid.DataSource is not null?"PASS":"FAIL",Rows=initialGrid.Rows.Count});Save();
                                }
                                var loaded=grid is not null;
                                results.Add(new{Role=identity.Role,Module=pair.Key,Tab=tab?.SelectedTab?.Text,Dpi=module.DeviceDpi,Status=loaded?"PASS":"FAIL",Rows=grid?.Rows.Count,GridWidth=grid?.Width,ColumnWidth=grid?.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).Sum(c=>c.Width),WorkspaceWidth=workspace?.Width,Buttons=buttons,States=All(module).OfType<Label>().Where(l=>l.Visible).Select(l=>l.Text).ToArray()});Save();
                            }
                        }
                        catch(Exception ex){results.Add(new{Role=identity.Role,Module=pair.Key,Status="FAIL",Error=ex.ToString()});Save();}
                        finally{if(module is not null){host.Controls.Remove(module);module.Dispose();}}
                    }
                }
            }
            catch(Exception ex){results.Add(new{Status="FAIL",Error=ex.ToString()});Save();}
            finally{Save();host.Close();}
        };
        Application.Run(host);
        Console.WriteLine($"Read-only technical records: {results.Count}");
    }
}

