using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;
internal static class Program
{
 sealed class Pending(PendingOperation? value=null):IPendingOperationStore
 { public PendingOperation? Read(int a,string op)=>value;public void Save(PendingOperation op)=>throw new Exception("Read-only probe");public void Clear(int a,string op)=>throw new Exception("Read-only probe"); }
 static readonly List<object> results=[];
 static object? Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
 static void Check(string id,bool ok,object? actual=null){results.Add(new{Id=id,Result=ok?"PASS":"FAIL",Actual=actual});if(!ok)Environment.ExitCode=1;}
 [STAThread]static void Main(string[] args)
 {
  if(args.Length!=3||args[0]!="CoffeeManager_Test_G05_20261029_194514_8bfcd1ce_demo"||args[1]!="20261029_194514_8bfcd1ce")throw new ArgumentException("Exact disposable test target required");
  Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
  using var host=new MainForm{Text="B06 component regression",ShowInTaskbar=false};
  host.Shown+=async(_,_)=>{
   try{
    var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
    using var connection=new SqlConnection(cs);await connection.OpenAsync();using var cmd=connection.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G05TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=args[1])throw new Exception("Wrong marker");
    var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);var purchase=new PurchaseService(new PurchaseRepository(factory),permission);var issue=new StockIssueService(new StockIssueRepository(factory),permission);
    using(var baseline=new OrbitDataGridView())Check("SharedDefaultReadOnly",baseline.ReadOnly);
    foreach(var (role,actor) in new[]{("ADMIN",9001001),("STORE",9001002)}){
     using var session=new SessionContext((await auth.ReadIdentityAsync(actor,default))!);
     foreach(var isPurchase in new[]{true,false}){
      using OrbitDialogForm dialog=isPurchase?new PurchaseEditorDialog(purchase,session,new Pending()):new StockIssueEditorDialog(issue,purchase,session,new Pending());
      var prefix=role+(isPurchase?".Purchase":".Issue");
      dialog.Shown+=async(_,_)=>{try{
       var until=DateTime.UtcNow.AddSeconds(30);while(Field(dialog,"busy") is true){if(DateTime.UtcNow>until)throw new TimeoutException();await Task.Delay(20);}
       var grid=(DataGridView)Field(dialog,"grid")!;var body=(Control)Field(dialog,"body")!;
       Check(prefix+".Editable",!grid.ReadOnly&&grid.EditMode==DataGridViewEditMode.EditOnEnter&&dialog.DeviceDpi==120);
       foreach(var name in new[]{"Code","Name","Unit"})Check(prefix+".Locked."+name,grid.Columns[name].ReadOnly);
       if(isPurchase)Check(prefix+".Immediate",((TextBox)Field(dialog,"immediate")!).ReadOnly==(role=="STORE"));
       Descendants(body).OfType<Button>().Single(b=>b.Text=="Thêm dòng").PerformClick();
       foreach(var column in isPurchase?new[]{"Quantity","Price"}:new[]{"Quantity"}){
        grid.CurrentCell=grid.Rows[0].Cells[column];grid.BeginEdit(true);await Task.Delay(50);
        var edit=grid.EditingControl as TextBox;
        Check(prefix+".Editor."+column,edit is not null&&edit.SelectionLength==edit.TextLength,new{edit?.TextLength,edit?.SelectionLength});
        if(edit is not null){edit.Text=column=="Price"?"10000":"1";Check(prefix+".EndEdit."+column,grid.EndEdit());}
       }
       Check(prefix+".BoundQuantity",Convert.ToDecimal(grid.Rows[0].Cells["Quantity"].Value)==1m);
       if(isPurchase){
        var read=dialog.GetType().GetMethod("ReadLines",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var lines=(List<PurchaseLine>)read.Invoke(dialog,null)!;Check(prefix+".Preview",await purchase.PreviewAsync(session,lines)==10000m);
        foreach(var invalid in new[]{0m,-1m,0.00001m}){grid.Rows[0].Cells["Quantity"].Value=invalid;bool rejected=false;try{read.Invoke(dialog,null);}catch(TargetInvocationException e)when(e.InnerException is ArgumentException){rejected=true;}Check(prefix+".InvalidQuantity."+invalid,rejected);}
        grid.Rows[0].Cells["Quantity"].Value=1m;grid.Rows[0].Cells["Price"].Value=0.5m;bool priceRejected=false;try{read.Invoke(dialog,null);}catch(TargetInvocationException e)when(e.InnerException is ArgumentException){priceRejected=true;}Check(prefix+".FractionalPrice",priceRejected);
       }
       Check(prefix+".EnterNotCommit",dialog.AcceptButton is null);
       dialog.GetType().GetField("unknown",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(dialog,true);dialog.GetType().GetMethod("State",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(dialog,null);
       Check(prefix+".UnknownDisablesBody",!body.Enabled);
      }catch(Exception e){Check(prefix+".Exception",false,e.ToString());}finally{typeof(OrbitDialogForm).GetMethod("Abort",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(dialog,null);}};
      await OrbitDialogHost.ShowAsync(dialog,host);
     }
    }
    using var cash=new SessionContext((await auth.ReadIdentityAsync(9001003,default))!);bool denied=false;try{await purchase.LookupsAsync(cash,"Material");}catch(UserOperationException){denied=true;}Check("CASH.InventoryDenied",denied);
   }catch(Exception e){Check("Harness",false,e.ToString());}finally{File.WriteAllText(args[2],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));host.Close();}
  };Application.Run(host);
 }
 static IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Descendants(c))yield return child;}}
}
