using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CafeManager.WinForms.UI;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, ref Rect rect);
    [STAThread] private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "B04 — kiểm tra popup lịch", ClientSize = new Size(600, 420), StartPosition = FormStartPosition.CenterScreen };
        using var field = new OrbitDateField { Location = new Point(30, 70), Width = 520 };
        field.Value = new DateTime(2026, 10, 20);
        form.Controls.Add(field);
        if (args.Contains("--edge"))
        {
            form.StartPosition = FormStartPosition.Manual;
            var area = Screen.PrimaryScreen!.WorkingArea;
            form.Location = new Point(area.Right - form.Width, area.Bottom - form.Height);
            field.Location = new Point(30, form.ClientSize.Height - 80);
        }
        var calendar = (MonthCalendar)typeof(OrbitDateField).GetField("calendar", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
        var popup = (ToolStripDropDown)typeof(OrbitDateField).GetField("popup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
        form.Shown += (_, _) =>
        {
            typeof(OrbitDateField).GetMethod("OpenCalendar", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(field, null);
            form.BeginInvoke(() =>
            {
                Rect required = new();
                SendMessage(calendar.Handle, 0x1009, IntPtr.Zero, ref required); // MCM_GETMINREQRECT, chính HWND của harness.
                var result = new { field.DeviceDpi, Calendar = calendar.Size, calendar.SingleMonthSize, NativeRequired = new Size(required.Right - required.Left, required.Bottom - required.Top), Host = popup.Items[0].Size, Popup = popup.ClientSize, CalendarBounds = calendar.Bounds, HostBounds = popup.Items[0].Bounds, PopupBounds = popup.Bounds, WorkingArea = Screen.FromControl(field).WorkingArea };
                File.WriteAllText(args[0], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                var fits = calendar.Width >= result.NativeRequired.Width && calendar.Height >= result.NativeRequired.Height
                    && popup.ClientRectangle.Contains(calendar.Bounds) && result.WorkingArea.Contains(popup.Bounds);
                if (args.Contains("--edge")) fits &= popup.Bottom <= field.PointToScreen(Point.Empty).Y;
                Console.WriteLine($"Native popup bounds at {field.DeviceDpi} DPI: {(fits ? "PASS" : "FAIL")}");
                Environment.ExitCode = fits ? 0 : 1;
                if (!args.Contains("--interactive")) form.Close();
            });
        };
        Application.Run(form);
    }
}
