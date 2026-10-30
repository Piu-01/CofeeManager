using System.Reflection;
using System.Text.Json;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;

internal static class Program
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object o,string name,Type? type=null)=>(T)(type??o.GetType()).GetField(name,Flags)!.GetValue(o)!;
    static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
    [STAThread] static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        using var main=new MainForm{Text="Delivery implementation technical layout smoke"};
        using var page=new DeliveryControl(null!,null!,null!,null!){Dock=DockStyle.Fill};
        var content=Field<Panel>(main,"content");content.Controls.Add(page);
        var workspace=Field<OrbitListWorkspace>(page,"Workspace",typeof(OrbitRecordListControl));var grid=Field<OrbitDataGridView>(page,"Grid",typeof(OrbitRecordListControl));
        var results=new List<object>();int failed=0;
        void Check(string name,bool ok,object? actual=null){if(!ok)failed++;results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
        bool Inside(Control control){var rect=control.RectangleToScreen(control.ClientRectangle);for(var p=control.Parent;p!=null;p=p.Parent)if(!p.RectangleToScreen(p.ClientRectangle).Contains(rect))return false;return true;}
        main.Shown+=async(_,_)=>
        {
            try
            {
                Check("DeviceDpi125",main.DeviceDpi==120,main.DeviceDpi);
                foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
                {
                    main.ClientSize=size;await Task.Yield();
                    var bodies=new List<Size>();
                    foreach(int count in new[]{0,1,50})
                    {
                        grid.DataSource=Enumerable.Range(1,count).Select(i=>new DeliveryRow("HD99999999",DateTime.Now,"Người nhận có tên dài để mở chi tiết","0900000026","Địa chỉ giao hàng dài, không được làm grid hoặc footer mất vùng hiển thị",null,"Dang giao","Chua thanh toan",new byte[8],count)).ToList();
                        page.PerformLayout();workspace.PerformLayout();await Task.Yield();bodies.Add(workspace.Body.Size);
                        Check($"{size} {count} module fills content",page.Bounds==content.ClientRectangle,new{Page=page.Bounds.ToString(),Content=content.ClientRectangle.ToString()});
                        Check($"{size} {count} grid fills body",grid.Bounds==workspace.Body.ClientRectangle,new{Grid=grid.Bounds.ToString(),Body=workspace.Body.ClientRectangle.ToString()});
                        Check($"{size} {count} grid to footer",workspace.Pagination.Top-workspace.Body.Bottom is >=0 and <=2&&workspace.Pagination.Bottom<=workspace.Pagination.Parent!.Height,new{BodyBottom=workspace.Body.Bottom,FooterTop=workspace.Pagination.Top,FooterBottom=workspace.Pagination.Bottom,CardHeight=workspace.Pagination.Parent.Height});
                        Check($"{size} {count} no spare horizontal allocation",grid.Columns["Address"]!.AutoSizeMode==DataGridViewAutoSizeColumnMode.Fill&&grid.Columns.Cast<DataGridViewColumn>().Sum(c=>c.Width)+grid.RowHeadersWidth>=grid.ClientSize.Width-3,new{Columns=grid.Columns.Cast<DataGridViewColumn>().Sum(c=>c.Width),Viewport=grid.ClientSize.Width});
                    }
                    Check($"{size} empty/single/many keep same body",bodies.Distinct().Count()==1,bodies.Select(s=>s.ToString()));
                    results.Add(new { Observation = $"{size} available body height", Height = workspace.Body.Height, Requirement = "Fills remaining card between actions and footer; no fixed minimum200px specified" });
                    foreach(Control filter in workspace.Filters.Controls)Check($"{size} filter {filter.GetType().Name} inside",Inside(filter));
                    foreach(Control action in workspace.RowActions.Controls)Check($"{size} action {action.Text} inside",Inside(action));
                    Check($"{size} pager inside",Inside(workspace.Pagination));
                    var search=Children(workspace).OfType<OrbitSearchBox>().Single();var searchRoot=search.Parent!;
                    Check($"{size} search full remaining row",workspace.SearchActions.Left-searchRoot.Right is >=0 and <=12&&searchRoot.Width>workspace.Filters.Width/2&&workspace.Filters.Top>=searchRoot.Bottom,new{Search=searchRoot.Bounds.ToString(),Actions=workspace.SearchActions.Bounds.ToString(),Filters=workspace.Filters.Bounds.ToString()});
                    Check($"{size} full code",grid.Columns["Code"]!.Width>=TextRenderer.MeasureText("HD99999999",grid.Columns["Code"]!.DefaultCellStyle.Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+grid.DefaultCellStyle.Padding.Horizontal);
                    Check($"{size} phone remains text",grid.Rows[0].Cells["Phone"].FormattedValue?.ToString()=="0900000026");
                }
                Check("Delivery profile readonly and stacked",grid.Profile==OrbitGridProfile.Delivery&&grid.ReadOnly&&workspace.StackedTools&&workspace.SearchActions.Controls.Count==2);
                Check("Address fill and Code frozen",grid.Columns["Address"]!.AutoSizeMode==DataGridViewAutoSizeColumnMode.Fill&&grid.Columns["Code"]!.Frozen);
                Check("Plain filters",workspace.Filters.Controls.OfType<OrbitFilterBox>().All(f=>f.Plain)&&workspace.Filters.Controls.OfType<OrbitFilterBox>().Count()==2);
                // Cùng control/bounds: profile trước đây không có cột Name nên không cột nào fill.
                grid.Profile=OrbitGridProfile.Catalogs;var oldColumns=grid.Columns.Cast<DataGridViewColumn>().Sum(c=>c.Width);var oldFill=grid.Columns.Cast<DataGridViewColumn>().Any(c=>c.AutoSizeMode==DataGridViewAutoSizeColumnMode.Fill);
                grid.Profile=OrbitGridProfile.Delivery;results.Add(new{Observation="Previous generic profile on same six columns",OldColumns=oldColumns,OldHadFill=oldFill,Viewport=grid.ClientSize.Width,NewColumns=grid.Columns.Cast<DataGridViewColumn>().Sum(c=>c.Width),Note="Technical allocation comparison, not visual acceptance or proof of prior screenshot height"});
            }
            catch(Exception e){Check("Runner exception",false,e.ToString());}
            finally{File.WriteAllText(Path.Combine(args[0],"layout-smoke.json"),JsonSerializer.Serialize(new{Failed=failed,Scope="Constructor/layout only, null service/session, no query/auth/SQL/business action/visual QA",Results=results},new JsonSerializerOptions{WriteIndented=true}));main.Dispose();Application.ExitThread();}
        };
        Application.Run(main);return failed==0?0:1;
    }
}

