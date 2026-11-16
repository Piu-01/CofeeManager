using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Printing.FastReport;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CafeManager.WinForms.Forms;

// Free FastReport không có PrintPreviewControl: xem PDF sinh sẵn trong WebView2, in qua print UI của Edge.
internal sealed class PdfPreviewForm : OrbitDialogForm
{
    private readonly WebView2 web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
    private readonly Label state = new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10, 4, 10, 8) };
    private readonly OrbitButton print = new() { Text = L.T("In…"), AutoSize = true };
    private readonly OrbitButton save = new() { Text = L.T("Lưu PDF…"), AutoSize = true };
    private readonly OrbitButton open = new() { Text = L.T("Mở bằng trình đọc PDF"), AutoSize = true };
    private readonly Func<InvoicePaper, string>? repaper;
    private readonly string fileTitle;
    private string path;
    private bool external, ready, changing;

    public PdfPreviewForm(string path, string title, Func<InvoicePaper, string>? repaper = null)
    {
        this.path = path;
        this.repaper = repaper;
        fileTitle = title;
        Text = L.T("Xem trước · ") + title;
        print.Enabled = save.Enabled = open.Enabled = false;
        print.Click += (_, _) => Print();
        save.Click += (_, _) => SavePdf();
        open.Click += (_, _) => OpenExternal();
        var close = new OrbitButton { Text = L.T("Đóng"), AutoSize = true };
        close.Click += (_, _) => Close();
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(10, 8, 10, 8),
        };
        bar.Controls.Add(print);
        bar.Controls.Add(save);
        bar.Controls.Add(open);
        bar.Controls.Add(close);
        if (repaper is not null)
        {
            var paper = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Margin = new Padding(12, 4, 4, 4) };
            paper.Items.AddRange([L.T("Khổ A4"), L.T("Khổ 80mm")]);
            paper.SelectedIndex = 0;
            paper.SelectedIndexChanged += async (_, _) =>
            {
                if (changing) return;
                await ChangePaper(paper.SelectedIndex == 0 ? InvoicePaper.A4 : InvoicePaper.Roll80);
            };
            bar.Controls.Add(new Label { Text = L.T("Khổ in"), AutoSize = true, Margin = new Padding(12, 8, 4, 4) });
            bar.Controls.Add(paper);
        }
        state.Text = L.T("Đang chuẩn bị bản xem trước…");
        Controls.Add(web);
        Controls.Add(state);
        Controls.Add(bar);
        AppTheme.Apply(this);
        Size = new Size(1000, 700);
        MinimumSize = new Size(720, 520);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            await web.EnsureCoreWebView2Async();
            if (IsDisposed) return;
            web.CoreWebView2.Navigate(new Uri(path).AbsoluteUri);
            ready = true;
            print.Enabled = save.Enabled = open.Enabled = true;
            state.Text = L.T("Bản xem trước đã sẵn sàng. In mở hộp in của Windows/Edge; đóng hộp này không thay đổi hóa đơn đã chốt.");
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or System.Runtime.InteropServices.COMException or IOException)
        {
            if (IsDisposed) return;
            state.Text = L.T("Không hiển thị được bản xem trước trong app. ") + ex.Message;
            open.Enabled = File.Exists(path);
            web.Visible = false;
            if (File.Exists(path)) OpenExternal();
        }
    }

    private async Task ChangePaper(InvoicePaper paper)
    {
        if (repaper is null) return;
        changing = true;
        print.Enabled = save.Enabled = false;
        state.Text = L.T("Đang tạo lại PDF theo khổ đã chọn…");
        try
        {
            var next = await Task.Run(() => repaper(paper));
            if (IsDisposed) { PrintOutput.TryDelete(next); return; }
            PrintOutput.TryDelete(path);
            path = next;
            web.CoreWebView2?.Navigate(new Uri(path).AbsoluteUri);
            state.Text = L.T("Bản xem trước đã sẵn sàng. In mở hộp in của Windows/Edge; đóng hộp này không thay đổi hóa đơn đã chốt.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            state.Text = L.T("Chưa tạo được PDF theo khổ này: ") + ex.Message;
        }
        finally
        {
            changing = false;
            if (!IsDisposed) print.Enabled = save.Enabled = ready;
        }
    }

    private void Print()
    {
        if (!ready || web.CoreWebView2 is null) return;
        try
        {
            web.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
            state.Text = L.T("Đã mở hộp in. Hủy in không ảnh hưởng hóa đơn/dữ liệu đã chốt.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            state.Text = L.T("Chưa mở được hộp in: ") + ex.Message + L.T(" Có thể dùng “Mở bằng trình đọc PDF”.");
        }
    }

    private void SavePdf()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = L.T("PDF (*.pdf)|*.pdf"),
            FileName = SafeName(fileTitle) + ".pdf",
            AddExtension = true,
            DefaultExt = "pdf",
        };
        if (dialog.ShowDialog(OrbitDialogHost.NativeOwner(this)) != DialogResult.OK) return;
        try
        {
            File.Copy(path, dialog.FileName, true);
            state.Text = L.T("Đã lưu PDF: ") + dialog.FileName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            state.Text = L.T("Chưa lưu được PDF: ") + ex.Message;
        }
    }

    private void OpenExternal()
    {
        try
        {
            PrintOutput.OpenExternal(path);
            external = true;
            state.Text = L.T("Đã mở PDF bằng trình đọc bên ngoài. In từ đó nếu hộp in trong app không dùng được.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            state.Text = L.T("Không mở được trình đọc PDF: ") + ex.Message;
        }
    }

    private static string SafeName(string value) =>
        string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            web.Dispose();
            if (!external) PrintOutput.TryDelete(path);
        }
        base.Dispose(disposing);
    }
}
