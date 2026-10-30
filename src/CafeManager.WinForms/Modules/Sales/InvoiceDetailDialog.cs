using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class InvoiceDetailDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    public bool ReturnToPos { get; private set; }
    internal InvoiceDetailDialog(InvoiceDetailDto detail, InvoiceService service, SessionContext session)
    {
        var h = detail.Order.Header; Text = "Chi tiết hóa đơn · " + h.Code;
        var lines = SalesUi.Grid(("Product", "Mã"), ("Name", "Tên hiện tại"), ("Quantity", "SL"), ("Price", "Giá snapshot"), ("Note", "Ghi chú"), ("Amount", "Thành tiền"));
        lines.DataSource = detail.Order.Lines.ToList();
        var summary = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12), Text =
            $"{h.Code} · {h.Created:dd/MM/yyyy HH:mm} · {SalesStates.Label(h.Channel)} · {SalesStates.Label(h.Status)}\n{SalesStates.Label(h.Delivery)} · Khách {h.Customer ?? "Vãng lai"}\nMón {SalesUi.Money(h.Total)} · Giảm {SalesUi.Money(h.Discount)} · Ship {SalesUi.Money(h.Ship)} · Phải trả {SalesUi.Money(h.Payable)}\n" +
            (detail.SnapshotKind == "LegacyUnknown" ? "Nguồn/tỷ lệ ưu đãi lịch sử chưa lưu." : $"{SalesStates.Label(detail.Source)} {detail.PercentDiscount}% · Điểm mua đã cộng {detail.Points}") +
            "\n" + string.Join(" · ", detail.Order.Payments.Select(x => $"{x.Name}: {SalesUi.Money(x.Amount)}")) };
        var state = new Label { Dock = DockStyle.Bottom, AutoSize = true };
        var print = SalesUi.Button("Xem trước / In lại", async (_, _) =>
        {
            try
            {
                await service.DemandPrintAsync(session); if (IsDisposed) return;
                using var dialog = new InvoicePrintPreviewForm(detail, service, session); await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this);
            }
            catch (Exception ex) when (ex is global::System.Drawing.Printing.InvalidPrinterException or global::System.ComponentModel.Win32Exception or InvalidOperationException or UnauthorizedAccessException or UserOperationException)
            { if (!IsDisposed) state.Text = "Chưa in được; hóa đơn giữ nguyên. " + ex.Message; }
        });
        print.Enabled = h.Status == "Da thanh toan";
        var pos = SalesUi.Button("Quay lại POS", (_, _) => { ReturnToPos = true; DialogResult = DialogResult.OK; Close(); }); pos.Enabled = h.IsOpen;
        Controls.Add(lines); Controls.Add(summary); Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 60, Controls = { SalesUi.Bar(print, pos, SalesUi.Button("Đóng", (_, _) => Close())) } }); Controls.Add(state);
        AppTheme.Apply(this); SalesUi.Fit(this, new Size(900, 620));
    }
}


