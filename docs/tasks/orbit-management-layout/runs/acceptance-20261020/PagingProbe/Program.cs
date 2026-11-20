using System.Collections;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.UI;
internal sealed class ListFixture:RecordListControl
{
    private readonly int total;
    public readonly List<int> Skips=new();
    public ListFixture(int total,SessionContext session)
    {
        this.total=total;Configure("Pagination fixture",session,(q,skip,t)=>{Skips.Add(skip);return Task.FromResult<IList>(Enumerable.Range(skip,Math.Min(50,Math.Max(0,total-skip))).Select(i=>new FixtureRow(i.ToString(),"Dòng kiểm UI "+i)).ToList());},("Code","Mã"),("Name","Tên"));
    }
    public string PageText=>Workspace.Page.Text;
    public int RowCount=>Grid.Rows.Count;
    public Button Find(string text)=>GetAll(this).OfType<Button>().Single(b=>b.Text==text);
    static IEnumerable<Control> GetAll(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in GetAll(child))yield return nested;}}
    internal record FixtureRow(string Code,string Name);
}
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var results=new List<object>();using var session=new SessionContext(new AccountIdentity(0,"TEST","UI fixture","fixture","ADMIN",true,true,new byte[8]));
        using var host=new Form{ClientSize=new Size(1366,768),ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-10000,-10000)};
        host.Shown+=async(_,_)=>
        {
            foreach(var total in new[]{0,1,49,50,51,101})
            {
                using var list=new ListFixture(total,session){Dock=DockStyle.Fill};host.Controls.Add(list);await Task.Delay(80);
                var initial=list.RowCount==Math.Min(total,50)&&!list.Find("Trước").Enabled&&list.Find("Sau").Enabled==(total>=50);
                var pages=0;while(list.Find("Sau").Enabled&&pages<4){list.Find("Sau").PerformClick();await Task.Delay(40);pages++;}
                var end=list.PageText;var previous=list.Find("Trước").Enabled;
                if(previous){list.Find("Trước").PerformClick();await Task.Delay(40);}
                results.Add(new{Total=total,Status=initial&&pages==total/50?"PASS":"FAIL",End=end,Skips=list.Skips.ToArray(),ActualDpi=list.DeviceDpi});host.Controls.Remove(list);
            }
            File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));host.Close();
        };
        Application.Run(host);Console.WriteLine($"Paging UI fixture cases: {results.Count}");
    }
}
