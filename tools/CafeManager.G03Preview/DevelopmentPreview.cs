using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using System.Text.Json;
internal static class DevelopmentPreview
{
    internal static void Run(string folder)
    {
        var output=Path.GetFullPath(folder);Directory.CreateDirectory(output);
        var results=new List<object>();
        var factory=new ConnectionFactory(DevelopmentConnection.Read());var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
        IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var nested in All(c))yield return nested;}}
        using var host=new Form{Text="G03 · Đọc màn development — kiểm kỹ thuật",ClientSize=new Size(1220,680)};
        host.Shown+=async(_,_)=>
        {
            try
            {
                foreach(var username in new[]{"admin","thungan","kho"})
                {
                    // Dùng identity hiện có để nối dependencies/read UI; không thay kiểm thử đăng nhập.
                    var credential=await auth.ReadCredentialAsync(username)??throw new InvalidOperationException("Development account unavailable");
                    using var session=new SessionContext(credential.Identity);
                    var modules=new Dictionary<NavigationTarget,Func<UserControl>>
                    {
                        [NavigationTarget.Materials]=()=>new CafeManager.WinForms.Modules.Inventory.MaterialsControl(new MaterialService(new MaterialRepository(factory),permissions),session),
                        [NavigationTarget.Products]=()=>new CafeManager.WinForms.Modules.Menu.ProductsControl(new ProductService(new ProductRepository(factory),permissions),session)
                    };
                    if(session.Identity.Role is "ADMIN" or "CASH")
                    {
                        modules[NavigationTarget.Tables]=()=>new CafeManager.WinForms.Modules.Tables.TablesControl(new TableService(new TableRepository(factory),permissions),session);
                        modules[NavigationTarget.Customers]=()=>new CafeManager.WinForms.Modules.Customers.CustomersControl(new CustomerService(new CustomerRepository(factory),permissions),session);
                        modules[NavigationTarget.Loyalty]=()=>new CafeManager.WinForms.Modules.Loyalty.LoyaltyControl(new LoyaltyService(new LoyaltyRepository(factory),permissions),new CustomerService(new CustomerRepository(factory),permissions),session);
                    }
                    using var main=new MainForm(session,permissions,auth.ReadStateAsync,null,modules);main.Show(host);await Task.Delay(350);
                    foreach(var pair in modules)
                    {
                        var navigation=typeof(MainForm).GetMethod("Navigate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                        var switching=typeof(MainForm).GetField("navigating",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                        for(var tick=0;tick<50 && (bool)switching.GetValue(main)!;tick++)await Task.Delay(100);
                        await (Task)navigation.Invoke(main,[pair.Key])!;
                        var expected=pair.Key switch{NavigationTarget.Materials=>"MaterialsControl",NavigationTarget.Products=>"ProductsControl",NavigationTarget.Tables=>"TablesControl",NavigationTarget.Customers=>"CustomersControl",_=>"LoyaltyControl"};
                        if(!All(main).Any(c=>c.GetType().Name==expected&&c.Visible))throw new InvalidOperationException("Navigation did not show "+expected);
                        for(var tick=0;tick<50;tick++){await Task.Delay(100);var visible=All(main).OfType<DataGridView>().Where(g=>g.Visible).ToArray();if(visible.Length>0&&visible.All(g=>g.DataSource is not null)&&!All(main).OfType<Label>().Any(l=>l.Visible&&l.Text.StartsWith("Đang tải")))break;}
                        main.Refresh();using var image=new Bitmap(main.Width,main.Height);main.DrawToBitmap(image,new Rectangle(Point.Empty,main.Size));
                        var file=$"{session.Identity.Role}-{pair.Key}.png";image.Save(Path.Combine(output,file));
                        var grids=All(main).OfType<DataGridView>().Where(g=>g.Visible).Select(g=>new{Rows=g.RowCount,Columns=g.ColumnCount}).ToArray();
                        var messages=All(main).OfType<Label>().Where(l=>l.Visible&&(l.Text.StartsWith("Chưa cập nhật")||l.Text.StartsWith("Trang ")||l.Text.StartsWith("Chưa có")||l.Text.StartsWith("Đang tải"))).Select(l=>l.Text).ToArray();
                        results.Add(new{Role=session.Identity.Role,Module=pair.Key.ToString(),File=file,Grids=grids,Messages=messages,Kind="Technical read-only UI; identity injection, not login acceptance"});
                    }
                    main.Close();
                }
            }
            catch(Exception ex){results.Add(new{Error=ex.Message});Environment.ExitCode=1;}
            finally{File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));host.Close();}
        };
        Application.Run(host);
    }
}


