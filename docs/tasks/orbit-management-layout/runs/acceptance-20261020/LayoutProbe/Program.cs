using System.Text.Json;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Catalogs;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Modules.Menu;
using CafeManager.WinForms.Modules.Customers;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var results=new List<object>();
        void Save()=>File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        foreach(var entry in new (string Name,Func<Control> Create)[]{("Layout",()=>new OrbitManagementLayout(new TextBox(),new Label())),("RecordList",()=>new RecordListControl()),("MainForm",()=>new MainForm()),("Catalogs",()=>new CatalogsControl()),("Materials",()=>new MaterialsControl()),("Products-empty",()=>new ProductsControl()),("Customers-empty",()=>new CustomersControl())})
        {
            try{var c=entry.Create();using var host=new Form{ClientSize=new Size(1100,640)};if(c is not Form)host.Controls.Add(c);c.CreateControl();c.PerformLayout();if(c is Form)c.Dispose();results.Add(new{Id=entry.Name,Status="PASS",Dpi=c.DeviceDpi});}
            catch(Exception ex){results.Add(new{Id=entry.Name,Status="FAIL",Error=ex.ToString()});}
            Save();
        }
        Console.WriteLine(JsonSerializer.Serialize(results));
        Environment.ExitCode=results.Any(r=>(string)r.GetType().GetProperty("Status")!.GetValue(r)! =="FAIL")?1:0;
    }
}


