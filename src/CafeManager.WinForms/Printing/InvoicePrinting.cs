using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing.FastReport;

namespace CafeManager.WinForms.Printing;

// In hóa đơn đi qua PDF FastReport + preview WebView2; quyền in vẫn do BLL kiểm trước khi mở preview.
internal static class InvoicePrinting
{
    internal static async Task<bool> PreviewAsync(Control owner, InvoiceDetailDto detail,
        InvoiceService service, SessionContext session, bool reprint = true)
    {
        await service.DemandPrintAsync(session);
        if (owner.IsDisposed) return false;
        PrintOutput.CleanupOld();
        var code = detail.Order.Header.Code;
        var path = await Task.Run(() => InvoicePdfReport.Write(detail, reprint, InvoicePaper.A4));
        if (owner.IsDisposed)
        {
            PrintOutput.TryDelete(path);
            return false;
        }
        using var dialog = new PdfPreviewForm(path, L.T("Hóa đơn ") + code,
            paper => InvoicePdfReport.Write(detail, reprint, paper));
        await UI.OrbitDialogHost.ShowAsync(dialog, owner);
        return true;
    }
}
