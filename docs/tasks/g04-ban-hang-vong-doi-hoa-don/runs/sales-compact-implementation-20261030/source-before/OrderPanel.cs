using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OrderPanel : UserControl
{
    internal readonly DataGridView Lines = SalesUi.Grid(("Name", "Món"), ("Quantity", "SL"), ("Price", "Giá"), ("Note", "Ghi chú"), ("Amount", "Tiền dòng"));
    internal readonly Label Caption = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
    internal readonly Label Totals = new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8) };
    internal readonly FlowLayoutPanel Actions = new() { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true };
    public OrderPanel()
    {
        Dock = DockStyle.Fill; Controls.Add(Lines); Controls.Add(Caption); Controls.Add(Totals); Controls.Add(Actions);
        foreach (var name in new[] { "Price", "Amount" }) { Lines.Columns[name]!.DefaultCellStyle.Format = "N0"; Lines.Columns[name]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
    }
    internal void ShowOrder(SalesOrderDto? order)
    {
        Lines.DataSource = order?.Lines.ToList();
        Caption.Text = order is null ? "Chưa tạo đơn · thêm món đầu để lưu" : $"{order.Header.Code} · {SalesStates.Label(order.Header.Channel)} · {SalesStates.Label(order.Header.Status)}\nBàn {order.Header.TableCode ?? "—"} · Khách {order.Header.Customer ?? "Vãng lai"} · {SalesStates.Label(order.Header.Delivery)}";
        Totals.Text = order is null ? "Tổng tiền sẽ lấy từ SQL sau khi thêm món." :
            $"Tiền món {SalesUi.Money(order.Header.Total)}\n{SalesStates.Label(order.Header.PreviewSource)} {order.Header.PreviewPercent?.ToString() ?? ""}% · Giảm {SalesUi.Money(order.Header.Discount)}\nShip {SalesUi.Money(order.Header.Ship)} · Phải trả {SalesUi.Money(order.Header.Payable)}";
    }
}
