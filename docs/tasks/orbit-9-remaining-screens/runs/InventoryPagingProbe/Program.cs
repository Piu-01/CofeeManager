using System.Reflection;
using System.Text.Json;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.UI;

static class Program
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static FieldInfo Field(string name) => typeof(MaterialsControl).GetField(name, Private)!;
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var host = new Form { ClientSize = new Size(1166, 718), ShowInTaskbar = false, Location = new Point(-10000, -10000), StartPosition = FormStartPosition.Manual };
        var root = new MaterialsControl { Dock = DockStyle.Fill }; host.Controls.Add(root);
        var grid = (DataGridView)Field("grid").GetValue(root)!;
        var workspace = (OrbitListWorkspace)Field("workspace").GetValue(root)!;
        var filter = (ComboBox)Field("unitFilter").GetValue(root)!;
        var render = typeof(MaterialsControl).GetMethod("RenderRows", Private)!;
        var results = new List<object>();
        void Check(string name, bool pass) { results.Add(new { Case = name, Pass = pass }); if (!pass) Environment.ExitCode = 1; }
        void Load(int count)
        {
            var rows = Enumerable.Range(1, count).Select(i => new MaterialRow($"NVL{i:00}", $"Fixture {i}", i % 2 == 0 ? "Lit" : "Kg", i / 10000m, null, new byte[8], DateTime.Now)).ToList();
            Field("snapshot").SetValue(root, rows); Field("page").SetValue(root, 0); render.Invoke(root, null); host.PerformLayout(); grid.Refresh(); Application.DoEvents();
        }
        host.Shown += (_, _) =>
        {
            try
            {
                foreach (var count in new[] { 0, 14, 49, 50, 51 })
                {
                    Load(count);
                    Check($"FirstPage-{count}", grid.Rows.Count == Math.Min(50, count) && workspace.Pagination.Next.Enabled == (count > 50) && !workspace.Pagination.Previous.Enabled);
                }
                Load(51); var headerTop = workspace.Body.Top; var footerTop = workspace.Pagination.Top; var rowHeight = grid.Rows[0].Height;
                Check("InternalScrollAvailable", grid.DisplayedRowCount(false) < 50 && grid.ScrollBars is ScrollBars.Both or ScrollBars.Vertical);
                grid.FirstDisplayedScrollingRowIndex = 49;
                Check("ScrollReachesRow50", grid.FirstDisplayedScrollingRowIndex > 0 && (string)grid.Rows[49].Cells[0].Value! == "NVL50" && workspace.Body.Top == headerTop && workspace.Pagination.Top == footerTop);
                workspace.Pagination.Next.PerformClick();
                Check("NextPage51", grid.Rows.Count == 1 && (string)grid.Rows[0].Cells[0].Value! == "NVL51" && !workspace.Pagination.Next.Enabled && workspace.Pagination.Previous.Enabled);
                workspace.Pagination.Previous.PerformClick();
                Check("PreviousPage", grid.Rows.Count == 50 && (string)grid.Rows[0].Cells[0].Value! == "NVL01");
                filter.Items.AddRange(new object[] { "Kg", "Lit" }); filter.SelectedIndex = 1;
                Check("FilterResetsAndCounts", grid.Rows.Count == 26 && !workspace.Pagination.Next.Enabled && workspace.Pagination.Range.Text.Contains("26"));
                filter.SelectedIndex = 0;
                foreach (var size in new[] { new Size(1080, 670), new Size(1166, 718) })
                {
                    host.ClientSize = size; host.PerformLayout(); Application.DoEvents();
                    results.Add(new { Case = $"ResizeMetrics-{size.Width}", Rows = grid.Rows.Count, Height = grid.Rows[0].Height, ExpectedHeight = rowHeight, FooterTop = workspace.Pagination.Top, BodyBottom = workspace.Body.Bottom, FooterParent = workspace.Pagination.Parent?.GetType().Name, BodyParent = workspace.Body.Parent?.GetType().Name });
                    Check($"Resize-{size.Width}", grid.Rows.Count == 50 && grid.Rows[0].Height == rowHeight && workspace.Pagination.Top >= workspace.Body.Bottom);
                }
            }
            catch (Exception ex) { results.Add(new { Case = "Exception", Error = ex.ToString(), Pass = false }); Environment.ExitCode = 1; }
            finally { File.WriteAllText(args[0], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })); host.Close(); }
        };
        Application.Run(host);
    }
}
