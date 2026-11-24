using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class InvoiceDetailDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    public bool ReturnToPos { get; private set; }
    internal InvoiceDetailDialog(InvoiceDetailDto detail, InvoiceService service, SessionContext session)
    {
        var h = detail.Order.Header; Text = L.T("Chi tiết hóa đơn · ") + h.Code;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(980, 660);
        var culture = SalesUi.Culture; string Money(decimal value) => value.ToString("#,##0' ₫'", culture);

        // Chỉ giữ thông tin cần để đối chiếu; dòng 0 đồng hoặc khách vãng lai không hiện.
        var summary = new OrbitSummaryPanel(L.T("Thông tin hóa đơn"));
        summary.AddValue(L.T("Ngày lập")).Text = h.Created.ToString("dd/MM/yyyy HH:mm", culture);
        summary.AddValue(L.T("Kênh")).Text = SalesStates.Label(h.Channel);
        summary.AddValue(L.T("Trạng thái"), prominent: true).Text = SalesStates.Label(h.Status);
        if (!string.IsNullOrWhiteSpace(h.TableCode)) summary.AddValue(L.T("Bàn")).Text = h.TableCode;
        if (!string.IsNullOrWhiteSpace(h.Customer)) summary.AddValue(L.T("Khách")).Text = h.Customer;
        if (detail.Order.Payments.Count > 0)
            summary.AddValue(L.T("Thanh toán")).Text = string.Join(" · ", detail.Order.Payments.Select(x => x.Name + " " + Money(x.Amount)));
        summary.AddDivider();
        summary.AddValue(L.T("Tiền món")).Text = Money(h.Total);
        if (h.Discount > 0) summary.AddValue(L.T("Giảm")).Text = "− " + Money(h.Discount);
        if (h.Ship > 0) summary.AddValue(L.T("Phí giao hàng")).Text = Money(h.Ship);
        summary.AddValue(L.T("Phải trả"), prominent: true).Text = Money(h.Payable);
        if (h.Discount > 0)
        {
            summary.AddNote().Text = detail.SnapshotKind == "LegacyUnknown"
                ? L.T("Nguồn/tỷ lệ ưu đãi lịch sử chưa lưu.")
                : detail.Source == "TierAndPromotion"
                    ? L.F("Nguồn giảm: {0} · KM {1}% + hạng {2}%", SalesStates.Label(detail.Source), detail.PromotionPercent ?? 0, detail.TierPercent ?? 0)
                    : L.F("Nguồn giảm: {0} {1}%", SalesStates.Label(detail.Source), detail.PercentDiscount);
            if (detail.Promotions.Count > 0)
                summary.AddNote().Text = L.F("Ưu đãi: {0}", string.Join(" · ", detail.Promotions.Select(x => L.F("{0} {1}% ({2})", x.Name, x.Percent, Money(x.Discount)))));
        }
        if (detail.Points > 0) summary.AddNote().Text = L.F("Điểm mua đã cộng {0}", detail.Points);
        var gifts = detail.Order.Lines.Where(x => x.GiftPromotion is not null).Select(x => $"{x.Name} ×{x.Quantity}").ToArray();
        var proposals = detail.GiftProposals ?? [];
        if (gifts.Length > 0) summary.AddNote().Text = L.T("Tặng kèm: ") + string.Join(" · ", gifts);
        else if (proposals.Count > 0) summary.AddNote().Text = L.T("Tặng kèm khi thanh toán: ") + string.Join(" · ", proposals.Select(x => $"{x.ProductName} ×{x.Quantity}"));

        var lines = SalesUi.Grid(("Code", L.T("Mã món")), ("Name", L.T("Tên hiện tại")), ("Quantity", L.T("SL")),
            ("Price", L.T("Đơn giá")), ("Note", L.T("Ghi chú")), ("GiftPromotion", L.T("Quà")), ("Amount", L.T("Thành tiền")));
        lines.DataSource = detail.Order.Lines.ToList();
        lines.RowHeadersVisible = false;
        if (!detail.Order.Lines.Any(x => x.GiftPromotion is not null)) lines.Columns.Remove("GiftPromotion");
        lines.Columns["Code"]!.FillWeight = 10; lines.Columns["Name"]!.FillWeight = 28;
        lines.Columns["Quantity"]!.FillWeight = 7; lines.Columns["Price"]!.FillWeight = 14;
        lines.Columns["Note"]!.FillWeight = 17; lines.Columns["Amount"]!.FillWeight = 13;
        if (lines.Columns["GiftPromotion"] is { } gift) gift.FillWeight = 9;
        foreach (var column in lines.Columns.Cast<DataGridViewColumn>())
            if (column.Name is "Quantity" or "Price" or "Amount")
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.FormatProvider = culture;
                column.DefaultCellStyle.Format = column.Name == "Quantity" ? "0" : "#,##0' ₫'";
            }
        lines.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || lines.Columns[e.ColumnIndex].Name != "GiftPromotion") return;
            if (e.Value is string) { e.Value = L.T("Tặng kèm"); e.FormattingApplied = true; }
        };

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(summary, 0, 0); body.Controls.Add(lines, 0, 1);

        var state = new Label();
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = Padding.Empty, Margin = Padding.Empty };
        var close = new OrbitButton { Text = L.T("Đóng") }; close.Click += (_, _) => Close();
        actions.Controls.Add(close);
        // Chỉ đơn đã chốt mới in ở đây; đơn chưa trả in tạm tại POS.
        if (h.Status == "Da thanh toan")
        {
            var print = new OrbitButton { Text = L.T("In hóa đơn") };
            print.Click += async (_, _) =>
            {
                print.Enabled = false; state.Text = L.T("Đang mở bản xem trước…");
                try { await Printing.InvoicePrinting.PreviewAsync(this, detail, service, session); if (!IsDisposed) state.Text = ""; }
                catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception or InvalidOperationException or IOException
                    or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
                { if (!IsDisposed) state.Text = L.T("Chưa in được; hóa đơn giữ nguyên. ") + ex.Message; }
                finally { if (!IsDisposed) print.Enabled = true; }
            };
            OrbitManagementTheme.Button(print, ManagementButtonStyle.Primary, "printer");
            actions.Controls.Add(print);
        }
        // Đơn còn mở mới quay lại POS; đơn đã chốt/hủy không còn thao tác sửa.
        if (h.IsPayable)
        {
            var pos = new OrbitButton { Text = L.T("Quay lại POS") };
            pos.Click += (_, _) => { ReturnToPos = true; DialogResult = DialogResult.OK; Close(); };
            actions.Controls.Add(pos);
        }
        var layout = new OrbitDialogLayout(this, body, state, actions);
        Controls.Add(layout); layout.EnableResponsiveActions(this, true);
    }
}
