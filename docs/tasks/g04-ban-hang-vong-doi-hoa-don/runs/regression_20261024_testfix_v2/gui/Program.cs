using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Modules.Sales;
internal static class Program
{
 [STAThread] static void Main(string[] args)
 {
  ApplicationConfiguration.Initialize();
  var evidence = new Dictionary<string,object?> { ["Scope"]="Actual CheckoutDialog keyboard/validation UI with isolated in-memory order; no login/SQL/commit claim",["Pid"]=Environment.ProcessId };
  var factory=new ConnectionFactory("Server=localhost;Database=CoffeeManager_UnusedProbe;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
  using var session=new SessionContext(new AccountIdentity(1,"NV001","Fixture GUI","fixture","ADMIN",true,true,new byte[8]));
  var service=new SalesService(new SalesRepository(factory),new PermissionService(new AuthRepository(factory)));
  var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
  var o=new SalesOrderDto(h,[new(1,"SP001","Cà phê fixture tiếng Việt",1,81000,"Ít đá, không đường",81000)],[]);
  var p=new CheckoutPreview(h.Code,h.Revision,81000,0,0,81000,"None",0,0,0,81,new byte[32]);
  var type=typeof(SalesControl).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
  using var dialog=(Form)Activator.CreateInstance(type,service,session,o,p,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
  object? Field(string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(dialog);
  var samples=new List<object>();
  using var timer=new System.Windows.Forms.Timer{Interval=1000};
  var started=DateTime.UtcNow;
  timer.Tick+=(_,_)=>{
   evidence["DeviceDpi"]=dialog.DeviceDpi;
   samples.Add(new{Time=DateTime.UtcNow,State=((Label)Field("state")!).Text,ConfirmEnabled=((Button)Field("confirm")!).Enabled,Busy=Field("busy"),Unknown=Field("unknown"),Tendered=((TextBox)Field("tendered")!).Text,Rows=((DataGridView)Field("grid")!).Rows.Count});
   evidence["Samples"]=samples;
   File.WriteAllText(args[0],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
   if(DateTime.UtcNow-started>TimeSpan.FromMinutes(8)){session.Revoke();dialog.Close();}
  };
  dialog.Shown+=(_,_)=>timer.Start();
  Application.Run(dialog);timer.Stop();
  evidence["Closed"]=true;evidence["ElapsedSeconds"]=(DateTime.UtcNow-started).TotalSeconds;
  try {
   var many=o with {Header=h with {Status="Da thanh toan",PaidAt=DateTime.Now},Lines=Enumerable.Range(1,100).Select(i=>new SalesLine(i,"SP001","Cà phê tiếng Việt "+i,1,81000,"Ít đá, không đường — ghi chú Việt",81000)).ToArray()};
   using var doc=new CafeManager.WinForms.Printing.InvoicePrintDocument(new(many,"Checkout","None",0,81,DateTime.Now));
   var controller=new System.Drawing.Printing.PreviewPrintController();doc.PrintController=controller;
   doc.Print();var pages1=controller.GetPreviewPageInfo();evidence["PrintPages1"]=pages1.Length;foreach(var page in pages1)page.Image.Dispose();
   doc.Print();var pages2=controller.GetPreviewPageInfo();evidence["PrintPages2"]=pages2.Length;foreach(var page in pages2)page.Image.Dispose();
   evidence["PrintCursor"]=typeof(CafeManager.WinForms.Printing.InvoicePrintDocument).GetField("cursor",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(doc);evidence["PrinterJobSent"]=false;
  }catch(Exception e){evidence["PrintError"]=e.ToString();}
  File.WriteAllText(args[0],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
 }
}
