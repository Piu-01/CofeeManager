using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitField : Panel
{
    private static readonly ConditionalWeakTable<Control, object> inputHooks = new(), treeHooks = new();
    private readonly Control input;
    private readonly bool managementPalette;
    public OrbitField(Control input, bool managementPalette = false)
    {
        this.input = input; this.managementPalette = managementPalette; TabStop = false; TabIndex = input.TabIndex;
        BackColor = managementPalette ? OrbitManagementTheme.Input : AppTheme.Surface; Margin = input.Margin; Dock = input.Dock;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        input.Dock = DockStyle.None; input.Margin = Padding.Empty;
        if (input is TextBox text) text.BorderStyle = BorderStyle.None;
        Controls.Add(input);
        input.Enter += (_, _) => Invalidate(); input.Leave += (_, _) => Invalidate();
    }

    public static void Wrap(Control input, bool managementPalette = false)
    {
        if (input.Parent is not TableLayoutPanel parent) return;
        var cell = parent.GetPositionFromControl(input);
        var frame = new OrbitField(input, managementPalette);
        parent.Controls.Add(frame, cell.Column, cell.Row);
    }

    public static void ApplyInputBehavior(Control root)
    {
        if (!treeHooks.TryAdd(root, new object())) return;
        if (root is TextBoxBase or ComboBox) SelectAllOnEntry(root);
        root.ControlAdded += (_, e) => { if (e.Control is { } added) ApplyInputBehavior(added); };
        if (root is DataGridView grid)
            grid.EditingControlShowing += (_, e) =>
            {
                ApplyInputBehavior(e.Control);
                var editor = e.Control;
                if (editor.IsHandleCreated && (grid.EditMode == DataGridViewEditMode.EditOnEnter || Control.MouseButtons == MouseButtons.Left)) editor.BeginInvoke((Action)(() =>
                { if (!grid.IsDisposed && grid.IsCurrentCellInEditMode && grid.EditingControl == editor) SelectContents(editor); }));
            };
        foreach (Control child in root.Controls) ApplyInputBehavior(child);
    }
    private static void SelectContents(Control input)
    {
        if (!input.Enabled || input.IsDisposed) return;
        if (input is TextBoxBase { ReadOnly: false } text) text.SelectAll();
        else if (input is ComboBox combo && combo.DropDownStyle != ComboBoxStyle.DropDownList) combo.SelectAll();
    }
    public static void SelectAllOnEntry(Control input)
    {
        if (!inputHooks.TryAdd(input, new object())) return;
        var firstClick = false;
        input.Enter += (_, _) => { SelectContents(input); firstClick = true; };
        // Mouse-down native có thể đặt lại caret sau Enter; chọn lại ở lần mouse-up đầu.
        input.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || !firstClick) return;
            firstClick = false; SelectContents(input);
        };
        input.KeyDown += (_, _) => firstClick = false;
        input.Leave += (_, _) => firstClick = false;
    }

    public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width, Math.Max(managementPalette ? OrbitManagementTheme.Px(this, 40) : 0, input.PreferredSize.Height + (int)(18 * DeviceDpi / 96F)));
    protected override void OnLayout(LayoutEventArgs levent)
    {
        var inset = (int)(10 * DeviceDpi / 96F);
        Height = GetPreferredSize(new Size(Width, 0)).Height;
        input.SetBounds(inset, Math.Max(0, (Height - input.PreferredSize.Height) / 2), Math.Max(1, Width - inset * 2), input.PreferredSize.Height);
        base.OnLayout(levent);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var radius = 8 * DeviceDpi / 96F;
        var bounds = new RectangleF(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(bounds, new SizeF(radius, radius));
        using var pen = new Pen(ContainsFocus ? (managementPalette ? OrbitManagementTheme.Lilac : AppTheme.Accent) : (managementPalette ? OrbitManagementTheme.InputBorder : AppTheme.InputBorder), ContainsFocus ? 2 : 1);
        e.Graphics.DrawPath(pen, path);
    }
}
