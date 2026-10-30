using CafeManager.Models.Dtos.Finance;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Finance;

public sealed class FinanceDetailDialog : OrbitDialogForm
{
    public FinanceDetailDialog(FinanceRow row,DocumentLinks? links=null)
    {
        Text="Chi tiết thu chi — "+row.Code; AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=AutoScaleMode.Dpi; ClientSize=new Size(620,440);
        var content=new TextBox { ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,
            Text=G05Display.Text($"{row.Code} — {row.Type}\r\nNgày: {row.Date:dd/MM/yyyy HH:mm}\r\nDanh mục: {row.Category}\r\nSố tiền: {row.Amount:N0} ₫\r\nNguồn: {OriginText(row.Origin)}\r\nPhiếu nhập: {row.PurchaseCode}\r\nBảng lương: {row.PayrollId}\r\nCông nợ: {row.DebtCode}\r\nTài sản: {row.AssetCode}\r\nGhi chú: {row.Note}\r\n\r\nPhiếu đã xác nhận, chỉ đọc.") };
        var close=new OrbitButton { Text="Đóng" }; close.Click+=(_,_)=>Close();
        var actions=new FlowLayoutPanel { AutoSize=true }; actions.Controls.Add(close);
        if(links is not null){if(row.PurchaseCode is not null){var purchase=new OrbitButton{Text="Phiếu nhập nguồn"};purchase.Click+=async(_,_)=>await links.ShowPurchaseAsync(this,row.PurchaseCode);actions.Controls.Add(purchase);}if(row.DebtCode is not null){var debt=new OrbitButton{Text="Công nợ nguồn"};debt.Click+=async(_,_)=>await links.ShowDebtAsync(this,row.DebtCode);actions.Controls.Add(debt);}}
        var dialogLayout=new OrbitDialogLayout(this,content,new Label(),actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,false);
    }
    internal static string OriginText(string origin) => origin switch { "Manual"=>"Ghi tay","PurchaseImmediate"=>"Trả ngay nhập hàng","SupplierDebt"=>"Trả nợ nhà cung cấp","Payroll"=>"Chi lương",_=>"Dữ liệu cũ chưa đủ nguồn" };
}



