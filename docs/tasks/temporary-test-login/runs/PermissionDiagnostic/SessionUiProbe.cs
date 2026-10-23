using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;

static class SessionUiProbe
{
    public static Task<object> Run(string connection)
    {
        var completion = new TaskCompletionSource<object>();
        var thread = new Thread(() => {
            ApplicationContext? context = null;
            try {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                var repository = new AuthRepository(new ConnectionFactory(connection)); var permissions = new PermissionService(repository);
                var type = typeof(LoginForm).Assembly.GetType("CafeManager.WinForms.ApplicationSessionContext")!;
                context = (ApplicationContext)Activator.CreateInstance(type, new object?[] { new AuthService(repository, new PasswordService()), permissions, repository, null, null, null })!;
                var target = new TemporaryTestTarget(connection, "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo", "20261019_215457_29e552a9");
                Func<CancellationToken, Task<SessionContext>> create = async token => new SessionContext(await target.ReadAdminIdentityAsync(token));
                type.GetProperty("TemporaryTestLogin", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(context, create);
                type.GetMethod("Start")!.Invoke(context, null);
                var login = Application.OpenForms.OfType<LoginForm>().Single();
                if (login.DeviceDpi != 120) { completion.SetResult(new { Check = "ActualLoginHandlerMainForm", Status = "NOT RUN", Dpi = login.DeviceDpi, Reason = "Only125percent permitted" }); login.Close(); return; }
                using var timer = new System.Windows.Forms.Timer { Interval = 100 }; int ticks = 0; bool clicked = false;
                login.LoggedIn += session => login.BeginInvoke((Action)(() => {
                    var mains = Application.OpenForms.OfType<MainForm>().ToArray();
                    if (mains.Length != 1 || session.Identity.Role != "ADMIN") completion.TrySetException(new Exception("Incorrect MainForm/session"));
                    else completion.TrySetResult(new { Check = "ActualLoginHandlerMainForm", Status = "PASS", Dpi = login.DeviceDpi, MainForms = mains.Length, Role = session.Identity.Role });
                    timer.Stop(); context.ExitThread();
                }));
                timer.Tick += (_, _) => {
                    if (!clicked) { clicked = true; var button = (Button)typeof(LoginForm).GetField("temporaryTestButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(login)!; button.PerformClick(); }
                    if (++ticks > 100) { completion.TrySetException(new Exception("Login TEST did not open MainForm")); timer.Stop(); context.ExitThread(); }
                };
                timer.Start(); Application.Run(context);
            } catch (Exception e) { completion.TrySetException(e); } finally { context?.Dispose(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return completion.Task;
    }
}
