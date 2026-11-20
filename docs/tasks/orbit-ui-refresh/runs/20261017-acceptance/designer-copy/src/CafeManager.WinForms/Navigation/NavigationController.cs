using CafeManager.Business.Security;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Navigation;

public enum NavigationTarget { Home, Catalogs, SystemInfo }

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
            await permissions.DemandAsync(session, target == NavigationTarget.Catalogs ? AppPermission.CatalogAdmin : AppPermission.SystemInfo, session.Token);
            session.EnsureCurrent(); if (disposed || content.IsDisposed) return false;
            if (!modules.TryGetValue(target, out var control)) { control = create(); control.Dock = DockStyle.Fill; modules.Add(target, control); }
            content.Controls.Clear(); content.Controls.Add(control); current = control;
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
