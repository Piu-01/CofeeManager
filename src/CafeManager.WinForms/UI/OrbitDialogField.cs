using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

internal sealed class OrbitDialogField : TableLayoutPanel
{
    private readonly Label caption;
    private readonly Control input;
    public bool Expands { get; }

    public OrbitDialogField(string label, Control input, bool expands = false)
    {
        this.input = input; Expands = expands;
        ColumnCount = 1; RowCount = 2; Dock = DockStyle.Fill; AutoSize = !expands; AutoSizeMode = AutoSizeMode.GrowAndShrink; TabStop = false;
        Margin = new Padding(16, 12, 16, 4); Padding = Padding.Empty; BackColor = OrbitManagementTheme.Surface;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(expands ? SizeType.Percent : SizeType.AutoSize, expands ? 100 : 0));
        caption = new Label { Text = label, AutoSize = true, Dock = DockStyle.Fill, ForeColor = OrbitManagementTheme.Muted, Font = OrbitManagementTheme.Table, Margin = new Padding(0, 0, 0, 8), UseMnemonic = true };
        input.AccessibleName = label; input.Margin = Padding.Empty;
        OrbitManagementTheme.DialogInput(input);
        Control presented = input is TextBox or ComboBox ? new OrbitField(input, true) : input;
        presented.Dock = expands ? DockStyle.Fill : DockStyle.Top;
        if (input is Label note) { note.AutoSize = true; note.MaximumSize = Size.Empty; }
        Controls.Add(caption, 0, 0); Controls.Add(presented, 0, 1);
        if (expands) MinimumSize = new Size(0, 180);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        var width = Math.Max(1, ClientSize.Width - Padding.Horizontal);
        if (caption is not null) caption.MaximumSize = new Size(width, 0);
        if (input is Label note) note.MaximumSize = new Size(width, 0);
        base.OnLayout(levent);
    }
}
