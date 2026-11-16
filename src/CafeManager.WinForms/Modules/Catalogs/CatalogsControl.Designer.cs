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
        searchInput.MaxLength = 100; searchInput.PlaceholderText = L.T("Tìm tên danh mục…"); searchInput.AccessibleName = L.T("Tìm tên danh mục");
        management = new OrbitListWorkspace(searchBox); statusLabel = management.Status;
        management.Eyebrow.Text = "REFERENCE WORKSPACE"; management.Title.Text = L.T("Danh mục nền"); management.Description.Text = L.T("Dữ liệu tham chiếu · CoffeeManager");
        tabs = new OrbitSectionTabs();
        foreach (var caption in new[] { L.T("Sản phẩm"), L.T("Nhà cung cấp"), L.T("Phương thức"), L.T("Đối tác"), L.T("Thu / chi"), L.T("Chức vụ"), L.T("Ca làm") }) tabs.Add(caption);
        management.Tabs.Controls.Add(tabs);
        searchButton = MakeButton("", ManagementButtonStyle.Icon, "search", L.T("Tìm"));
        reloadButton = MakeButton(L.T("Tải / đối chiếu"), ManagementButtonStyle.Secondary, "refresh", L.T("Tải / đối chiếu"));
        addButton = MakeButton(L.T("Thêm"), ManagementButtonStyle.Primary, "plus", L.T("Thêm"));
        editButton = MakeButton(L.T("Sửa"), ManagementButtonStyle.Action, "pencil", L.T("Sửa"));
        deleteButton = MakeButton(L.T("Xóa"), ManagementButtonStyle.Danger, "trash", L.T("Xóa"));
        previousButton = management.Pagination.Previous; nextButton = management.Pagination.Next;
        management.Filters.Controls.AddRange([searchButton, reloadButton]);
        management.PrimaryActions.Controls.Add(addButton); management.RowActions.Controls.AddRange([editButton, deleteButton]);
        grid = new OrbitDataGridView { Dock = DockStyle.Fill };
        codeColumn = Column("Code", L.T("Mã")); nameColumn = Column("Name", L.T("Tên")); phoneColumn = Column("Phone", L.T("SDT"), false);
        ratioColumn = Column("Ratio", L.T("Chiết khấu (%)"), false); allowanceColumn = Column("Allowance", L.T("Phụ cấp"), false); factorColumn = Column("Factor", L.T("Hệ số tăng ca"), false);
        startColumn = Column("StartTime", L.T("Bắt đầu"), false); endColumn = Column("EndTime", L.T("Kết thúc"), false); typeColumn = Column("Type", L.T("Loại"), false);
        supportedColumn = new DataGridViewCheckBoxColumn { Name = "Supported", DataPropertyName = "Supported", HeaderText = L.T("Hỗ trợ thủ công"), ReadOnly = true, Visible = false };
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
