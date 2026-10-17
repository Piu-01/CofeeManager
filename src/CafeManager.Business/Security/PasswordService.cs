using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CafeManager.Business.Security;

public sealed class PasswordService
{
    private readonly PasswordHasher<string> hasher = new(Options.Create(new PasswordHasherOptions
    { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, IterationCount = 100000 }));
    public string Hash(string password) => hasher.HashPassword("CoffeeManager", password);
    public PasswordVerificationResult Verify(string hash, string password)
    {
        try { return hasher.VerifyHashedPassword("CoffeeManager", hash, password); }
        catch (Exception e) when (e is FormatException or ArgumentException or IndexOutOfRangeException)
        { return PasswordVerificationResult.Failed; }
    }
    public static string Username(string value)
    {
        var trimmed = value.Trim(' ');
        if (trimmed.Length is < 1 or > 50 || trimmed.Any(c => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-')))
            throw new ArgumentException("Tên đăng nhập gồm 1–50 ký tự ASCII: chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.");
        return trimmed;
    }
}
