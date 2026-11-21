using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.WinForms.Theme;using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;
public sealed class InventoryControl : UserControl
{
    public InventoryControl(PurchaseService purchases,StockIssueService issues,StocktakeService stocktakes,SessionContext session,IPendingOperationStore pending,DocumentLinks links)
    {
        SuspendLayout();
        Dock=DockStyle.Fill;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        AppTheme.Apply(this);
        var tabs=new OrbitTabControl{Dock=DockStyle.Fill,FlatManagementTabs=true};
        // Cấu hình chrome trước khi tạo handle; chỉ trang đang chọn mới tạo control/query.
        AppTheme.Apply(tabs);
        tabs.AddLazyPage(L.T("Nhập hàng"),()=>new PurchasesControl(purchases,session,pending,links));
        tabs.AddLazyPage(L.T("Xuất kho"),()=>new StockIssuesControl(issues,purchases,session,pending));
        tabs.AddLazyPage(L.T("Kiểm kê"),()=>new StocktakesControl(stocktakes,purchases,session,pending));
        Controls.Add(tabs);ResumeLayout(true);
    }
}




