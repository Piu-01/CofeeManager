using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;

internal static class Program
{
    [STAThread]private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();var results=new List<object>();var failed=0;
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedEditorProbe;Integrated Security=True;TrustServerCertificate=True");
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var p=new CheckoutPreview(h.Code,h.Revision,81000,0,0,81000,"None",0,0,0,0,new byte[32]);var t=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var form=(Form)Activator.CreateInstance(t,new SalesService(new SalesRepository(f),new PermissionService(new AuthRepository(f))),session,new SalesOrderDto(h,[],[]),p,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        object Field(string name)=>t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        void Check(bool ok,string message="Assertion failed"){if(!ok)throw new InvalidOperationException(message);}
        void Test(string name,Action action){try{action();results.Add(new{Name=name,Outcome="PASS"});}catch(Exception ex){failed++;results.Add(new{Name=name,Outcome="FAIL",Error=(ex.InnerException??ex).ToString()});}File.WriteAllText(args[0],JsonSerializer.Serialize(new{Scope="Frozen actual DataGridView editing control and production key handler invoked as components, not physical key input",Results=results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));}
        using var timer=new System.Windows.Forms.Timer{Interval=700};timer.Tick+=(_,_)=>
        {
            timer.Stop();var grid=(DataGridView)Field("grid");var confirm=(Button)Field("confirm");var tendered=(TextBox)Field("tendered");
            Test("Editable amount live preview; appearance excluded",()=>{grid.CurrentCell=grid.Rows[0].Cells["Amount"];Check(grid.BeginEdit(true));var editor=(TextBox)grid.EditingControl!;editor.Text="80000";Check(!confirm.Enabled);Check(((Label)Field("allocatedValue")).Text=="80.000 ₫");});
            Test("Amount edit commits correct allocation",()=>{((TextBox)grid.EditingControl!).Text="81000";grid.EndEdit();var captured=(IReadOnlyList<PaymentAllocation>)t.GetMethod("CaptureAllocations",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;Check(captured.Count==1&&captured[0].MaPT=="PT001"&&captured[0].SoTien==81000);Check(confirm.Enabled);});
            Test("Method combo binds code, bank disables tender",()=>{grid.CurrentCell=grid.Rows[0].Cells["Method"];Check(grid.BeginEdit(true));var combo=(ComboBox)grid.EditingControl!;combo.SelectedValue="PT002";grid.EndEdit();tendered.Text="abc";Check(confirm.Enabled&&!tendered.Enabled);var captured=(IReadOnlyList<PaymentAllocation>)t.GetMethod("CaptureAllocations",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;Check(captured.Single().MaPT=="PT002");});
            Test("Reference edit length rejected by live validation",()=>{grid.CurrentCell=grid.Rows[0].Cells["Reference"];Check(grid.BeginEdit(true));((TextBox)grid.EditingControl!).Text=new string('x',101);Check(!confirm.Enabled);((TextBox)grid.EditingControl!).Text="ref";grid.EndEdit();Check(confirm.Enabled);});
            Test("Production Enter handler ends edit without checkout",()=>{grid.CurrentCell=grid.Rows[0].Cells["Amount"];Check(grid.BeginEdit(true));((TextBox)grid.EditingControl!).Text="82000"; var endEvents=0;grid.CellEndEdit+=(_,_)=>endEvents++;var msg=Message.Create(form.Handle,0x100,(IntPtr)13,IntPtr.Zero);object[] values=[msg,Keys.Enter];var consumed=(bool)t.GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,values)!;var bound=grid.Rows[0].DataBoundItem!;var amount=(string)bound.GetType().GetProperty("Amount")!.GetValue(bound)!;Check(consumed&&endEvents>0&&decimal.Parse(amount,System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))==82000,"Consumed="+consumed+";EndEvents="+endEvents+";BoundAmount="+amount+";Editor="+grid.EditingControl?.Text+";Notice="+((Label)Field("state")).Text);Check((Guid)Field("requestId")==Guid.Empty);Check((SalesOrderDto?)t.GetProperty("ResultOrder")!.GetValue(form) is null);});
            t.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);form.Close();
        };
        form.Shown+=(_,_)=>timer.Start();Application.Run(form);return failed==0?0:1;
    }
}


