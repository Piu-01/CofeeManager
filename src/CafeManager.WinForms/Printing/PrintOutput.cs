using System.Diagnostics;

namespace CafeManager.WinForms.Printing;

// File PDF tạm dùng cho preview; dọn file cũ khi app in để không tích rác trong Temp.
internal static class PrintOutput
{
    private static string Folder => Path.Combine(Path.GetTempPath(), "CoffeeManager");

    public static string NewPath(string prefix, string code)
    {
        Directory.CreateDirectory(Folder);
        var safe = string.Concat(code.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        return Path.Combine(Folder, $"{prefix}-{safe}-{Guid.NewGuid():N}.pdf");
    }

    public static void CleanupOld()
    {
        if (!Directory.Exists(Folder)) return;
        foreach (var file in Directory.EnumerateFiles(Folder, "*.pdf"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1)) File.Delete(file);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static void TryDelete(string path)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            catch (IOException) { Thread.Sleep(120); }
            catch (UnauthorizedAccessException) { return; }
        }
    }

    public static void OpenExternal(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
