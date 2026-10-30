using System.Text.Json;
using CafeManager.WinForms.UI;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        var rows=new List<object>();
        foreach(var direct in new[]{false,true})
        {
            using var host=new Form{ClientSize=new Size(1100,650),ShowInTaskbar=false,AutoScaleMode=AutoScaleMode.Dpi};
            var tabs=new TabControl{Dock=DockStyle.Fill};tabs.TabPages.Add(new TabPage("Home"));var page=new TabPage("Hidden first");tabs.TabPages.Add(page);host.Controls.Add(tabs);
            var workspace=new OrbitListWorkspace(new OrbitSearchBox());page.Controls.Add(workspace);
            var add=new OrbitButton{Text="Lập phiếu nhập",Dock=direct?DockStyle.None:DockStyle.Fill};workspace.PrimaryActions.Controls.Add(add);
            var refresh=new OrbitButton{Text="Tìm / Tải lại"};
            if(direct)workspace.RowActions.Controls.Add(refresh);
            else{var flow=new FlowLayoutPanel{Dock=DockStyle.Fill};flow.Controls.Add(refresh);workspace.RowActions.Controls.Add(flow);}
            host.Shown+=(_,_)=>host.BeginInvoke(()=>{tabs.SelectedTab=page;host.BeginInvoke(()=>{workspace.PerformLayout();rows.Add(new{Direct=direct,Dpi=workspace.DeviceDpi,Workspace=workspace.ClientSize,Primary=workspace.PrimaryActions.Bounds,Add=add.Bounds,AddVisible=add.Visible,Actions=workspace.RowActions.Bounds,Refresh=refresh.Bounds,RefreshVisible=refresh.Visible});host.Close();});});
            Application.Run(host);
        }
        File.WriteAllText(args[0],JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
    }
}
