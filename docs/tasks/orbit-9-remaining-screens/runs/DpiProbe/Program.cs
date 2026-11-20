using System.Text.Json;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

static class Program
{
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);
        using var form = new Form { Text = "Orbit live DPI diagnostic — no database", AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96), ClientSize = new Size(900, 500), StartPosition = FormStartPosition.CenterScreen, BackColor = OrbitManagementTheme.Background };
        var title = new Label { Text = "Nguyên liệu & tồn kho — chữ native Label", Dock = DockStyle.Top, Height = 65, Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Text };
        var grid = new OrbitDataGridView { Dock = DockStyle.Fill };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", DataPropertyName = "Code", HeaderText = "Mã" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "Nguyên liệu" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Tồn hiện tại" });
        var reload = new Button { Text = "Chuyển màn cache", Dock = DockStyle.Bottom, Height = 45 };
        var tabs = new OrbitSectionTabs();
        var workspace = new OrbitListWorkspace(new OrbitSearchBox());
        workspace.Tabs.Controls.Add(tabs); workspace.Title.Text = "Kiểm tra tab và màn cache";
        tabs.Add("Khuyến mãi"); tabs.Add("Hạng thành viên"); tabs.Add("Điểm khách hàng");
        var root = new UserControl { AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96), Dock = DockStyle.Fill };
        var cached = new UserControl { AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96), Dock = DockStyle.Fill };
        var lazyGrid = new OrbitDataGridView { Dock = DockStyle.Fill, Profile = OrbitGridProfile.Points };
        lazyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "Mã GD" });
        lazyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", DataPropertyName = "Reason", HeaderText = "Lý do" });
        cached.Controls.Add(lazyGrid);
        var host = new Panel { Dock = DockStyle.Fill };
        root.Controls.Add(workspace); workspace.Body.Controls.Add(grid);
        void Bind() => grid.DataSource = new[] { new { Code = "NVL01", Name = "Cà phê hạt — kiểm tra chữ rõ nét", Quantity = "96,7400" }, new { Code = "NVL02", Name = "Sữa tươi", Quantity = "98,1600" } };
        void Log(string reason)
        {
            using var graphics = grid.CreateGraphics();
            var font = grid.DefaultCellStyle.Font!;
            var item = new { Reason = reason, form.DeviceDpi, RootDpi = root.DeviceDpi, CachedDpi = cached.DeviceDpi, GridDpi = grid.DeviceDpi, WorkspaceDpi = workspace.DeviceDpi, TabsDpi = tabs.DeviceDpi, TabsBounds = tabs.Bounds, Display = tabs.DisplayRectangle, Scroll = tabs.HorizontalScroll.Visible, Buttons = tabs.Controls.Cast<Button>().Select(b => new { b.Bounds, Preferred = b.GetPreferredSize(Size.Empty), b.DeviceDpi }), GraphicsDpi = graphics.DpiY, FormFont = form.Font.Size, LabelFont = title.Font.Size, GridFont = grid.Font.Size, CellFont = font.Size, HeaderFont = grid.ColumnHeadersDefaultCellStyle.Font!.Size, ThemeFont = OrbitManagementTheme.Table.Size, Measured = TextRenderer.MeasureText(graphics, "Nguyên liệu", font), Grid = grid.Size, RowHeight = grid.Rows.Count > 0 ? grid.Rows[0].Height : 0 };
            File.AppendAllText(args[0], JsonSerializer.Serialize(item) + Environment.NewLine);
            File.AppendAllText(args[0], JsonSerializer.Serialize(new { Reason = reason, LazyDpi = lazyGrid.DeviceDpi, LazyHandle = lazyGrid.IsHandleCreated, HeaderHeight = lazyGrid.ColumnHeadersHeight, HeaderFont = lazyGrid.ColumnHeadersDefaultCellStyle.Font!.Size, HeaderUnit = lazyGrid.ColumnHeadersDefaultCellStyle.Font!.Unit.ToString() }) + Environment.NewLine);
        }
        form.Controls.Add(host); form.Controls.Add(reload);
        var keepParent = args.Contains("--keep-parent");
        form.Shown += (_, _) => { cached.Visible = false; host.Controls.Add(cached); if (!keepParent) host.Controls.Clear(); host.Controls.Add(root); Bind(); Log("shown"); };
        form.DpiChanged += (_, _) => form.BeginInvoke(async () => { await Task.Delay(200); Log("dpi-changed"); grid.Invalidate(); });
        lazyGrid.VisibleChanged += (_, _) => { if (lazyGrid.Visible && lazyGrid.IsHandleCreated) lazyGrid.BeginInvoke(() => { Log("lazy-visible-before-refresh"); lazyGrid.Profile = lazyGrid.Profile; Log("lazy-visible-after-refresh"); }); };
        reload.Click += (_, _) =>
        {
            var next = root.Visible ? cached : root;
            if (keepParent) { foreach (Control page in host.Controls) page.Visible = false; if (next.Parent != host) host.Controls.Add(next); next.Visible = true; next.BringToFront(); }
            else { host.Controls.Clear(); host.Controls.Add(next); }
            Log("switch");
        };
        Application.Run(form);
    }
}
