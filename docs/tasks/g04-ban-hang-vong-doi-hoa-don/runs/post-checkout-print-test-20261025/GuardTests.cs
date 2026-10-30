using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.Printing;

internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedResetProbe;Integrated Security=True;TrustServerCertificate=True");
        var permissions=new PermissionService(new AuthRepository(f));
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        using var pos=new SalesControl(new SalesService(new SalesRepository(f),permissions),new TableService(new TableRepository(f),permissions),new CustomerService(new CustomerRepository(f),permissions),new LoyaltyService(new LoyaltyRepository(f),permissions),session);
        var t=typeof(SalesControl);var results=new List<object>();var failed=0;
        object? Field(string name)=>t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(pos);
        void Set(string name,object? value)=>t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(pos,value);
        void Call(string name,params object[] values)=>t.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(pos,values);
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Case(string name,Action action){try{action();results.Add(new{Name=name,Outcome="PASS"});}catch(Exception ex){failed++;results.Add(new{Name=name,Outcome="FAIL",Error=(ex.InnerException??ex).ToString()});}}
        IEnumerable<Control> Descendants(Control c)=>c.Controls.Cast<Control>().SelectMany(child=>new[]{child}.Concat(Descendants(child)));
        var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001","KH001",null,null,null,"Mang di",null,null,null,null,DateTime.Now,0,0,81000,0,0,81000,"Da thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var order=new SalesOrderDto(h,[new SalesLine(1,"SP001","Cà phê đen",1,81000,"G04 ít đá, không đường",81000)],[new SalesPayment(1,"PT001","Tiền mặt",81000,null)]);
        Set("order",order);Set("draft",new OrderHeaderInput("Mang di",Customer:"KH001"));Set("headerDraft",new OrderHeaderInput("Mang di",Customer:"KH001"));Set("productDraft",("SP001",1,(string?)"Ghi chú nháp"));Set("lineDraft",new OrderLineRequest(h.Code,h.Revision,Guid.NewGuid(),"Update",LineId:1,Quantity:2));
        Case("PC06 POS has no Xem/In action",()=>Check(!Descendants(pos).OfType<Button>().Any(b=>b.Text=="Xem / In"),"Old invoice action remains"));
        Case("PC07 fixture holds draft before reset",()=>Check(pos.HasUnsavedChanges,"Fixture missing draft"));
        Case("PC01 Reset clears paid order and all drafts even while busy",()=>{Call("SetBusy",true);Call("ResetDraft");Check(Field("order") is null&&!pos.HasUnsavedChanges,"Order/draft retained");foreach(var n in new[]{"draft","headerDraft","productDraft","lineDraft"})Check(Field(n) is null,n+" retained");Check(pos.IsWriting,"Reset unlocked caller while printing");});
        Case("PC01 Empty order panel",()=>{var panel=(Control)Field("orderPanel")!;Check(panel.Controls.OfType<DataGridView>().Single().DataSource is null,"Old rows retained");Check(panel.Controls.OfType<Label>().Any(l=>l.Text.Contains("Chưa tạo đơn")),"Old caption retained");});
        Case("PC07 New draft actions enabled after caller completes",()=>{Call("SetBusy",false);Check(!pos.IsWriting&&((Button)Field("header")!).Enabled&&!((Button)Field("pay")!).Enabled,"Wrong new draft actions");});
        foreach(var mode in new[]{"busy","unknown","draft","paid"})
        {
            Case("PC05 guard retains order: "+mode,()=>
            {
                pos.DiscardChanges();Set("order",mode=="paid"?order:order with { Header=h with { Status="Chua thanh toan", PaidAt=null } });
                Set("unknown",mode=="unknown");Call("SetBusy",mode=="busy");
                if(mode=="draft")Set("productDraft",("SP001",1,(string?)"Unsaved"));
                var previous=Field("order");
                ((Task)t.GetMethod("Checkout",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(pos,null)!).GetAwaiter().GetResult();
                Check(ReferenceEquals(previous,Field("order")),"Guard cleared/replaced order");
                if(mode=="draft")Check(pos.HasUnsavedChanges,"Draft cleared before commit");
            });
        }
        Set("unknown",false);Call("ResetDraft");Call("SetBusy",false);
        var detail=new InvoiceDetailDto(order,"Confirmed","None",0,0,DateTime.Now);
        using var first=new InvoicePrintDocument(detail,reprint:false);using var again=new InvoicePrintDocument(detail);
        string Heading(InvoicePrintDocument d)=>(string)typeof(InvoicePrintDocument).GetField("heading",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(d)!;
        var lines=(List<string>)typeof(InvoicePrintDocument).GetField("lines",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(first)!;
        Case("PC03 First print has no IN LAI",()=>Check(!Heading(first).Contains("IN LẠI"),"First print mislabeled"));
        Case("PC06 History print is marked IN LAI",()=>Check(Heading(again).Contains("IN LẠI"),"Reprint label missing"));
        Case("PC03 Invoice content uses amount81000 and Vietnamese note",()=>{Check(lines.Contains("PHẢI TRẢ: 81.000 ₫"),"Wrong payable");Check(lines.Contains("Tiền mặt: 81.000 ₫"),"Wrong payment");Check(lines.Any(l=>l.Contains("G04 ít đá, không đường")),"Note missing");Check(!lines.Any(l=>l.Contains("100.000")||l.Contains("19.000")),"Invented tender/change");});
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{Scope="Production POS reset component and print content; no modal selection or physical print/SQL",Results=results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Reset/print component {results.Count-failed}/{results.Count} PASS");return failed==0?0:1;
    }
}

