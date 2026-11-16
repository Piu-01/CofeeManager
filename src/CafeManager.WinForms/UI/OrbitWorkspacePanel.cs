namespace CafeManager.WinForms.UI;

// Vùng chứa màn hình của MainForm: bật double buffer để chuyển màn không lộ nền trống
// khi control con được thêm/ẩn/hiện. Chỉ ảnh hưởng vẽ, không đổi bố cục hay nghiệp vụ.
internal sealed class OrbitWorkspacePanel : Panel
{
    public OrbitWorkspacePanel() { DoubleBuffered = true; }
}
