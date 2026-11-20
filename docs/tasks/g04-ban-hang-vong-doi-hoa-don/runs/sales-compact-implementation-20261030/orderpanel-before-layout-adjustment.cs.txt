using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OrderPanel : OrbitSurface
{
    internal readonly DataGridView Lines = SalesUi.Grid(("Name", "Món"), ("Quantity", "SL"), ("Price", "Giá"), ("Amount", "Giá / Tiền dòng"), ("Note", "Ghi chú"));
    internal readonly Label Caption = new() { Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text, UseMnemonic = false, AutoEllipsis = true };
    internal readonly Label Totals = new() { Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Caramel, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false, AutoEllipsis = true };
    internal readonly Panel HeaderActions = new(), Actions = new(), PaymentActions = new();
    private readonly OrbitBadge status = new();
    private readonly Label context = new() { Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, UseMnemonic = false, AutoEllipsis = true };
    private readonly Label totalCaption = new() { Text = "Phải thanh toán", Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, UseMnemonic = false, AutoEllipsis = true };
    private SalesOrderDto? current;
    private bool unknown;
    public OrderPanel()
    {
        Dock = DockStyle.Fill; Lines.Dock = DockStyle.None;
        ((OrbitDataGridView)Lines).Profile = OrbitGridProfile.OrderLines;
        Controls.AddRange([Lines, Caption, context, status, HeaderActions, Actions, totalCaption, Totals, PaymentActions]);
    }
    internal void ShowOrder(SalesOrderDto? order)
    {
        var selectedId = (Lines.CurrentRow?.DataBoundItem as SalesLine)?.LineId;
        current = order; Lines.DataSource = order?.Lines.ToList();
        if (selectedId is { } id)
            foreach (DataGridViewRow row in Lines.Rows) if (row.DataBoundItem is SalesLine line && line.LineId == id) { Lines.CurrentCell = row.Cells["Name"]; break; }
        Caption.Text = order?.Header.Code ?? "Chưa tạo đơn";
        status.Text = order is null ? "Nháp" : SalesStates.Label(order.Header.Status);
        status.ForeColor = order?.Header.Status == "Huy" ? AppTheme.Error : order?.Header.Status == "Da thanh toan" ? OrbitManagementTheme.Lilac : OrbitManagementTheme.Caramel;
        context.Text = order is null ? "Thêm món đầu tiên để lưu đơn" : $"{SalesStates.Label(order.Header.Channel)} · Bàn {order.Header.TableCode ?? "—"} · Khách {order.Header.Customer ?? "Vãng lai"}";
        UpdateTotal();
    }
    internal void SetResultUnknown(bool value) { unknown = value; UpdateTotal(); }
    private void UpdateTotal()
    {
        var valid = current is not null && !unknown && (!current.Header.IsOpen || current.Header.PreviewValid);
        Totals.Text = valid ? SalesUi.Money(current!.Header.Payable) : "—";
        totalCaption.Text = current is null || valid ? "Phải thanh toán" : "Chưa xác minh tổng";
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); if (Lines is null) return;
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var inner = Math.Max(1, Width - P(24));
        status.SetBounds(Width - P(126), P(14), P(114), P(23));
        Caption.SetBounds(P(12), P(12), Math.Max(1, status.Left - P(20)), P(28));
        context.SetBounds(P(12), P(45), inner, P(22));
        HeaderActions.SetBounds(P(12), P(72), inner, P(33));
        foreach (Control control in HeaderActions.Controls) control.SetBounds(0, 0, Math.Min(inner, control.GetPreferredSize(Size.Empty).Width), P(29));
        var paymentHeight = P(85); PaymentActions.SetBounds(P(12), Height - P(12) - paymentHeight, inner, paymentHeight);
        var y = 0; foreach (Control control in PaymentActions.Controls) { var h = P(y == 0 ? 42 : 28); control.SetBounds(0, P(y), inner, h); y += y == 0 ? 50 : 36; }
        var totalY = PaymentActions.Top - P(51);
        totalCaption.SetBounds(P(12), totalY, P(110), P(40));
        Totals.SetBounds(P(122), totalY, Math.Max(1, Width - P(134)), P(40));
        Actions.SetBounds(P(12), totalY - P(44), inner, P(33));
        var gap = P(5); var count = Actions.Controls.Count; var actionWidth = Math.Max(1, (inner - Math.Max(0, count - 1) * gap) / Math.Max(1, count));
        for (var i = 0; i < count; i++) Actions.Controls[i].SetBounds(i * (actionWidth + gap), 0, actionWidth, P(31));
        Lines.SetBounds(P(1), P(116), Math.Max(1, Width - P(2)), Math.Max(1, Actions.Top - P(124)));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if (Lines is null) return;
        using var pen = new Pen(OrbitManagementTheme.Border);
        e.Graphics.DrawLine(pen, 1, Lines.Top - 1, Width - 2, Lines.Top - 1);
        e.Graphics.DrawLine(pen, 1, Actions.Top - OrbitManagementTheme.Px(this, 7), Width - 2, Actions.Top - OrbitManagementTheme.Px(this, 7));
    }
}
