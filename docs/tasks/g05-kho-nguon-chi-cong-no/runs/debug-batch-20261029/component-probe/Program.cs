using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Finance;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private sealed class NoPending : IPendingOperationStore
    {
        public PendingOperation? Read(int actor,string operation)=>null;
        public void Save(PendingOperation operation)=>throw new InvalidOperationException("Component probe must not write pending operations");
        public void Clear(int actor,string operation)=>throw new InvalidOperationException("Component probe must not clear pending operations");
    }
    [STAThread]
    private static void Main(string[] args)
    {
        if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Exact test database, marker, output");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
        using(var guard=new SqlConnection(cs)){guard.Open();using var cmd=guard.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G05TestMarker";if((string?)cmd.ExecuteScalar()!=args[1])throw new InvalidOperationException("Wrong marker");}
        var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
        using var host=new Form{ClientSize=new Size(1280,720),ShowInTaskbar=false,AutoScaleMode=AutoScaleMode.Dpi};
        var pages=new TabControl{Dock=DockStyle.Fill};pages.TabPages.Add(new TabPage("Initial"));host.Controls.Add(pages);var output=new List<object>();
        void Record(string part,bool ok,object actual)=>output.Add(new{Part=part,Result=ok?"PASS":"FAIL",Actual=actual});
        host.Shown+=async(_,_)=>
        {
            try
            {
                if(host.DeviceDpi!=120){output.Add(new{Part="DPI",Result="BLOCKED",Dpi=host.DeviceDpi});return;}
                using var c=new SqlConnection(cs);await c.OpenAsync();using var cmd=c.CreateCommand();cmd.CommandText="SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1";
                var actor=Convert.ToInt32(await cmd.ExecuteScalarAsync());using var session=new SessionContext((await auth.ReadIdentityAsync(actor,default))!);
                var finance=new FinanceService(new FinanceRepository(factory),permissions);var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);var issue=new StockIssueService(new StockIssueRepository(factory),permissions);var count=new StocktakeService(new StocktakeRepository(factory),permissions);var debt=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);
                var pending=new NoPending();var links=new DocumentLinks(purchase,finance,debt,session);
                (string Name,Control View)[] views=[("Finance",new FinanceControl(finance,session,pending,links)),("Purchases",new PurchasesControl(purchase,session,pending,links)),("Issues",new StockIssuesControl(issue,purchase,session,pending)),("Stocktakes",new StocktakesControl(count,purchase,session,pending)),("Debts",new SupplierDebtsControl(debt,session,pending,links))];
                foreach(var (name,view) in views){var page=new TabPage(name);page.Controls.Add(view);pages.TabPages.Add(page);}
                foreach(var (name,view) in views)
                {
                    pages.SelectedTab=(TabPage)view.Parent!;var workspace=Descendants(view).OfType<OrbitListWorkspace>().Single();await Loaded(workspace);
                    var grid=Descendants(view).OfType<DataGridView>().Single();
                    foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
                    {
                        host.ClientSize=size;workspace.PerformLayout();await Task.Yield();
                        var primary=workspace.PrimaryActions.Controls.Cast<Control>().Where(x=>x.Visible).ToArray();var actions=workspace.RowActions.Controls.Cast<Control>().Where(x=>x.Visible).ToArray();var filters=workspace.Filters.Controls.Cast<Control>().Where(x=>x.Visible).ToArray();
                        bool Fits(Control child)=>child.Width>0&&child.Height>0&&child.Parent!.ClientRectangle.Contains(child.Bounds);
                        Record("B01."+name+".Layout."+size.Width,primary.Length==1&&actions.Length>=2&&primary.All(Fits)&&actions.All(Fits)&&filters.All(Fits)&&workspace.Body.Height>0&&workspace.Pagination.Bottom<=workspace.Pagination.Parent!.Height,new{Dpi=host.DeviceDpi,Size=size,Primary=primary.Select(x=>new{x.Text,x.Width,x.Height}),ActionCount=actions.Length,FilterCount=filters.Length,Body=workspace.Body.ClientSize});
                    }
                    var first=Convert.ToString(grid.Rows[0].Cells[0].Value);var nextEnabled=workspace.Pagination.Next.Enabled;workspace.Pagination.Next.PerformClick();await Loaded(workspace);
                    var second=Convert.ToString(grid.Rows[0].Cells[0].Value);var previousEnabled=workspace.Pagination.Previous.Enabled;workspace.Pagination.Previous.PerformClick();await Loaded(workspace);
                    Record("B01."+name+".Pager",nextEnabled&&previousEnabled&&first!=second&&Convert.ToString(grid.Rows[0].Cells[0].Value)==first,new{First=first,Second=second,PreviousEnabled=previousEnabled,Rows=grid.Rows.Count});
                    if(name=="Finance")
                    {
                        var type=workspace.Filters.Controls.OfType<ComboBox>().First();type.SelectedIndex=1;workspace.RowActions.Controls.OfType<Button>().Single(x=>x.Text=="Tìm / Tải lại").PerformClick();await Loaded(workspace);
                        var rows=(IEnumerable)grid.DataSource!;Record("B01.Finance.Filter",rows.Cast<FinanceRow>().All(x=>x.Type=="Thu"),new{Rows=grid.Rows.Count,Filter=type.Text});
                    }
                }
            }
            catch(Exception e){output.Add(new{Part="Component",Result="FAIL",Error=e.ToString()});Environment.ExitCode=1;}
            finally{File.WriteAllText(args[2],JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true}));host.Close();}
        };
        Application.Run(host);
    }
    private static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    private static async Task Loaded(OrbitListWorkspace workspace)
    {
        var watch=Stopwatch.StartNew();do{await Task.Delay(30);if(watch.Elapsed.TotalSeconds>30)throw new TimeoutException(workspace.Status.Text);}while(workspace.Status.Text=="Đang tải…");
        if(workspace.Status.Text.StartsWith("Chưa cập nhật"))throw new InvalidOperationException(workspace.Status.Text);
    }
}
