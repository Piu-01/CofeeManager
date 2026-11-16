using CafeManager.Business.Security;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Navigation;

public enum NavigationTarget { Home, Catalogs, SystemInfo, Materials, Products, Tables, Customers, Loyalty, Sales, Invoices, Delivery, Finance, Inventory, Debt, HumanResources, Assets, Reports }

public sealed class NavigationController(Panel content, SessionContext session, PermissionService permissions) : IDisposable
{
    private readonly Dictionary<NavigationTarget, UserControl> modules = [];
    private bool switching;
    private bool disposed;
    private UserControl? current;
    public async Task<bool> ConfirmLeaveAsync(Control owner)
    {
        if (current is not IUnsavedChanges dirty) return true;
        if (dirty.IsWriting) { await Forms.OrbitConfirmationDialog.ShowAsync(owner, L.T("Đang gửi hoặc cần đối chiếu kết quả. Hãy hoàn tất bước hiện tại."), "CoffeeManager"); return false; }
        if (current is IAsyncLeaveConfirmation confirmation) return await confirmation.ConfirmLeaveAsync(owner);
        if (!dirty.HasUnsavedChanges) return true;
        if (await Forms.OrbitConfirmationDialog.ShowAsync(owner, L.T("Bỏ nháp và rời? Chọn Không để ở lại và lưu bằng nút Lưu."), L.T("Dữ liệu chưa lưu"), MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
        dirty.DiscardChanges(); return true;
    }
    public async Task<bool> NavigateAsync(NavigationTarget target, Func<UserControl> create, Control owner)
    {
        if (switching || disposed) return false;
        switching = true;
        try
        {
            if (!await ConfirmLeaveAsync(owner) || disposed) return false;
            await permissions.DemandAsync(session, target switch { NavigationTarget.Reports => AppPermission.FinancialReports, NavigationTarget.Assets => AppPermission.Finance, NavigationTarget.HumanResources => AppPermission.EmployeeManage, NavigationTarget.Debt => AppPermission.DebtRead, NavigationTarget.Inventory => AppPermission.Inventory, NavigationTarget.Finance => AppPermission.Finance, NavigationTarget.Catalogs => AppPermission.CatalogAdmin, NavigationTarget.Materials => AppPermission.MaterialRead, NavigationTarget.Products => AppPermission.MenuRead, NavigationTarget.Tables or NavigationTarget.Sales or NavigationTarget.Invoices or NavigationTarget.Delivery => AppPermission.Sales, NavigationTarget.Customers => AppPermission.Customers, NavigationTarget.Loyalty => AppPermission.Sales, _ => AppPermission.SystemInfo }, session.Token);
            session.EnsureCurrent(); if (disposed || content.IsDisposed) return false;
            if (!modules.TryGetValue(target, out var control)) { var createTiming = Diagnostics.UiTiming.Start(); control = create(); Diagnostics.UiTiming.Log("nav", "create-module", createTiming, target.ToString()); control.Dock = DockStyle.Fill; modules.Add(target, control); }
            // Giữ màn cache trong cây control để màn đang ẩn vẫn nhận thay đổi DPI.
            // Dựng/bố trí màn mới trong lúc layout bị treo rồi mới cho hiện, để chỉ có một lượt paint.
            var showTiming = Diagnostics.UiTiming.Start();
            content.SuspendLayout();
            NativeRedraw.Freeze(content);
            try
            {
                if (control.Parent != content) { control.Visible = false; content.Controls.Add(control); control.CreateControl(); }
                foreach (Control page in content.Controls) if (page != control) page.Visible = false;
                control.Visible = true; control.BringToFront(); current = control;
            }
            finally
            {
                content.ResumeLayout(true);
                NativeRedraw.Thaw(content);
            }
            Diagnostics.UiTiming.Log("nav", "show-module", showTiming, target.ToString());
            return true;
        }
        finally { switching = false; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var control in modules.Values) control.Dispose();
        modules.Clear();
    }
}
