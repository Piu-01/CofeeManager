using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Customers;

static class Program
{
    static IEnumerable<Control> All(Control root) { yield return root; foreach(Control child in root.Controls) foreach(var item in All(child)) yield return item; }
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        // Chỉ kiểm cấu trúc control khi chưa hiển thị; không mở kết nối DB hoặc đăng nhập.
        var factory = new ConnectionFactory("Server=localhost;Database=Unused_Probe;Integrated Security=true;TrustServerCertificate=true");
        var service = new CustomerService(new CustomerRepository(factory), new PermissionService(new AuthRepository(factory)));
        var results = new List<object>();
        foreach(var role in new[] { "ADMIN", "CASH" })
        {
            using var session = new SessionContext(new AccountIdentity(1,"NVTEST","Fixture","fixture",role,true,true,new byte[8]));
            foreach(var allowGuest in new[] { false, true })
            {
                using var picker = new CustomerPickerDialog(service,session,allowGuest);
                var actions = All(picker).OfType<Button>().Select(b=>b.AccessibleName).Where(n=>n is not null).ToArray();
                var pass = !actions.Any(n=>n is "Thêm" or "Sửa" or "Xóa" or "Mở sổ điểm" or "Hóa đơn") && actions.Contains("Chọn khách") && actions.Contains("Đóng") && actions.Contains("Khách vãng lai")==allowGuest && picker.CancelButton is Button { Text: "Đóng" };
                results.Add(new { Case="PickerActions", Role=role, AllowGuest=allowGuest, Actions=actions, Pass=pass }); if(!pass)Environment.ExitCode=1;
            }
            using var page = new CustomersControl(service,session);
            var mainActions=All(page).OfType<Button>().Select(b=>b.AccessibleName).ToArray();
            var mainPass=new[] { "Thêm", "Sửa", "Xóa", "Mở sổ điểm", "Hóa đơn" }.All(mainActions.Contains) && !mainActions.Contains("Chọn khách") && !mainActions.Contains("Đóng");
            results.Add(new { Case="ManagementPageActionsRetained", Role=role, Pass=mainPass }); if(!mainPass)Environment.ExitCode=1;
        }
        File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
    }
}
