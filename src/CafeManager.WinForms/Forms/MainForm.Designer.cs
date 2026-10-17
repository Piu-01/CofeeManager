namespace CafeManager.WinForms.Forms;

partial class MainForm
{
    private TableLayoutPanel layout = null!;
    private FlowLayoutPanel sidebar = null!;
    private Panel content = null!;
    private Label identityLabel = null!;
    private Label statusLabel = null!;
    private Button homeButton = null!;
    private Button catalogsButton = null!;
    private Button systemButton = null!;
    private Button logoutButton = null!;
    private void InitializeComponent()
    {
        layout = new TableLayoutPanel(); sidebar = new FlowLayoutPanel(); content = new Panel();
        identityLabel = new Label(); statusLabel = new Label(); homeButton = new Button(); catalogsButton = new Button(); systemButton = new Button(); logoutButton = new Button();
        SuspendLayout(); layout.SuspendLayout(); sidebar.SuspendLayout();
        layout.Dock = DockStyle.Fill; layout.ColumnCount = 2; layout.RowCount = 3;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebar.Dock = DockStyle.Fill; sidebar.FlowDirection = FlowDirection.TopDown; sidebar.WrapContents = false; sidebar.AutoScroll = true; sidebar.Padding = new Padding(12);
        homeButton.Text = "Trang phiên"; homeButton.Size = new Size(170, 44); homeButton.Click += Home_Click;
        catalogsButton.Text = "Danh mục nền"; catalogsButton.Size = new Size(170, 44); catalogsButton.Click += Catalogs_Click;
        systemButton.Text = "Thông tin hệ thống"; systemButton.Size = new Size(170, 44); systemButton.Click += System_Click;
        logoutButton.Text = "Đăng xuất"; logoutButton.Size = new Size(170, 44); logoutButton.Click += Logout_Click;
        sidebar.Controls.Add(homeButton); sidebar.Controls.Add(catalogsButton); sidebar.Controls.Add(systemButton); sidebar.Controls.Add(logoutButton);
        identityLabel.AutoSize = true; identityLabel.Dock = DockStyle.Fill; identityLabel.Padding = new Padding(16);
        statusLabel.AutoSize = true; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(12);
        content.Dock = DockStyle.Fill;
        layout.Controls.Add(sidebar, 0, 0); layout.SetRowSpan(sidebar, 3);
        layout.Controls.Add(identityLabel, 1, 0); layout.Controls.Add(content, 1, 1); layout.Controls.Add(statusLabel, 1, 2);
        AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1100, 660); MinimumSize = new Size(640, 400); Text = "CoffeeManager"; StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(layout); sidebar.ResumeLayout(false); layout.ResumeLayout(false); layout.PerformLayout(); ResumeLayout(false);
    }
}
