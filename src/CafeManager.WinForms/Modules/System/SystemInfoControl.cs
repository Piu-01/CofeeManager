using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.System;

public sealed class SystemInfoControl : UserControl
{
    private Func<AppLanguage, Task>? applyLanguage;
    private readonly Label information;
    internal void SetLanguageHandler(Func<AppLanguage, Task> handler) => applyLanguage = handler;
    public SystemInfoControl()
    {
        Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var heading = new UI.OrbitHeader { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), ColumnCount = 1, RowCount = 1 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); heading.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label { Text = L.T("Thông tin hệ thống"), AutoSize = true, Font = AppTheme.TitleFont, Dock = DockStyle.Fill };
        heading.Controls.Add(title);
        information = new Label { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), Margin = new Padding(0, 20, 0, 0), Text = L.T("CoffeeManager · .NET 10\r\nChưa tải thông tin schema.") };
        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(information, 0, 1); Controls.Add(layout); AppTheme.Apply(this);
        Controls.Add(new UI.OrbitContentHost(layout, 880));
        title.BackColor = Color.Transparent; information.BackColor = AppTheme.Surface; information.ForeColor = AppTheme.Muted;
        var choices = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = new Padding(0, 20, 0, 0) };
        var languages = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, AccessibleName = L.T("Ngôn ngữ") };
        languages.Items.AddRange([L.T("Tiếng Việt"), "English"]);
        languages.SelectedIndex = new Configuration.LanguagePreference().Read() == AppLanguage.English ? 1 : 0;
        var apply = new UI.OrbitButton { Text = L.T("Áp dụng"), AutoSize = true };
        choices.Controls.Add(new Label { Text = L.T("Ngôn ngữ"), AutoSize = true, Anchor = AnchorStyles.Left });
        choices.Controls.Add(languages); choices.Controls.Add(apply); layout.Controls.Add(choices, 0, 2); AppTheme.Apply(choices);
        apply.Click += async (_, _) =>
        {
            apply.Enabled = false;
            try
            {
                var language = languages.SelectedIndex == 1 ? AppLanguage.English : AppLanguage.Vietnamese;
                if (applyLanguage is { } apply) await apply(language);
            }
            finally { if (!IsDisposed) apply.Enabled = true; }
        };
    }
    public void SetSchema(int version) => information.Text = L.F("CoffeeManager · .NET 10\r\nSchema version: {0}\r\nKết nối được xác nhận khi đọc metadata. Không có tích hợp thanh toán online.", version);
}
