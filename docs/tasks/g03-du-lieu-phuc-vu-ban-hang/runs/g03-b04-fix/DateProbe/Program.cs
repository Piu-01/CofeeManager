using CafeManager.WinForms.UI;
using System.Text.Json;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var results = new List<object>();
        void Check(string id, Action action)
        {
            try { action(); results.Add(new { Id = id, Status = "PASS" }); }
            catch (Exception ex) { results.Add(new { Id = id, Status = "FAIL", Error = ex.Message }); }
        }
        using var field = new OrbitDateField();
        var input = field.Controls.OfType<TextBox>().Single();
        void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
        Check("Date-only-roundtrip", () => { field.Value = new DateTime(2026, 10, 20, 15, 30, 0); Assert(field.Value == new DateTime(2026, 10, 20) && input.Text == "20/10/2026"); });
        Check("Leap-day", () => { input.Text = "29/02/2024"; Assert(field.Value == new DateTime(2024, 2, 29)); });
        foreach (var text in new[] { "29/02/2026", "31/04/2026", "02/29/2024", "", "01/01/1752", "2026-10-20" })
            Check("Reject-" + text, () => { input.Text = text; try { _ = field.Value; throw new Exception("Invalid date accepted"); } catch (ArgumentException) { Assert(input.Text == text); } });
        Check("Invalid-text-invalidates-preview", () => { int changes = 0; field.ValueChanged += (_, _) => changes++; input.Text = "invalid"; Assert(changes == 1 && input.Text == "invalid"); });
        Check("Correct-after-invalid", () => { input.Text = "21/10/2026"; Assert(field.Value == new DateTime(2026, 10, 21)); });
        Check("Native-calendar-selection", () => { field.Value = new DateTime(2026, 10, 20); var popup = typeof(OrbitDateField).GetField("calendar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(field) as MonthCalendar; popup!.CreateControl(); Assert(popup.SelectionStart == field.Value); });
        File.WriteAllText(args[0], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Date field checks: {results.Count}");
        if (results.Any(r => r.GetType().GetProperty("Status")!.GetValue(r) as string == "FAIL")) Environment.ExitCode = 1;
    }
}
