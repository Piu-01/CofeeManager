using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Forms;

public sealed class InvoicePrintPreviewForm : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly InvoicePrintDocument document;
    private readonly Label state = new() { Dock = DockStyle.Bottom, AutoSize = true };
    public InvoicePrintPreviewForm(InvoiceDetailDto detail, InvoiceService service, SessionContext session)
    {
        Text = "Xem trước / In lại · " + detail.Order.Header.Code;
        document = new InvoicePrintDocument(detail);
        var preview = new SafePreviewControl(message => state.Text = message) { Dock = DockStyle.Fill, Document = document, AutoZoom = true };
        var print = new UI.OrbitButton { Text = "In…", AutoSize = true, Dock = DockStyle.Top };
        print.Click += async (_, _) =>
        {
            print.Enabled = false;
            try
            {
                if (await InvoicePrinting.PrintAsync(this, detail, service, session) && !IsDisposed)
                    state.Text = "Đã gửi yêu cầu in. Dữ liệu thanh toán giữ nguyên.";
            }
            catch (Exception ex) when (ex is System.Drawing.Printing.InvalidPrinterException or System.ComponentModel.Win32Exception or InvalidOperationException or UnauthorizedAccessException or UserOperationException)
            { if (!IsDisposed) state.Text = "Chưa in được. Kết quả hóa đơn không thay đổi. " + ex.Message; }
            finally { if (!IsDisposed) print.Enabled = true; }
        };
        Controls.Add(preview); Controls.Add(print); Controls.Add(state);
        AppTheme.Apply(this); Modules.Sales.SalesUi.Fit(this, new Size(900, 650));
    }
    protected override void Dispose(bool disposing) { if (disposing) document.Dispose(); base.Dispose(disposing); }

    private sealed class SafePreviewControl(Action<string> report) : PrintPreviewControl
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            try { base.OnPaint(e); }
            catch (Exception ex) when (ex is System.Drawing.Printing.InvalidPrinterException or System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Document = null;
                report("Không tạo được preview. Hóa đơn giữ nguyên; kiểm tra máy in và mở lại. " + ex.Message);
            }
        }
    }
}

