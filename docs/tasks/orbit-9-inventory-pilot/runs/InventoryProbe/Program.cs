using System.Reflection;
using System.Text.Json;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Configuration;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;

static class Program
{
    static object? Field(object instance,string name)=>instance.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(instance);
    static IEnumerable<Control> All(Control control){foreach(Control child in control.Controls){yield return child;foreach(var nested in All(child))yield return nested;}}
    [STAThread] static void Main(string[] args)
    {
        if(args.Contains("--dpi96"))Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled); ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,TrustServerCertificate=true};
        using(var c=new SqlConnection(guard.ConnectionString)){c.Open();using var command=c.CreateCommand();command.CommandText="SELECT RunId FROM dbo.G03TestMarker";if(command.ExecuteScalar() as string!="20261019_215457_29e552a9")throw new Exception("Marker mismatch");}
        var connection=new SqlConnectionStringBuilder(DevelopmentConnection.Read()){InitialCatalog=db};
        var factory=new ConnectionFactory(connection.ConnectionString); var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
        var results=new List<object>();
        void Save()=>File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        using var host=new Form{Text="Inventory in-process regression",ClientSize=new Size(1166,718),ShowInTaskbar=false,Location=new Point(-10000,-10000),StartPosition=FormStartPosition.Manual};
        host.Shown+=async(_,_)=>{
            try{
                if(args.Contains("--crud"))
                {
                    var identity=(await auth.ReadCredentialAsync("admin"))!.Identity;using var session=new SessionContext(identity);
                    var service=new MaterialService(new MaterialRepository(factory),permissions);
                    string? code=null;var prefix="Orbit9UI-"+Guid.NewGuid().ToString("N")[..10];var created=false;
                    for(var i=99;i>=90;i--)if(await service.GetAsync(session,$"NVL{i:00}")==null){code=$"NVL{i:00}";break;}
                    if(code==null)throw new Exception("No free fixture code; do not touch existing data");
                    try
                    {
                        var createId=Guid.NewGuid();await service.SaveAsync(session,new(code,prefix,"Kg"),null,createId);created=true;
                        var row=(await service.GetAsync(session,code))!;results.Add(new{Case="Create",Pass=row.Name==prefix&&row.Quantity==0&&row.OpeningKind==null,Code=code,RequestId=createId});Save();
                        var updateId=Guid.NewGuid();await service.SaveAsync(session,new(code,prefix+" renamed","Kg"),row.Revision,updateId);
                        row=(await service.GetAsync(session,code))!;results.Add(new{Case="Update",Pass=row.Name==prefix+" renamed"&&row.Quantity==0,RequestId=updateId});Save();
                        var deleteId=Guid.NewGuid();await service.DeleteAsync(session,row,deleteId);created=false;
                        results.Add(new{Case="DeleteOwnedFixture",Pass=await service.GetAsync(session,code)==null,RequestId=deleteId});
                        var opened=(await service.ListAsync(session,"",0)).First(r=>r.OpeningKind!=null);
                        var denied=false;try{await service.OpenAsync(session,new(opened.Code,opened.Quantity,"Orbit9UI repeated opening rejection"),opened.Revision,Guid.NewGuid());}catch(UserOperationException){denied=true;}
                        var unchanged=(await service.GetAsync(session,opened.Code))!;
                        results.Add(new{Case="RepeatOpeningRejected",Pass=denied&&opened.Quantity==unchanged.Quantity&&opened.Revision.SequenceEqual(unchanged.Revision),Code=opened.Code,Before=opened.Quantity,After=unchanged.Quantity});
                    }
                    finally
                    {
                        if(created&&await service.GetAsync(session,code) is {} row&&row.Name.StartsWith(prefix)&&row.OpeningKind==null&&row.Quantity==0)await service.DeleteAsync(session,row,Guid.NewGuid());
                        results.Add(new{Case="FixtureCleanup",Pass=await service.GetAsync(session,code)==null,Code=code,Note="Only owned unused material deleted through existing BLL; receipts retained"});Save();
                    }
                    return;
                }                using(var shell=new MainForm()){shell.Show();await Task.Delay(100);results.Add(new{Case="Shell",Dpi=shell.DeviceDpi,Client=shell.ClientSize,Sidebar=((Control)Field(shell,"sidebar")!).Width});shell.Close();}
                if(args.Contains("--hover-only"))
                {
                    using var sidebar=new OrbitSidebar{FlowDirection=FlowDirection.TopDown};host.Controls.Add(sidebar);
                    sidebar.Controls.Add(new Label{Text="CoffeeManager"});sidebar.Controls.Add(new Label{Text="KHÔNG GIAN QUẢN LÝ"});
                    for(var i=0;i<8;i++)sidebar.Controls.Add(new Button{Text="Navigation "+i});
                    sidebar.Controls.Add(new Button{Name="logout",Text="Đăng xuất"});
                    sidebar.Person.Text="Nguyen Van An";sidebar.Role.Text="Quản trị viên · ADMIN";
                    sidebar.SessionStatus=new Label{Text="Phiên đang hoạt động."};sidebar.Controls.Add(sidebar.SessionStatus);
                    sidebar.Size=new Size(OrbitManagementTheme.Px(sidebar,200),OrbitManagementTheme.Px(sidebar,630));sidebar.PerformLayout();
                    results.Add(new{Case="SidebarShortViewport",Dpi=sidebar.DeviceDpi,Height=sidebar.Height,Client=sidebar.ClientSize,ProfileBottom=sidebar.SessionStatus.Bottom,Pass=sidebar.SessionStatus.Bottom<=sidebar.ClientSize.Height,Note="Owned sidebar with eight native navigation buttons and logout; unchanged button size; TopDown matches MainForm.Designer"});host.Controls.Remove(sidebar);Save();
                    host.Controls.Add(sidebar);sidebar.Refresh();
                    var oldAvatar=new Rectangle(OrbitManagementTheme.Px(sidebar,21),sidebar.Person.Top+OrbitManagementTheme.Px(sidebar,3),OrbitManagementTheme.Px(sidebar,32),OrbitManagementTheme.Px(sidebar,32));
                    var invalidatedOldAvatar=false;
                    sidebar.Invalidated+=(_,e)=>invalidatedOldAvatar|=e.InvalidRect.Contains(oldAvatar);
                    sidebar.Height=OrbitManagementTheme.Px(sidebar,900);sidebar.PerformLayout();
                    results.Add(new{Case="SidebarAvatarResizeInvalidation",Pass=invalidatedOldAvatar&&sidebar.Person.Top>oldAvatar.Top,OldAvatar=oldAvatar,NewProfileTop=sidebar.Person.Top,InvalidatedOldAvatar=invalidatedOldAvatar,Note="Owned native sidebar resize; verifies old painted avatar region invalidated, not desktop screenshot"});host.Controls.Remove(sidebar);Save();
                }
                foreach(var user in args.Contains("--hover-only") ? new[]{"admin"} : new[]{"admin","thungan","kho"}){
                    var identity=(await auth.ReadCredentialAsync(user))!.Identity;using var session=new SessionContext(identity);
                    var material=new MaterialsControl(new MaterialService(new MaterialRepository(factory),permissions),session){Dock=DockStyle.Fill};host.Controls.Add(material);
                    await (Task)material.GetType().GetMethod("LoadRows",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(material,null)!;
                    await Task.Delay(200);material.PerformLayout();
                    var grid=(DataGridView)Field(material,"grid")!;var units=(ComboBox)Field(material,"unitFilter")!;
                    var workspace=All(material).OfType<OrbitListWorkspace>().Single();
                    results.Add(new{Case="Loaded",Role=identity.Role,Dpi=material.DeviceDpi,Font=grid.DefaultCellStyle.Font?.Size,HeaderFont=grid.ColumnHeadersDefaultCellStyle.Font?.Size,Rows=grid.Rows.Count,Count=workspace.Count.Text,Range=workspace.Pagination.Range.Text,Columns=grid.Columns.Cast<DataGridViewColumn>().Select(c=>new{c.Name,c.Width}).ToArray(),Buttons=All(material).OfType<Button>().Where(b=>b.Visible).Select(b=>new{b.Text,b.AccessibleName,b.Width,b.Height,b.Enabled,Bounds=b.Bounds,Parent=b.Parent!.ClientSize}).ToArray()});Save();
                    if(args.Contains("--hover-only"))
                    {
                        int SampleBackground()
                        {
                            using var bitmap=new Bitmap(grid.Width,grid.Height);grid.DrawToBitmap(bitmap,grid.ClientRectangle);
                            var rect=grid.GetCellDisplayRectangle(1,1,false);return bitmap.GetPixel(rect.Right-20,rect.Top+5).ToArgb();
                        }
                        typeof(DataGridView).GetMethod("OnCellMouseEnter",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(grid,new object[]{new DataGridViewCellEventArgs(1,1)});
                        grid.Refresh();var hovered=SampleBackground();
                        typeof(Control).GetMethod("OnMouseLeave",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(grid,new object[]{EventArgs.Empty});
                        grid.Refresh();var normal=SampleBackground();
                        results.Add(new{Case="HoverLeavePixels",Pass=hovered==Color.FromArgb(42,32,51).ToArgb()&&normal==OrbitManagementTheme.Surface.ToArgb(),Hovered=hovered,Normal=normal,ExpectedSurface=OrbitManagementTheme.Surface.ToArgb(),Note="Pixels from actual owned grid DrawToBitmap; not a desktop screenshot"});Save();
                        using(var pixels=new Bitmap(grid.Width,grid.Height))
                        {
                            grid.DrawToBitmap(pixels,grid.ClientRectangle);var rect=grid.GetCellDisplayRectangle(1,1,false);
                            var boundary=pixels.GetPixel(rect.Left,rect.Top+5).ToArgb();
                            pixels.Save(Path.ChangeExtension(args[0],"grid.png"));
                            results.Add(new{Case="NoVerticalCellBorder",Pass=boundary==OrbitManagementTheme.Surface.ToArgb(),Boundary=boundary,Expected=OrbitManagementTheme.Surface.ToArgb(),Cell=rect,NeighborPixels=Enumerable.Range(-3,10).Select(dx=>new{X=rect.Left+dx,Pixel=pixels.GetPixel(rect.Left+dx,rect.Top+5).ToArgb()}).ToArray(),Note="Owned native grid pixels, not desktop capture"});Save();
                        }
                        host.Controls.Remove(material);material.Dispose();continue;
                    }                    var initial=grid.Rows.Cast<DataGridViewRow>().Select(r=>r.Cells[0].Value?.ToString()).ToArray();
                    workspace.Pagination.Next.PerformClick();var second=grid.Rows.Cast<DataGridViewRow>().Select(r=>r.Cells[0].Value?.ToString()).ToArray();
                    results.Add(new{Case="Paging",Role=identity.Role,First=initial,Second=second,Range=workspace.Pagination.Range.Text,Disjoint=!initial.Intersect(second).Any()});
                    if(units.Items.Count>1){units.SelectedIndex=1;results.Add(new{Case="Unit",Role=identity.Role,Unit=units.SelectedItem,Rows=grid.Rows.Count,AllMatch=grid.Rows.Cast<DataGridViewRow>().All(r=>r.Cells["Unit"].Value as string==units.SelectedItem as string)});}
                    host.ClientSize=new Size(1050,530);await Task.Delay(100);results.Add(new{Case="Resize",Role=identity.Role,FooterVisible=workspace.Pagination.Bottom<=workspace.Height,GridHeight=grid.Height,RowHeight=grid.RowTemplate.Height});host.ClientSize=new Size(1166,718);
                    if(identity.Role=="ADMIN")
                    {
                        units.SelectedIndex=0;
                        foreach(var count in new[]{0,1,7,8,14,49,50,51,99})
                        {
                            var fixture=Enumerable.Range(1,count).Select(i=>new CafeManager.Models.Dtos.Inventory.MaterialRow($"NVL{i:00}",$"UI fixture {i}","Kg",i/10000m,null,new byte[8],DateTime.Now)).ToList();
                            material.GetType().GetField("snapshot",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(material,fixture);
                            material.GetType().GetField("page",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(material,0);
                            material.GetType().GetMethod("RenderRows",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(material,null);
                            var codes=new List<string>();var sizes=new List<int>();
                            do{sizes.Add(grid.Rows.Count);codes.AddRange(grid.Rows.Cast<DataGridViewRow>().Select(r=>r.Cells["Code"].Value?.ToString()??""));if(!workspace.Pagination.Next.Enabled)break;workspace.Pagination.Next.PerformClick();}while(sizes.Count<20);
                            var pass=codes.Count==count&&codes.Distinct().Count()==count&&sizes.All(n=>n<=7)&&!workspace.Pagination.Next.Enabled;
                            results.Add(new{Case="TypedPagingFixture",Count=count,Pass=pass,Pages=sizes.Count,Range=workspace.Pagination.Range.Text,Note="In-process typed UI fixture only; no SQL writes"});
                        }
                    }
                    if(identity.Role=="ADMIN")
                    {
                        var search=(TextBox)Field(material,"search")!;var reload=material.GetType().GetMethod("LoadRows",BindingFlags.NonPublic|BindingFlags.Instance)!;
                        search.Text="";await (Task)reload.Invoke(material,null)!;
                        var beforeCount=workspace.Count.Text;var beforeRows=grid.Rows.Count;
                        search.Text=new string('x',101);await (Task)reload.Invoke(material,null)!;
                        results.Add(new{Case="SearchValidationKeepsData",Pass=workspace.Status.Text.StartsWith("Chưa cập nhật")&&workspace.Count.Text==beforeCount&&grid.Rows.Count==beforeRows,State=workspace.Status.Text,Count=workspace.Count.Text});
                        search.Text="Orbit9_No_Match_"+Guid.NewGuid().ToString("N");await (Task)reload.Invoke(material,null)!;
                        results.Add(new{Case="Empty",Pass=grid.Rows.Count==0&&All(material).OfType<OrbitEmptyState>().Single().Visible,Count=workspace.Count.Text});
                        search.Text="NVL02";await (Task)reload.Invoke(material,null)!;
                        results.Add(new{Case="SearchCode",Pass=grid.Rows.Count==1&&grid.Rows[0].Cells["Code"].Value as string=="NVL02",Range=workspace.Pagination.Range.Text});Save();
                    }
                    host.Controls.Remove(material);material.Dispose();Save();
                }
            }catch(Exception ex){results.Add(new{Case="Exception",Error=ex.ToString()});}
            finally{Save();host.Close();}
        };
        Application.Run(host);
    }
}






