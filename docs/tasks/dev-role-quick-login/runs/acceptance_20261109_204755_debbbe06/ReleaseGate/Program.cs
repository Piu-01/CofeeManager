using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.WinForms.Forms;
internal static class Gate
{
    [STAThread]
    static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("CAFEMANAGER_USE_DEVELOPMENT", "1");
        Environment.SetEnvironmentVariable("CAFEMANAGER_DEV_QUICK_LOGIN", "1");
        var app = typeof(LoginForm).Assembly;
        var factory = app.GetType("CafeManager.WinForms.Configuration.DevelopmentQuickLogin")!.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = factory.Invoke(null, ["Server=localhost;Database=CoffeeManager_Development;Integrated Security=true"]);
        using var login = new LoginForm();
        var results = new { Kind = args[1], FactoryDisabled = result is null, DefaultDevButtonCount = Count(login), Hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(app.Location))), Source = app.Location };
        File.WriteAllText(args[0], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        Environment.ExitCode = result is null && Count(login) == 0 ? 0 : 1;
    }
    static int Count(Control root) => root.Controls.Cast<Control>().Sum(c => (c is Button && c.Text is "ADMIN" or "CASH" or "STORE" or "Cấu hình đăng nhập nhanh" ? 1 : 0) + Count(c));
}
