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
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var f=new ConnectionFactory("Server=localhost;Initial Catalog=CoffeeManager_UnusedDisplayProbe;Integrated Security=True;TrustServerCertificate=True");
        using var session=new SessionContext(new AccountIdentity(1,"NV001","Component","probe","CASH",true,true,new byte[8]));
        var h=new SalesOrderHeader("HD99999999",DateTime.Now,"NV001",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
        var p=new CheckoutPreview(h.Code,h.Revision,81000,0,0,81000,"None",0,0,0,0,new byte[32]);
        var t=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var form=(Form)Activator.CreateInstance(t,new SalesService(new SalesRepository(f),new PermissionService(new AuthRepository(f))),session,new SalesOrderDto(h,[],[]),p,new List<SalesLookup>{new("PT001","Tiền mặt"),new("PT002","Chuyển khoản")})!;
        Control Field(string name)=>(Control)t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        object Parents(Control c) { var list=new List<object>(); for(var p=c.Parent;p is not null;p=p.Parent)list.Add(new{Type=p.GetType().Name,Bounds=p.RectangleToScreen(p.ClientRectangle).ToString(),p.AutoSize});return list; }
        object Info(Control c)
        {
            var screen=c.RectangleToScreen(c.ClientRectangle);var clipped=new List<string>();
            for(var parent=c.Parent;parent is not null;parent=parent.Parent)if(!parent.RectangleToScreen(parent.ClientRectangle).Contains(screen))clipped.Add(parent.GetType().Name);
            return new{Type=c.GetType().Name,c.Text,Top=c.Top,Height=c.Height,ParentHeight=c.Parent?.ClientSize.Height,Bounds=screen.ToString(),ClippedBy=clipped,Parents=Parents(c),Back=c.BackColor.ToArgb().ToString("X8"),Fore=c.ForeColor.ToArgb().ToString("X8")};
        }
        var snapshots=new List<object>();var step=0;using var timer=new System.Windows.Forms.Timer{Interval=700};
        timer.Tick+=(_,_)=>
        {
            var grid=(DataGridView)Field("grid");
            snapshots.Add(new{Phase=step==0?"Initial":"After method focus/blank selection",Dpi=form.DeviceDpi,Form=form.Size.ToString(),Grid=Info(grid),Tender=Info(Field("tendered")),Notice=Info(Field("state")),Payable=Info(Field("payableValue")),Editor=grid.EditingControl is{}editor?Info(editor):null});
            File.WriteAllText(args[0],JsonSerializer.Serialize(snapshots,new JsonSerializerOptions{WriteIndented=true}));
            if(step++==0){grid.CurrentCell=grid.Rows[0].Cells["Method"];grid.BeginEdit(true);if(grid.EditingControl is ComboBox combo)combo.SelectedValue="";}
            else{timer.Stop();t.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);form.Close();}
        };
        form.Shown+=(_,_)=>timer.Start();Application.Run(form);
    }
}


