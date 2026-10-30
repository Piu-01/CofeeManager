using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;

internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var factory=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedFeatureTest;Integrated Security=True;TrustServerCertificate=True");
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var header=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var preview=new CheckoutPreview(header.Code,header.Revision,81000,0,0,81000,"None",0,0,0,0,new byte[32]);
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var form=(Form)Activator.CreateInstance(type,new SalesService(new SalesRepository(factory),new PermissionService(new AuthRepository(factory))),session,new SalesOrderDto(header,[],[]),preview,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        T Field<T>(string name)=>(T)type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        var tender=Field<TextBox>("tendered");var grid=Field<DataGridView>("grid");var overlay=Field<Form>("stars");
        var results=new List<object>();var failed=0;var phase=0;
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Case(string name,Action action){try{action();results.Add(new{Name=name,Outcome="PASS"});}catch(Exception ex){failed++;results.Add(new{Name=name,Outcome="FAIL",Error=(ex.InnerException??ex).ToString()});}}
        void MouseUp()=>typeof(Control).GetMethod("OnMouseUp",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(tender,[new MouseEventArgs(MouseButtons.Left,1,3,3,0)]);
        void Burst()=>overlay.GetType().GetMethod("Burst")!.Invoke(overlay,[Field<Button>("confirm")]);
        using var timer=new System.Windows.Forms.Timer{Interval=700};
        timer.Tick+=(_,_)=>
        {
            switch(phase++)
            {
                case 0:
                    Case("125 percent",()=>Check(form.DeviceDpi==120,"DPI="+form.DeviceDpi));
                    Case("Focus selects all original amount",()=>{grid.Focus();tender.Focus();Check(tender.SelectionLength==tender.TextLength,"Not all selected");});
                    Case("First mouse-up restores selection after native caret placement",()=>{tender.Select(tender.TextLength,0);MouseUp();Check(tender.SelectionLength==tender.TextLength,"Mouse-up selection");});
                    Case("Second click allows caret",()=>{tender.Select(2,0);MouseUp();Check(tender.SelectionStart==2&&tender.SelectionLength==0,"Caret overwritten");});
                    Case("New entry replaces rather than appends",()=>{grid.Focus();tender.Focus();tender.SelectedText="100000";Check(tender.Text=="100000","Input appended");Check(Field<Label>("differenceValue").Text=="19.000 ₫","Wrong change");});
                    Case("Leave format and re-entry retain full selection",()=>{grid.Focus();Check(tender.Text=="100.000","Format");tender.Focus();Check(tender.SelectionLength==tender.TextLength,"Re-entry");});
                    Case("Overlay can show without stealing control focus",()=>{Burst();Check(overlay.Visible&&overlay.Enabled,"Overlay not shown");Check(!overlay.ContainsFocus&&tender.ContainsFocus,"Overlay took focus");});
                    Case("Overlay native no-activate and transparent flags",()=>{var p=(CreateParams)overlay.GetType().GetProperty("CreateParams",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(overlay)!;Check((p.ExStyle&0x08000020)==0x08000020,"Native flags missing");});
                    break;
                case 3:
                    Case("Overlay timer hides and releases particles",()=>{Check(!overlay.Visible,"Still visible after2100ms");var items=(System.Collections.IList)overlay.GetType().GetField("particles",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(overlay)!;Check(items.Count==0,"Particles retained");});
                    Case("Display failure cannot propagate or retain effect timer",()=>{overlay.Enabled=false;try{Burst();}finally{overlay.Enabled=true;}Check(!overlay.Visible,"Effect left visible after failed show");var effectTimer=(System.Windows.Forms.Timer)overlay.GetType().GetField("timer",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(overlay)!;Check(!effectTimer.Enabled,"Effect timer retained");});
                    Case("Effect can show after display failure",()=>{tender.Focus();Burst();Check(overlay.Visible&&!overlay.ContainsFocus,"Recovery");});
                    timer.Stop();type.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);form.Close();break;
            }
            File.WriteAllText(args[0],JsonSerializer.Serialize(new{Scope="Real frozen CheckoutDialog/overlay; direct focus and synthetic MouseUp handler, not physical mouse or Confirm/SQL",Results=results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        };
        form.Shown+=(_,_)=>timer.Start();Application.Run(form);
        Console.WriteLine($"Feature regression {results.Count-failed}/{results.Count} PASS");return failed==0?0:1;
    }
}

