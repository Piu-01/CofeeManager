namespace CafeManager.WinForms.UI;

public enum ModuleState { Loading, Empty, Error, Conflict, Unknown, Saved }

public static class OperationStatePresenter
{
    public static void Show(Label label, ModuleState state, string message)
    {
        label.ForeColor = state switch
        {
            ModuleState.Error => Theme.AppTheme.Error,
            ModuleState.Conflict or ModuleState.Unknown => Theme.AppTheme.Warning,
            ModuleState.Saved => Theme.AppTheme.Success,
            _ => Theme.AppTheme.Muted
        };
        label.Text = state switch
        {
            ModuleState.Loading => L.T("Đang tải/gửi · ") + message,
            ModuleState.Empty => L.T("Không có dữ liệu · ") + message,
            ModuleState.Error => L.T("Chưa cập nhật · ") + message,
            ModuleState.Conflict => L.T("Xung đột · ") + message,
            ModuleState.Unknown => L.T("Kết quả chưa xác định · ") + message,
            ModuleState.Saved => L.T("Đã lưu · ") + message,
            _ => message
        };
    }
}

public interface IUnsavedChanges
{
    bool HasUnsavedChanges { get; }
    bool IsWriting { get; }
    void DiscardChanges();
}

// Màn có nháp cần hỏi Giữ/Không giữ/Ở lại tự xử lý toàn bộ luồng rời màn.
public interface IAsyncLeaveConfirmation
{
    Task<bool> ConfirmLeaveAsync(Control owner);
}
