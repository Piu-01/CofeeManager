using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing.FastReport;

namespace CafeManager.WinForms.Printing;

// In phiếu nhập nội bộ qua PDF FastReport + preview WebView2; dữ liệu lấy từ bản đã đọc khi mở.
internal static class PurchasePrinting
{
    internal static async Task<bool> PreviewAsync(Control owner, PurchaseRow row)
    {
        if (owner.IsDisposed) return false;
        PrintOutput.CleanupOld();
        var path = await Task.Run(() => PurchaseSlipPdfReport.Write(row));
        if (owner.IsDisposed) { PrintOutput.TryDelete(path); return false; }
        using var dialog = new PdfPreviewForm(path, L.T("Phiếu nhập ") + row.Code);
        await UI.OrbitDialogHost.ShowAsync(dialog, owner);
        return true;
    }
}
