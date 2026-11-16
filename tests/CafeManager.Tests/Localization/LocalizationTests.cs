using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Localization;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.Models.States;
using Xunit;

namespace CafeManager.Tests.Localization;

[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class LocalizationCollection { }

[Collection("Localization"), Trait("Category", "Unit")]
public sealed class LocalizationTests : IDisposable
{
    public LocalizationTests() => LanguageService.Reset();
    public void Dispose() => LanguageService.Reset();

    [Fact]
    public void QuantityAndPrintLabelsRoundTripWithoutChangingVietnamese()
    {
        Assert.Equal("SL", L.T("SL")); Assert.Equal("In…", L.T("In…"));
        LanguageService.Set(AppLanguage.English);
        Assert.Equal("QTY", L.T("SL")); Assert.Equal("Print…", L.T("In…"));
        LanguageService.Reset();
        Assert.Equal("SL", L.T("SL")); Assert.Equal("In…", L.T("In…"));
    }

    [Fact]
    public void DefaultAndUnknownTranslationsKeepOriginalVietnamese()
    {
        Assert.Equal(AppLanguage.Vietnamese, LanguageService.Current);
        Assert.Equal("Lưu", L.T("Lưu"));
        LanguageService.Set(AppLanguage.English);
        Assert.Equal("Một câu chưa có bản dịch\r\n", L.T("Một câu chưa có bản dịch\r\n"));
        Assert.Equal("", L.T(""));
    }

    [Fact]
    public void SwitchingBackRestoresVietnameseAndNormalizesLookupNewlines()
    {
        LanguageService.Set(AppLanguage.English);
        Assert.Equal("Save", L.T("Lưu"));
        Assert.Equal(L.T("CoffeeManager · .NET 10\nSchema version: {0}\nKết nối được xác nhận khi đọc metadata. Không có tích hợp thanh toán online."),
            L.T("CoffeeManager · .NET 10\r\nSchema version: {0}\r\nKết nối được xác nhận khi đọc metadata. Không có tích hợp thanh toán online."));
        LanguageService.Reset();
        Assert.Equal("Lưu", L.T("Lưu"));
    }

    [Fact]
    public void FormattingKeepsBusinessValuesIndependentOfWindowsCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            LanguageService.Set(AppLanguage.English);
            var money = 81000m.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
            var date = new DateTime(2026, 11, 10).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            Assert.Equal("TOTAL DUE: 81.000 ₫", L.F("PHẢI TRẢ: {0}", money));
            Assert.Equal("As of 10/11/2026", L.F("Tại ngày {0}", date));
            Assert.Equal("Page 3", L.F("Trang {0}", 3));
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void DictionaryHasUniqueNonemptyEnglishValuesAndMatchingPlaceholders()
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream("CafeManager.Localization.en.json");
        Assert.NotNull(stream);
        using var json = JsonDocument.Parse(stream);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        LanguageService.Set(AppLanguage.English);
        foreach (var item in json.RootElement.EnumerateObject())
        {
            Assert.True(keys.Add(item.Name), item.Name);
            var value = item.Value.GetString();
            Assert.False(string.IsNullOrWhiteSpace(value), item.Name);
            Assert.DoesNotMatch("[Đđ\\u1ea0-\\u1ef9]", value!);
            Assert.Equal(CompositeFormat.Parse(item.Name).MinimumArgumentCount, CompositeFormat.Parse(value!).MinimumArgumentCount);
            static string[] Slots(string text) => Regex.Matches(text, @"(?<!\{)\{\d+[^}]*\}").Select(m => m.Value).Order().ToArray();
            Assert.Equal(Slots(item.Name), Slots(value!));
            Assert.Equal(value, L.T(item.Name));
        }
        Assert.NotEmpty(keys);
    }

    [Fact]
    public void LanguageDoesNotChangeDomainCodesOrUserData()
    {
        LanguageService.Set(AppLanguage.English);
        var request = SalesService.Normalize(new("Giao hang", Recipient: "Khách Việt", Phone: "0901234567", Address: "Địa chỉ Việt"));
        Assert.Equal("Giao hang", request.Channel);
        Assert.Equal("Khách Việt", request.Recipient);
        Assert.Equal("Địa chỉ Việt", request.Address);
        var category = (ExpenseCategoryRequest)CatalogService.Normalize(new ExpenseCategoryRequest("TC999", "Tên gốc", ExpenseType.Chi), true);
        Assert.Equal(ExpenseType.Chi, category.Type);
        Assert.Equal("Tên gốc", category.Name);
        Assert.Equal("Giao hang", request.Channel);
        Assert.Equal("Delivery", SalesStates.Label(request.Channel));
        var option = new FinanceCategory("TC999", "Tên gốc", "Chi");
        Assert.Equal("Tên gốc (Expense)", option.ToString());
        Assert.Equal("Chi", option.Type);
        Assert.Equal("Damaged stock", FinanceStates.IssueLabel("Huy do hong"));
    }

    [Fact]
    public void SqlErrorMappingUsesCurrentLanguageWithoutChangingErrorMetadata()
    {
        var type = typeof(ConnectionFactory).Assembly.GetType("CafeManager.DataAccess.SqlErrorMapper")!;
        var method = type.GetMethod("MapNumber", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [typeof(int), typeof(bool)])!;
        // Public caller uses this numeric mapping; reflection avoids creating a real SQL error/connection.
        var vietnamese = method.Invoke(null, [53403, false])!;
        LanguageService.Set(AppLanguage.English);
        var english = method.Invoke(null, [53403, false])!;
        var message = vietnamese.GetType().GetProperty("Message")!;
        Assert.NotEqual(message.GetValue(vietnamese), message.GetValue(english));
        LanguageService.Reset();
        Assert.Equal(message.GetValue(vietnamese), message.GetValue(method.Invoke(null, [53403, false])));
        foreach (var property in new[] { "Number", "Kind", "RequiresReconciliation" })
            Assert.Equal(vietnamese.GetType().GetProperty(property)!.GetValue(vietnamese), english.GetType().GetProperty(property)!.GetValue(english));
    }
}
