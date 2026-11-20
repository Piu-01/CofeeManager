using System.Reflection;
using System.Runtime.InteropServices;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Assets;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Assets;
using CafeManager.WinForms.Modules.Reports;
using CafeManager.WinForms.UI;

// Kiểm contract binding/bounds/owner ở125%, không chụp hình hoặc nghiệm thu input/visual.
internal static class UiTechnicalProbe
{
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowEnabled(IntPtr hwnd);
    public static object Run(ConnectionFactory factory,PermissionService permissions,SessionContext session,AssetService assets,AssetRow asset)
    {
        var evidence=new Dictionary<string,object?>();Exception? failure=null;
        var thread=new Thread(()=>{
            MainForm? main=null;
            try{
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);main=new MainForm();main.ClientSize=new(1280,720);
                var reports=new ReportService(new ReportRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
                var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll,assets);
                var sources=new ReportSourceNavigation(session,links,new InvoiceService(new InvoiceRepository(factory),permissions),new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),new AttendanceService(new AttendanceRepository(factory),permissions));
                var tabs=new TabControl{Dock=DockStyle.Fill};var assetList=new AssetsControl(assets,session,links);var reportList=new ReportsControl(reports,session,sources);
                var assetTab=new TabPage("Tài sản");assetTab.Controls.Add(assetList);var reportTab=new TabPage("Báo cáo");reportTab.Controls.Add(reportList);tabs.TabPages.AddRange([assetTab,reportTab]);
                var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main)!;content.Controls.Add(tabs);
                using var watchdog=new System.Windows.Forms.Timer{Interval=20000};watchdog.Tick+=(_,_)=>{failure=new TimeoutException("UI technical probe timed out");Finish();};
                main.Shown+=async(_,_)=>{
                    watchdog.Start();try{
                        evidence["DeviceDpi"]=main.DeviceDpi;if(main.DeviceDpi!=120){evidence["Layout"]="NOT RUN: actual desktop is not125%; no scale change";return;}
                        await Wait(()=>Descendants(assetList).OfType<DataGridView>().Any(g=>g.DataSource is not null));
                        evidence["AssetBinding"]=Descendants(assetList).OfType<DataGridView>().First().Rows.Count;
                        tabs.SelectedTab=reportTab;await Task.Delay(300);Descendants(reportList).OfType<OrbitButton>().Single(b=>b.Text=="Xem báo cáo").PerformClick();
                        await Wait(()=>Descendants(reportList).OfType<DataGridView>().Any(g=>g.DataSource is not null));evidence["ReportBinding"]=Descendants(reportList).OfType<DataGridView>().First().Rows.Count;
                        var bounds=new List<object>();foreach(var size in new[]{new Size(1280,720),new Size(1366,768)}){main.ClientSize=size;await Task.Delay(100);var grid=Descendants(reportList).OfType<DataGridView>().First();if(grid.Width<=0||grid.Height<=0)throw new InvalidOperationException("Report grid has no space");bounds.Add(new{main.ClientSize,grid.Width,grid.Height});}evidence["TechnicalBounds"]=bounds;
                        using var detail=new AssetDetailDialog(asset,assets,session,links);detail.Shown+=(_,_)=>{evidence["ModalOwner"]=detail.Owner==main&&!IsWindowEnabled(main.Handle)&&!detail.ShowInTaskbar&&detail.FormBorderStyle!=FormBorderStyle.None&&detail.AcceptButton is null;detail.BeginInvoke((Action)(()=>detail.Close()));};await OrbitDialogHost.ShowAsync(detail,main);
                        if(Environment.GetEnvironmentVariable("G07_NATIVE_STOP") is string stop&&stop.Length>0){watchdog.Stop();evidence["NativeSession"]="Open for computer-use functional checks; separate transcript";File.WriteAllText(stop+".ready",DateTimeOffset.Now.ToString("O"));for(int tick=0;tick<480&&!File.Exists(stop);tick++)await Task.Delay(1000);evidence["NativeSessionStopped"]=File.Exists(stop);}
                    }catch(Exception e){failure=e;}finally{watchdog.Stop();Finish();}
                };
                Application.Run(main);
                void Finish(){if(main.IsDisposed)return;main.Dialogs.AbortAll();typeof(MainForm).GetField("closeApproved",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(main,true);main.Close();}
            }catch(Exception e){failure=e;}finally{main?.Dispose();}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(failure is not null)throw new InvalidOperationException("UI technical probe failed",failure);evidence["NativeInput"]="NOT RUN";evidence["Visual"]="SKIPPED by assignment";return evidence;
    }
    private static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var inner in Descendants(child))yield return inner;}}
    private static async Task Wait(Func<bool> ready){for(int i=0;i<100&&!ready();i++)await Task.Delay(100);if(!ready())throw new TimeoutException("Binding not ready");}
}
