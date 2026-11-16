using CafeManager.Models.Localization;
using CafeManager.WinForms.Configuration;
using Xunit;

namespace CafeManager.Tests.Localization;

[Collection("Localization"), Trait("Category", "Unit")]
public sealed class LanguagePreferenceTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "CoffeeManager-LanguageTests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(folder, "language.txt");

    [Fact]
    public void MissingCorruptAndUnknownPreferencesDefaultToVietnamese()
    {
        var preference = new LanguagePreference(FilePath);
        Assert.Equal(AppLanguage.Vietnamese, preference.Read());
        Directory.CreateDirectory(folder);
        foreach (var text in new[] { "", "fr", "{broken", "EN", "en\nvi" })
        { File.WriteAllText(FilePath, text); Assert.Equal(AppLanguage.Vietnamese, preference.Read()); }
    }

    [Fact]
    public void ChoicePersistsAcrossInstancesWithoutChangingActiveSession()
    {
        var before = LanguageService.Current;
        var preference = new LanguagePreference(FilePath);
        Assert.True(preference.TrySave(AppLanguage.English));
        Assert.Equal("en", File.ReadAllText(FilePath));
        Assert.Equal(AppLanguage.English, new LanguagePreference(FilePath).Read());
        Assert.Equal(before, LanguageService.Current);
        Assert.True(preference.TrySave(AppLanguage.Vietnamese));
        Assert.Equal(AppLanguage.Vietnamese, new LanguagePreference(FilePath).Read());
        Assert.Single(Directory.GetFiles(folder));
    }

    [Fact]
    public void InaccessibleTargetReturnsFailureWithoutChangingExistingChoice()
    {
        var preference = new LanguagePreference(FilePath);
        Assert.True(preference.TrySave(AppLanguage.English));
        using (var locked = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        { Assert.False(preference.TrySave(AppLanguage.Vietnamese)); Assert.Equal(AppLanguage.Vietnamese, preference.Read()); }
        Assert.Equal(AppLanguage.English, preference.Read());
        Assert.Single(Directory.GetFiles(folder));
        Assert.False(preference.TrySave((AppLanguage)99));
    }

    [Fact]
    public void DirectoryInsteadOfFileDoesNotCrash()
    {
        Directory.CreateDirectory(folder);
        var preference = new LanguagePreference(folder);
        Assert.Equal(AppLanguage.Vietnamese, preference.Read());
        Assert.False(preference.TrySave(AppLanguage.English));
    }

    public void Dispose()
    {
        if (!Directory.Exists(folder)) return;
        foreach (var path in Directory.GetFiles(folder)) File.Delete(path);
        Directory.Delete(folder);
    }
}
