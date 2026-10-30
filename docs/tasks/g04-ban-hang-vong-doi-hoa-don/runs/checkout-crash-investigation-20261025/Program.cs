using System.Reflection;
using System.Text.Json;
using CafeManager.WinForms.Forms;

internal static class Program
{
    [STAThread] private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        using var owner = new Form { Text="G04 isolated effect reproduction", Size=new Size(600,400) };
        var button = new Button { Text="Fixture action", Dock=DockStyle.Bottom }; owner.Controls.Add(button);
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.UI.StarDustOverlay")!;
        var overlay=(Form)Activator.CreateInstance(type,owner)!;
        using var timer=new System.Windows.Forms.Timer{Interval=500};
        timer.Tick+=(_,_)=>
        {
            timer.Stop(); Exception? error=null;
            try { type.GetMethod("Burst")!.Invoke(overlay,[button]); }
            catch(Exception ex) { error=ex.InnerException??ex; }
            File.WriteAllText(args[0],JsonSerializer.Serialize(new { OwnerEnabled=owner.Enabled,OverlayEnabled=overlay.Enabled,Error=error?.ToString(),Scope="Isolated production StarDustOverlay.Burst, no SQL or checkout submission" },new JsonSerializerOptions{WriteIndented=true}));
            owner.Close();
        };
        owner.Shown+=(_,_)=>timer.Start();Application.Run(owner);
    }
}
