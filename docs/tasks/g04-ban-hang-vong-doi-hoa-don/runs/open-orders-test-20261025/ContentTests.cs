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
    [STAThread]private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedContentFixture;Integrated Security=True;TrustServerCertificate=True");
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var service=new SalesService(new SalesRepository(f),new PermissionService(new AuthRepository(f)));
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.OpenOrdersDialog")!;
        var row=new SalesOrderRow("HD99999999",new DateTime(2026,10,25,9,7,0),"Mang di",null,null,123456789,"Chua thanh toan",null,new byte[8],1);
        Form New(SalesOrderRow data)
        {
            var form=(Form)Activator.CreateInstance(type,service,session)!;form.BindingContext=new BindingContext();form.CreateControl();
            var grid=Field<DataGridView>(form,"grid");grid.CreateControl();grid.DataSource=new List<SalesOrderRow>{data};
            type.GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);
            if(grid.Rows.Count>0)grid.CurrentCell=grid.Rows[0].Cells["Code"];
            type.GetMethod("UpdateSelection",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null);return form;
        }
        T Field<T>(Form form,string name)=>(T)type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        var results=new List<object>();var failed=0;
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Case(string name,Action action){try{action();results.Add(new{Name=name,Outcome="PASS"});}catch(Exception ex){failed++;results.Add(new{Name=name,Outcome="FAIL",Error=(ex.InnerException??ex).ToString()});}}
        Case("OO03 null customer and large typed money",()=>{using var form=New(row);var g=Field<DataGridView>(form,"grid");Check(g.Rows.Count==1,"No bound row");Check(Convert.ToString(g.Rows[0].Cells["Customer"].FormattedValue)=="—","Null customer");Check(Convert.ToString(g.Rows[0].Cells["Payable"].FormattedValue)=="123.456.789 ₫","Money");Check(g.Columns["Payable"].DefaultCellStyle.Alignment==DataGridViewContentAlignment.MiddleRight,"Money not right");});
        Case("OO03 long Vietnamese customer retains original code and date",()=>{var name="Khách hàng tiếng Việt ghi chú dài để kiểm tra cột";using var form=New(row with{Customer=name});var g=Field<DataGridView>(form,"grid");Check(Convert.ToString(g.Rows[0].Cells["Customer"].Value)==name,"Name altered");Check(Field<Label>(form,"selection").Text=="Đang chọn HD99999999","Code truncated");Check(Convert.ToString(g.Rows[0].Cells["Created"].FormattedValue)=="25/10/2026 09:07","Date");});
        Case("OO04 doubleclick actual row handler returns exact code",()=>{using var form=New(row);var g=Field<DataGridView>(form,"grid");typeof(DataGridView).GetMethod("OnCellDoubleClick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(g,[new DataGridViewCellEventArgs(0,0)]);Check((string?)type.GetProperty("SelectedCode")!.GetValue(form)==row.Code,"Wrong code");});
        Case("OO04 header doubleclick cannot open order",()=>{using var form=New(row);var g=Field<DataGridView>(form,"grid");typeof(DataGridView).GetMethod("OnCellDoubleClick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(g,[new DataGridViewCellEventArgs(0,-1)]);Check(type.GetProperty("SelectedCode")!.GetValue(form)is null,"Header opened row");});
        Case("OO04 loading selection cannot open stale row",()=>{using var form=New(row);type.GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,false);type.GetMethod("SelectOrder",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null);Check(type.GetProperty("SelectedCode")!.GetValue(form)is null,"Opened stale row");});
        Case("OO04 Escape handler closes without selected code",()=>{using var form=New(row);object[] values=[Message.Create(IntPtr.Zero,0,IntPtr.Zero,IntPtr.Zero),Keys.Escape];var handled=(bool)type.GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,values)!;Check(handled&&type.GetProperty("SelectedCode")!.GetValue(form)is null,"Escape selected order");});
        Case("OO06 repeated dispose cancels pending token safely",()=>{var form=New(row);var token=Field<CancellationTokenSource>(form,"lifetime").Token;form.Dispose();form.Dispose();Check(token.IsCancellationRequested&&form.IsDisposed,"Lifetime retained");});
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{Scope="Hidden real dialog components with synthetic DTO/session; Shown/reload never run, no auth/SQL/physical-input claim",Results=results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Content/handlers {results.Count-failed}/{results.Count} PASS");return failed==0?0:1;
    }
}
