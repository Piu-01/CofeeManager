using System.Diagnostics;
using System.Globalization;

namespace CafeManager.WinForms.Diagnostics;

// Chẩn đoán hiệu năng tạm thời: chỉ ghi khi biến môi trường CAFEMANAGER_DIAG=1.
// Không thay đổi hành vi nghiệp vụ; có thể gỡ cùng đợt đo.
internal static class UiTiming
{
    private static readonly bool enabled = Environment.GetEnvironmentVariable("CAFEMANAGER_DIAG") == "1";
    private static readonly object gate = new();
    private static readonly string logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CoffeeManager", "diagnostics", "ui-timing.log");
    private static bool headerWritten;

    public static bool Enabled => enabled;

    public static long Start() => enabled ? Stopwatch.GetTimestamp() : 0;

    public static void Log(string area, string step, long start, string extra = "")
    {
        if (!enabled || start == 0) return;
        var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        Write(area, step, ms, extra);
    }

    public static void Mark(string area, string step, string extra = "") => Write(area, step, null, extra);

    private static void Write(string area, string step, double? ms, string extra)
    {
        if (!enabled) return;
        try
        {
            lock (gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                if (!headerWritten && !File.Exists(logPath))
                    File.AppendAllText(logPath, "time\tarea\tstep\tms\textra" + Environment.NewLine);
                headerWritten = true;
                var line = string.Format(CultureInfo.InvariantCulture, "{0:HH:mm:ss.fff}\t{1}\t{2}\t{3}\t{4}",
                    DateTime.Now, area, step, ms.HasValue ? ms.Value.ToString("0.0", CultureInfo.InvariantCulture) : "-", extra);
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
