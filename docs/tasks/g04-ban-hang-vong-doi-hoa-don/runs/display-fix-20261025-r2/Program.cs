using System.Collections;
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
using CafeManager.WinForms.Theme;

internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        var factory = new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedLayoutTests;Integrated Security=True;TrustServerCertificate=True");
        using var session = new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var header = new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var preview = new CheckoutPreview(header.Code,header.Revision,81000,0,0,81000,"None",0,0,0,0,new byte[32]);
        var type = typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var form = (Form)Activator.CreateInstance(type,new SalesService(new SalesRepository(factory),new PermissionService(new AuthRepository(factory))),session,new SalesOrderDto(header,[],[]),preview,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        T Field<T>(string name) => (T)type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        void Invoke(string name) => type.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null);
        var grid = Field<DataGridView>("grid"); var rows = Field<IList>("rows");
        var results = new List<object>(); var failed = 0; var phase = 0;
        bool Inside(Control c)
        {
            var bounds = c.RectangleToScreen(c.ClientRectangle);
            for (var p=c.Parent; p is not null; p=p.Parent)
                if (!p.RectangleToScreen(p.ClientRectangle).Contains(bounds)) return false;
            return true;
        }
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Case(string name, Action action)
        {
            try { action(); results.Add(new { Name=name, Outcome="PASS" }); }
            catch (Exception ex) { failed++; results.Add(new { Name=name, Outcome="FAIL", Error=(ex.InnerException??ex).ToString() }); }
        }
        void Reachable(string prefix)
        {
            foreach (var name in new[]{"grid","tendered","state","payableValue","allocatedValue","receivedValue","differenceValue","confirm","back","check","add","remove"})
                Case(prefix+"/"+name,()=>Check(Inside(Field<Control>(name)),name+" clipped"));
        }
        var viewport = grid.Parent!.Parent!.Parent!.Parent as Panel ?? throw new InvalidOperationException("Expected viewport");
        void Draft(int count)
        {
            grid.EndEdit(); rows.Clear();
            var draftType=type.GetNestedType("PaymentDraft",BindingFlags.Public)!;
            for(var i=0;i<count;i++)
            {
                var row=Activator.CreateInstance(draftType)!;
                draftType.GetProperty("Method")!.SetValue(row,"PT001");
                draftType.GetProperty("Amount")!.SetValue(row,(81000/count).ToString()); rows.Add(row);
            }
            Invoke("UpdateSummary");
        }
        void ScrollTo(Control target)
        {
            viewport.ScrollControlIntoView(target); viewport.PerformLayout();
        }
        using var timer=new System.Windows.Forms.Timer{Interval=700};
        timer.Tick+=(_,_)=>
        {
            switch(phase++)
            {
                case 0:
                    Case("DPI125",()=>Check(form.DeviceDpi==120,"Actual "+form.DeviceDpi));
                    Reachable("Default1150x750");
                    grid.CurrentCell=grid.Rows[0].Cells["Method"]; grid.BeginEdit(true);
                    break;
                case 1:
                    Reachable("After method focus");
                    Case("Themed owner-drawn editor and code/name binding",()=>
                    {
                        var combo=(ComboBox)grid.EditingControl!;
                        Check(combo.DrawMode==DrawMode.OwnerDrawFixed&&combo.BackColor==OrbitManagementTheme.Input,"Editor style");
                        combo.SelectedValue="PT002"; grid.EndEdit(); (form.BindingContext[rows] as CurrencyManager)?.EndCurrentEdit();
                        Check((string)rows[0]!.GetType().GetProperty("Method")!.GetValue(rows[0])! == "PT002","Code mapping");
                        Check(!Field<TextBox>("tendered").Enabled,"Bank cash field");
                    });
                    Case("Blank method friendly notice",()=>
                    {
                        rows[0]!.GetType().GetProperty("Method")!.SetValue(rows[0],""); Invoke("UpdateSummary");
                        Check(Field<Control>("state").Text=="Chọn phương thức thanh toán.","Friendly validation");
                        Check(!Field<Button>("confirm").Enabled,"Invalid cannot confirm");
                    });
                    Draft(1); form.Size=new Size(1280,720);
                    break;
                case 2:
                    Reachable("1280x720"); form.Size=new Size(1366,768); break;
                case 3:
                    Reachable("1366x768"); Draft(6); break;
                case 4:
                    Case("Six rows bounded to three visible rows",()=>Check(grid.Height<=grid.ColumnHeadersHeight+grid.RowTemplate.Height*3+SystemInformation.HorizontalScrollBarHeight+2,"Unbounded grid "+grid.Height));
                    Case("Sixth row reachable through internal grid scrolling",()=>{grid.FirstDisplayedScrollingRowIndex=5; Check(grid.Rows[5].Displayed,"Last row not displayed");});
                    ScrollTo(Field<Control>("state")); break;
                case 5:
                    Case("Multirow notice reachable via viewport scroll",()=>Check(Inside(Field<Control>("state")),"Notice inaccessible"));
                    Case("Multirow footer stays outside scrolling content",()=>Check(Inside(Field<Control>("confirm"))&&Inside(Field<Control>("back")),"Footer clipped"));
                    Draft(1); form.Size=new Size(740,700); ScrollTo(Field<Control>("tendered")); break;
                case 6:
                    Case("Narrow cash input reachable",()=>Check(Inside(Field<Control>("tendered")),"Cash input clipped"));
                    ScrollTo(Field<Control>("differenceValue")); break;
                case 7:
                    Case("Stacked summary reachable",()=>Check(Inside(Field<Control>("differenceValue")),"Summary clipped"));
                    Case("Narrow footer reachable",()=>Check(Inside(Field<Control>("confirm"))&&Inside(Field<Control>("back")),"Footer clipped"));
                    form.Size=new Size(740,500); ScrollTo(Field<Control>("state")); break;
                case 8:
                    Case("Short working area notice scrollable",()=>Check(Inside(Field<Control>("state")),"Notice inaccessible"));
                    Case("Short working area footer reachable",()=>Check(Inside(Field<Control>("confirm"))&&Inside(Field<Control>("back")),"Footer clipped"));
                    timer.Stop(); type.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true); form.Close(); break;
            }
            File.WriteAllText(args[0],JsonSerializer.Serialize(new { Scope="Dark mode, real WinForms component at125%; technical bounds/binding only, no visual QA or physical input/SQL checkout", Results=results, Failed=failed },new JsonSerializerOptions{WriteIndented=true}));
        };
        form.Shown+=(_,_)=>timer.Start(); Application.Run(form);
        Console.WriteLine($"Layout/component {results.Count-failed}/{results.Count} PASS"); return failed==0?0:1;
    }
}
