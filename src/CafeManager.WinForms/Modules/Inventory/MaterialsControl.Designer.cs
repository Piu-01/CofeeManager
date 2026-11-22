using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Inventory;

public partial class MaterialsControl
{
    private OrbitDataGridView grid = null!;
    private TextBox search = null!;
    private ComboBox unitFilter = null!;
    private Label state = null!;
    private OrbitListWorkspace workspace = null!;
    private OrbitEmptyState empty = null!;
    private OrbitButton add = null!, previous = null!, next = null!;
    private readonly ToolTip hints = new();
    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = OrbitManagementTheme.Background;
        grid = new OrbitDataGridView { Dock = DockStyle.Fill };
        foreach (var column in new[] { ("Code", L.T("MÃ")), ("Name", L.T("NGUYÊN LIỆU")), ("Unit", L.T("ĐƠN VỊ")), ("Quantity", L.T("TỒN HIỆN TẠI")) })
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = column.Item1, DataPropertyName = column.Item1, HeaderText = column.Item2 });
        grid.Columns["Code"].DefaultCellStyle.Font = OrbitManagementTheme.Code;
        grid.Columns["Code"].DefaultCellStyle.ForeColor = OrbitManagementTheme.Muted;
        grid.Columns["Unit"].DefaultCellStyle.ForeColor = OrbitManagementTheme.Muted;
        grid.Columns["Quantity"].DefaultCellStyle.Format = "N4";
        grid.Columns["Quantity"].DefaultCellStyle.FormatProvider = global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
        grid.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Quantity"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        var searchBox = new OrbitSearchBox(); search = searchBox.Input;
        workspace = new OrbitListWorkspace(searchBox); state = workspace.Status;
        workspace.Title.Text = L.T("Nguyên liệu & tồn kho"); workspace.Description.Text = L.T("Tra cứu nguyên liệu, đơn vị và số lượng tồn.");
        add = new OrbitButton { Text = L.T("Thêm nguyên liệu") };
        var refresh = new OrbitButton { AccessibleName = L.T("Tải lại danh sách") };
        OrbitManagementTheme.Button(add, ManagementButtonStyle.Primary, "plus");
        OrbitManagementTheme.Button(refresh, ManagementButtonStyle.Icon, "refresh"); hints.SetToolTip(refresh, L.T("Tải lại danh sách"));
        var filterBox = new OrbitFilterBox { Size = new Size(124, 40) }; unitFilter = filterBox.Input;
        unitFilter.Items.Add(L.T("Tất cả đơn vị")); unitFilter.SelectedIndex = 0;
        workspace.PrimaryActions.Controls.Add(add); workspace.Filters.Controls.AddRange([filterBox, refresh]);
        previous = workspace.Pagination.Previous; next = workspace.Pagination.Next;
        empty = new OrbitEmptyState { Dock = DockStyle.Fill, Visible = false };
        workspace.Body.Controls.AddRange([grid, empty]); Controls.Add(workspace);
        refresh.Click += async (_, _) => { page = 0; await LoadRows(); };
        previous.Click += (_, _) => { if (page > 0) { page--; RenderRows(); } };
        next.Click += (_, _) => { if ((page + 1) * PageSize < FilteredRows().Count) { page++; RenderRows(); } };
        unitFilter.SelectedIndexChanged += (_, _) => { if (!bindingUnits) { page = 0; RenderRows(); } };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 0; await LoadRows(); } };
        add.Click += async (_, _) => await Create();
        ResumeLayout(true);
    }
}

