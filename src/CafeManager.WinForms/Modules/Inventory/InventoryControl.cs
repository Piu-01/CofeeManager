using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.WinForms.Theme;using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class InventoryControl : UserControl
{
    public InventoryControl(MaterialService materials,PurchaseService purchases,StockIssueService issues,StocktakeService stocktakes,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        Dock=DockStyle.Fill;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        var tabs=new TabControl{Dock=DockStyle.Fill};var stock=new TabPage("Tồn kho");stock.Controls.Add(new MaterialsControl(materials,session));var receipts=new TabPage("Nhập hàng");receipts.Controls.Add(new PurchasesControl(purchases,session,pending,links));var exports=new TabPage("Xuất kho");exports.Controls.Add(new StockIssuesControl(issues,purchases,session,pending));var counts=new TabPage("Kiểm kê");counts.Controls.Add(new StocktakesControl(stocktakes,purchases,session,pending));tabs.TabPages.AddRange([stock,receipts,exports,counts]);Controls.Add(tabs);AppTheme.Apply(this);
    }
}




