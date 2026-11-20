using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitDataGridView : DataGridView
{
    private int hovered = -1;
    private OrbitGridProfile profile;
    [System.ComponentModel.DefaultValue(OrbitGridProfile.Inventory)]
    public OrbitGridProfile Profile { get => profile; set { profile = value; if (profile == OrbitGridProfile.Payment) { ReadOnly = false; EditMode = DataGridViewEditMode.EditOnEnter; } else if (profile is OrbitGridProfile.OpenOrders or OrbitGridProfile.InvoiceHistory or OrbitGridProfile.Delivery) { ReadOnly = true; EditMode = DataGridViewEditMode.EditProgrammatically; } ApplyMetrics(); } }
    public OrbitDataGridView()
    {
        DoubleBuffered = true; ReadOnly = true; AutoGenerateColumns = false;
        AllowUserToAddRows = AllowUserToDeleteRows = AllowUserToResizeRows = AllowUserToResizeColumns = false;
        MultiSelect = false; SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        BorderStyle = BorderStyle.None; BackgroundColor = OrbitManagementTheme.Surface;
        GridColor = Color.FromArgb(48, 38, 54); CellBorderStyle = DataGridViewCellBorderStyle.None;
        EnableHeadersVisualStyles = false; ColumnHeadersBorderStyle = RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        Font = OrbitManagementTheme.Table;
        DefaultCellStyle = new DataGridViewCellStyle { BackColor = OrbitManagementTheme.Surface, ForeColor = OrbitManagementTheme.Text, SelectionBackColor = OrbitManagementTheme.Selected, SelectionForeColor = OrbitManagementTheme.Text, Font = OrbitManagementTheme.Table, Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(13, 0, 13, 0), WrapMode = DataGridViewTriState.False };
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(37, 31, 46), ForeColor = OrbitManagementTheme.Muted, SelectionBackColor = Color.FromArgb(37, 31, 46), SelectionForeColor = OrbitManagementTheme.Muted, Font = OrbitManagementTheme.Header, Padding = new Padding(13, 0, 13, 0), WrapMode = DataGridViewTriState.False };
        RowHeadersDefaultCellStyle = DefaultCellStyle.Clone();
        CellMouseEnter += (_, e) => { var old = hovered; hovered = e.RowIndex; if (old >= 0 && old < Rows.Count) InvalidateRow(old); if (hovered >= 0) InvalidateRow(hovered); };
        MouseLeave += (_, _) => { var old = hovered; hovered = -1; if (old >= 0 && old < Rows.Count) InvalidateRow(old); };
        CellToolTipTextNeeded += (_, e) =>
        {
            if (profile == OrbitGridProfile.Delivery && e.RowIndex >= 0 && e.ColumnIndex >= 0 && Columns[e.ColumnIndex].DataPropertyName is "Recipient" or "Address")
                e.ToolTipText = Convert.ToString(Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
        };
        HandleCreated += (_, _) => ApplyMetrics(); DpiChangedAfterParent += (_, _) => ApplyMetrics();
        EditingControlShowing += (_, e) =>
        {
            if (profile != OrbitGridProfile.Payment || e.Control is not { } editor) return;
            e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = OrbitManagementTheme.Input;
            e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor = OrbitManagementTheme.Text;
            EditingPanel.BackColor = OrbitManagementTheme.Input;
            editor.BackColor = OrbitManagementTheme.Input; editor.ForeColor = OrbitManagementTheme.Text;
            editor.Font = OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Body);
            if (editor is ComboBox combo)
            {
                combo.FlatStyle = FlatStyle.Flat; combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.ItemHeight = OrbitManagementTheme.Px(this, 26);
                combo.DrawItem -= PaintPaymentMethod; combo.DrawItem += PaintPaymentMethod;
            }
            if (editor is TextBox text) { text.BorderStyle = BorderStyle.None; text.TextAlign = CurrentCell?.OwningColumn?.Name == "Amount" ? HorizontalAlignment.Right : HorizontalAlignment.Left; }
        };
    }
    private void PaintPaymentMethod(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? OrbitManagementTheme.Selected : OrbitManagementTheme.Input);
        e.Graphics.FillRectangle(fill, e.Bounds);
        var text = e.Index >= 0 && e.Index < combo.Items.Count ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
        TextRenderer.DrawText(e.Graphics, text, combo.Font, Rectangle.Inflate(e.Bounds, -4, 0), OrbitManagementTheme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
    }
    private void ApplyMetrics()
    {
        DefaultCellStyle.Font = OrbitManagementTheme.DrawingFont(this, profile == OrbitGridProfile.Payment ? OrbitManagementTheme.Body : OrbitManagementTheme.Table);
        ColumnHeadersDefaultCellStyle.Font = OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Header);
        RowHeadersWidth = OrbitManagementTheme.Px(this, 38); ColumnHeadersHeight = OrbitManagementTheme.Px(this, 40);
        RowHeadersVisible = profile != OrbitGridProfile.Points;
        RowTemplate.Height = OrbitManagementTheme.Px(this, profile switch { OrbitGridProfile.Areas => 28, OrbitGridProfile.Customers => 34, OrbitGridProfile.Promotions or OrbitGridProfile.Points => 36, OrbitGridProfile.Ranks or OrbitGridProfile.Catalogs => 33, _ => 46 });
        DefaultCellStyle.Padding = ColumnHeadersDefaultCellStyle.Padding = new Padding(OrbitManagementTheme.Px(this, 13), 0, OrbitManagementTheme.Px(this, 13), 0);
        foreach (DataGridViewRow row in Rows) row.Height = RowTemplate.Height;
        foreach (DataGridViewColumn col in Columns)
        {
            var property = col.DataPropertyName.Length > 0 ? col.DataPropertyName : col.Name;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            col.HeaderText = col.HeaderText.ToUpperInvariant();
            if (profile == OrbitGridProfile.OpenOrders)
            {
                ApplyOpenOrderColumn(col, property);
                continue;
            }
            if (profile == OrbitGridProfile.InvoiceHistory) { ApplyInvoiceColumn(col, property); continue; }
            if (profile == OrbitGridProfile.Delivery) { ApplyDeliveryColumn(col, property); continue; }
            if (profile == OrbitGridProfile.Payment)
            {
                col.DefaultCellStyle.BackColor = col.DefaultCellStyle.SelectionBackColor = OrbitManagementTheme.Input;
                col.DefaultCellStyle.ForeColor = col.DefaultCellStyle.SelectionForeColor = OrbitManagementTheme.Text;
                if (col is DataGridViewComboBoxColumn combo) combo.FlatStyle = FlatStyle.Flat;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = property == "Method" ? 42 : property == "Amount" ? 26 : 32;
                col.MinimumWidth = OrbitManagementTheme.Px(this, property == "Method" ? 140 : 110);
                col.DefaultCellStyle.Alignment = col.HeaderCell.Style.Alignment = property == "Amount" ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft;
                continue;
            }
            col.HeaderCell.Style.Padding = new Padding(OrbitManagementTheme.Px(this, col.Name is "Unit" or "Quantity" ? 4 : 13), 0, OrbitManagementTheme.Px(this, col.Name is "Unit" or "Quantity" ? 4 : 13), 0);
            var stretch = property == (profile == OrbitGridProfile.Ranks ? "Code" : profile == OrbitGridProfile.Points ? "Reason" : "Name");
            col.AutoSizeMode = stretch ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None;
            col.MinimumWidth = OrbitManagementTheme.Px(this, stretch ? 110 : 45);
            if (!stretch) col.Width = OrbitManagementTheme.Px(this, ColumnWidth(property));
            if (property is "Code" or "Id") { col.DefaultCellStyle.Font = OrbitManagementTheme.DrawingFont(this, profile == OrbitGridProfile.Ranks ? OrbitManagementTheme.Table : OrbitManagementTheme.Code); col.DefaultCellStyle.ForeColor = profile == OrbitGridProfile.Ranks ? OrbitManagementTheme.Text : OrbitManagementTheme.Muted; }
            if (profile != OrbitGridProfile.Inventory && property is "Price" or "Points" or "MinimumPoints" or "Discount" or "Ratio" or "Allowance" or "Factor")
            {
                col.DefaultCellStyle.Alignment = col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (property is "Points" or "MinimumPoints") { col.DefaultCellStyle.Format = "N0"; col.DefaultCellStyle.FormatProvider = global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"); }
                if (property == "Discount") col.DefaultCellStyle.Format = "0'%'";
            }
        }
    }
    private void ApplyInvoiceColumn(DataGridViewColumn column, string property)
    {
        var culture = global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
        column.AutoSizeMode = property == "Customer" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None;
        var width = OrbitManagementTheme.Px(this, property switch { "Code" => 125, "Created" => 170, "Channel" => 110, "Payable" => 135, "Status" => 150, _ => 115 });
        var font = OrbitManagementTheme.DrawingFont(this, property == "Code" ? OrbitManagementTheme.Code : OrbitManagementTheme.Table);
        column.DefaultCellStyle.Font = font;
        if (property is "Code" or "Created" or "Payable")
        {
            var texts = Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(r => property == "Payable" && r.Cells[column.Index].Value is decimal amount
                ? amount.ToString("#,##0' ₫'", culture) : Convert.ToString(r.Cells[column.Index].Value) ?? "");
            if (property == "Created") texts = ["31/12/9998 23:59"];
            else if (property == "Code") texts = texts.Append("HD99999999");
            var measured = texts.Append("").Max(text => TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width);
            width = Math.Max(width, measured + DefaultCellStyle.Padding.Horizontal + OrbitManagementTheme.Px(this, 8));
        }
        column.MinimumWidth = width; if (property != "Customer") column.Width = width;
        if (property == "Code") column.Frozen = true;
        if (property == "Created") column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (property == "Payable")
        {
            column.DefaultCellStyle.Format = "#,##0' ₫'"; column.DefaultCellStyle.FormatProvider = culture;
            column.DefaultCellStyle.Alignment = column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }
    private void ApplyDeliveryColumn(DataGridViewColumn column, string property)
    {
        column.AutoSizeMode = property == "Address" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None;
        var width = OrbitManagementTheme.Px(this, property switch { "Code" => 125, "Phone" => 130, "Recipient" => 150, "Address" => 200, _ => 145 });
        var font = OrbitManagementTheme.DrawingFont(this, property == "Code" ? OrbitManagementTheme.Code : OrbitManagementTheme.Table);
        column.DefaultCellStyle.Font = font;
        var headerWidth = TextRenderer.MeasureText(column.HeaderText, ColumnHeadersDefaultCellStyle.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        width = Math.Max(width, headerWidth + ColumnHeadersDefaultCellStyle.Padding.Horizontal + OrbitManagementTheme.Px(this, 8));
        if (property is "Code" or "Phone" or "Delivery" or "PaymentStatus")
        {
            var texts = Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(r => Convert.ToString(r.Cells[column.Index].Value) ?? "");
            if (property == "Code") texts = texts.Append("HD99999999");
            else if (property is "Delivery" or "PaymentStatus") texts = texts.Select(Models.States.SalesStates.Label);
            var measured = texts.Append("").Max(text => TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width);
            width = Math.Max(width, measured + DefaultCellStyle.Padding.Horizontal + OrbitManagementTheme.Px(this, 8));
        }
        column.MinimumWidth = width; if (property != "Address") column.Width = width;
        if (property == "Code") column.Frozen = true;
    }
    private void ApplyOpenOrderColumn(DataGridViewColumn column, string property)
    {
        column.AutoSizeMode = property == "Customer" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None;
        var width = OrbitManagementTheme.Px(this, property switch { "Code" => 150, "Created" => 180, "Channel" => 110, _ => 140 });
        if (property == "Code")
        {
            column.DefaultCellStyle.Font = OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Code);
            var codes = Rows.Cast<DataGridViewRow>().Select(r => Convert.ToString(r.Cells[column.Index].Value) ?? "").Append("HD99999999");
            var textWidth = codes.Max(code => TextRenderer.MeasureText(code, column.DefaultCellStyle.Font, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width);
            width = Math.Max(width, textWidth + DefaultCellStyle.Padding.Horizontal + OrbitManagementTheme.Px(this, 8));
            column.Frozen = true;
        }
        column.MinimumWidth = width;
        if (property != "Customer") column.Width = width;
        if (property == "Created") column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (property == "Payable")
        {
            column.DefaultCellStyle.Format = "#,##0' ₫'";
            column.DefaultCellStyle.FormatProvider = global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            column.DefaultCellStyle.Alignment = column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }
    private int ColumnWidth(string property) => profile switch
    {
        OrbitGridProfile.Inventory => property switch { "Code" => 76, "Unit" => 64, _ => 116 },
        OrbitGridProfile.Menu => property switch { "Code" => 90, "Category" => 160, "Price" => 150, "Status" => 150, _ => 120 },
        OrbitGridProfile.Areas => 140,
        OrbitGridProfile.Customers => property switch { "Code" => 95, "Phone" => 170, "Points" => 100, "Tier" => 100, _ => 110 },
        OrbitGridProfile.Promotions => property == "Discount" ? 110 : 145,
        OrbitGridProfile.Ranks => 280,
        OrbitGridProfile.Points => property switch { "OccurredAt" => 180, "Points" => 80, _ => 145 },
        OrbitGridProfile.Tables => property switch { "Code" => 90, "Area" => 150, "Status" => 170, _ => 170 },
        _ => property switch { "Code" => 120, "Supported" => 190, "Phone" => 170, "Allowance" => 150, "Ratio" or "Factor" => 140, _ => 120 }
    };
    protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e) { base.OnDataBindingComplete(e); ApplyMetrics(); }
    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        // Bảng nhập giữ painting native cho combobox và vùng editor, không vẽ đè text nhập.
        if (profile == OrbitGridProfile.Payment && e.RowIndex >= 0 && e.ColumnIndex >= 0) { base.OnCellPainting(e); return; }
        if (e.RowIndex >= 0 && e.ColumnIndex == -1)
        {
            var graphicsState = e.Graphics!.Save();
            var chosen = Rows[e.RowIndex].Selected;
            using var fill = new SolidBrush(chosen ? OrbitManagementTheme.Selected : hovered == e.RowIndex ? Color.FromArgb(42, 32, 51) : OrbitManagementTheme.Surface); e.Graphics!.FillRectangle(fill, e.CellBounds);
            var size = OrbitManagementTheme.Px(this, 14); var bounds = new Rectangle(e.CellBounds.Left + (e.CellBounds.Width - size) / 2, e.CellBounds.Top + (e.CellBounds.Height - size) / 2, size, size);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var circle = new SolidBrush(chosen ? Color.FromArgb(106, 82, 55) : Color.FromArgb(59, 59, 58)); e.Graphics.FillEllipse(circle, bounds);
            using var outline = new Pen(chosen ? OrbitManagementTheme.Caramel : Color.FromArgb(117, 117, 115)); e.Graphics.DrawEllipse(outline, bounds);
            if (chosen) { bounds.Inflate(-3, -3); using var dot = new SolidBrush(OrbitManagementTheme.Caramel); e.Graphics.FillEllipse(dot, bounds); using var line = new Pen(OrbitManagementTheme.Caramel, OrbitManagementTheme.Px(this, 3)); e.Graphics.DrawLine(line, e.CellBounds.Left + 1, e.CellBounds.Top, e.CellBounds.Left + 1, e.CellBounds.Bottom); }
            using var border = new Pen(GridColor); e.Graphics.DrawLine(border, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            e.Graphics.Restore(graphicsState); e.Handled = true; return;
        }
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            // CellStyle có thể được dùng lại; luôn trả nền về surface khi chuột rời dòng.
            e.CellStyle!.BackColor = hovered == e.RowIndex && !Rows[e.RowIndex].Selected ? Color.FromArgb(42, 32, 51) : OrbitManagementTheme.Surface;
            if (e.AdvancedBorderStyle is { } borderStyle)
            {
                borderStyle.Left = borderStyle.Right = borderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
                borderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;
            }
            var selected = Rows[e.RowIndex].Selected;
            e.Graphics!.SmoothingMode = global::System.Drawing.Drawing2D.SmoothingMode.None;
            using var fill = new SolidBrush(selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor);
            e.Graphics!.FillRectangle(fill, e.CellBounds);
            var padding = e.CellStyle.Padding;
            var textBounds = new Rectangle(e.CellBounds.Left + padding.Left, e.CellBounds.Top + padding.Top,
                Math.Max(0, e.CellBounds.Width - padding.Horizontal), Math.Max(0, e.CellBounds.Height - padding.Vertical));
            var alignment = e.CellStyle.Alignment is DataGridViewContentAlignment.MiddleRight or DataGridViewContentAlignment.TopRight or DataGridViewContentAlignment.BottomRight
                ? TextFormatFlags.Right : TextFormatFlags.Left;
            var property = Columns[e.ColumnIndex].DataPropertyName;
            if (Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
            {
                var size = OrbitManagementTheme.Px(this, 14); var box = new Rectangle(e.CellBounds.Left + (e.CellBounds.Width - size) / 2, e.CellBounds.Top + (e.CellBounds.Height - size) / 2, size, size);
                var check = e.Value is true;
                using var checkFill = new SolidBrush(check ? OrbitManagementTheme.Caramel : Color.FromArgb(59, 59, 58)); e.Graphics.FillRectangle(checkFill, box);
                using var checkPen = new Pen(check ? Color.FromArgb(80, 56, 40) : OrbitManagementTheme.Muted);
                e.Graphics.DrawRectangle(checkPen, box);
                if (check) e.Graphics.DrawLines(checkPen, [new Point(box.Left + 3, box.Top + size/2), new(box.Left + size/2 - 1, box.Bottom - 3), new(box.Right - 2, box.Top + 3)]);
            }
            else if ((profile == OrbitGridProfile.Menu && property == "Status") || (profile == OrbitGridProfile.Customers && property == "Tier")
                || (profile == OrbitGridProfile.OpenOrders && property == "Channel")
                || (profile == OrbitGridProfile.InvoiceHistory && property is "Channel" or "Status")
                || (profile == OrbitGridProfile.Delivery && property is "Delivery" or "PaymentStatus")) PaintBadge(e, textBounds, property);
            else TextRenderer.DrawText(e.Graphics, Convert.ToString(e.FormattedValue), e.CellStyle.Font ?? Font, textBounds,
                    selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor,
                    alignment | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            using var horizontal = new Pen(GridColor);
            e.Graphics!.DrawLine(horizontal, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            e.Handled = true;
        }
        base.OnCellPainting(e);
    }
    private void PaintBadge(DataGridViewCellPaintingEventArgs e, Rectangle textBounds, string property)
    {
        var deliveryStatus = profile == OrbitGridProfile.Delivery && property is "Delivery" or "PaymentStatus";
        var status = property == "Status" || deliveryStatus; var text = Convert.ToString(e.FormattedValue) ?? "";
        var raw = Convert.ToString(e.Value);
        var active = raw == "Dang ban" || profile == OrbitGridProfile.InvoiceHistory && status && raw == "Da thanh toan" || deliveryStatus && raw is "Da giao" or "Da thanh toan";
        var pending = profile == OrbitGridProfile.InvoiceHistory && status && raw == "Chua thanh toan" || deliveryStatus && raw is "Cho giao" or "Dang giao" or "Chua thanh toan";
        var canceled = profile == OrbitGridProfile.InvoiceHistory && status && raw == "Huy" || deliveryStatus && raw is "Huy" or "Huy giao";
        var warm = !status && text is "BKIM" or "VANG" || (profile is OrbitGridProfile.OpenOrders or OrbitGridProfile.InvoiceHistory) && property == "Channel" && raw == "Tai quan";
        var ink = status ? active ? Color.FromArgb(189, 215, 167) : pending ? OrbitManagementTheme.Caramel : canceled ? AppTheme.Error : OrbitManagementTheme.Muted : warm ? OrbitManagementTheme.Caramel : OrbitManagementTheme.Lilac;
        var font = OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small);
        var scale = DeviceDpi / 96F; var width = TextRenderer.MeasureText(e.Graphics!, text, font).Width + (int)((status ? 22 : 14) * scale);
        var box = new RectangleF(textBounds.Left, e.CellBounds.Top + (e.CellBounds.Height - 19 * scale) / 2, Math.Min(textBounds.Width, width), 19 * scale);
        var state = e.Graphics!.Save(); e.Graphics.SmoothingMode = global::System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(box, 4 * scale);
        using var fill = new SolidBrush(status && active ? Color.FromArgb(35, 49, 33) : warm || pending ? Color.FromArgb(59, 44, 31) : canceled ? Color.FromArgb(60, 36, 42) : Color.FromArgb(45, 37, 54)); e.Graphics.FillPath(fill, path);
        using var line = new Pen(Color.FromArgb(110, ink)); e.Graphics.DrawPath(line, path);
        if (status) { using var dot = new SolidBrush(ink); e.Graphics.FillEllipse(dot, box.Left + 7 * scale, box.Top + 8 * scale, 4 * scale, 4 * scale); }
        var label = Rectangle.Round(box); label.X += (int)((status ? 16 : 3) * scale); label.Width -= (int)((status ? 19 : 6) * scale);
        TextRenderer.DrawText(e.Graphics, text, font, label, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        e.Graphics.Restore(state);
    }
}

public enum OrbitGridProfile { Inventory, Menu, Tables, Areas, Customers, Promotions, Ranks, Points, Catalogs, Payment, OpenOrders, InvoiceHistory, Delivery }
