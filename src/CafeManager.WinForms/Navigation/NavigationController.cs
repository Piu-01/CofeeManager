using CafeManager.Business.Security;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Navigation;

public enum NavigationTarget { Home, Catalogs, SystemInfo, Materials, Products, Tables, Customers, Loyalty }

public sealed class NavigationController(Panel content, SessionContext session, PermissionService permissions) : IDisposable
{
    private readonly Dictionary<NavigationTarget, UserControl> modules = [];
    private bool switching;
    private bool disposed;
    private UserControl? current;
    public bool ConfirmLeave(IWin32Window owner)
    {
        if (current is not IUnsavedChanges dirty) return true;
        if (dirty.IsWriting) { MessageBox.Show(owner, "Đang gửi hoặc cần đối chiếu kết quả. Hãy hoàn tất bước hiện tại.", "CoffeeManager"); return false; }
        if (!dirty.HasUnsavedChanges) return true;
        if (MessageBox.Show(owner, "Bỏ nháp và rời? Chọn Không để ở lại và lưu bằng nút Lưu.", "Dữ liệu chưa lưu", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
        dirty.DiscardChanges(); return true;
    }
    public async Task<bool> NavigateAsync(NavigationTarget target, Func<UserControl> create, IWin32Window owner)
    {
        if (switching || disposed || !ConfirmLeave(owner)) return false;
        switching = true;
        try
        {
            await permissions.DemandAsync(session, target switch { NavigationTarget.Catalogs => AppPermission.CatalogAdmin, NavigationTarget.Materials => AppPermission.MaterialRead, NavigationTarget.Products => AppPermission.MenuRead, NavigationTarget.Tables => AppPermission.Sales, NavigationTarget.Customers => AppPermission.Customers, NavigationTarget.Loyalty => AppPermission.Sales, _ => AppPermission.SystemInfo }, session.Token);
            session.EnsureCurrent(); if (disposed || content.IsDisposed) return false;
            if (!modules.TryGetValue(target, out var control)) { control = create(); control.Dock = DockStyle.Fill; modules.Add(target, control); }
            // Giữ màn cache trong cây control để màn đang ẩn vẫn nhận thay đổi DPI.
            foreach (Control page in content.Controls) if (page != control) page.Visible = false;
            if (control.Parent != content) content.Controls.Add(control);
            control.Visible = true; control.BringToFront(); current = control;
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
