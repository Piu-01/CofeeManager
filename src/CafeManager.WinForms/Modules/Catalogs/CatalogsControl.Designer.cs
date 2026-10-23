using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Catalogs;

partial class CatalogsControl
{
    private OrbitSectionTabs tabs = null!;
    private TextBox searchInput = null!;
    private Button searchButton = null!, reloadButton = null!, addButton = null!, editButton = null!, deleteButton = null!, previousButton = null!, nextButton = null!;
    private Label statusLabel = null!;
    private OrbitDataGridView grid = null!;
    private DataGridViewTextBoxColumn codeColumn = null!, nameColumn = null!, phoneColumn = null!, ratioColumn = null!, allowanceColumn = null!, factorColumn = null!, startColumn = null!, endColumn = null!, typeColumn = null!;
    private DataGridViewCheckBoxColumn supportedColumn = null!;
    private void InitializeComponent()
    {
        SuspendLayout();
        var searchBox = new OrbitSearchBox(); searchInput = searchBox.Input;
        searchInput.MaxLength = 100; searchInput.PlaceholderText = "Tìm tên danh mục…"; searchInput.AccessibleName = "Tìm tên danh mục";
        management = new OrbitListWorkspace(searchBox); statusLabel = management.Status;
        management.Eyebrow.Text = "REFERENCE WORKSPACE"; management.Title.Text = "Danh mục nền"; management.Description.Text = "Dữ liệu tham chiếu · CoffeeManager";
        tabs = new OrbitSectionTabs();
        foreach (var caption in new[] { "Sản phẩm", "Nhà cung cấp", "Phương thức", "Đối tác", "Thu / chi", "Chức vụ", "Ca làm" }) tabs.Add(caption);
        management.Tabs.Controls.Add(tabs);
        searchButton = MakeButton("", ManagementButtonStyle.Icon, "search", "Tìm");
        reloadButton = MakeButton("Tải / đối chiếu", ManagementButtonStyle.Secondary, "refresh", "Tải / đối chiếu");
        addButton = MakeButton("Thêm", ManagementButtonStyle.Primary, "plus", "Thêm");
        editButton = MakeButton("Sửa", ManagementButtonStyle.Action, "pencil", "Sửa");
        deleteButton = MakeButton("Xóa", ManagementButtonStyle.Danger, "trash", "Xóa");
        previousButton = management.Pagination.Previous; nextButton = management.Pagination.Next;
        management.Filters.Controls.AddRange([searchButton, reloadButton]);
        management.PrimaryActions.Controls.Add(addButton); management.RowActions.Controls.AddRange([editButton, deleteButton]);
        grid = new OrbitDataGridView { Dock = DockStyle.Fill };
        codeColumn = Column("Code", "Mã"); nameColumn = Column("Name", "Tên"); phoneColumn = Column("Phone", "SDT", false);
        ratioColumn = Column("Ratio", "Chiết khấu (%)", false); allowanceColumn = Column("Allowance", "Phụ cấp", false); factorColumn = Column("Factor", "Hệ số tăng ca", false);
        startColumn = Column("StartTime", "Bắt đầu", false); endColumn = Column("EndTime", "Kết thúc", false); typeColumn = Column("Type", "Loại", false);
        supportedColumn = new DataGridViewCheckBoxColumn { Name = "Supported", DataPropertyName = "Supported", HeaderText = "Hỗ trợ thủ công", ReadOnly = true, Visible = false };
        grid.Columns.AddRange([codeColumn, nameColumn, phoneColumn, ratioColumn, allowanceColumn, factorColumn, startColumn, endColumn, typeColumn, supportedColumn]);
        grid.Profile = OrbitGridProfile.Catalogs; grid.CellFormatting += Grid_CellFormatting;
        management.Body.Controls.AddRange([grid, emptyState]); Controls.Add(management);
        searchButton.Click += Search_Click; reloadButton.Click += Reload_Click; addButton.Click += Add_Click; editButton.Click += Edit_Click; deleteButton.Click += Delete_Click;
        previousButton.Click += Previous_Click; nextButton.Click += Next_Click;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; BackColor = OrbitManagementTheme.Background;
        ResumeLayout(true);
    }
    private static OrbitButton MakeButton(string text, ManagementButtonStyle style, string icon, string accessibleName)
    {
        var button = new OrbitButton { Text = text, AccessibleName = accessibleName }; OrbitManagementTheme.Button(button, style, icon); return button;
    }
    private static DataGridViewTextBoxColumn Column(string property, string header, bool visible = true) => new() { Name = property, DataPropertyName = property, HeaderText = header, Visible = visible };
}
