using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;

namespace CafeManager.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var connectionString = Environment.GetEnvironmentVariable("CAFEMANAGER_APP_CONNECTION");
        TableStatusService? service = null;
        string? configurationError = null;
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var factory = new ConnectionFactory(connectionString);
                var log = new DataOperationLog(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CoffeeManager", "logs", "data.log"));
                service = new TableStatusService(new TableStatusRepository(factory, log));
            }
            catch (ArgumentException) { configurationError = "Cấu hình kết nối chưa hợp lệ; kiểm tra cấu hình local."; }
        }
        else configurationError = "Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.";
        Application.Run(new StartupForm(service, configurationError));
    }
}
