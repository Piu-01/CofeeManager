namespace CafeManager.WinForms.Forms;

partial class LoginForm
{
    private TextBox usernameInput = null!;
    private TextBox passwordInput = null!;
    private CheckBox showPassword = null!;
    private Button loginButton = null!;
    private Label statusLabel = null!;
    private TableLayoutPanel layout = null!;
    private Label title = null!;
    private Label usernameLabel = null!;
    private Label passwordLabel = null!;
    private void InitializeComponent()
    {
        layout = new TableLayoutPanel(); title = new Label(); usernameLabel = new Label(); passwordLabel = new Label();
        usernameInput = new TextBox(); passwordInput = new TextBox(); showPassword = new CheckBox();
        loginButton = new Button(); statusLabel = new Label();
        SuspendLayout(); layout.SuspendLayout();
        layout.Dock = DockStyle.Fill; layout.Padding = new Padding(32); layout.ColumnCount = 1; layout.RowCount = 8;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.AutoScroll = true;
        title.Text = "CoffeeManager · Đăng nhập"; title.Font = new Font("Segoe UI", 17F, FontStyle.Bold); title.AutoSize = true;
        usernameLabel.Text = "&Tên đăng nhập"; usernameLabel.AutoSize = true;
        usernameInput.Dock = DockStyle.Top; usernameInput.MaxLength = 50; usernameInput.AccessibleName = "Tên đăng nhập"; usernameInput.TabIndex = 0;
        passwordLabel.Text = "&Mật khẩu"; passwordLabel.AutoSize = true;
        passwordInput.Dock = DockStyle.Top; passwordInput.MaxLength = 128; passwordInput.UseSystemPasswordChar = true; passwordInput.AccessibleName = "Mật khẩu"; passwordInput.TabIndex = 1;
        showPassword.Text = "&Hiện mật khẩu"; showPassword.AutoSize = true; showPassword.TabIndex = 2; showPassword.CheckedChanged += ShowPassword_Changed;
        loginButton.Text = "Đăng &nhập"; loginButton.AutoSize = true; loginButton.Padding = new Padding(12, 8, 12, 8); loginButton.TabIndex = 3; loginButton.Click += Login_Click;
        statusLabel.AutoSize = true; statusLabel.Dock = DockStyle.Fill; statusLabel.Padding = new Padding(0, 12, 0, 0);
        layout.Controls.Add(title, 0, 0); layout.Controls.Add(usernameLabel, 0, 1); layout.Controls.Add(usernameInput, 0, 2);
        layout.Controls.Add(passwordLabel, 0, 3); layout.Controls.Add(passwordInput, 0, 4); layout.Controls.Add(showPassword, 0, 5);
        layout.Controls.Add(loginButton, 0, 6); layout.Controls.Add(statusLabel, 0, 7);
        AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(580, 500); MinimumSize = new Size(480, 400); Text = "CoffeeManager · Đăng nhập";
        StartPosition = FormStartPosition.CenterScreen; Controls.Add(layout);
        layout.ResumeLayout(false); layout.PerformLayout(); ResumeLayout(false);
    }
}
