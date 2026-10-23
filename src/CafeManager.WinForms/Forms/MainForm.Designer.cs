namespace CafeManager.WinForms.Forms;

partial class MainForm
{
    private TableLayoutPanel layout = null!;
    private FlowLayoutPanel sidebar = null!;
    private Panel content = null!;
    private Label identityLabel = null!;
    private Label breadcrumbLabel = null!;
    private TableLayoutPanel topbar = null!;
    private Label statusLabel = null!;
    private Button homeButton = null!;
    private Button catalogsButton = null!;
    private Button systemButton = null!;
    private Button logoutButton = null!;
    private Label brandLabel = null!;
    private Label brandCaption = null!;
    private void InitializeComponent()
    {
        layout = new TableLayoutPanel(); sidebar = new CafeManager.WinForms.UI.OrbitSidebar(); content = new Panel();
        breadcrumbLabel = new Label(); topbar = new TableLayoutPanel(); identityLabel = new Label(); statusLabel = new Label(); homeButton = new CafeManager.WinForms.UI.OrbitButton(); catalogsButton = new CafeManager.WinForms.UI.OrbitButton(); systemButton = new CafeManager.WinForms.UI.OrbitButton(); logoutButton = new CafeManager.WinForms.UI.OrbitButton();
        brandLabel = new Label(); brandCaption = new Label();
        SuspendLayout(); layout.SuspendLayout(); sidebar.SuspendLayout();
        layout.Dock = DockStyle.Fill; layout.ColumnCount = 2; layout.RowCount = 3;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        sidebar.Dock = DockStyle.Fill; sidebar.FlowDirection = FlowDirection.TopDown; sidebar.WrapContents = false; sidebar.AutoScroll = true; sidebar.Padding = Padding.Empty;
        brandLabel.Text = "CoffeeManager"; brandLabel.AutoSize = true; brandLabel.Margin = new Padding(4, 0, 0, 6);
        brandCaption.Text = "KHÔNG GIAN QUẢN LÝ"; brandCaption.AutoSize = true; brandCaption.Font = new Font("Segoe UI", 9F); brandCaption.Margin = new Padding(4, 0, 0, 32);
        sidebar.Controls.Add(brandLabel); sidebar.Controls.Add(brandCaption); sidebar.Controls.SetChildIndex(brandLabel, 0); sidebar.Controls.SetChildIndex(brandCaption, 1);
        homeButton.Text = "Trang phiên"; homeButton.Size = new Size(200, 44); homeButton.Margin = new Padding(0, 0, 0, 10); homeButton.Click += Home_Click;
        catalogsButton.Text = "Danh mục nền"; catalogsButton.Size = new Size(200, 44); catalogsButton.Margin = new Padding(0, 0, 0, 10); catalogsButton.Click += Catalogs_Click;
        systemButton.Text = "Thông tin hệ thống"; systemButton.Size = new Size(200, 44); systemButton.Margin = new Padding(0, 0, 0, 10); systemButton.Click += System_Click;
        logoutButton.Name = "logout"; logoutButton.Text = "Đăng xuất"; logoutButton.Size = new Size(200, 44); logoutButton.Margin = new Padding(0, 24, 0, 0); logoutButton.Click += Logout_Click;
        sidebar.Controls.Add(homeButton); sidebar.Controls.Add(catalogsButton); sidebar.Controls.Add(systemButton); sidebar.Controls.Add(logoutButton);
        topbar.Dock=DockStyle.Fill;topbar.AutoSize=false;topbar.ColumnCount=2;topbar.Margin=Padding.Empty;
        topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));topbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        breadcrumbLabel.Text="Quản lý";breadcrumbLabel.AutoSize=true;breadcrumbLabel.Anchor=AnchorStyles.Left;breadcrumbLabel.Padding=new Padding(26,0,8,0);
        topbar.Controls.Add(breadcrumbLabel,0,0);topbar.Controls.Add(identityLabel,1,0);
        identityLabel.AutoSize = true; identityLabel.Anchor = AnchorStyles.Right; identityLabel.Padding = new Padding(16, 0, 26, 0);
        statusLabel.AutoSize = true; statusLabel.Dock = DockStyle.None; statusLabel.Padding = new Padding(8);
        content.Dock = DockStyle.Fill; content.Margin = new Padding(0);
        layout.Controls.Add(sidebar, 0, 0); layout.SetRowSpan(sidebar, 3);
        layout.Controls.Add(topbar, 1, 0); layout.Controls.Add(content, 1, 1); sidebar.Controls.Add(statusLabel);
        AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1366, 768); MinimumSize = new Size(360, 320); Text = "CoffeeManager"; StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(layout); sidebar.ResumeLayout(false); layout.ResumeLayout(false); layout.PerformLayout(); ResumeLayout(false);
    }
}





