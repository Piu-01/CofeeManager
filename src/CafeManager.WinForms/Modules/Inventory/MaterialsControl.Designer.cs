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
    private OrbitButton add = null!, edit = null!, remove = null!, open = null!, previous = null!, next = null!;
    private readonly ToolTip hints = new();
    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = OrbitManagementTheme.Background;
        grid = new OrbitDataGridView { Dock = DockStyle.Fill };
        foreach (var column in new[] { ("Code", "MÃ"), ("Name", "NGUYÊN LIỆU"), ("Unit", "ĐƠN VỊ"), ("Quantity", "TỒN HIỆN TẠI") })
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
        workspace.Title.Text = "Nguyên liệu & tồn kho"; workspace.Description.Text = "Tra cứu nguyên liệu, đơn vị và số lượng tồn.";
        add = new OrbitButton { Text = "Thêm nguyên liệu" }; edit = new OrbitButton { Text = "Sửa" };
        remove = new OrbitButton { Text = "Xóa" }; open = new OrbitButton { Text = "Mở sổ" };
        var refresh = new OrbitButton { AccessibleName = "Tải lại danh sách" };
        OrbitManagementTheme.Button(add, ManagementButtonStyle.Primary, "plus");
        OrbitManagementTheme.Button(open, ManagementButtonStyle.Action, "notebook");
        OrbitManagementTheme.Button(edit, ManagementButtonStyle.Action, "pencil");
        OrbitManagementTheme.Button(remove, ManagementButtonStyle.Danger, "trash");
        OrbitManagementTheme.Button(refresh, ManagementButtonStyle.Icon, "refresh"); hints.SetToolTip(refresh, "Tải lại danh sách");
        var filterBox = new OrbitFilterBox { Size = new Size(124, 40) }; unitFilter = filterBox.Input;
        unitFilter.Items.Add("Tất cả đơn vị"); unitFilter.SelectedIndex = 0;
        workspace.PrimaryActions.Controls.Add(add); workspace.Filters.Controls.AddRange([filterBox, refresh]);
        workspace.RowActions.Controls.AddRange([open, edit, remove]);
        previous = workspace.Pagination.Previous; next = workspace.Pagination.Next;
        empty = new OrbitEmptyState { Dock = DockStyle.Fill, Visible = false };
        workspace.Body.Controls.AddRange([grid, empty]); Controls.Add(workspace);
        refresh.Click += async (_, _) => { page = 0; await LoadRows(); };
        previous.Click += (_, _) => { if (page > 0) { page--; RenderRows(); } };
        next.Click += (_, _) => { if ((page + 1) * PageSize < FilteredRows().Count) { page++; RenderRows(); } };
        unitFilter.SelectedIndexChanged += (_, _) => { if (!bindingUnits) { page = 0; RenderRows(); } };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; page = 0; await LoadRows(); } };
        grid.SelectionChanged += (_, _) => UpdateRowActions();
        add.Click += async (_, _) => await Edit(true); edit.Click += async (_, _) => await Edit(false);
        remove.Click += async (_, _) => await Delete(); open.Click += async (_, _) => await OpenBalance();
        ResumeLayout(true);
    }
}

