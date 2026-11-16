using CafeManager.Models.Localization;

namespace CafeManager.WinForms.Configuration;

public sealed class LanguagePreference(string? path = null)
{
    private readonly string path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoffeeManager", "language.txt");

    public AppLanguage Read()
    {
        try { return File.ReadAllText(path).Trim() == "en" ? AppLanguage.English : AppLanguage.Vietnamese; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        { return AppLanguage.Vietnamese; }
    }

    public bool TrySave(AppLanguage language)
    {
        if (!Enum.IsDefined(language)) return false;
        string? temporary = null;
        try
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, language == AppLanguage.English ? "en" : "vi");
            File.Move(temporary, fullPath, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        { return false; }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
        }
    }
}
