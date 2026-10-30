using System.Reflection;
using System.Text.Json;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;

internal static class Program
{
    private static readonly List<object> Results = [];
    private static int failed;
    private static void Check(string name, bool value, object? actual = null)
    { if (!value) failed++; Results.Add(new { Name = name, Outcome = value ? "PASS" : "FAIL", Actual = actual }); }
    private static async Task<T> Done<T>(Task<T> task) => await task.WaitAsync(TimeSpan.FromSeconds(5));
    private static void MouseUp(Control input) => typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(input, [new MouseEventArgs(MouseButtons.Left, 1, 3, 3, 0)]);
    private static IEnumerable<Control> Children(Control root)
    { foreach (Control c in root.Controls) { yield return c; foreach (var nested in Children(c)) yield return nested; } }
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var main = new MainForm { Text = "ED acceptance — component runner" };
        var background = main.Dialogs.Controls.Cast<Control>().Single();
        var content = Children(background).OfType<Panel>().First(c => c.Dock == DockStyle.Fill && c is not TableLayoutPanel);
        var rootInput = new TextBox { Text = "Root input", Dock = DockStyle.Top }; content.Controls.Add(rootInput);
        using var deadline = new System.Windows.Forms.Timer { Interval = 45000 };
        deadline.Tick += (_, _) => { Check("Runner deadline", false); main.Dispose(); Application.ExitThread(); };
        Application.ThreadException += (_, e) => { Check("UI thread exception", false, e.Exception.ToString()); main.Dispose(); Application.ExitThread(); };
        main.Shown += async (_, _) =>
        {
            deadline.Start();
            try
            {
                rootInput.Focus();
                using (var parent = new OrbitDialogForm { Text = "Parent", Size = new Size(700, 450) })
                {
                    var input = new TextBox { Text = "Draft parent", Dock = DockStyle.Top }; parent.Controls.Add(input);
                    var opened = OrbitDialogHost.ShowAsync(parent, main); await Task.Yield();
                    Check("ED01 one native top-level / child in main", !parent.TopLevel && !parent.ShowInTaskbar && OrbitDialogHost.FindMainForm(parent) == main && Application.OpenForms.Cast<Form>().Count(f => f.TopLevel) == 1);
                    Check("ED02 whole background disabled", !background.Enabled && parent.Enabled);
                    input.Focus();
                    using var child = new OrbitDialogForm { Text = "Nested", Size = new Size(400, 240) };
                    var nested = OrbitDialogHost.ShowAsync(child, input); await Task.Yield();
                    Check("ED03 only top enabled", child.Enabled && !parent.Enabled && parent.Visible);
                    using var unrelated = new OrbitDialogForm();
                    Check("ED08 repeated root opener rejected", await Done(OrbitDialogHost.ShowAsync(unrelated, main)) == DialogResult.Cancel && unrelated.Parent is null);
                    foreach (var size in new[] { new Size(1280,720), new Size(1366,768), new Size(800,600) })
                    {
                        main.ClientSize = size; await Task.Yield();
                        Check("ED12 nested bounds " + size, main.RectangleToScreen(main.ClientRectangle).Contains(child.RectangleToScreen(child.ClientRectangle)), new { Main = main.ClientSize.ToString(), Child = child.Size.ToString(), Dpi = main.DeviceDpi });
                    }
                    child.DialogResult = DialogResult.OK; child.Close();
                    Check("ED03 typed OK result once", await Done(nested) == DialogResult.OK && nested.IsCompletedSuccessfully);
                    Check("ED03 parent input/focus restored", parent.Enabled && input.Text == "Draft parent" && input.Focused);
                    parent.Close(); Check("ED04 cancel closes parent", await Done(opened) == DialogResult.Cancel);
                    Check("ED02 root enabled and focus restored", background.Enabled && rootInput.Focused && !main.Dialogs.IsOpen);
                }
                using (var parent = new GuardedDialog { Text = "Unsaved draft" })
                {
                    var opened = OrbitDialogHost.ShowAsync(parent, main);
                    parent.Confirm = true;
                    var attempt = parent.AttemptClose(); await Task.Yield();
                    var confirmation = Application.OpenForms.OfType<OrbitConfirmationDialog>().Single();
                    Check("ED04 discard confirmation is embedded", !confirmation.TopLevel && !parent.Enabled);
                    Children(confirmation).OfType<Button>().Single(b => b.Text == "Không").PerformClick();
                    Check("ED04 reject discard keeps draft", !await Done(attempt) && !opened.IsCompleted && parent.Enabled);
                    attempt = parent.AttemptClose(); await Task.Yield();
                    confirmation = Application.OpenForms.OfType<OrbitConfirmationDialog>().Single();
                    Children(confirmation).OfType<Button>().Single(b => b.Text == "Có").PerformClick();
                    Check("ED04 accept discard completes", await Done(attempt) && await Done(opened) == DialogResult.Cancel);
                }
                using (var busy = new GuardedDialog { Text = "Busy", Busy = true })
                {
                    var opened = OrbitDialogHost.ShowAsync(busy, main);
                    Check("ED05 busy cannot close", !await Done(busy.AttemptClose()) && !opened.IsCompleted && main.Dialogs.IsOpen);
                    busy.Busy = false; await busy.AttemptClose(); await Done(opened);
                }
                using (var parent = new OrbitDialogForm())
                using (var child = new OrbitDialogForm())
                {
                    var one = OrbitDialogHost.ShowAsync(parent, main); var two = OrbitDialogHost.ShowAsync(child, parent);
                    main.Dialogs.AbortAll();
                    Check("ED05 abort releases nested tasks", await Done(one) == DialogResult.Cancel && await Done(two) == DialogResult.Cancel && !main.Dialogs.IsOpen && background.Enabled);
                    Check("ED05 disposed owner cannot open orphan", await Done(OrbitDialogHost.ShowAsync(new OrbitDialogForm(), child)) == DialogResult.Cancel);
                }
                using (var entry = new OrbitDialogForm { Text = "Input", Size = new Size(600,500) })
                {
                    var fields = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
                    var text = new TextBox { Text = "81.000", Width = 180 };
                    var multi = new TextBox { Text = "Ghi chú ít đá\r\nKhông đường", Multiline = true, Height = 70 };
                    var readOnly = new TextBox { Text = "Readonly", ReadOnly = true };
                    var next = new TextBox { Text = "Next" };
                    var masked = new MaskedTextBox { Mask = "000000", Text = "123456" };
                    var numeric = new NumericUpDown { Value = 12 };
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Text = "Editable" };
                    var choices = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }; choices.Items.AddRange(["One", "Two"]); choices.SelectedIndex = 1;
                    fields.Controls.AddRange([text, multi, readOnly, next, masked, numeric, combo, choices]); entry.Controls.Add(fields);
                    var opened = OrbitDialogHost.ShowAsync(entry, main); await Task.Yield();
                    next.Focus(); text.Focus();
                    Check("ED13 enter selects existing text", text.SelectionLength == text.Text.Length && text.Text == "81.000");
                    text.Select(2,0); MouseUp(text);
                    Check("ED13 first mouse-up corrects native caret", text.SelectionLength == text.Text.Length);
                    text.Select(2,0); MouseUp(text);
                    Check("ED13 subsequent click keeps caret", text.SelectionLength == 0 && text.SelectionStart == 2);
                    next.Focus(); text.Focus(); MouseUp(text); text.SelectedText = "100000";
                    Check("ED13 typing replacement no manual delete", text.Text == "100000");
                    text.Select(3,0); MouseUp(text);
                    Check("ED13 replacement does not reselect on TextChanged", text.SelectionLength == 0 && text.SelectionStart == 3);
                    next.Focus(); text.Focus(); Check("ED13 reentry selects new value", text.SelectionLength == 6);
                    multi.Focus(); Check("ED13 multiline Vietnamese selected", multi.SelectedText == multi.Text);
                    readOnly.Select(0,0); readOnly.Focus(); MouseUp(readOnly);
                    Check("ED13 readonly unchanged", readOnly.SelectionLength == 0 && readOnly.Text == "Readonly");
                    masked.Focus(); Check("ED13 masked input", masked.SelectionLength == masked.Text.Length);
                    var numberText = numeric.Controls.OfType<TextBox>().Single(); numberText.Focus(); MouseUp(numberText);
                    Check("ED13 quantity internal editor", numberText.SelectionLength == numberText.Text.Length && numeric.Value == 12);
                    combo.Focus(); MouseUp(combo); Check("ED13 editable combo", combo.SelectionLength == combo.Text.Length && combo.Text == "Editable");
                    choices.Focus(); MouseUp(choices); Check("ED13 list-only selection preserved", choices.SelectedIndex == 1);
                    OrbitField.ApplyInputBehavior(entry); OrbitField.ApplyInputBehavior(entry);
                    var dynamic = new TextBox { Text = "Dynamic" }; fields.Controls.Add(dynamic); dynamic.Focus(); MouseUp(dynamic);
                    Check("ED13 dynamic input covered", dynamic.SelectionLength == dynamic.Text.Length);
                    dynamic.Select(1,0); MouseUp(dynamic); Check("ED13 repeated registration keeps normal caret", dynamic.SelectionLength == 0 && dynamic.SelectionStart == 1);
                    entry.Close(); await Done(opened);
                }
                for (var i=0;i<20;i++)
                {
                    using var dialog = new OrbitDialogForm(); var opened = OrbitDialogHost.ShowAsync(dialog, main); dialog.Close(); await Done(opened);
                }
                Check("ED11 twenty dialog cycles release forms", Application.OpenForms.Count == 1 && main.Dialogs.Controls.Count == 1 && background.Enabled);
                Results.Add(new { Dpi = main.DeviceDpi, Scope = "Real WinForms; component calls/PerformClick/reflection, no physical input, no visual QA, no SQL" });
            }
            catch (Exception ex) { Check("Runner exception", false, ex.ToString()); }
            finally { deadline.Stop(); main.Dispose(); Application.ExitThread(); }
        };
        Application.Run(main);
        File.WriteAllText(Path.Combine(args[0], "host-input-results.json"), JsonSerializer.Serialize(new { Failed = failed, Results }, new JsonSerializerOptions { WriteIndented = true }));
        return failed == 0 ? 0 : 1;
    }
    private sealed class GuardedDialog : OrbitDialogForm
    {
        internal bool Busy, Confirm;
        internal Task<bool> AttemptClose() => (Task<bool>)typeof(OrbitDialogForm).GetMethod("RequestCloseAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(this, null)!;
        protected override async Task<bool> CanCloseAsync() => !Busy && (!Confirm || await OrbitConfirmationDialog.ShowAsync(this,"Bỏ nháp?","Nháp",MessageBoxButtons.YesNo) == DialogResult.Yes);
    }
}

