using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;

internal static partial class Probe
{
    static byte[] EncryptFixture(string json) => ProtectedData.Protect(Encoding.UTF8.GetBytes(json),
        Encoding.UTF8.GetBytes("CoffeeManager.Development.QuickLogin.v1"), DataProtectionScope.CurrentUser);
    static string EnvelopeJson(int version = 1, string server = "localhost", string? database = null, object? accounts = null)
        => JsonSerializer.Serialize(new { Version = version, Server = server, Database = database ?? Db, Accounts = accounts });
    static async Task RegressionEdges()
    {
        var good = File.ReadAllBytes(StoragePath!);
        var account = new Dictionary<string, object?> { ["ADMIN"] = new { Username = "admin", Password = Guid.NewGuid().ToString("N") } };
        var cases = new Dictionary<string, byte[]>
        {
            ["CorruptCiphertext"] = [1, 2, 3],
            ["BadVersion"] = EncryptFixture(EnvelopeJson(version: 2, accounts: account)),
            ["WrongServer"] = EncryptFixture(EnvelopeJson(server: "other", accounts: account)),
            ["WrongDatabase"] = EncryptFixture(EnvelopeJson(database: Db + "_other", accounts: account)),
            ["NullAccounts"] = EncryptFixture(EnvelopeJson()),
            ["NullRoot"] = EncryptFixture("null"),
            ["MalformedJson"] = EncryptFixture("{broken"),
            ["UnknownRole"] = EncryptFixture(EnvelopeJson(accounts: new Dictionary<string, object?> { ["ROOT"] = new { Username = "admin", Password = Guid.NewGuid().ToString("N") } })),
            ["EmptyUsername"] = EncryptFixture(EnvelopeJson(accounts: new Dictionary<string, object?> { ["ADMIN"] = new { Username = " ", Password = Guid.NewGuid().ToString("N") } })),
            ["EmptyPassword"] = EncryptFixture(EnvelopeJson(accounts: new Dictionary<string, object?> { ["ADMIN"] = new { Username = "admin", Password = "" } })),
            ["Oversize"] = new byte[65537]
        };
        try
        {
            foreach (var pair in cases)
                foreach (var operation in new[] { "Get", "Save", "Delete" })
                    await Case("DQ-B01." + pair.Key + "." + operation, () => Sync(() =>
                    {
                        File.WriteAllBytes(StoragePath!, pair.Value);
                        var hash = SHA256.HashData(pair.Value); var refused = false;
                        try
                        {
                            if (operation == "Save") Call(Config, operation, "ADMIN", Credentials["ADMIN"]);
                            else Call(Config, operation, "ADMIN");
                        }
                        catch (InvalidOperationException ex)
                        {
                            refused = true;
                            Assert(ex.Message.StartsWith("Không đọc/ghi được cấu hình đăng nhập nhanh.") && ex.InnerException is null, "Unmapped or leaking error");
                        }
                        Assert(refused, "Invalid data accepted");
                        Assert(File.Exists(StoragePath!) && hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(StoragePath!))), "Failed operation changed previous file");
                        Assert(!Directory.GetFiles(Path.GetDirectoryName(StoragePath!)!, Path.GetFileName(StoragePath!) + ".*.tmp").Any(), "Temporary file leaked");
                    }));
            foreach (var role in new[] { "ADMIN", "CASH", "STORE" })
                await Case("DQ-B01.LoginInvalidConfig." + role, async () =>
                {
                    File.WriteAllBytes(StoragePath!, cases["BadVersion"]);
                    using var login = Login(); login.Show(); var count = 0;
                    login.LoggedIn += session => { count++; session.Dispose(); };
                    await Quick(login, role);
                    Assert(count == 0 && Field<Label>(login, "statusLabel").Text.StartsWith("Không đọc/ghi"), "Login emitted or failed to report configuration error");
                    Assert(Field<Button>(login, "loginButton").Enabled && Field<List<Button>>(login, "devButtons").All(b => b.Enabled), "Login actions stuck disabled");
                    login.Close();
                });
            foreach (var operation in new[] { "save", "delete" })
                await Case("DQ-B01.DialogError." + operation, async () =>
                {
                    File.WriteAllBytes(StoragePath!, cases["BadVersion"]); var hash = SHA256.HashData(File.ReadAllBytes(StoragePath!));
                    var type = App.GetType("CafeManager.WinForms.Forms.DevelopmentQuickLoginDialog")!;
                    using var owner = Login(); owner.Show();
                    using var dialog = (Form)Activator.CreateInstance(type, Any, null, [Auth, Config, "CASH"], null)!;
                    dialog.Show(owner);
                    Field<TextBox>(dialog, "username").Text = Credentials["CASH"].Username;
                    Field<TextBox>(dialog, "password").Text = Credentials["CASH"].Password;
                    Field<Button>(dialog, operation).PerformClick();
                    for (var i = 0; i < 200 && Field<CancellationTokenSource?>(dialog, "pending") is not null; i++) await Task.Delay(25);
                    Assert(Field<Label>(dialog, "status").Text.StartsWith("Không đọc/ghi"), "Dialog reported wrong outcome");
                    Assert(Field<TextBox>(dialog, "username").Text == Credentials["CASH"].Username && Field<TextBox>(dialog, "password").Text == Credentials["CASH"].Password, "Input lost on error");
                    Assert(Field<Button>(dialog, operation).Enabled && hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(StoragePath!))), "Dialog actions stuck or file changed");
                    dialog.Close(); owner.Close();
                });
        }
        finally { File.WriteAllBytes(StoragePath!, good); }
    }
}
