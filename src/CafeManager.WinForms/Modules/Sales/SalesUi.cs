using System.Globalization;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal static class SalesUi
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("vi-VN");
    public static string Money(decimal value) => value.ToString("N0", Culture) + " ₫";
    public static OrbitButton Button(string caption, EventHandler? click = null)
    {
        var button = new OrbitButton { Text = caption, AutoSize = true, MinimumSize = new Size(72, 38), Margin = new Padding(4), AccessibleName = caption };
        if (click is not null) button.Click += click;
        return button;
    }
    public static FlowLayoutPanel Bar(params Control[] items)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(4) };
        bar.Controls.AddRange(items); return bar;
    }
    public static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, DisplayMember = "Name", ValueMember = "Code", Margin = new Padding(4) };
    public static DataGridView Grid(params (string Property, string Caption)[] columns)
    {
        var grid = new OrbitDataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        foreach (var (property, caption) in columns) grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, Name = property, HeaderText = caption });
        AppTheme.Apply(grid); return grid;
    }
    public static void Fit(Form form, Size desired)
    {
        form.AutoScaleMode = AutoScaleMode.Dpi; form.AutoScaleDimensions = new SizeF(96, 96);
        form.StartPosition = FormStartPosition.CenterParent;
        var area = Screen.FromControl(form.Owner ?? form).WorkingArea;
        var scale = form.DeviceDpi / 96F;
        form.Size = new Size(Math.Min(area.Width, (int)(desired.Width * scale)), Math.Min(area.Height, (int)(desired.Height * scale)));
    }
    public static decimal ParseMoney(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Culture, out var amount))
            throw new ArgumentException(L.T("Nhập số tiền nguyên VND hợp lệ."));
        Business.Services.LoyaltyService.Money(amount); return amount;
    }
}

