using System.Reflection;
using System.Runtime.InteropServices;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.Modules.Reports;
using CafeManager.WinForms.UI;

internal static class DashboardUiProbe
{
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowEnabled(IntPtr hwnd);
    public static object Run(ConnectionFactory factory,PermissionService permissions,SessionContext session)
    {
        Exception? error=null;var evidence=new Dictionary<string,object?>();
        var thread=new Thread(()=>
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using var main=new MainForm();main.ClientSize=new(1366,768);
            var service=new ReportService(new ReportRepository(factory),permissions);
            var assets=new AssetService(new AssetRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
            var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll,assets);
            var navigation=new ReportSourceNavigation(session,links,new InvoiceService(new InvoiceRepository(factory),permissions),new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),new AttendanceService(new AttendanceRepository(factory),permissions));
            using var dashboard=new AdminDashboardControl(service,session,navigation);
            var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main)!;content.Controls.Add(dashboard);
            T Field<T>(string name)=>(T)typeof(AdminDashboardControl).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(dashboard)!;
            async Task Wait(Func<bool> condition)
            {for(var n=0;n<200;n++){if(condition())return;await Task.Delay(50);}throw new TimeoutException("Dashboard condition timed out: "+Field<Label>("state").Text);}
            using var watchdog=new System.Windows.Forms.Timer { Interval=45000 };watchdog.Tick+=(_,_)=>{error=new TimeoutException("Native probe watchdog");main.Close();};
            main.Shown+=async(_,_)=>
            {
                watchdog.Start();
                try
                {
                    evidence["DeviceDpi"]=main.DeviceDpi;
                    if(main.DeviceDpi!=120){evidence["Status"]="NOT RUN: actual DPI is not125%; no scale changes";return;}
                    await Wait(()=>Field<bool>("ready"));
                    var labels=Field<Dictionary<string,Label>>("values");if(labels.Count!=4||labels.Values.Any(v=>string.IsNullOrEmpty(v.AccessibleName)))throw new InvalidOperationException("KPI binding/accessibility");
                    Field<OrbitButton>("monthly").PerformClick();await Wait(()=>Field<bool>("ready")&&Field<bool>("month"));if(string.IsNullOrWhiteSpace(Field<Label>("periodLabel").Text))throw new InvalidOperationException("Month label missing");
                    Field<OrbitButton>("today").PerformClick();await Wait(()=>Field<bool>("ready")&&!Field<bool>("month"));
                    var bounds=new List<object>();
                    foreach(var width in new[]{1600,1150,700})
                    {
                        main.ClientSize=new(width,800);await Task.Delay(120);
                        var metrics=Field<TableLayoutPanel>("metrics");var charts=Field<TableLayoutPanel>("charts");
                        var logical=metrics.Width*96F/dashboard.DeviceDpi;var expected=logical>=1080?4:logical>=530?2:1;
                        if(metrics.ColumnCount!=expected||metrics.Controls.Count!=4||metrics.Controls.Cast<Control>().Any(c=>c.Width<=0||c.Height<=0))throw new InvalidOperationException("Technical KPI bounds");
                        if(charts.ColumnCount!=(charts.Width*96F/dashboard.DeviceDpi>=760?2:1))throw new InvalidOperationException("Technical chart columns");
                        bounds.Add(new{Width=width,KpiColumns=metrics.ColumnCount,ChartColumns=charts.ColumnCount});
                    }
                    evidence["TechnicalBounds"]=bounds;
                    var trend=Field<Control>("trend");trend.Focus();
                    trend.GetType().GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(trend,[new KeyEventArgs(Keys.End)]);
                    if(string.IsNullOrWhiteSpace(trend.AccessibleDescription))throw new InvalidOperationException("Chart keyboard description");
                    evidence["ChartDescription"]=trend.AccessibleDescription;
                    // Lần click thực qua handler; modal được đóng trên cùng UI thread sau khi quan sát owner.
                    bool modal=false;using var closer=new System.Windows.Forms.Timer { Interval=50 };
                    closer.Tick+=(_,_)=>
                    {
                        var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=main&&f.Owner==main);
                        if(dialog is null)return;
                        modal=dialog.Modal&&!dialog.ShowInTaskbar&&!IsWindowEnabled(main.Handle)&&dialog.FormBorderStyle!=FormBorderStyle.None;
                        closer.Stop();dialog.Close();
                    };
                    closer.Start();Field<Dictionary<string,OrbitButton>>("alerts")["orders.open"].PerformClick();
                    await Wait(()=>modal&&!main.Dialogs.IsOpen);evidence["OwnedSourceDialog"]=modal;
                    Field<OrbitButton>("refresh").PerformClick();await Wait(()=>Field<bool>("ready"));
                    dashboard.Hide();await Task.Delay(50);if(Field<bool>("ready"))throw new InvalidOperationException("Hidden snapshot retained");
                    dashboard.Show();await Wait(()=>Field<bool>("ready"));evidence["HideShowReload"]=true;evidence["Status"]="PASS technical/function subset; no visual acceptance";
                }
                catch(Exception e){error=e;}
                finally {watchdog.Stop();main.Close();}
            };
            Application.Run(main);
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        if(error is not null)throw new InvalidOperationException("Dashboard UI probe",error);return evidence;
    }
}
