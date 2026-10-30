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
            throw new ArgumentException("Nhập số tiền nguyên VND hợp lệ.");
        Business.Services.LoyaltyService.Money(amount); return amount;
    }
}

internal sealed class OrderLineDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = int.MaxValue, Value = 1, Width = 140 };
    private readonly TextBox note = new() { MaxLength = 100, Dock = DockStyle.Fill };
    public int Quantity => (int)quantity.Value;
    public string? Note => string.IsNullOrWhiteSpace(note.Text) ? null : note.Text.Trim();
    public OrderLineDialog(string name, int current = 1, string? remark = null)
    {
        Text = "Món · " + name; quantity.Value = current; note.Text = remark;
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16), ColumnCount = 1 };
        body.Controls.Add(new Label { Text = name, AutoSize = true }); body.Controls.Add(new Label { Text = "Số lượng ly", AutoSize = true });
        body.Controls.Add(quantity); body.Controls.Add(new Label { Text = "Ghi chú (tối đa100 ký tự)", AutoSize = true }); body.Controls.Add(note);
        var ok = SalesUi.Button("Áp dụng", (_, _) => { DialogResult = DialogResult.OK; Close(); });
        var cancel = SalesUi.Button("Quay lại", (_, _) => Close());
        Controls.Add(body); Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 56, Controls = { SalesUi.Bar(ok, cancel) } });
        CancelButton = cancel; AppTheme.Apply(this); SalesUi.Fit(this, new Size(480, 300));
    }
}

