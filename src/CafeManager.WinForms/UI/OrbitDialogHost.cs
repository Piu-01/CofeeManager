using CafeManager.WinForms.Forms;

namespace CafeManager.WinForms.UI;

// Một điểm mở Form modal native: owner chain, taskbar và kết quả dùng chung.
public sealed class OrbitDialogHost : IDisposable
{
    private sealed class Entry(OrbitDialogForm dialog, Form owner, Control? focus)
    {
        public OrbitDialogForm Dialog { get; } = dialog;
        public Form Owner { get; } = owner;
        public Control? Focus { get; } = focus;
        public TaskCompletionSource<DialogResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Started, Finished;
    }
    private readonly MainForm main;
    private readonly List<Entry> stack = [];
    private bool aborting, disposed;
    public bool IsOpen => stack.Count != 0;
    public OrbitDialogHost(MainForm main) => this.main = main;

    public static MainForm FindMainForm(Control owner)
    {
        for (Control? current = owner; current is not null; current = current.Parent)
        {
            if (current is MainForm root) return root;
            if (current is OrbitDialogForm { DialogHost: { } manager }) return manager.main;
        }
        for (var form = owner.FindForm()?.Owner; form is not null; form = form.Owner)
            if (form is MainForm root) return root;
        throw new InvalidOperationException(L.T("Hộp nghiệp vụ cần có cửa sổ cha CoffeeManager."));
    }
    public static Task<DialogResult> ShowAsync(OrbitDialogForm dialog, Control owner) => owner.IsDisposed || owner.Disposing
        ? Task.FromResult(DialogResult.Cancel) : FindMainForm(owner).Dialogs.OpenAsync(dialog, owner);
    public static IWin32Window NativeOwner(Control owner) => owner.FindForm() ?? FindMainForm(owner);

    // Login thuộc vòng đời app, chưa có MainForm để dùng stack hộp nghiệp vụ.
    internal static DialogResult ShowLoginDialog(OrbitDialogForm dialog, LoginForm owner)
    {
        if (owner.IsDisposed || owner.Disposing) return DialogResult.Cancel;
        dialog.ShowInTaskbar = false; dialog.MinimizeBox = dialog.MaximizeBox = false;
        dialog.StartPosition = FormStartPosition.CenterParent;
        OrbitField.ApplyInputBehavior(dialog);
        return dialog.ShowDialog(owner);
    }

    private Task<DialogResult> OpenAsync(OrbitDialogForm dialog, Control owner)
    {
        if (aborting || disposed || main.IsDisposed || main.Disposing || owner.IsDisposed) return Task.FromResult(DialogResult.Cancel);
        if (dialog.DialogHost is not null) throw new InvalidOperationException(L.T("Hộp đã được mở."));
        var parent = owner.FindForm() ?? main;
        if (stack.Count > 0 && parent != stack[^1].Dialog) return Task.FromResult(DialogResult.Cancel);
        var entry = new Entry(dialog, parent, FocusedDescendant(parent));
        dialog.DialogHost = this; dialog.TopLevel = true; dialog.Dock = DockStyle.None;
        dialog.ShowInTaskbar = false; dialog.MinimizeBox = dialog.MaximizeBox = false;
        if (dialog.FormBorderStyle == FormBorderStyle.None) dialog.FormBorderStyle = FormBorderStyle.Sizable;
        dialog.StartPosition = FormStartPosition.CenterParent;
        OrbitField.ApplyInputBehavior(dialog);
        stack.Add(entry);
        try
        {
            // Queue trên UI hiện tại để caller await; modal Windows tự khóa đúng owner.
            main.BeginInvoke((Action)(() =>
            {
                if (entry.Finished) return;
                DialogResult result = DialogResult.Cancel;
                try
                {
                    if (disposed || dialog.IsDisposed || parent.IsDisposed) return;
                    entry.Started = true;
                    result = dialog.ShowDialog(parent);
                }
                catch (Exception ex) { entry.Completion.TrySetException(ex); }
                finally { Finish(entry, result); }
            }));
        }
        catch { Finish(entry, DialogResult.Cancel); throw; }
        return entry.Completion.Task;
    }
    private void Finish(Entry entry, DialogResult result)
    {
        if (entry.Finished) return;
        entry.Finished = true; stack.Remove(entry); entry.Dialog.DialogHost = null;
        if (!aborting && !disposed && entry.Focus is { IsDisposed: false, CanFocus: true } focus) focus.Focus();
        entry.Completion.TrySetResult(result == DialogResult.None ? DialogResult.Cancel : result);
    }
    public async Task<bool> CloseAllAsync()
    {
        while (stack.Count > 0)
        {
            var entry = stack[^1];
            if (!await entry.Dialog.RequestCloseAsync()) return false;
            await entry.Completion.Task;
        }
        return true;
    }
    public void AbortAll()
    {
        aborting = true;
        try
        {
            foreach (var entry in stack.ToArray().Reverse())
            {
                if (!entry.Dialog.IsDisposed) entry.Dialog.Abort();
                if (!entry.Started) Finish(entry, DialogResult.Cancel);
            }
        }
        finally { aborting = false; }
    }
    private static Control? FocusedDescendant(Control root)
    { foreach (Control child in root.Controls) { if (child.Focused) return child; if (child.ContainsFocus) return FocusedDescendant(child); } return null; }
    public void Dispose() { if (disposed) return; disposed = true; AbortAll(); }
}
