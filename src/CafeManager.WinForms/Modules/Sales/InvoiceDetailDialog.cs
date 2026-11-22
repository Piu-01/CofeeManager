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
        var h = detail.Order.Header; Text = L.T("Chi tiết hóa đơn · ") + h.Code;
        var lines = SalesUi.Grid(("Product", L.T("Mã")), ("Name", L.T("Tên hiện tại")), ("Quantity", L.T("SL")), ("Price", L.T("Giá snapshot")), ("Note", L.T("Ghi chú")), ("GiftPromotion", L.T("Quà")), ("Amount", L.T("Thành tiền")));
        lines.DataSource = detail.Order.Lines.ToList();
        lines.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || lines.Columns[e.ColumnIndex].Name != "GiftPromotion") return;
            if (e.Value is string) { e.Value = L.T("Tặng kèm"); e.FormattingApplied = true; }
        };
        var gifts = detail.Order.Lines.Where(x => x.GiftPromotion is not null).Select(x => $"{x.Name} ×{x.Quantity}").ToArray();
        var proposals = detail.GiftProposals ?? [];
        var giftText = gifts.Length > 0 ? L.T("Tặng kèm: ") + string.Join(" · ", gifts)
            : proposals.Count > 0 ? L.T("Tặng kèm khi thanh toán: ") + string.Join(" · ", proposals.Select(x => $"{x.ProductName} ×{x.Quantity}")) : "";
        var summary = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12), Text =
            L.F("{0} · {1} · {2} · {3}\n{4} · Khách {5}\nMón {6} · Giảm {7} · Ship {8} · Phải trả {9}\n", h.Code, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm}", h.Created), SalesStates.Label(h.Channel), SalesStates.Label(h.Status), SalesStates.Label(h.Delivery), h.Customer ?? L.T("Vãng lai"), SalesUi.Money(h.Total), SalesUi.Money(h.Discount), SalesUi.Money(h.Ship), SalesUi.Money(h.Payable)) +
            (detail.SnapshotKind == "LegacyUnknown" ? L.T("Nguồn/tỷ lệ ưu đãi lịch sử chưa lưu.") : detail.Source == "TierAndPromotion"
                ? L.F("{0} · hạng {1}% + KM {2}% = {3}% · Điểm mua đã cộng {4}", SalesStates.Label(detail.Source), detail.TierPercent ?? 0, detail.PromotionPercent ?? 0, detail.PercentDiscount, detail.Points)
                : L.F("{0} {1}% · Điểm mua đã cộng {2}", SalesStates.Label(detail.Source), detail.PercentDiscount, detail.Points)) +
            (detail.Promotions.Count == 0 ? "" : L.T(" · Ưu đãi: ") + string.Join(" · ", detail.Promotions.Select(x => L.F("{0} {1}% ({2})", x.Name, x.Percent, SalesUi.Money(x.Discount))))) +
            (giftText.Length == 0 ? "" : "\n" + giftText) +
            "\n" + string.Join(" · ", detail.Order.Payments.Select(x => $"{x.Name}: {SalesUi.Money(x.Amount)}")) };
        var state = new Label { Dock = DockStyle.Bottom, AutoSize = true };
        var print = SalesUi.Button(L.T("Xem trước / In lại"), async (_, _) =>
        {
            try
            {
                await Printing.InvoicePrinting.PreviewAsync(this, detail, service, session);
            }
            catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException or UserOperationException)
            { if (!IsDisposed) state.Text = L.T("Chưa in được; hóa đơn giữ nguyên. ") + ex.Message; }
        });
        print.Enabled = h.Status == "Da thanh toan";
        var pos = SalesUi.Button(L.T("Quay lại POS"), (_, _) => { ReturnToPos = true; DialogResult = DialogResult.OK; Close(); }); pos.Enabled = h.IsPayable;
        Controls.Add(lines); Controls.Add(summary); Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 60, Controls = { SalesUi.Bar(print, pos, SalesUi.Button(L.T("Đóng"), (_, _) => Close())) } }); Controls.Add(state);
        AppTheme.Apply(this); SalesUi.Fit(this, new Size(900, 620));
    }
}


