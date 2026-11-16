using System.Globalization;
using System.Text.Json;

namespace CafeManager.Models.Localization;

public static class L
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> english = new(() =>
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream("CafeManager.Localization.en.json")
            ?? throw new InvalidOperationException("Missing embedded English translations.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    });

    public static string T(string vietnamese) => LanguageService.Current == AppLanguage.English &&
        english.Value.TryGetValue(vietnamese.Replace("\r\n", "\n"), out var value) && !string.IsNullOrEmpty(value) ? value : vietnamese;

    public static string F(string vietnameseFormat, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(vietnameseFormat), args);
}
