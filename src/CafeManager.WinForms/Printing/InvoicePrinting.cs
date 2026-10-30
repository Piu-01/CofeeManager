using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;

namespace CafeManager.WinForms.Printing;

// Dùng cùng đường kiểm quyền/chọn máy in cho POS và lịch sử hóa đơn.
internal static class InvoicePrinting
{
    internal static async Task<bool> PrintAsync(Control owner, InvoiceDetailDto detail,
        InvoiceService service, SessionContext session, bool reprint = true)
    {
        await service.DemandPrintAsync(session);
        if (owner.IsDisposed) return false;
        using var document = new InvoicePrintDocument(detail, reprint);
        using var dialog = new PrintDialog { Document = document, UseEXDialog = true };
        if (dialog.ShowDialog(UI.OrbitDialogHost.NativeOwner(owner)) != DialogResult.OK) return false;
        document.Print();
        return true;
    }
}
