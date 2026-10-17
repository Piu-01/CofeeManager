using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CafeManager.WinForms.Forms;
static class Probe
{
 [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using var form=new LoginForm();form.Shown+=(_,_)=>{var timer=new System.Windows.Forms.Timer{Interval=250};timer.Tick+=(_,_)=>{timer.Stop();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var button=(Button)typeof(LoginForm).GetField("loginButton",flags)!.GetValue(form)!;var layout=(TableLayoutPanel)typeof(LoginForm).GetField("layout",flags)!.GetValue(form)!;using var bitmap=new Bitmap(form.Width,form.Height);using var g=Graphics.FromImage(bitmap);var dc=g.GetHdc();try{PrintWindow(form.Handle,dc,2);}finally{g.ReleaseHdc(dc);}bitmap.Save(Path.Combine(args[0],"login-after-designer-save.png"));File.WriteAllText(Path.Combine(args[0],"bounds.json"),JsonSerializer.Serialize(new{Dpi=form.DeviceDpi,ButtonHeight=button.Height,FontHeight=button.Font.Height,RowHeights=layout.GetRowHeights(),RowStyles=layout.RowStyles.Cast<RowStyle>().Select(r=>new{r.SizeType,r.Height}),Controls=layout.Controls.Cast<Control>().Select(c=>new{c.Name,c.Bounds,c.Visible})},new JsonSerializerOptions{WriteIndented=true}));form.Close();};timer.Start();};Application.Run(form);}
}
