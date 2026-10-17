namespace CafeManager.WinForms.UI;

public enum ModuleState { Loading, Empty, Error, Conflict, Unknown, Saved }

public static class OperationStatePresenter
{
    public static void Show(Label label, ModuleState state, string message)
    {
        label.Text = state switch
        {
            ModuleState.Loading => "Đang tải/gửi · " + message,
            ModuleState.Empty => "Không có dữ liệu · " + message,
            ModuleState.Error => "Chưa cập nhật · " + message,
            ModuleState.Conflict => "Xung đột · " + message,
            ModuleState.Unknown => "Kết quả chưa xác định · " + message,
            ModuleState.Saved => "Đã lưu · " + message,
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
