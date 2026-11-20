using System.Diagnostics;
using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.HumanResources;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static partial class Program
{
    static async Task Supplement()
    {
        var cs=$"Server=localhost;Initial Catalog={database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False;Connect Timeout=5";
        using var owner=new SqlConnection(cs);await owner.OpenAsync();using(var marker=owner.CreateCommand()){marker.CommandText="SELECT RunId FROM dbo.UiTabsTestMarker";if((string?)await marker.ExecuteScalarAsync()!=runId)throw new InvalidOperationException("Marker mismatch.");}
        var hash=await DataHash(owner);var gated=false;var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var hit=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory=new ConnectionFactory(cs);
        typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(connection,token)=>{if(gated){hit.TrySetResult();await gate.Task.WaitAsync(token);}using var init=connection.CreateCommand();init.CommandText="EXECUTE AS USER='UiTabsRuntime';";await init.ExecuteNonQueryAsync(token);}));
        var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);
        var purchases=new PurchaseService(new PurchaseRepository(factory),permissions);var employees=new EmployeeService(new EmployeeRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
        var links=new DocumentLinks(purchases,new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll);
        var modules=new Dictionary<NavigationTarget,Func<UserControl>>{
            [NavigationTarget.Inventory]=()=>new InventoryControl(new MaterialService(new MaterialRepository(factory),permissions),purchases,new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),session,new MemoryPending(),links),
            [NavigationTarget.HumanResources]=()=>new HumanResourcesControl(employees,new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,new PasswordService()),session,new AttendanceService(new AttendanceRepository(factory),permissions),payroll,links)};
        using var form=new MainForm(session,permissions,auth.ReadStateAsync,null,modules);
        async Task Permission(string procedure,bool deny){if(procedure is not ("sp_MaterialList" or "sp_EmployeeList"))throw new InvalidOperationException("Procedure not allowlisted.");using var command=owner.CreateCommand();command.CommandText=(deny?"DENY":"REVOKE")+" EXECUTE ON dbo."+procedure+(deny?" TO":" FROM")+" UiTabsRuntime;";await command.ExecuteNonQueryAsync();}
        static Control Page(Control module)=>module.Controls.OfType<TabControl>().Single().SelectedTab!.Controls[0];
        static OrbitListWorkspace Workspace(Control body)=>Desc(body).OfType<OrbitListWorkspace>().Single();
        static DataGridView Grid(Control body)=>Desc(body).OfType<DataGridView>().First();
        static void Refresh(Control body)=>Desc(body).OfType<Button>().Single(b=>b.AccessibleName=="Tải lại danh sách").PerformClick();
        try
        {
            form.Show();await Wait(()=>Field<Label>(form,"statusLabel").Text=="Phiên đang hoạt động.");await form.OpenWorkspaceAsync("Inventory");var inventory=Current(form)!;await Ready(inventory);var stock=Page(inventory);var grid=Grid(stock);var initialRows=grid.Rows.Count;
            Check("Data-bearing stock fixture",initialRows>0);
            await Permission("sp_MaterialList",true);Refresh(stock);await Wait(()=>Workspace(stock).Status.Text.StartsWith("Chưa cập nhật"));
            Check("Stock SQL error explicit, keeps old rows",grid.Rows.Count==initialRows&&Workspace(stock).Status.Text.Contains("Chưa cập nhật"));
            await Permission("sp_MaterialList",false);Refresh(stock);await Wait(()=>Workspace(stock).Status.Text.StartsWith("Đã cập nhật"));Check("Stock retry after SQL recovery",grid.Rows.Count==initialRows);
            var search=Desc(stock).OfType<TextBox>().First(t=>t.PlaceholderText.StartsWith("Tìm"));search.Text="NO_MATCH_UI_TABS_6719";Refresh(stock);await Wait(()=>Workspace(stock).Status.Text.StartsWith("Không tìm thấy"));Check("Empty query differs from SQL failure",grid.Rows.Count==0&&!Workspace(stock).Status.Text.StartsWith("Chưa cập nhật"));
            search.Text="";Refresh(stock);await Wait(()=>Workspace(stock).Status.Text.StartsWith("Đã cập nhật"));
            var tabs=inventory.Controls.OfType<TabControl>().Single();gated=true;tabs.SelectedIndex=1;await hit.Task.WaitAsync(TimeSpan.FromSeconds(5));tabs.SelectedIndex=0;gated=false;gate.TrySetResult();await Ready(inventory);await Task.Delay(150);
            Check("Rapid tab switch cancels old load, retains current stock",tabs.SelectedIndex==0&&Grid(Page(inventory)).Columns.Contains("Quantity")&&ReferenceEquals(stock,Page(inventory)));
            tabs.SelectedIndex=1;await Ready(inventory);Check("Cancelled purchase tab can load on revisit",Grid(Page(inventory)).DataSource is not null);
            var current=Current(form);using(var modal=new OrbitDialogForm{Text="Tab regression modal",ClientSize=new Size(300,180)}){modal.Shown+=async(_,_)=>{Check("Modal owner remains MainForm",modal.Owner==form);await form.OpenWorkspaceAsync("HumanResources");Check("Modal blocks module switch",ReferenceEquals(Current(form),current));modal.Close();};await OrbitDialogHost.ShowAsync(modal,form);}
            await Permission("sp_EmployeeList",true);await form.OpenWorkspaceAsync("HumanResources");var hr=Current(form)!;await Wait(()=>hr.Controls.OfType<TabControl>().Any(t=>t.SelectedTab?.Controls.Count==1));var staff=Page(hr);await Wait(()=>Workspace(staff).Status.Text.StartsWith("Chưa cập nhật"));
            Check("HR SQL failure not false empty success",Grid(staff).DataSource is null&&Workspace(staff).Status.Text.StartsWith("Chưa cập nhật"));
            await Permission("sp_EmployeeList",false);Refresh(staff);await Wait(()=>Workspace(staff).Status.Text.StartsWith("Đã cập nhật"));Check("HR recovers without rebuilding module",Grid(staff).Rows.Count>0&&ReferenceEquals(staff,Page(hr)));
            await form.OpenWorkspaceAsync("Inventory");tabs.SelectedIndex=0;await Ready(inventory);
            gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);hit=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);gated=true;tabs.SelectedIndex=2;await hit.Task.WaitAsync(TimeSpan.FromSeconds(5));session.Revoke();gated=false;gate.TrySetResult();await Wait(()=>form.IsDisposed);await Task.Delay(100);
            Check("Session revoke during page load disposes form safely",form.IsDisposed&&session.IsRevoked);
            Check("Supplement leaves business data unchanged",hash==await DataHash(owner));
        }
        finally{gated=false;gate.TrySetResult();await Permission("sp_MaterialList",false);await Permission("sp_EmployeeList",false);if(!form.IsDisposed){typeof(MainForm).GetField("closeApproved",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(form,true);form.Dialogs.AbortAll();form.Close();}}
    }
}
