using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;

// Smoke kỹ thuật trong pha triển khai: không SQL, không input native hay nghiệm thu hình thức.
internal static class Program
{
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr handle);
    private static readonly List<object> Results=[];
    private static int failed;
    private static void Check(string name,bool ok,object? actual=null){if(!ok)failed++;Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
    private static async Task Wait(Func<bool> ready){for(var i=0;i<250;i++){if(ready())return;await Task.Delay(20);}throw new TimeoutException("Native modal deadline");}
    private static IEnumerable<Control> Children(Control root){foreach(Control c in root.Controls){yield return c;foreach(var n in Children(c))yield return n;}}
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var main=new MainForm{Text="Native dialog — technical smoke"};
        using var deadline=new System.Windows.Forms.Timer{Interval=25000};
        deadline.Tick+=(_,_)=>{Check("Runner deadline",false);main.Dispose();Application.ExitThread();};
        main.Shown+=async(_,_)=>
        {
            deadline.Start();
            try
            {
                using(var parent=new OrbitDialogForm{Text="Parent"})
                {
                    var opened=OrbitDialogHost.ShowAsync(parent,main);await Wait(()=>parent.Visible&&parent.Modal);
                    Check("Native title/X frame and ownerMain",parent.TopLevel&&parent.FormBorderStyle!=FormBorderStyle.None&&parent.ControlBox&&parent.Owner==main);
                    Check("No taskbar entry",!parent.ShowInTaskbar);
                    Check("Windows locks main owner",!IsWindowEnabled(main.Handle)&&IsWindowEnabled(parent.Handle));
                    Check("Main lookup through owner chain",OrbitDialogHost.FindMainForm(parent)==main);
                    using(var child=new OrbitDialogForm{Text="Child"})
                    {
                        var nested=OrbitDialogHost.ShowAsync(child,parent);await Wait(()=>child.Visible&&child.Modal);
                        Check("Native child belongs to parent",child.Owner==parent&&child.TopLevel&&!child.ShowInTaskbar);
                        Check("Only top native dialog enabled",!IsWindowEnabled(main.Handle)&&!IsWindowEnabled(parent.Handle)&&IsWindowEnabled(child.Handle));
                        Check("One taskbar form",Application.OpenForms.Cast<Form>().Count(f=>f.ShowInTaskbar)==1);
                        Check("Native printer owner is active child",ReferenceEquals(OrbitDialogHost.NativeOwner(child),child));
                        child.DialogResult=DialogResult.OK;child.Close();
                        Check("Native OK result preserved",await nested.WaitAsync(TimeSpan.FromSeconds(5))==DialogResult.OK);
                        Check("Windows re-enables parent only",IsWindowEnabled(parent.Handle)&&!IsWindowEnabled(main.Handle));
                    }
                    parent.Close();Check("Native Cancel completes",await opened.WaitAsync(TimeSpan.FromSeconds(5))==DialogResult.Cancel);
                    Check("Main enabled after modal returns",IsWindowEnabled(main.Handle)&&!main.Dialogs.IsOpen);
                }
                using(var draft=new DraftDialog{Text="Dirty draft"})
                {
                    var opened=OrbitDialogHost.ShowAsync(draft,main);await Wait(()=>draft.Visible&&draft.Modal);
                    Results.Add(new { Diagnostic="Before Close", ClosingGuard=(bool)typeof(OrbitDialogForm).GetField("closing",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(draft)!, Visible=draft.Visible, Result=draft.DialogResult.ToString() });draft.Close();await Wait(()=>Application.OpenForms.OfType<OrbitConfirmationDialog>().Any(f=>f.Visible));
                    var confirmation=Application.OpenForms.OfType<OrbitConfirmationDialog>().Single();
                    Check("Discard prompt native owner",confirmation.Owner==draft&&!confirmation.ShowInTaskbar&&confirmation.TopLevel);
                    Children(confirmation).OfType<Button>().Single(b=>b.Text=="Không").PerformClick();
                    await Wait(()=>IsWindowEnabled(draft.Handle)&&!Application.OpenForms.OfType<OrbitConfirmationDialog>().Any());
                    Check("No discard keeps draft/resultNone",draft.Visible&&draft.Modal&&!opened.IsCompleted&&draft.DialogResult==DialogResult.None);
                    Results.Add(new { Diagnostic="Before Close", ClosingGuard=(bool)typeof(OrbitDialogForm).GetField("closing",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(draft)!, Visible=draft.Visible, Result=draft.DialogResult.ToString() });draft.Close();await Wait(()=>Application.OpenForms.OfType<OrbitConfirmationDialog>().Any(f=>f.Visible));
                    confirmation=Application.OpenForms.OfType<OrbitConfirmationDialog>().Single();
                    Children(confirmation).OfType<Button>().Single(b=>b.Text=="Có").PerformClick();
                    Check("Accept discard closes exactly once",await opened.WaitAsync(TimeSpan.FromSeconds(5))==DialogResult.Cancel&&!main.Dialogs.IsOpen);
                }
                using(var parent=new OrbitDialogForm())
                using(var child=new OrbitDialogForm())
                {
                    var one=OrbitDialogHost.ShowAsync(parent,main);await Wait(()=>parent.Visible);
                    var two=OrbitDialogHost.ShowAsync(child,parent);await Wait(()=>child.Visible);
                    main.Dialogs.AbortAll();
                    Check("Abort completes child and parent",await two.WaitAsync(TimeSpan.FromSeconds(5))==DialogResult.Cancel&&await one.WaitAsync(TimeSpan.FromSeconds(5))==DialogResult.Cancel&&!main.Dialogs.IsOpen);
                }
                using(var queued=new OrbitDialogForm())
                {
                    var task=OrbitDialogHost.ShowAsync(queued,main);main.Dialogs.AbortAll();
                    Check("Abort queued dialog cannot show orphan",await task==DialogResult.Cancel&&!main.Dialogs.IsOpen);
                    await Task.Delay(30);Check("No modal remains",Application.OpenForms.Count==1);
                }
                Results.Add(new{DeviceDpi=main.DeviceDpi,Scope="Implementation smoke: native window metadata/OS enabled state and component closes; no visual QA or SQL"});
            }
            catch(Exception ex){Check("Runner exception",false,ex.ToString());}
            finally{deadline.Stop();main.Dispose();Application.ExitThread();}
        };
        Application.Run(main);
        File.WriteAllText(Path.Combine(args[0],"native-smoke.json"),JsonSerializer.Serialize(new{Failed=failed,Results},new JsonSerializerOptions{WriteIndented=true}));
        return failed==0?0:1;
    }
    private sealed class DraftDialog:OrbitDialogForm
    {
        protected override async Task<bool> CanCloseAsync()=>await OrbitConfirmationDialog.ShowAsync(this,"Bỏ nháp?","Nháp",MessageBoxButtons.YesNo)==DialogResult.Yes;
    }
}

