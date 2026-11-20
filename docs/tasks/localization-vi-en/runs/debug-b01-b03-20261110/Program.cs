using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Localization;
using CafeManager.WinForms.Forms;

internal static class Regression
{
    static readonly List<object> results=[];
    static IEnumerable<Control> Desc(Control root){yield return root;foreach(Control child in root.Controls)foreach(var c in Desc(child))yield return c;}
    static object Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(o)!;
    static void Check(string id,bool ok,object actual)=>results.Add(new{Id=id,Status=ok?"PASS":"FAIL",Actual=actual});
    [STAThread]static void Main(string[] args)
    {
        var output=Path.GetFullPath(args[0]);var root=Path.GetFullPath(args[1]);
        var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CoffeeManager","language.txt");
        var existed=File.Exists(path);var backup=existed?File.ReadAllBytes(path):null;
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetColorMode(SystemColorMode.Dark);
        using var host=new MainForm();
        host.Shown+=async(_,_)=>
        {
            try
            {
                var program=File.ReadAllText(Path.Combine(root,"src/CafeManager.WinForms/Program.cs"));
                Check("B03-production-templates",Regex.Matches(program,"configurationError\\s*(?:=|\\?\\?=)\\s*\"[^\"]+\"").Count==3&&!Regex.IsMatch(program,"configurationError\\s*(?:=|\\?\\?=)\\s*L.T"),new{ExpectedRawTemplates=3});
                var templates=new[]{"Chưa có cấu hình development hợp lệ trên tài khoản Windows này.","Cấu hình kết nối chưa hợp lệ; kiểm tra cấu hình local.","Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu."};
                for(var index=0;index<templates.Length;index++)foreach(var initial in Enum.GetValues<AppLanguage>())
                {
                    LanguageService.Set(initial);using var login=new LoginForm(null,templates[index]);login.Show();await Task.Delay(40);
                    var user=(TextBox)Field(login,"usernameInput");var pass=(TextBox)Field(login,"passwordInput");
                    user.Text="loc.regression";pass.Text="synthetic-preserved-only";
                    foreach(var target in new[]{initial==AppLanguage.English?AppLanguage.Vietnamese:AppLanguage.English,initial})
                    {
                        login.GetType().GetMethod("ChangeLanguage",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(login,[target]);
                        var status=((Label)Field(login,"statusLabel")).Text;
                        Check($"B03-{index}-{initial}-{target}",status==L.T(templates[index])&&login.Text==L.T("CoffeeManager · Đăng nhập")&&user.Text=="loc.regression"&&pass.Text=="synthetic-preserved-only"&&!((Button)Field(login,"loginButton")).Enabled,new{Expected=L.T(templates[index]),Actual=status,InputPreserved=user.Text=="loc.regression"&&pass.Text=="synthetic-preserved-only",LoginEnabled=((Button)Field(login,"loginButton")).Enabled});
                    }
                    login.Dispose();
                }
                var now=new DateTime(2026,11,10,10,0,0);
                var header=new SalesOrderHeader("HD20261110",now,"NV001",null,null,null,null,"Mang di",null,null,null,null,now,0,0,81000,0,0,81000,"Da thanh toan",new byte[8],true,"None",0,now);
                var detail=new InvoiceDetailDto(new(header,[new(1,"SP001","Món Việt",3,27000,"Ghi chú Việt",81000)],[new(1,"PT001","Cash",81000,null)]),"New","None",0,81,now);
                using var session=new SessionContext(new AccountIdentity(1,"NV001","Fixture","fixture","ADMIN",true,true,new byte[8]));
                var assembly=typeof(LoginForm).Assembly;
                foreach(var language in Enum.GetValues<AppLanguage>())
                {
                    LanguageService.Set(language);var qty=language==AppLanguage.English?"QTY":"SL";
                    using var panel=(Control)Activator.CreateInstance(assembly.GetType("CafeManager.WinForms.Modules.Sales.OrderPanel")!,true)!;
                    host.Controls.Add(panel);var grid=(DataGridView)panel.GetType().GetField("Lines",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(panel)!;
                    grid.DataSource=detail.Order.Lines.ToList();await Task.Delay(40);
                    Check("B01-POS-"+language,grid.Columns["Quantity"]!.HeaderText==qty&&grid.Columns["Quantity"]!.DataPropertyName=="Quantity"&&Convert.ToInt32(grid.Rows[0].Cells["Quantity"].Value)==3,new{Header=grid.Columns["Quantity"]!.HeaderText,Quantity=grid.Rows[0].Cells["Quantity"].Value,Dpi=host.DeviceDpi});
                    host.Controls.Remove(panel);
                    var type=assembly.GetType("CafeManager.WinForms.Modules.Sales.InvoiceDetailDialog")!;
                    using var dialog=(Form)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,[detail,null,session],null)!;
                    dialog.Show(host);await Task.Delay(40);var dg=Desc(dialog).OfType<DataGridView>().First();
                    Check("B01-detail-"+language,dg.Columns["Quantity"]!.HeaderText==qty&&Convert.ToInt32(dg.Rows[0].Cells["Quantity"].Value)==3,new{Header=dg.Columns["Quantity"]!.HeaderText,Quantity=dg.Rows[0].Cells["Quantity"].Value});dialog.Dispose();
                    using var preview=new InvoicePrintPreviewForm(detail,null!,session);preview.Show(host);await Task.Delay(60);
                    var print=Desc(preview).OfType<Button>().Single();
                    Check("B02-preview-"+language,print.Text==(language==AppLanguage.English?"Print…":"In…")&&preview.Text==L.T("Xem trước / In lại · ")+header.Code,new{Button=print.Text,preview.Text,preview.ShowInTaskbar,OwnerCorrect=ReferenceEquals(preview.Owner,host)});preview.Dispose();
                    Check("B01-02-data-"+language,detail.Order.Header.Payable==81000&&detail.Order.Lines[0].Name=="Món Việt"&&detail.Order.Lines[0].Note=="Ghi chú Việt",new{Payable=detail.Order.Header.Payable,NamesAndNotesPreserved=true});
                }
            }
            catch(Exception e){Check("unexpected-exception",false,new{Type=e.GetType().Name,Message=e.Message,Stack=e.StackTrace});}
            finally
            {
                if(existed)File.WriteAllBytes(path,backup!);else if(File.Exists(path))File.Delete(path);
                File.WriteAllText(Path.Combine(output,"component-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
                Environment.ExitCode=results.Any(r=>(string?)r.GetType().GetProperty("Status")!.GetValue(r)=="FAIL")?1:0;
                host.Dispose();Application.ExitThread();
            }
        };
        Application.Run(host);
    }
}
