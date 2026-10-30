using System.Reflection;
using System.Text.Json;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;

// Prototype trình bày: không session/auth/SQL; không gọi handler nghiệp vụ.
internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        using var main = new MainForm { Text = "Invoice V4 — technical layout smoke" };
        using var page = new InvoicesControl(null!, null!);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var content = (Panel)typeof(MainForm).GetField("content", flags)!.GetValue(main)!;
        var workspace = (OrbitListWorkspace)typeof(OrbitRecordListControl).GetField("Workspace", flags)!.GetValue(page)!;
        var grid = (OrbitDataGridView)typeof(OrbitRecordListControl).GetField("Grid", flags)!.GetValue(page)!;
        var range = workspace.Filters.Controls.OfType<OrbitDateRangeField>().Single();
        page.Dock = DockStyle.Fill; content.Controls.Add(page);
        var results = new List<object>(); var failed = false;
        void Check(string name, bool ok, object? actual = null) { results.Add(new { Name = name, Outcome = ok ? "PASS" : "FAIL", Actual = actual }); if (!ok) failed = true; }
        bool Inside(Control input)
        {
            var rectangle = input.RectangleToScreen(input.ClientRectangle);
            for (var parent = input.Parent; parent is not null; parent = parent.Parent)
                if (!parent.RectangleToScreen(parent.ClientRectangle).Contains(rectangle)) return false;
            return true;
        }
        main.Shown += async (_, _) =>
        {
            try
            {
                grid.DataSource = new List<SalesOrderRow> { new("HD99999999", DateTime.Now, "Mang di", null, "Khách demo smoke", 81000, "Da thanh toan", null, new byte[8], 1) };
                foreach (var size in new[] { new Size(1280,720), new Size(1366,768) })
                {
                    main.ClientSize = size; await Task.Yield();
                    foreach (var control in workspace.Filters.Controls.Cast<Control>().Concat(workspace.SearchActions.Controls.Cast<Control>()).Concat(workspace.RowActions.Controls.Cast<Control>()).Append(range.From).Append(range.To).Append(workspace.Pagination))
                        Check(size+" bounds "+(control.AccessibleName ?? control.GetType().Name), Inside(control), control.Bounds.ToString());
                    Check(size+" table body usable", grid.Height > grid.ColumnHeadersHeight + grid.RowTemplate.Height, new { Height=grid.Height, Dpi=grid.DeviceDpi });
                    var code = grid.Rows[0].Cells["Code"];
                    Check(size+" full code", grid.Columns["Code"].Width - code.InheritedStyle.Padding.Horizontal >= TextRenderer.MeasureText("HD99999999", code.InheritedStyle.Font, Size.Empty, TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width);
                }
                Check("Invoice profile only", grid.Profile == OrbitGridProfile.InvoiceHistory && grid.ReadOnly && workspace.StackedTools);
                Check("Two search icon actions", workspace.SearchActions.Controls.Count == 2 && workspace.SearchActions.Controls.Cast<OrbitButton>().All(b=>b.ManagementStyle==CafeManager.WinForms.Theme.ManagementButtonStyle.Icon));
                results.Add(new { DeviceDpi=main.DeviceDpi, Scope="Presentation-only constructor/layout smoke, null service/session never queried; in-memory demo row; no visual QA/business action/auth/SQL" });
            }
            catch (Exception ex) { Check("Smoke exception", false, ex.ToString()); }
            finally { main.Dispose(); Application.ExitThread(); }
        };
        Application.Run(main);
        File.WriteAllText(Path.Combine(args[0], "layout-smoke.json"), JsonSerializer.Serialize(new { Failed=failed,Results=results }, new JsonSerializerOptions { WriteIndented=true }));
        return failed ? 1 : 0;
    }
}
