using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Localization;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Supplement
{
    [STAThread]static void Main(string[] args)
    {
        Program.Output=Path.Combine(Path.GetFullPath(args[0]),"supplement");Directory.CreateDirectory(Program.Output);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetColorMode(SystemColorMode.Dark);
        using var host=new Form{Text="Localization supplemental checks",Size=new(480,120)};
        host.Shown+=async(_,_)=>
        {
            var preference=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CoffeeManager","language.txt");
            var existed=File.Exists(preference);var original=existed?File.ReadAllBytes(preference):null;
            try
            {
                using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0],"manifest.json")));
                foreach(var item in manifest.RootElement.GetProperty("Created").EnumerateArray())
                {
                    var language=item.GetProperty("Language").GetString()!;var db=item.GetProperty("Database").GetString()!;
                    if(!System.Text.RegularExpressions.Regex.IsMatch(db,@"^CoffeeManager_Test_LOC_20261110_\d{6}_[a-f0-9]{8}_(vi|en|clean)$"))throw new Exception("Non-test target");
                    var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
                    await using var owner=new SqlConnection(builder.ConnectionString);await owner.OpenAsync();
                    if((string?)await StateSnapshot.Scalar(owner,"SELECT RunId FROM dbo.LocalizationTestMarker")!=manifest.RootElement.GetProperty("RunId").GetString())throw new Exception("Marker mismatch");
                    if(language=="clean")
                    {
                        foreach(var lang in new[]{AppLanguage.Vietnamese,AppLanguage.English})
                        {
                            LanguageService.Set(lang);var f=new ConnectionFactory(builder.ConnectionString);
                            using var bootstrap=new BootstrapForm(new BootstrapService(new SetupRepository(f),new AuthRepository(f),new PasswordService()));
                            bootstrap.Show();await Task.Delay(350);UiProbe.Inspect(bootstrap,"bootstrap.clean-loaded",lang.ToString());
                            ((Button)UiProbe.Field(bootstrap,"completeButton")!).PerformClick();await Task.Delay(150);UiProbe.Inspect(bootstrap,"bootstrap.validation",lang.ToString());bootstrap.Dispose();
                        }
                        continue;
                    }
                    LanguageService.Set(language=="vi"?AppLanguage.Vietnamese:AppLanguage.English);
                    var factory=OperationProbe.RuntimeFactory(builder.ConnectionString);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);
                    var id=Convert.ToInt32(await StateSnapshot.Scalar(owner,"SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'"));
                    using var admin=new SessionContext((await auth.ReadIdentityAsync(id,default))!);
                    await Concurrency(owner,factory,permission,admin,language);
                    await Customers(factory,permission,admin,language);
                    await ScrollReachability(factory,permission,admin,language);
                    foreach(var role in new[]{"ADMIN","CASH","STORE"})
                    {
                        SessionContext session=admin;
                        if(role!="ADMIN")
                        {
                            var employees=new EmployeeService(new EmployeeRepository(factory),permission);var accounts=new EmployeeAccountService(new EmployeeAccountRepository(factory),permission,new PasswordService());
                            var employee=await employees.SaveAsync(admin,new("Kiểm ngôn ngữ "+role,null,"Part-time",0,1000,"CV003"),null,Guid.NewGuid());
                            var password=Guid.NewGuid().ToString("N")+"!a1";var username="loc."+role.ToLowerInvariant();
                            await accounts.WriteAsync(admin,"Create",new(employee,null,username,role,true),password,password,Guid.NewGuid());
                            session=await new AuthService(auth,new PasswordService()).LoginAsync(new(username,password));
                        }
                        using var main=new MainForm(session,permission,auth.ReadStateAsync,modules:new Dictionary<NavigationTarget,Func<UserControl>>{[NavigationTarget.Home]=()=>new CafeManager.WinForms.Modules.Home.SessionHomeControl(session.Identity)});
                        main.Show();await Task.Delay(200);await main.OpenWorkspaceAsync("SystemInfo");await Task.Delay(150);
                        var info=UiProbe.Desc(main).OfType<CafeManager.WinForms.Modules.System.SystemInfoControl>().Single();
                        Program.Result("system.access."+role,language,"System language setting accessible for role",info.Visible,new{Role=session.Identity.Role,info.Visible});
                        await PreferenceDialog(main,info,language);
                        main.Dispose();if(role!="ADMIN")session.Dispose();
                    }
                }
                await ModalOwner();
            }
            catch(Exception e){Program.Result("supplement-fatal","both","Finish supplemental checks",false,new{Type=e.GetType().Name,Message=e.Message,Stack=e.StackTrace});}
            finally{if(existed)File.WriteAllBytes(preference,original!);else if(File.Exists(preference))File.Delete(preference);Program.Save();host.Dispose();Application.ExitThread();}
        };
        Application.Run(host);
    }
    private static async Task Concurrency(SqlConnection owner,ConnectionFactory factory,PermissionService permission,SessionContext admin,string language)
    {
        try
        {
            var sales=new SalesService(new SalesRepository(factory),permission);var customer=new CustomerService(new CustomerRepository(factory),permission);
            var created=await customer.SaveAsync(admin,new("Khách Việt",null),null,Guid.NewGuid());
            var beforeStock=Convert.ToDecimal(await StateSnapshot.Scalar(owner,"SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));
            var beforePoints=(await customer.GetAsync(admin,created.Code))!.Points;
            var code=await sales.AllocateAsync(admin);await sales.CreateAsync(admin,new(code,Guid.NewGuid(),new("Mang di",Customer:created.Code),"SP001",1,"Ghi chú tiếng Việt"));
            var order=(await sales.GetAsync(admin,code))!;var preview=await sales.PreviewAsync(admin,order.Header);
            var request=new CheckoutRequest(code,preview.Revision,Guid.NewGuid(),preview.Token,[new("PT001",preview.Payable)]);
            using var session2=new SessionContext((await new AuthRepository(factory).ReadIdentityAsync(admin.Identity.AccountId,default))!);
            var second=new SalesService(new SalesRepository(factory),permission);
            await Task.WhenAll(sales.CheckoutAsync(admin,request),second.CheckoutAsync(session2,request));
            var paid=(await sales.GetAsync(admin,code))!;var afterPoints=(await customer.GetAsync(admin,created.Code))!.Points;
            var afterStock=Convert.ToDecimal(await StateSnapshot.Scalar(owner,"SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));
            Program.Result("checkout-two-sessions",language,"Concurrent same request creates one payment/consumption/points credit; Vietnamese note preserved",paid.Payments.Count==1&&paid.Header.Status=="Da thanh toan"&&afterStock==beforeStock-.018m&&afterPoints-beforePoints==preview.Points&&paid.Lines[0].Note=="Ghi chú tiếng Việt",new{Code=code,PaymentRows=paid.Payments.Count,StockDelta=afterStock-beforeStock,PointsDelta=afterPoints-beforePoints,ExpectedPoints=preview.Points,NotePreserved=paid.Lines[0].Note=="Ghi chú tiếng Việt"});
        }
        catch(Exception e){Program.Result("checkout-two-sessions",language,"Concurrent request succeeds exactly once",false,new{Type=e.GetType().Name,Message=e.Message});}
    }
    private static async Task Customers(ConnectionFactory factory,PermissionService permission,SessionContext admin,string language)
    {
        var service=new CustomerService(new CustomerRepository(factory),permission);var created=await service.SaveAsync(admin,new("Trong",null),null,Guid.NewGuid());
        using var form=new Form{Text="Customer data preservation",Size=new(1100,700)};
        using var control=new CafeManager.WinForms.Modules.Customers.CustomersControl(service,admin);form.Controls.Add(control);form.Show();await Task.Delay(350);
        var grid=UiProbe.Desc(control).OfType<DataGridView>().First();
        var row=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>r.DataBoundItem is CustomerRow c&&c.Code==created.Code);
        var displayed=row?.Cells["Name"].FormattedValue?.ToString();
        Program.Result("db-name-matches-state",language,"Customer Name Trong is never translated",displayed=="Trong",new{Actual=displayed});form.Dispose();
    }
    private static async Task PreferenceDialog(MainForm main,CafeManager.WinForms.Modules.System.SystemInfoControl info,string language)
    {
        var originalLanguage=LanguageService.Current;var opposite=originalLanguage==AppLanguage.English?AppLanguage.Vietnamese:AppLanguage.English;
        var choice=UiProbe.Desc(info).OfType<ComboBox>().Single();choice.SelectedIndex=opposite==AppLanguage.English?1:0;
        var apply=UiProbe.Desc(info).OfType<Button>().Single();var modalFound=false;var ownerCorrect=false;
        using var timer=new System.Windows.Forms.Timer{Interval=30};
        timer.Tick+=(_,_)=>
        {
            var dialog=Application.OpenForms.OfType<OrbitConfirmationDialog>().FirstOrDefault();if(dialog is null)return;
            modalFound=true;ownerCorrect=ReferenceEquals(dialog.Owner,main)&&!dialog.ShowInTaskbar;
            var later=UiProbe.Desc(dialog).OfType<Button>().FirstOrDefault(b=>b.Text==L.T("Để sau"));later?.PerformClick();
        };
        timer.Start();apply.PerformClick();
        for(var i=0;i<50&&!apply.Enabled;i++)await Task.Delay(30);
        timer.Stop();
        var preference=new CafeManager.WinForms.Configuration.LanguagePreference();
        Program.Result("preference.defer."+main.Text,language,"Save preference, Later keeps session language; native modal owner is MainForm",modalFound&&ownerCorrect&&LanguageService.Current==originalLanguage&&preference.Read()==opposite,new{ModalFound=modalFound,OwnerCorrect=ownerCorrect,Current=LanguageService.Current.ToString(),Saved=preference.Read().ToString()});
    }
    private static async Task ModalOwner()
    {
        using var main=new MainForm();main.Show();var seen=false;var correct=false;
        using var timer=new System.Windows.Forms.Timer{Interval=30};timer.Tick+=(_,_)=>
        {var dialog=Application.OpenForms.OfType<OrbitConfirmationDialog>().FirstOrDefault();if(dialog is null)return;seen=true;correct=ReferenceEquals(dialog.Owner,main)&&dialog.Modal&&!dialog.ShowInTaskbar;UiProbe.Desc(dialog).OfType<Button>().Last().PerformClick();};
        timer.Start();var result=await OrbitConfirmationDialog.ShowAsync(main,"Fixture confirmation","Fixture",MessageBoxButtons.YesNo);timer.Stop();
        Program.Result("modal-owner-result-once","both","Native modal has owner and cancellation returns No once",seen&&correct&&result==DialogResult.No,new{Seen=seen,OwnerAndModalCorrect=correct,Result=result.ToString()});main.Dispose();
    }
    private static async Task ScrollReachability(ConnectionFactory factory,PermissionService permission,SessionContext session,string language)
    {
        var reports=new ReportService(new ReportRepository(factory),permission);
        var modules=Enum.GetValues<NavigationTarget>().ToDictionary(t=>t,t=>(Func<UserControl>)(()=>new UserControl()));
        modules[NavigationTarget.Home]=()=>new CafeManager.WinForms.Modules.Home.AdminDashboardControl(reports,session);
        using var main=new MainForm(session,permission,new AuthRepository(factory).ReadStateAsync,()=>new UserControl(),modules);
        main.Show();await Task.Delay(400);
        foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
        {
            main.Location=Screen.FromControl(main).WorkingArea.Location;main.Size=size;await Task.Delay(150);
            var before=new List<string>();var after=new List<string>();var count=0;
            foreach(var button in UiProbe.Desc(main).OfType<Button>().Where(b=>b.Visible).ToArray())
            {
                count++;
                if(!Screen.FromControl(main).WorkingArea.IntersectsWith(button.RectangleToScreen(button.ClientRectangle)))before.Add(button.Text);
                for(var parent=button.Parent;parent is not null;parent=parent.Parent)
                    if(parent is ScrollableControl {AutoScroll:true} scroll)scroll.ScrollControlIntoView(button);
                await Task.Delay(5);
                var bounds=button.RectangleToScreen(button.ClientRectangle);
                if(!Screen.FromControl(main).WorkingArea.IntersectsWith(bounds))after.Add(button.Text);
            }
            Program.Result("layout.scroll-reachability."+size.Width,language,"Scrollable Home/sidebar buttons can be brought into WorkingArea; no screenshot/visual judgment",after.Count==0,new{Count=count,OutsideBeforeScroll=before,OutsideAfterScroll=after,Dpi=main.DeviceDpi});
        }
        main.Dispose();
    }
}
