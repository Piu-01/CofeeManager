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
    [STAThread]private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedLayoutProbe;Integrated Security=True;TrustServerCertificate=True");
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var preview=new CheckoutPreview(h.Code,h.Revision,81000,0,0,81000,"None",0,0,0,0,new byte[32]);
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var dialog=(Form)Activator.CreateInstance(type,new SalesService(new SalesRepository(f),new PermissionService(new AuthRepository(f))),session,new SalesOrderDto(h,[],[]),preview,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        var snapshots=new List<object>();var sizes=new[]{new Size(1280,720),new Size(1366,768),new Size(740,700)};var step=-1;
        using var timer=new System.Windows.Forms.Timer{Interval=700};
        timer.Tick+=(_,_)=>
        {
            if(step>=0)
            {
                var confirm=(Button)type.GetField("confirm",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(dialog)!;
                var client=dialog.RectangleToScreen(dialog.ClientRectangle);var button=confirm.RectangleToScreen(confirm.ClientRectangle);
                var chain=new List<object>();for(Control? p=confirm;p is not null;p=p.Parent)chain.Add(new{Type=p.GetType().Name,Bounds=p.Bounds.ToString(),Client=p.ClientRectangle.ToString(),Preferred=p.PreferredSize.ToString(),p.AutoSize,p.Dock,p.Visible,Parent=p.Parent?.GetType().Name});
                snapshots.Add(new{Requested=sizes[step].ToString(),Actual=dialog.Size.ToString(),Dpi=dialog.DeviceDpi,Client=client.ToString(),Confirm=button.ToString(),ConfirmInside=client.Contains(button),Chain=chain});
                File.WriteAllText(args[0],JsonSerializer.Serialize(snapshots,new JsonSerializerOptions{WriteIndented=true}));
            }
            step++;if(step==sizes.Length){timer.Stop();type.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(dialog,true);dialog.Close();return;}
            dialog.Size=sizes[step];
        };
        dialog.Shown+=(_,_)=>timer.Start();Application.Run(dialog);
    }
}
