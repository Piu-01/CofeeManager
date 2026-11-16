namespace CafeManager.Models.Localization;

public static class LanguageService
{
    private static int current;
    public static AppLanguage Current => (AppLanguage)Volatile.Read(ref current);
    public static void Set(AppLanguage language)
    {
        if (!Enum.IsDefined(language)) throw new ArgumentOutOfRangeException(nameof(language));
        Volatile.Write(ref current, (int)language);
    }
    public static void Reset() => Set(AppLanguage.Vietnamese);
}
