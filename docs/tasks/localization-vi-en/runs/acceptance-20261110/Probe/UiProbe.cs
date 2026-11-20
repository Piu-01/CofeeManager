using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Localization;
using CafeManager.Models.Requests.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;
using UI=CafeManager.WinForms.Modules;

internal static class UiProbe
{
    internal static IEnumerable<Control> Desc(Control root)
    {yield return root;foreach(Control child in root.Controls)foreach(var item in Desc(child))yield return item;}
    internal static object? Field(object instance,string name)
    {for(var type=instance.GetType();type is not null;type=type.BaseType){var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(field is not null)return field.GetValue(instance);}throw new MissingFieldException(name);}
    internal static void Inspect(Control control,string id,string language)
    {
        var texts=new List<string>();
        foreach(var c in Desc(control).Where(c=>c.Visible))
        {
            if(c is Form or Label or Button or CheckBox or TabPage)texts.Add(c.Text);
            if(c is DataGridView grid)foreach(DataGridViewColumn column in grid.Columns)if(column.Visible)texts.Add(column.HeaderText);
        }
        var banned=texts.Where(t=>Regex.IsMatch(t,"[Đđ\\u1ea0-\\u1ef9]")||t is "In…" or "SL").Distinct().ToArray();
        var english=language is "en" or "English";
        Program.Result(id+".text",language,"App-owned captions/headings in selected language",!english||banned.Length==0,new{Texts=texts,Banned=banned,BoundControls=Desc(control).OfType<DataGridView>().Select(g=>new{g.Name,Rows=g.Rows.Count,Columns=g.Columns.Count}).ToArray()});
    }
    internal static async Task Run(SqlConnection owner,string connection,string language,string output)
    {
        var factory=OperationProbe.RuntimeFactory(connection);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
        var actor=Convert.ToInt32(await StateSnapshot.Scalar(owner,"SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'"));
        using var session=new SessionContext((await auth.ReadIdentityAsync(actor,default))!);
        var reports=new ReportService(new ReportRepository(factory),permissions);
        var assets=new AssetService(new AssetRepository(factory),permissions);
        var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
        var materials=new MaterialService(new MaterialRepository(factory),permissions);var purchases=new PurchaseService(new PurchaseRepository(factory),permissions);
        var issues=new StockIssueService(new StockIssueRepository(factory),permissions);var stocktakes=new StocktakeService(new StocktakeRepository(factory),permissions);
        var debts=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);var finance=new FinanceService(new FinanceRepository(factory),permissions);
        var invoices=new InvoiceService(new InvoiceRepository(factory),permissions);var sales=new SalesService(new SalesRepository(factory),permissions);
        var customers=new CustomerService(new CustomerRepository(factory),permissions);var loyalty=new LoyaltyService(new LoyaltyRepository(factory),permissions);
        var tables=new TableService(new TableRepository(factory),permissions);var pending=new MemoryPending();
        var links=new DocumentLinks(purchases,finance,debts,session,payroll,assets);
        var attendance=new AttendanceService(new AttendanceRepository(factory),permissions);
        var sources=new UI.Reports.ReportSourceNavigation(session,links,invoices,issues,stocktakes,attendance);
        UserControl Inventory()=>new UI.Inventory.InventoryControl(materials,purchases,issues,stocktakes,session,pending,links);
        var modules=new Dictionary<NavigationTarget,Func<UserControl>>
        {
            [NavigationTarget.Home]=()=>new UI.Home.AdminDashboardControl(reports,session,sources),
            [NavigationTarget.Sales]=()=>new UI.Sales.SalesControl(sales,tables,customers,loyalty,session,invoices),
            [NavigationTarget.Invoices]=()=>new UI.Sales.InvoicesControl(invoices,session),
            [NavigationTarget.Delivery]=()=>new UI.Sales.DeliveryControl(new DeliveryService(new DeliveryRepository(factory),permissions),sales,invoices,session),
            [NavigationTarget.Products]=()=>new UI.Menu.ProductsControl(new ProductService(new ProductRepository(factory),permissions),session),
            [NavigationTarget.Tables]=()=>new UI.Tables.TablesControl(tables,session),
            [NavigationTarget.Customers]=()=>new UI.Customers.CustomersControl(customers,session),
            [NavigationTarget.Loyalty]=()=>new UI.Loyalty.LoyaltyControl(loyalty,customers,session),
            [NavigationTarget.Materials]=()=>new UI.Inventory.MaterialsControl(materials,session),
            [NavigationTarget.Inventory]=Inventory,
            [NavigationTarget.Finance]=()=>new UI.Finance.FinanceControl(finance,session,pending,links,assets),
            [NavigationTarget.Debt]=()=>new UI.Finance.SupplierDebtsControl(debts,session,pending,links),
            [NavigationTarget.HumanResources]=()=>new UI.HumanResources.HumanResourcesControl(new EmployeeService(new EmployeeRepository(factory),permissions),new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,new PasswordService()),session,attendance,payroll,links),
            [NavigationTarget.Assets]=()=>new UI.Assets.AssetsControl(assets,session,links),
            [NavigationTarget.Reports]=()=>new UI.Reports.ReportsControl(reports,session,sources)
        };
        using var main=new MainForm(session,permissions,auth.ReadStateAsync,()=>new UI.Catalogs.CatalogsControl(new CatalogService(new CatalogRepository(factory),permissions),session),modules);
        main.Show();await Task.Delay(600);
        Program.Result("actual-dpi",language,"Native DPI remains 125% (120), no OS Scale changes",main.DeviceDpi==120,new{main.DeviceDpi,WorkingArea=Screen.FromControl(main).WorkingArea.ToString()});
        foreach(var target in Enum.GetValues<NavigationTarget>())
        {
            try
            {
                await main.OpenWorkspaceAsync(target.ToString());await Task.Delay(400);
                Inspect(main,"module."+target,language);
                foreach(var tabs in Desc(main).OfType<TabControl>().Where(t=>t.Visible).ToArray())
                {
                    for(var i=0;i<tabs.TabCount;i++)
                    {tabs.SelectedIndex=i;await Task.Delay(250);Inspect(tabs,"module."+target+".tab"+i,language);}
                }
            }
            catch(Exception e){Program.Result("module."+target,language,"Module loads without error",false,new{Type=e.GetType().Name,Message=e.Message});}
        }
        await main.OpenWorkspaceAsync("Home");await Task.Delay(300);
        foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
        {
            main.Size=size;await Task.Delay(150);
            var controls=Desc(main).Where(c=>c.Visible&&c is Button&&c.Width>0&&c.Height>0).ToArray();
            var unreachable=controls.Where(c=>!Screen.FromControl(main).WorkingArea.IntersectsWith(c.RectangleToScreen(c.ClientRectangle))).Select(c=>c.Text).ToArray();
            Program.Result("layout.home."+size.Width,language,"Visible Home buttons intersect WorkingArea at actual 125%",unreachable.Length==0,new{Size=main.Size.ToString(),Dpi=main.DeviceDpi,Unreachable=unreachable});
        }
        foreach(var kind in Enum.GetValues<ReportKind>())
        {
            try{var page=await reports.ReadAsync(session,new(kind,DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),DateOnly.FromDateTime(DateTime.Today)));Program.Result("report."+kind,language,"Typed report loads",true,new{Rows=page.Rows.Count,Metadata=page.Metadata});}
            catch(Exception e){Program.Result("report."+kind,language,"Typed report loads",false,new{Type=e.GetType().Name,Message=e.Message});}
        }
        var paidCode=(string)(await StateSnapshot.Scalar(owner,"SELECT TOP(1) MaHD FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' ORDER BY MaHD DESC"))!;
        var detail=(await invoices.DetailAsync(session,paidCode))!;
        using(var document=new CafeManager.WinForms.Printing.InvoicePrintDocument(detail))
        {
            var lines=(IEnumerable<string>)Field(document,"lines")!;
            Program.Result("print-content",language,"Invoice content keeps amounts/snapshots; EN-owned lines translated",language!="en"||!lines.Any(l=>Regex.IsMatch(l,"[Đđ\\u1ea0-\\u1ef9]")&&!l.Contains("Ít đường")&&!l.Contains("Không đá")),new{document.DocumentName,Lines=lines.ToArray()});
        }
        using(var preview=new InvoicePrintPreviewForm(detail,invoices,session)){preview.Show(main);await Task.Delay(200);Inspect(preview,"invoice-preview",language);preview.Dispose();}
        using(var picker=new CustomerPickerDialog(customers,session,true)){picker.Show(main);await Task.Delay(250);Inspect(picker,"customer-picker",language);picker.Dispose();}
        using(var editor=new CatalogEditorDialog(new CatalogService(new CatalogRepository(factory),permissions),session,CafeManager.Models.Dtos.Catalogs.CatalogKind.ExpenseCategory,null))
        {editor.Show(main);await Task.Delay(100);var type=(ComboBox)Field(editor,"typeInput")!;type.SelectedIndex=1;var request=editor.GetType().GetMethod("Request",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(editor,null);Program.Result("expense-combo-code",language,"Localized expense label still creates ExpenseType.Chi",request is CafeManager.Models.Requests.Catalogs.ExpenseCategoryRequest{Type:CafeManager.Models.Dtos.Catalogs.ExpenseType.Chi},new{Label=type.Text});editor.Dispose();}
        main.Dispose();
    }
    internal static async Task LoginCases()
    {
        var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CoffeeManager","language.txt");
        var exists=File.Exists(path);var before=exists?File.ReadAllBytes(path):null;
        try
        {
            foreach(var initial in new[]{AppLanguage.Vietnamese,AppLanguage.English})
            {
                LanguageService.Set(initial);
                var error=L.T("Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.");
                using var login=new LoginForm(null,error);login.Show();await Task.Delay(100);
                var user=(TextBox)Field(login,"usernameInput")!;var password=(TextBox)Field(login,"passwordInput")!;
                user.Text="loc.fixture";password.Text="synthetic-input-only";
                var target=initial==AppLanguage.Vietnamese?AppLanguage.English:AppLanguage.Vietnamese;
                login.GetType().GetMethod("ChangeLanguage",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(login,[target]);
                var actual=((Label)Field(login,"statusLabel")!).Text;var expected=L.T("Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.");
                Program.Result("login-relocalize-config-error",initial+"→"+target,"Configuration error also switches with login captions",actual==expected,new{Actual=actual,Expected=expected});
                Program.Result("login-keeps-input",initial.ToString(),"Language switch keeps username/password; no login",user.Text=="loc.fixture"&&password.Text=="synthetic-input-only",new{UsernamePreserved=user.Text=="loc.fixture",PasswordPreserved=password.Text=="synthetic-input-only"});
                Inspect(login,"login",target.ToString());login.Dispose();
            }
        }
        finally{if(exists){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,before!);}else if(File.Exists(path))File.Delete(path);}
    }
    private sealed class MemoryPending:IPendingOperationStore
    {
        private readonly Dictionary<(int,string),PendingOperation> data=[];
        public PendingOperation? Read(int actor,string operation)=>data.GetValueOrDefault((actor,operation));
        public void Save(PendingOperation operation)=>data[(operation.Actor,operation.Operation)]=operation;
        public void Clear(int actor,string operation)=>data.Remove((actor,operation));
    }
}
