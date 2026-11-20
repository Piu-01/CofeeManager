using System.Text.Json;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string log = args[0];
        bool taskbar = args.Contains("taskbar");
        void Record(string name, string value = "") => File.AppendAllText(log,
            JsonSerializer.Serialize(new { At = DateTimeOffset.Now, Event = name, Value = value }) + Environment.NewLine);
        var parent = new Form { Text = "G03 diagnostic parent", Width = 650, Height = 420 };
        var open = new Button { Text = "Open native modal", Dock = DockStyle.Top };
        var mainInput = new TextBox { AccessibleName = "Parent test input", Dock = DockStyle.Bottom };
        parent.Controls.Add(open); parent.Controls.Add(mainInput);
        parent.Activated += (_, _) => Record("ParentActivated");
        parent.Deactivate += (_, _) => Record("ParentDeactivated");
        mainInput.TextChanged += (_, _) => Record("ParentTextChanged", mainInput.Text);
        open.Click += (_, _) =>
        {
            using var modal = new Form { Text = "G03 diagnostic modal", Width = 450, Height = 220, ShowInTaskbar = taskbar };
            var input = new TextBox { AccessibleName = "Diagnostic input", Dock = DockStyle.Top };
            var close = new Button { Text = "Close probe", Dock = DockStyle.Bottom };
            modal.Controls.Add(input); modal.Controls.Add(close);
            modal.Activated += (_, _) => Record("ModalActivated");
            modal.Deactivate += (_, _) => Record("ModalDeactivated");
            input.GotFocus += (_, _) => Record("InputGotFocus");
            input.LostFocus += (_, _) => Record("InputLostFocus");
            input.KeyDown += (_, e) => Record("InputKeyDown", e.KeyCode.ToString());
            input.TextChanged += (_, _) => Record("ModalTextChanged", input.Text);
            close.Click += (_, _) => modal.Close();
            Record("OpeningModal", taskbar.ToString());
            modal.ShowDialog(parent);
            Record("ModalClosed");
        };
        Record("Started", taskbar.ToString());
        Application.Run(parent);
    }
}
