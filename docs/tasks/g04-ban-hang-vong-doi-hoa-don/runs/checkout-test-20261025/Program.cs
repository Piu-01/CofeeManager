using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;

internal static class Program
{
    private static readonly List<object> Results=[];private static int failed;
    private static readonly Type DialogType=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        var folder=args[0]; Directory.CreateDirectory(folder);
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedCheckoutComponent;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
        var service=new SalesService(new SalesRepository(f),new PermissionService(new AuthRepository(f)));
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Fixture component","fixture","CASH",true,true,new byte[8]));
        Form New(decimal payable=81000)
        {
            var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,payable,0,0,payable,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
            var order=new SalesOrderDto(h,[],[]);var preview=new CheckoutPreview(h.Code,h.Revision,payable,0,0,payable,"None",0,0,0,0,new byte[32]);
            return (Form)Activator.CreateInstance(DialogType,service,session,order,preview,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        }
        void Case(string name,Action test){try{test();Record(name,true);}catch(Exception ex){Record(name,false,(ex.InnerException??ex).ToString());}Save(folder);}
        void Draft(Form dialog,params (string Method,string Amount,string? Reference)[] values)
        {
            var rows=Field<System.Collections.IList>(dialog,"rows");rows.Clear();
            var draftType=DialogType.GetNestedType("PaymentDraft",BindingFlags.Public)!;
            foreach(var value in values){var row=Activator.CreateInstance(draftType)!;draftType.GetProperty("Method")!.SetValue(row,value.Method);draftType.GetProperty("Amount")!.SetValue(row,value.Amount);draftType.GetProperty("Reference")!.SetValue(row,value.Reference);rows.Add(row);}
            Invoke(dialog,"UpdateSummary");
        }
        void Check(bool value,string message="Assertion failed"){if(!value)throw new InvalidOperationException(message);}
        Console.WriteLine("Constructing frozen CheckoutDialog...");
        Save(folder);
        try
        {
            using var dialog=New();
            Console.WriteLine("Constructor returned.");
            using var timer=new System.Windows.Forms.Timer{Interval=700};
            timer.Tick+=(_,_)=>
            {
                timer.Stop();
                var confirm=Field<Button>(dialog,"confirm");var tendered=Field<TextBox>(dialog,"tendered");var grid=Field<DataGridView>(dialog,"grid");
                Case("C01 initial invoice values",()=>{Check(confirm.Enabled);Check(Field<Label>(dialog,"payableValue").Text=="81.000 ₫");Check(Field<Label>(dialog,"allocatedValue").Text=="81.000 ₫");});
                Case("C01 tender100000/change19000",()=>{tendered.Text="100000";Check(confirm.Enabled);Check(Field<Label>(dialog,"differenceValue").Text=="19.000 ₫");});
                Case("C02 tender80000 disabled/short1000",()=>{tendered.Text="80000";Check(!confirm.Enabled);Check(Field<Label>(dialog,"differenceValue").Text.Contains("1.000"));});
                foreach(var value in new[]{"abc",new string('9',30),"-1","1,5"})Case("C02 invalid tender "+value,()=>{tendered.Text=value;Check(!confirm.Enabled);Check(!string.IsNullOrWhiteSpace(Field<Label>(dialog,"state").Text));Check(tendered.Text==value);});
                tendered.Text="100000";
                foreach(var method in new[]{"","PT010","XXXXX"})Case("C02 invalid method "+method,()=>{Draft(dialog,(method,"81000",null));Check(!confirm.Enabled);});
                foreach(var amount in new[]{"80000","82000","-1","1,5",new string('9',30),"abc"})Case("C02 invalid allocation "+amount,()=>{Draft(dialog,("PT001",amount,null));Check(!confirm.Enabled);});
                Case("C02 reference over100",()=>{Draft(dialog,("PT001","81000",new string('x',101)));Check(!confirm.Enabled);});
                Case("C05 mixed payment cash31000/bank50000",()=>{Draft(dialog,("PT001","31000",null),("PT002","50000","test"));tendered.Text="50000";Check(confirm.Enabled);Check(Field<Label>(dialog,"differenceValue").Text=="19.000 ₫");});
                Case("C05 bank only ignores invalid tender",()=>{Draft(dialog,("PT002","81000",null));tendered.Text="abc";Check(confirm.Enabled);Check(!tendered.Enabled);Check(Field<Label>(dialog,"receivedValue").Text=="Không áp dụng");});
                Case("C04 add/delete binding",()=>{Draft(dialog,("PT001","81000",null));tendered.Text="100000";Check(grid.Rows.Count==1);Draft(dialog,("PT001","31000",null),("PT002","50000",null));Check(grid.Rows.Count==2);Draft(dialog,("PT001","81000",null));Check(grid.Rows.Count==1&&confirm.Enabled);});
                Case("C07 busy/unknown controls",()=>{Invoke(dialog,"SetBusy",true);Check(!confirm.Enabled&&!grid.Enabled&&!Field<Button>(dialog,"back").Enabled);Set(dialog,"unknown",true);Invoke(dialog,"SetBusy",false);Check(!confirm.Enabled&&!grid.Enabled&&Field<Button>(dialog,"check").Enabled&&!Field<Button>(dialog,"back").Enabled);Set(dialog,"unknown",false);Invoke(dialog,"SetBusy",false);Check(confirm.Enabled&&grid.Enabled);});
                Case("C09 Payment grid editable",()=>Check(grid is OrbitDataGridView{Profile:OrbitGridProfile.Payment,ReadOnly:false}));
                foreach(var size in new[]{new Size(1280,720),new Size(1366,768),new Size(740,700)})Case("C08 reachable footer "+size,()=>{dialog.Size=size;dialog.PerformLayout();var bounds=dialog.RectangleToScreen(dialog.ClientRectangle);Check(bounds.Contains(confirm.RectangleToScreen(confirm.ClientRectangle)),"Confirm outside client");Check(bounds.Contains(Field<Button>(dialog,"back").RectangleToScreen(Field<Button>(dialog,"back").ClientRectangle)),"Back outside client");});
                Case("C08 actual125dpi",()=>Check(dialog.DeviceDpi==120,"Actual DPI="+dialog.DeviceDpi));
                Case("C05 zero payable",()=>{using var zero=New(0);Check(Field<Button>(zero,"confirm").Enabled);Check(Field<Button>(zero,"confirm").Text=="Chốt hóa đơn 0 ₫");Check(Field<System.Collections.IList>(zero,"rows").Count==0);});
                foreach(var profile in new[]{OrbitGridProfile.Menu,OrbitGridProfile.Inventory,OrbitGridProfile.Catalogs})Case("C09 existing grid "+profile,()=>{using var oldGrid=new OrbitDataGridView{Profile=profile};Check(oldGrid.ReadOnly);});
                Case("C09 existing dialog layout",()=>{using var form=new Form();using var body=new Panel();using var status=new Label();using var actions=new FlowLayoutPanel();actions.Controls.Add(new OrbitButton{Text="Lưu"});using var layout=new OrbitDialogLayout(form,body,status,actions);Check(layout.ColumnCount==1&&layout.RowCount==4);});
                Case("C10 repeated construction/disposal",()=>{for(var i=0;i<5;i++){using var repeat=New();Set(repeat,"saved",true);repeat.Dispose();Check(repeat.IsDisposed);}});
                Set(dialog,"saved",true);dialog.Close();
            };
            dialog.Shown+=(_,_)=>timer.Start();Application.Run(dialog);
        }
        catch(Exception ex){Record("Checkout construction/show",false,ex.ToString());}
        Save(folder);Console.WriteLine($"Component results {Results.Count-failed}/{Results.Count} PASS");return failed==0?0:1;
    }
    private static T Field<T>(Form f,string name)=>(T)DialogType.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(f)!;
    private static void Set(Form f,string name,object value)=>DialogType.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(f,value);
    private static void Invoke(Form f,string name,params object[] args)=>DialogType.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f,args);
    private static void Record(string name,bool ok,object? actual=null){if(!ok)failed++;Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
    private static void Save(string folder)=>File.WriteAllText(Path.Combine(folder,"component-results.json"),JsonSerializer.Serialize(new{Scope="Real frozen CheckoutDialog component; direct binding/state inspection, not mouse/keyboard, no SQL writes",Results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
}
