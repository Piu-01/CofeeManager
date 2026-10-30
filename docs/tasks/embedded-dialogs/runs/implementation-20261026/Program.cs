using System.Text.Json;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;

// Smoke triển khai của host, không truy cập DB/đăng nhập hoặc nghiệm thu checklist ED.
internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var results = new List<object>(); var failed = false;
        void Check(string name, bool actual) { results.Add(new { Name = name, Actual = actual }); if (!actual) throw new InvalidOperationException(name); }
        using var main = new MainForm { Text = "CoffeeManager — host technical smoke" };
        using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
        watchdog.Tick += (_, _) => { failed = true; results.Add(new { Error = "Host smoke deadline" }); main.Dispose(); Application.ExitThread(); };
        main.Shown += async (_, _) =>
        {
            watchdog.Start();
            try
            {
                using var parent = new OrbitDialogForm { Text = "Parent", Size = new Size(620, 420) };
                var input = new TextBox { Text = "81000", Dock = DockStyle.Top };
                parent.Controls.Add(input); OrbitField.ApplyInputBehavior(parent);
                var parentTask = OrbitDialogHost.ShowAsync(parent, main);
                await Task.Yield();
                Check("Embedded parent / one top-level main", !parent.TopLevel && parent.Parent is not null && main.Dialogs.IsOpen && !parent.ShowInTaskbar && Application.OpenForms.Cast<Form>().Count(f => f.TopLevel) == 1);
                using var child = new OrbitDialogForm { Text = "Child", Size = new Size(360, 200) };
                var childTask = OrbitDialogHost.ShowAsync(child, parent);
                await Task.Yield();
                Check("Nested top only", child.Enabled && !parent.Enabled && parent.Visible);
                child.DialogResult = DialogResult.OK; child.Close();
                Check("Child result", await childTask == DialogResult.OK);
                Check("Parent restored with input", parent.Enabled && parent.Visible && input.Text == "81000");
                parent.Close(); Check("Parent cancel", await parentTask == DialogResult.Cancel);
                Check("Host idle", !main.Dialogs.IsOpen);
                using var guarded = new GuardedDialog();
                var guardedTask = OrbitDialogHost.ShowAsync(guarded, main);
                guarded.Close(); await Task.Yield(); await Task.Yield();
                Check("Close guard retains live task", !guardedTask.IsCompleted && !guarded.IsDisposed);
                guarded.Allow = true; guarded.Close(); Check("Close guard then accepts", await guardedTask == DialogResult.Cancel);
                using var aborted = new OrbitDialogForm();
                var abortedTask = OrbitDialogHost.ShowAsync(aborted, main); main.Dialogs.AbortAll();
                Check("Abort completes result", await abortedTask == DialogResult.Cancel && !main.Dialogs.IsOpen);
                results.Add(new { DeviceDpi = main.DeviceDpi, Scope = "Implementation smoke only; no visual QA, no SQL, no native mouse input" });
            }
            catch (Exception ex) { failed = true; results.Add(new { Error = ex.ToString() }); }
            finally { watchdog.Stop(); main.Dispose(); Application.ExitThread(); }
        };
        Application.Run(main);
        File.WriteAllText(Path.Combine(args[0], "host-smoke.json"), JsonSerializer.Serialize(new { Failed = failed, Results = results }, new JsonSerializerOptions { WriteIndented = true }));
        return failed ? 1 : 0;
    }
    private sealed class GuardedDialog : OrbitDialogForm
    {
        internal bool Allow;
        protected override Task<bool> CanCloseAsync() => Task.FromResult(Allow);
    }
}
