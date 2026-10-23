using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.Models.Dtos.Inventory;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationConfiguration.Initialize();
        if(args.Length==2 && args[0]=="--development") { DevelopmentPreview.Run(args[1]); return; }
        var output = Path.GetFullPath(args.Single()); Directory.CreateDirectory(output);
        using var form = new Form { Text = "T09 · Kiểm kỹ thuật bố cục — dữ liệu mẫu", ClientSize = new Size(1100, 640) };
        using var module = new MaterialsControl { Dock = DockStyle.Fill }; form.Controls.Add(module);
        IEnumerable<Control> All(Control c) { foreach(Control child in c.Controls) { yield return child; foreach(var nested in All(child)) yield return nested; } }
        var grid = All(module).OfType<DataGridView>().Single(); var status = All(module).OfType<Label>().Single(l => l.Text != "Nguyên liệu và tồn");
        form.Shown += (_, _) =>
        {
            foreach (var state in new[] { "Đang tải — dữ liệu mẫu", "Chưa có nguyên liệu — dữ liệu mẫu", "Chưa cập nhật — lỗi mẫu", "Dữ liệu mẫu kỹ thuật" })
            {
                status.Text = state;
                if (state.StartsWith("Dữ liệu")) grid.DataSource = new[] { new MaterialRow("NVL98", "Nguyên liệu có tên tiếng Việt dài để kiểm tra bố cục", "Kg", 0.0180m, null, new byte[8], DateTime.Now) };
                form.Refresh(); using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, form.ClientRectangle);
                bitmap.Save(Path.Combine(output, $"state-{Array.IndexOf(new[] { "Đang tải — dữ liệu mẫu", "Chưa có nguyên liệu — dữ liệu mẫu", "Chưa cập nhật — lỗi mẫu", "Dữ liệu mẫu kỹ thuật" },state)}.png"));
            }
            using var editor = new OperationEditorDialog(); editor.AddText("Tên nguyên liệu", "Cà phê"); editor.AddText("Đơn vị chuẩn", "Kg"); editor.Show(form); editor.Refresh();
            using var image = new Bitmap(editor.Width, editor.Height); editor.DrawToBitmap(image, new Rectangle(Point.Empty, editor.Size)); image.Save(Path.Combine(output, "editor.png")); editor.Close(); form.Close();
        };
        Application.Run(form);
    }
}


