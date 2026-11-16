using CafeManager.Models.Dtos.Finance;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Finance;

public sealed class FinanceDetailDialog : OrbitDialogForm
{
    public FinanceDetailDialog(FinanceRow row,DocumentLinks? links=null)
    {
        Text=L.T("Chi tiết thu chi — ")+row.Code; AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=AutoScaleMode.Dpi; ClientSize=new Size(620,440);
        var content=new TextBox { ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,
            Text=L.F("{0} — {1}\r\nNgày: {2}\r\nDanh mục: {3}\r\nSố tiền: {4} ₫\r\nNguồn: {5}\r\nPhiếu nhập: {6}\r\nBảng lương: {7}\r\nCông nợ: {8}\r\nTài sản: {9}\r\nGhi chú: {10}\r\n\r\nPhiếu đã xác nhận, chỉ đọc.", row.Code, Models.States.FinanceStates.TypeLabel(row.Type), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm}", row.Date), row.Category, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Amount), OriginText(row.Origin), row.PurchaseCode, row.PayrollId, row.DebtCode, row.AssetCode, row.Note) };
        var close=new OrbitButton { Text=L.T("Đóng") }; close.Click+=(_,_)=>Close();
        var actions=new FlowLayoutPanel { AutoSize=true }; actions.Controls.Add(close);
        Tag="Finance:"+row.Code;
        if(links?.CanAssets==true&&row.AssetCode is not null){var asset=new OrbitButton{Text=L.T("Hồ sơ tài sản")};asset.Click+=async(_,_)=>await links.ShowAssetAsync(this,row.AssetCode);actions.Controls.Add(asset);}
        if(links is not null){if(row.PayrollId is int payrollId){var payroll=new OrbitButton{Text=L.T("Bảng lương nguồn")};payroll.Click+=async(_,_)=>await links.ShowPayrollAsync(this,payrollId);actions.Controls.Add(payroll);}if(row.PurchaseCode is not null){var purchase=new OrbitButton{Text=L.T("Phiếu nhập nguồn")};purchase.Click+=async(_,_)=>await links.ShowPurchaseAsync(this,row.PurchaseCode);actions.Controls.Add(purchase);}if(row.DebtCode is not null){var debt=new OrbitButton{Text=L.T("Công nợ nguồn")};debt.Click+=async(_,_)=>await links.ShowDebtAsync(this,row.DebtCode);actions.Controls.Add(debt);}}
        var dialogLayout=new OrbitDialogLayout(this,content,new Label(),actions);Controls.Add(dialogLayout);dialogLayout.EnableResponsiveActions(this,false);
    }
    internal static string OriginText(string origin) => origin switch { "Manual"=>L.T("Ghi tay"),"PurchaseImmediate"=>L.T("Trả ngay nhập hàng"),"SupplierDebt"=>L.T("Trả nợ nhà cung cấp"),"Payroll"=>L.T("Chi lương"),_=>L.T("Dữ liệu cũ chưa đủ nguồn") };
}
