#nullable enable
namespace CafeManager.WinForms.Forms;

partial class StartupForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel layout = null!;
    private FlowLayoutPanel toolbar = null!;
    private TableLayoutPanel header = null!;
    private Label subtitleLabel = null!;
    private TableLayoutPanel filter = null!;
    private Panel inputPanel = null!;
    private Panel contentPanel = null!;
    private Label contentHeading = null!;
    private Panel gridPanel = null!;
    private Label emptyStateLabel = null!;
    private Label titleLabel = null!;
    private Label areaLabel = null!;
    private TextBox areaInput = null!;
    private Button loadButton = null!;
    private Button cancelButton = null!;
    private Label statusLabel = null!;
    private DataGridView tableGrid = null!;
    private DataGridViewTextBoxColumn tableCodeColumn = null!;
    private DataGridViewTextBoxColumn tableNameColumn = null!;
    private DataGridViewTextBoxColumn areaCodeColumn = null!;
    private DataGridViewTextBoxColumn statusColumn = null!;
    private DataGridViewTextBoxColumn invoiceCodeColumn = null!;
    private ErrorProvider validation = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) { components?.Dispose(); }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        layout = new TableLayoutPanel();
        header = new CafeManager.WinForms.UI.OrbitHeader();
        titleLabel = new Label();
        subtitleLabel = new Label();
        toolbar = new FlowLayoutPanel();
        filter = new TableLayoutPanel();
        areaLabel = new Label();
        inputPanel = new Panel();
        areaInput = new TextBox();
        loadButton = new CafeManager.WinForms.UI.OrbitButton();
        cancelButton = new CafeManager.WinForms.UI.OrbitButton();
        statusLabel = new Label();
        contentPanel = new Panel();
        gridPanel = new Panel();
        tableGrid = new DataGridView();
        tableCodeColumn = new DataGridViewTextBoxColumn();
        tableNameColumn = new DataGridViewTextBoxColumn();
        areaCodeColumn = new DataGridViewTextBoxColumn();
        statusColumn = new DataGridViewTextBoxColumn();
        invoiceCodeColumn = new DataGridViewTextBoxColumn();
        emptyStateLabel = new Label();
        contentHeading = new Label();
        validation = new ErrorProvider(components);
        layout.SuspendLayout();
        header.SuspendLayout();
        toolbar.SuspendLayout();
        filter.SuspendLayout();
        inputPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        gridPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)tableGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)validation).BeginInit();
        SuspendLayout();
        // 
        // layout
        // 
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(statusLabel, 0, 2);
        layout.Controls.Add(contentPanel, 0, 3);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Margin = new Padding(4);
        layout.Name = "layout";
        layout.Padding = new Padding(16);
        layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Size = new Size(1250, 750);
        layout.TabIndex = 0;
        // 
        // header
        // 
        header.AutoSize = true;
        header.BackColor = CafeManager.WinForms.Theme.AppTheme.Espresso;
        header.ColumnCount = 1;
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(subtitleLabel, 0, 1);
        header.Dock = DockStyle.Fill;
        header.Location = new Point(30, 30);
        header.Margin = new Padding(0, 0, 0, 12);
        header.Name = "header";
        header.Padding = new Padding(16, 10, 16, 10);
        header.RowCount = 2;
        header.RowStyles.Add(new RowStyle());
        header.RowStyles.Add(new RowStyle());
        header.Size = new Size(1190, 113);
        header.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
        titleLabel.ForeColor = CafeManager.WinForms.Theme.AppTheme.Surface;
        titleLabel.Location = new Point(25, 20);
        titleLabel.Margin = new Padding(0, 0, 0, 10);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(454, 40);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "CoffeeManager · Trạng thái bàn";
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.ForeColor = CafeManager.WinForms.Theme.AppTheme.Selected;
        subtitleLabel.Location = new Point(25, 70);
        subtitleLabel.Margin = new Padding(0);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(505, 23);
        subtitleLabel.TabIndex = 1;
        subtitleLabel.Text = "NỀN KỸ THUẬT G01  ·  Mẫu kiểm chứng kết nối, chưa đăng nhập";
        // 
        // toolbar
        // 
        toolbar.AutoSize = true;
        toolbar.BackColor = CafeManager.WinForms.Theme.AppTheme.Surface;
        toolbar.Controls.Add(filter);
        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(cancelButton);
        toolbar.Dock = DockStyle.Fill;
        toolbar.Location = new Point(30, 163);
        toolbar.Margin = new Padding(0, 0, 0, 15);
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(12);
        toolbar.Size = new Size(1190, 124);
        toolbar.TabIndex = 1;
        // 
        // filter
        // 
        filter.AutoSize = true;
        filter.ColumnCount = 1;
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310F));
        filter.Controls.Add(areaLabel, 0, 0);
        filter.Controls.Add(inputPanel, 0, 1);
        filter.Location = new Point(20, 20);
        filter.Margin = new Padding(0);
        filter.Name = "filter";
        filter.RowCount = 2;
        filter.RowStyles.Add(new RowStyle());
        filter.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        filter.Size = new Size(275, 83);
        filter.TabIndex = 0;
        // 
        // areaLabel
        // 
        areaLabel.AutoSize = true;
        areaLabel.Location = new Point(0, 0);
        areaLabel.Margin = new Padding(0, 0, 0, 10);
        areaLabel.Name = "areaLabel";
        areaLabel.Size = new Size(263, 23);
        areaLabel.TabIndex = 0;
        areaLabel.Text = "&Mã khu vực · trống để xem tất cả";
        // 
        // inputPanel
        // 
        inputPanel.BackColor = CafeManager.WinForms.Theme.AppTheme.Background;
        inputPanel.Controls.Add(areaInput);
        inputPanel.Dock = DockStyle.Fill;
        inputPanel.Location = new Point(0, 33);
        inputPanel.Margin = new Padding(0);
        inputPanel.Name = "inputPanel";
        inputPanel.Padding = new Padding(12, 10, 12, 10);
        inputPanel.Size = new Size(275, 50);
        inputPanel.TabIndex = 1;
        // 
        // areaInput
        // 
        areaInput.AccessibleName = "Mã khu vực";
        areaInput.BackColor = CafeManager.WinForms.Theme.AppTheme.Background;
        areaInput.BorderStyle = BorderStyle.None;
        areaInput.Dock = DockStyle.Top;
        areaInput.ForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        areaInput.Location = new Point(12, 10);
        areaInput.Margin = new Padding(4);
        areaInput.Name = "areaInput";
        areaInput.PlaceholderText = "Ví dụ: KV001";
        areaInput.Size = new Size(251, 23);
        areaInput.TabIndex = 0;
        // 
        // loadButton
        // 
        loadButton.BackColor = CafeManager.WinForms.Theme.AppTheme.Primary;
        loadButton.FlatAppearance.BorderSize = 0;
        loadButton.FlatAppearance.MouseOverBackColor = CafeManager.WinForms.Theme.AppTheme.PrimaryLight;
        loadButton.FlatStyle = FlatStyle.Flat;
        loadButton.ForeColor = CafeManager.WinForms.Theme.AppTheme.OnPrimary;
        loadButton.Location = new Point(310, 54);
        loadButton.Margin = new Padding(15, 34, 0, 0);
        loadButton.Name = "loadButton";
        loadButton.Size = new Size(238, 50);
        loadButton.TabIndex = 1;
        loadButton.Text = "&Tải trạng thái bàn";
        loadButton.UseVisualStyleBackColor = false;
        loadButton.Click += LoadButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.BackColor = CafeManager.WinForms.Theme.AppTheme.Background;
        cancelButton.Enabled = false;
        cancelButton.FlatAppearance.BorderColor = CafeManager.WinForms.Theme.AppTheme.Border;
        cancelButton.FlatStyle = FlatStyle.Flat;
        cancelButton.ForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        cancelButton.Location = new Point(563, 54);
        cancelButton.Margin = new Padding(15, 34, 0, 0);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(138, 50);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "&Hủy tải";
        cancelButton.UseVisualStyleBackColor = false;
        cancelButton.Click += CancelButton_Click;
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.BackColor = CafeManager.WinForms.Theme.AppTheme.Selected;
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Location = new Point(30, 302);
        statusLabel.Margin = new Padding(0, 0, 0, 15);
        statusLabel.Name = "statusLabel";
        statusLabel.Padding = new Padding(20, 15, 20, 15);
        statusLabel.Size = new Size(1190, 53);
        statusLabel.TabIndex = 2;
        statusLabel.Text = "Sẵn sàng · Chọn khu vực rồi tải trạng thái bàn.";
        // 
        // contentPanel
        // 
        contentPanel.BackColor = CafeManager.WinForms.Theme.AppTheme.Surface;
        contentPanel.Controls.Add(gridPanel);
        contentPanel.Controls.Add(contentHeading);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(30, 370);
        contentPanel.Margin = new Padding(0);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1190, 350);
        contentPanel.TabIndex = 3;
        // 
        // gridPanel
        // 
        gridPanel.AccessibleName = "Nội dung danh sách bàn";
        gridPanel.AutoScroll = true;
        gridPanel.Controls.Add(tableGrid);
        gridPanel.Controls.Add(emptyStateLabel);
        gridPanel.Dock = DockStyle.Fill;
        gridPanel.Location = new Point(0, 65);
        gridPanel.Margin = new Padding(4);
        gridPanel.Name = "gridPanel";
        gridPanel.Padding = new Padding(20, 0, 20, 20);
        gridPanel.Size = new Size(1190, 285);
        gridPanel.TabIndex = 0;
        // 
        // tableGrid
        // 
        tableGrid.AllowUserToAddRows = false;
        tableGrid.AllowUserToDeleteRows = false;
        dataGridViewCellStyle1.BackColor = CafeManager.WinForms.Theme.AppTheme.Background;
        tableGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        tableGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tableGrid.BackgroundColor = CafeManager.WinForms.Theme.AppTheme.Surface;
        tableGrid.BorderStyle = BorderStyle.None;
        tableGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        tableGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dataGridViewCellStyle2.BackColor = CafeManager.WinForms.Theme.AppTheme.Selected;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        dataGridViewCellStyle2.SelectionBackColor = CafeManager.WinForms.Theme.AppTheme.Selected;
        dataGridViewCellStyle2.SelectionForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        tableGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        tableGrid.ColumnHeadersHeight = 40;
        tableGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        tableGrid.Columns.AddRange(new DataGridViewColumn[] { tableCodeColumn, tableNameColumn, areaCodeColumn, statusColumn, invoiceCodeColumn });
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = CafeManager.WinForms.Theme.AppTheme.Surface;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 10F);
        dataGridViewCellStyle3.ForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        dataGridViewCellStyle3.SelectionBackColor = CafeManager.WinForms.Theme.AppTheme.Selected;
        dataGridViewCellStyle3.SelectionForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        tableGrid.DefaultCellStyle = dataGridViewCellStyle3;
        tableGrid.Dock = DockStyle.Fill;
        tableGrid.EnableHeadersVisualStyles = false;
        tableGrid.GridColor = CafeManager.WinForms.Theme.AppTheme.Border;
        tableGrid.Location = new Point(20, 29);
        tableGrid.Margin = new Padding(4);
        tableGrid.MultiSelect = false;
        tableGrid.Name = "tableGrid";
        tableGrid.ReadOnly = true;
        tableGrid.RowHeadersVisible = false;
        tableGrid.RowHeadersWidth = 51;
        tableGrid.RowTemplate.Height = 36;
        tableGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        tableGrid.Size = new Size(1150, 236);
        tableGrid.TabIndex = 3;
        // 
        // tableCodeColumn
        // 
        tableCodeColumn.DataPropertyName = "MaBan";
        tableCodeColumn.HeaderText = "Mã bàn";
        tableCodeColumn.MinimumWidth = 6;
        tableCodeColumn.Name = "tableCodeColumn";
        tableCodeColumn.ReadOnly = true;
        // 
        // tableNameColumn
        // 
        tableNameColumn.DataPropertyName = "TenBan";
        tableNameColumn.HeaderText = "Tên bàn";
        tableNameColumn.MinimumWidth = 6;
        tableNameColumn.Name = "tableNameColumn";
        tableNameColumn.ReadOnly = true;
        // 
        // areaCodeColumn
        // 
        areaCodeColumn.DataPropertyName = "MaKV";
        areaCodeColumn.HeaderText = "Khu vực";
        areaCodeColumn.MinimumWidth = 6;
        areaCodeColumn.Name = "areaCodeColumn";
        areaCodeColumn.ReadOnly = true;
        // 
        // statusColumn
        // 
        statusColumn.DataPropertyName = "TrangThai";
        statusColumn.HeaderText = "Trạng thái";
        statusColumn.MinimumWidth = 6;
        statusColumn.Name = "statusColumn";
        statusColumn.ReadOnly = true;
        // 
        // invoiceCodeColumn
        // 
        invoiceCodeColumn.DataPropertyName = "MaHD";
        invoiceCodeColumn.HeaderText = "Hóa đơn mở";
        invoiceCodeColumn.MinimumWidth = 6;
        invoiceCodeColumn.Name = "invoiceCodeColumn";
        invoiceCodeColumn.ReadOnly = true;
        // 
        // emptyStateLabel
        // 
        emptyStateLabel.AccessibleName = "Trạng thái danh sách bàn";
        emptyStateLabel.Dock = DockStyle.Top;
        emptyStateLabel.Font = new Font("Segoe UI", 12F);
        emptyStateLabel.ForeColor = CafeManager.WinForms.Theme.AppTheme.Primary;
        emptyStateLabel.Location = new Point(20, 0);
        emptyStateLabel.Margin = new Padding(4, 0, 4, 0);
        emptyStateLabel.Name = "emptyStateLabel";
        emptyStateLabel.Padding = new Padding(15);
        emptyStateLabel.Size = new Size(1150, 29);
        emptyStateLabel.TabIndex = 4;
        emptyStateLabel.Text = "Chưa tải dữ liệu\r\n\r\nChọn khu vực hoặc để trống, rồi nhấn Tải trạng thái bàn.";
        emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // contentHeading
        // 
        contentHeading.Dock = DockStyle.Top;
        contentHeading.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        contentHeading.Location = new Point(0, 0);
        contentHeading.Margin = new Padding(4, 0, 4, 0);
        contentHeading.Name = "contentHeading";
        contentHeading.Padding = new Padding(20, 15, 20, 10);
        contentHeading.Size = new Size(1190, 65);
        contentHeading.TabIndex = 1;
        contentHeading.Text = "Danh sách bàn";
        // 
        // StartupForm
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = CafeManager.WinForms.Theme.AppTheme.Background;
        ClientSize = new Size(1250, 750);
        Controls.Add(layout);
        ForeColor = CafeManager.WinForms.Theme.AppTheme.Text;
        Margin = new Padding(4);
        MinimumSize = new Size(360, 320);
        Name = "StartupForm";
        StartPosition = FormStartPosition.CenterScreen;
        validation.ContainerControl = this;
        layout.ResumeLayout(false);
        layout.PerformLayout();
        header.ResumeLayout(false);
        header.PerformLayout();
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        filter.ResumeLayout(false);
        filter.PerformLayout();
        inputPanel.ResumeLayout(false);
        inputPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        gridPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)tableGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)validation).EndInit();
        ResumeLayout(false);
    }
}
