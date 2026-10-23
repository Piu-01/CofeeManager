namespace CafeManager.WinForms.Forms;

public partial class OperationEditorDialog
{
    private TableLayoutPanel fields = null!;
    private Label status = null!;
    private UI.OrbitButton save = null!, check = null!, cancel = null!;
    private ErrorProvider errors = null!;
    private UI.OrbitDialogLayout layout = null!;
    private void InitializeComponent()
    {
        fields = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 1, RowCount = 0, Padding = new Padding(0, 4, 0, 16) };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        status = new Label();
        save = new UI.OrbitButton { Text = "Lưu", AutoSize = true }; check = new UI.OrbitButton { Text = "Đối chiếu", AutoSize = true, Enabled = false };
        cancel = new UI.OrbitButton { Text = "Hủy", AutoSize = true };
        var footer = new FlowLayoutPanel();
        footer.Controls.AddRange([save, check, cancel]);
        layout = new UI.OrbitDialogLayout(this, fields, status, footer); Controls.Add(layout);
        errors = new ErrorProvider { ContainerControl = this, BlinkStyle = ErrorBlinkStyle.NeverBlink };
        save.Click += Save_Click; check.Click += Reconcile_Click; cancel.Click += (_, _) => Close();
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(620, 460);
        MinimumSize = new Size(460, 300); StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; MinimizeBox = false; CancelButton = cancel;
    }
}
