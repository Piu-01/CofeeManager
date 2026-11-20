using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;
class Program {
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static T F<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
 [STAThread] static int Main(string[] args){
 ApplicationConfiguration.Initialize(); using var session=new SessionContext(new AccountIdentity(0,"SMOKE","Constructor only","smoke","CASH",true,true,new byte[8]));session.Revoke();
 using var page=new SalesControl(null!,null!,null!,null!,session); using var main=new MainForm(); F<Panel>(main,"content").Controls.Add(page);
 var results=new List<object>();int failed=0;void Check(string n,bool ok){results.Add(new{Name=n,Outcome=ok?"PASS":"FAIL"});if(!ok)failed++;}
 main.Shown+=async(_,_)=>{try{
 Check("DPI125",page.DeviceDpi==120);
 foreach(var size in new[]{new Size(1280,720),new Size(1366,768),new Size(960,900)}){
 main.MinimumSize=Size.Empty;main.ClientSize=size;await Task.Yield();page.PerformLayout();
 foreach(var item in new[]{("previous","left","Trang trước"),("next","right","Trang sau")}){
 var b=(OrbitButton)F<Button>(page,item.Item1);Check(size+item.Item1+" icon only",b.Text==""&&b.ManagementIcon==item.Item2);
 Check(size+item.Item1+" labels",b.AccessibleName==item.Item3&&F<ToolTip>(page,"tips").GetToolTip(b)==item.Item3);
 Check(size+item.Item1+" bounds",b.Parent!.ClientRectangle.Contains(b.Bounds)&&b.Width>=38*page.DeviceDpi/96-1);
 var icon=18*page.DeviceDpi/96F;Check(size+item.Item1+" centered icon fits",b.Width>icon+2&&b.Height>icon+2);
 }
 var prev=F<Button>(page,"previous");var next=F<Button>(page,"next");Check(size+" enabled normal",prev.Enabled&&next.Enabled);
 page.GetType().GetMethod("SetBusy",Flags)!.Invoke(page,[true]);Check(size+" busy locks pager",!prev.Enabled&&!next.Enabled);prev.PerformClick();next.PerformClick();Check(size+" disabled no page mutation",F<int>(page,"menuPage")==0);
 page.GetType().GetMethod("SetBusy",Flags)!.Invoke(page,[false]);Check(size+" unlocks pager",prev.Enabled&&next.Enabled);
 }
 }catch(Exception e){results.Add(new{Exception=e.ToString()});failed++;}finally{File.WriteAllText(Path.Combine(args[0],"results.json"),JsonSerializer.Serialize(new{Failed=failed,Scope="Real WinForms technical bounds/labels/busy, revoked session, no SQL/no screenshots; normal callback query not executed",Results=results},new JsonSerializerOptions{WriteIndented=true}));main.Close();}};
 Application.Run(main);return failed==0?0:1;
 }
}
