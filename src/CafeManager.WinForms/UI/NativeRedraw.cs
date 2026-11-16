using System.Runtime.InteropServices;

namespace CafeManager.WinForms.UI;

// Tạm khóa vẽ của một vùng trong lúc thay màn: nếu để mỗi control con tự vẽ ở nhịp riêng,
// người dùng thấy các mảng đen (cửa sổ con chưa tới lượt WM_PAINT) trong lúc UI thread bận.
// Khóa trước khi thay rồi mở lại kèm một lượt vẽ trọn vẹn cho ra đúng một khung hình đầy đủ.
internal static class NativeRedraw
{
    private const int WM_SETREDRAW = 0x000B;
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    public static void Freeze(Control control)
    {
        if (control.IsHandleCreated) SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
    }

    public static void Thaw(Control control)
    {
        if (!control.IsHandleCreated || control.IsDisposed) return;
        SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
        // Refresh = Invalidate + Update: vẽ ngay, đồng bộ, cả cây con trước khi trả quyền cho message loop.
        control.Refresh();
    }
}
