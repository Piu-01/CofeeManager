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
    // Mã hóa đơn, số phải trả và trạng thái là thứ người dùng đối chiếu đầu tiên.
    private static readonly Font AmountFont = new("Segoe UI", 27F, FontStyle.Bold);
    private static readonly Font BodyStrong = new("Segoe UI", 10.5F, FontStyle.Bold);

    public bool ReturnToPos { get; private set; }

    internal InvoiceDetailDialog(InvoiceDetailDto detail, InvoiceService service, SessionContext session)
    {
        var h = detail.Order.Header;
        Text = L.T("Chi tiết hóa đơn · ") + h.Code;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1040, 700);
        var culture = SalesUi.Culture;
        string Money(decimal value) => value.ToString("#,##0' ₫'", culture);
        var paid = h.Status == "Da thanh toan";

        // ---- Đầu mục: hóa đơn nào, lúc nào, bàn/kênh và trạng thái ----
        var meta = new List<string> { h.Created.ToString("dd/MM/yyyy HH:mm", culture), SalesStates.Label(h.Channel) };
        if (!string.IsNullOrWhiteSpace(h.TableCode)) meta.Add(L.T("Bàn") + " " + h.TableCode);
        if (!string.IsNullOrWhiteSpace(h.Customer)) meta.Add(h.Customer);

        var head = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var headText = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 0, Margin = Padding.Empty };
        headText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void AddHeadText(Label label)
        {
            label.AutoSize = true;
            headText.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headText.Controls.Add(label, 0, headText.RowCount);
            headText.RowCount++;
        }
        AddHeadText(new Label { Text = L.T("Hóa đơn bán"), Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Caramel, Margin = new Padding(0, 0, 0, 4) });
        AddHeadText(new Label { Text = h.Code, Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Text, Margin = new Padding(0, 0, 0, 6) });
        AddHeadText(new Label { Text = string.Join("   ·   ", meta), Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Muted, Margin = Padding.Empty });
        var badge = new OrbitBadge
        {
            Text = SalesStates.Label(h.Status), Size = new Size(164, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right, Margin = new Padding(20, 2, 0, 0)
        };
        head.Controls.Add(headText, 0, 0); head.Controls.Add(badge, 1, 0);

        // ---- Bảng món: chỉ cột có dữ liệu, tiền căn phải ----
        var lines = SalesUi.Grid(("Product", L.T("Mã món")), ("Name", L.T("Tên hiện tại")), ("Quantity", L.T("SL")),
            ("Price", L.T("Đơn giá")), ("Note", L.T("Ghi chú")), ("GiftPromotion", L.T("Quà")), ("Amount", L.T("Thành tiền")));
        lines.DataSource = detail.Order.Lines.ToList();
        lines.RowHeadersVisible = false; lines.Margin = Padding.Empty;
        if (!detail.Order.Lines.Any(x => x.GiftPromotion is not null)) lines.Columns.Remove("GiftPromotion");
        if (!detail.Order.Lines.Any(x => !string.IsNullOrWhiteSpace(x.Note))) lines.Columns.Remove("Note");
        lines.Columns["Product"]!.FillWeight = 11; lines.Columns["Name"]!.FillWeight = 30;
        lines.Columns["Quantity"]!.FillWeight = 7; lines.Columns["Price"]!.FillWeight = 15;
        if (lines.Columns["Note"] is { } noteColumn) noteColumn.FillWeight = 16;
        lines.Columns["Amount"]!.FillWeight = 15;
        if (lines.Columns["GiftPromotion"] is { } giftColumn) giftColumn.FillWeight = 9;
        foreach (var column in lines.Columns.Cast<DataGridViewColumn>())
            if (column.Name is "Quantity" or "Price" or "Amount")
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.DefaultCellStyle.FormatProvider = culture;
                column.DefaultCellStyle.Format = column.Name == "Quantity" ? "0" : "#,##0' ₫'";
            }
        foreach (var column in new[] { "Amount" }.Select(name => lines.Columns[name]).OfType<DataGridViewColumn>())
            column.DefaultCellStyle.Font = BodyStrong;
        lines.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || lines.Columns[e.ColumnIndex].Name != "GiftPromotion") return;
            if (e.Value is string) { e.Value = L.T("Tặng kèm"); e.FormattingApplied = true; }
        };

        var itemsCard = new OrbitSurface { Dock = DockStyle.Fill, Padding = new Padding(16), Margin = Padding.Empty };
        var itemsBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        itemsBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        itemsBody.RowStyles.Add(new RowStyle(SizeType.AutoSize)); itemsBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var itemsHead = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 10), BackColor = OrbitManagementTheme.Surface };
        itemsHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); itemsHead.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        itemsHead.Controls.Add(new Label { Text = L.T("Món trong hóa đơn"), AutoSize = true, Font = OrbitManagementTheme.CardTitle, ForeColor = OrbitManagementTheme.Text, Margin = Padding.Empty }, 0, 0);
        itemsHead.Controls.Add(new Label { Text = L.F("{0} món", detail.Order.Lines.Count), AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(12, 0, 0, 0), Anchor = AnchorStyles.Right }, 1, 0);
        itemsBody.Controls.Add(itemsHead, 0, 0); itemsBody.Controls.Add(lines, 0, 1);
        itemsCard.Controls.Add(itemsBody);

        // ---- Cột tổng: số phải trả to, chi tiết và thanh toán ----
        var notes = new List<string>();
        if (h.Discount > 0)
        {
            notes.Add(detail.SnapshotKind == "LegacyUnknown"
                ? L.T("Nguồn/tỷ lệ ưu đãi lịch sử chưa lưu.")
                : detail.Source == "TierAndPromotion"
                    ? L.F("Nguồn giảm: {0} · KM {1}% + hạng {2}%", SalesStates.Label(detail.Source), detail.PromotionPercent ?? 0, detail.TierPercent ?? 0)
                    : L.F("Nguồn giảm: {0} {1}%", SalesStates.Label(detail.Source), detail.PercentDiscount));
            if (detail.Promotions.Count > 0)
                notes.Add(L.F("Ưu đãi: {0}", string.Join(" · ", detail.Promotions.Select(x => L.F("{0} {1}% ({2})", x.Name, x.Percent, Money(x.Discount))))));
        }
        if (detail.Points > 0) notes.Add(L.F("Điểm mua đã cộng {0}", detail.Points));
        var gifts = detail.Order.Lines.Where(x => x.GiftPromotion is not null).Select(x => $"{x.Name} ×{x.Quantity}").ToArray();
        var proposals = detail.GiftProposals ?? [];
        if (gifts.Length > 0) notes.Add(L.T("Tặng kèm: ") + string.Join(" · ", gifts));
        else if (proposals.Count > 0) notes.Add(L.T("Tặng kèm khi thanh toán: ") + string.Join(" · ", proposals.Select(x => $"{x.ProductName} ×{x.Quantity}")));

        var rail = new OrbitSurface { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = new Padding(16, 0, 0, 0) };
        var railScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = OrbitManagementTheme.Surface, Margin = Padding.Empty };
        var railBody = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 0, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        railBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var railNotes = new List<Label>();
        void AddBlock(Control control)
        {
            railBody.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            railBody.Controls.Add(control, 0, railBody.RowCount);
            railBody.RowCount++;
        }
        void AddCaption(string caption) => AddBlock(new Label { Text = caption, AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(0, 0, 0, 4) });
        void AddDivider() => AddBlock(new Panel { Height = 1, Dock = DockStyle.Top, BackColor = OrbitManagementTheme.Border, Margin = new Padding(0, 10, 0, 10) });
        void AddPair(string caption, string value, Font valueFont)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 6), BackColor = OrbitManagementTheme.Surface };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(new Label { Text = caption, AutoSize = true, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Muted, Margin = Padding.Empty }, 0, 0);
            row.Controls.Add(new Label { Text = value, AutoSize = true, Font = valueFont, ForeColor = OrbitManagementTheme.Text, Margin = new Padding(8, 0, 0, 0), TextAlign = ContentAlignment.MiddleRight }, 1, 0);
            AddBlock(row);
        }

        AddCaption(L.T("Phải trả"));
        AddBlock(new Label { Text = Money(h.Payable), AutoSize = true, Font = AmountFont, ForeColor = OrbitManagementTheme.Caramel, Margin = new Padding(0, 0, 0, 12) });
        AddPair(L.T("Tiền món"), Money(h.Total), OrbitManagementTheme.Body);
        if (h.Discount > 0) AddPair(L.T("Giảm"), "− " + Money(h.Discount), OrbitManagementTheme.Body);
        if (h.Ship > 0) AddPair(L.T("Phí giao hàng"), Money(h.Ship), OrbitManagementTheme.Body);
        if (detail.Order.Payments.Count > 0)
        {
            AddDivider(); AddCaption(L.T("Thanh toán"));
            foreach (var payment in detail.Order.Payments) AddPair(payment.Name, Money(payment.Amount), BodyStrong);
            if (paid) AddBlock(new Label { Text = h.PaidAt is { } paidAt ? L.T("Đã thu đủ") + " · " + paidAt.ToString("HH:mm · dd/MM/yyyy", culture) : L.T("Đã thu đủ"), AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(0, 2, 0, 0) });
        }
        if (notes.Count > 0)
        {
            AddDivider();
            foreach (var note in notes)
            {
                var label = new Label { Text = note, AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(0, 0, 0, 6), UseMnemonic = false };
                railNotes.Add(label); AddBlock(label);
            }
        }
        railBody.Layout += (_, _) =>
        {
            var width = Math.Max(1, railBody.ClientSize.Width);
            foreach (var label in railNotes) label.MaximumSize = new Size(width, 0);
        };
        railScroll.Controls.Add(railBody); rail.Controls.Add(railScroll);

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        split.Controls.Add(itemsCard, 0, 0); split.Controls.Add(rail, 1, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, BackColor = OrbitManagementTheme.Surface };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(head, 0, 0); body.Controls.Add(split, 0, 1);

        var state = new Label();
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = Padding.Empty, Margin = Padding.Empty };
        var close = new OrbitButton { Text = L.T("Đóng") }; close.Click += (_, _) => Close();
        // Chỉ đơn đã chốt mới in ở đây; đơn chưa trả in tạm tại POS.
        if (paid)
        {
            var print = new OrbitButton { Text = L.T("In hóa đơn"), IsPrimary = true };
            print.Click += async (_, _) =>
            {
                print.Enabled = false; state.Text = L.T("Đang mở bản xem trước…");
                try { await Printing.InvoicePrinting.PreviewAsync(this, detail, service, session); if (!IsDisposed) state.Text = ""; }
                catch (Exception ex) when (ex is global::System.ComponentModel.Win32Exception or InvalidOperationException or IOException
                    or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
                { if (!IsDisposed) state.Text = L.T("Chưa in được; hóa đơn giữ nguyên. ") + ex.Message; }
                finally { if (!IsDisposed) print.Enabled = true; }
            };
            actions.Controls.Add(print);
            OrbitManagementTheme.Button(print, ManagementButtonStyle.Primary, "printer");
        }
        actions.Controls.Add(close);
        // Đơn còn mở mới quay lại POS; đơn đã chốt/hủy không còn thao tác sửa.
        if (h.IsPayable)
        {
            var pos = new OrbitButton { Text = L.T("Quay lại POS") };
            pos.Click += (_, _) => { ReturnToPos = true; DialogResult = DialogResult.OK; Close(); };
            actions.Controls.Add(pos);
        }
        var footNote = new Label
        {
            Text = paid ? L.T("Hóa đơn đã chốt — chỉ xem và in lại.") : L.T("Đơn chưa thanh toán — in tạm hoặc quay lại POS."),
            AutoSize = true, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted,
            Anchor = AnchorStyles.Left, Margin = new Padding(0), MaximumSize = new Size(520, 0)
        };
        var layout = new OrbitDialogLayout(this, body, state, actions, header: head, leadingAction: footNote);
        Controls.Add(layout); layout.EnableResponsiveActions(this, true);
        // OrbitDialogLayout style lại nút khi nhận vào footer; gắn lại icon máy in sau bước đó.
        foreach (var primary in actions.Controls.OfType<OrbitButton>().Where(x => x.IsPrimary))
            OrbitManagementTheme.Button(primary, ManagementButtonStyle.Primary, "printer");
    }
}
