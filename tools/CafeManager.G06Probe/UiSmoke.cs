using System.Reflection;
using System.Runtime.InteropServices;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.HumanResources;
using CafeManager.WinForms.UI;

// Smoke control thật trên UI thread125%; không chụp hình, không nghiệm thu native input.
internal static class UiSmoke
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr handle);
    internal static Dictionary<string, object?> Run(EmployeeService employees, EmployeeAccountService accounts, AttendanceService attendance,
        PayrollService payroll, SessionContext session, DocumentLinks links, HumanResourcesContext context, PayrollRow row)
    {
        Dictionary<string,object?> result=[]; Exception? error=null;
        var thread=new Thread(()=>
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                using var main=new MainForm(); main.ClientSize=new(1280,720);
                using var module=new HumanResourcesControl(employees,accounts,session,attendance,payroll,links);
                var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!; content.Controls.Add(module); module.BringToFront();
                main.Shown+=async (_,_)=>
                {
                    try
                    {
                        await Task.Delay(800);
                        var tabs=module.Controls.OfType<TabControl>().Single();
                        var bindings=new List<object>();
                        foreach(TabPage tab in tabs.TabPages)
                        {
                            tabs.SelectedTab=tab; await Task.Delay(250); if(tab.Controls.Count!=1)throw new InvalidOperationException("Tab không có module.");
                            var list=tab.Controls.OfType<OrbitRecordListControl>().Single();
                            var grid=(DataGridView)typeof(OrbitRecordListControl).GetField("Grid",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(list)!;
                            for(var wait=0;grid.DataSource is null && wait<40;wait++)await Task.Delay(100);
                            if(grid.DataSource is null || grid.Width<=0 || grid.Height<=0)throw new InvalidOperationException("Grid binding/layout chưa sẵn sàng.");
                            bindings.Add(new{Tab=tab.Text,Rows=grid.Rows.Count,grid.Width,grid.Height});
                        }
                        result["Bindings"]=bindings;
                        var viewports=new List<object>();
                        foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
                        {
                            main.ClientSize=size; await Task.Delay(120);
                            viewports.Add(new{main.ClientSize.Width,main.ClientSize.Height,ModuleWidth=module.Width,ModuleHeight=module.Height});
                        }
                        result["Viewports"]=viewports;
                        result["DeviceDpi"]=main.DeviceDpi; result["Tabs"]=tabs.TabPages.Cast<TabPage>().Select(t=>t.Text).ToArray();
                        result["ClientWidth"]=main.ClientSize.Width; result["ClientHeight"]=main.ClientSize.Height;
                        using var detail=new PayrollDetailDialog(row,links);
                        bool observed=false; detail.Shown+=(_,_)=> { observed=detail.Owner==main && !IsWindowEnabled(main.Handle) && !detail.ShowInTaskbar && detail.AcceptButton is null && detail.FormBorderStyle!=FormBorderStyle.None; result["ModalActual"]=new{OwnerMatches=detail.Owner==main,NativeOwnerEnabled=IsWindowEnabled(main.Handle),detail.ShowInTaskbar,AcceptButtonIsNull=detail.AcceptButton is null,Border=detail.FormBorderStyle.ToString()}; detail.BeginInvoke((Action)(()=>detail.Close())); };
                        await OrbitDialogHost.ShowAsync(detail,main);
                        if(!observed || !main.Enabled)throw new InvalidOperationException("Owner/modal contract failed.");
                        result["PayrollModalOwner"]=observed;
                        main.Dialogs.AbortAll(); typeof(MainForm).GetField("closeApproved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(main,true); main.Close();
                    }
                    catch(Exception e){error=e; main.Dialogs.AbortAll(); typeof(MainForm).GetField("closeApproved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(main,true);main.Close();}
                };
                Application.Run(main);
            }
            catch(Exception e){error=e;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        if(error is not null)throw new InvalidOperationException("UI technical smoke failed.",error);
        result["Kind"]="Component WinForms real controls; not native keyboard/visual acceptance";return result;
    }
}
