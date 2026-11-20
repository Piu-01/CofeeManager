using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.HumanResources;
using Microsoft.Data.SqlClient;

// Smoke contract giữa các tầng, không nghiệm thu các ID T24–T26.
if (args.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(args[0], @"^CoffeeManager_Test_G06_\d{8}_\d{6}_[a-f0-9]{8}_demo$")) throw new ArgumentException("Cần DB test demo, marker, output và version.");
var evidence = new Dictionary<string, object?> { ["Kind"] = "Technical dependency smoke; runtime EXECUTE AS USER; not acceptance" };
var builder = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = args[0], IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, Pooling = false };
try
{
    await using var owner = new SqlConnection(builder.ConnectionString); await owner.OpenAsync();
    await using var marker = owner.CreateCommand(); marker.CommandText = "SELECT RunId FROM dbo.G06TestMarker";
    if ((string?)await marker.ExecuteScalarAsync() != args[1]) throw new InvalidOperationException("Marker mismatch.");
    var passwords = new PasswordService();
    await using var setup = owner.CreateCommand();
    setup.CommandText = "DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'); EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@Hash,@MaNV='NV001',@ExpectedStamp=@s; CREATE USER G06Runtime WITHOUT LOGIN; ALTER ROLE CafeManagerApp ADD MEMBER G06Runtime;";
    setup.Parameters.AddWithValue("@Hash", passwords.Hash(Guid.NewGuid().ToString("N"))); await setup.ExecuteNonQueryAsync();
    var factory = new ConnectionFactory(builder.ConnectionString);
    typeof(ConnectionFactory).GetProperty("SessionInitializer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(factory,
        (Func<SqlConnection, CancellationToken, Task>)(async (connection, token) => { await using var command = connection.CreateCommand(); command.CommandText = "EXECUTE AS USER='G06Runtime';"; await command.ExecuteNonQueryAsync(token); }));
    var auth = new AuthRepository(factory); var permissions = new PermissionService(auth);
    using var session = new SessionContext(await auth.ReadIdentityAsync(1, default) ?? throw new InvalidOperationException("Missing actor."));
    var employees = new EmployeeService(new EmployeeRepository(factory), permissions);
    var accounts = new EmployeeAccountService(new EmployeeAccountRepository(factory), permissions, passwords);
    var context = await employees.ContextAsync(session);
    var id = Guid.NewGuid(); var code = await employees.SaveAsync(session, new("Nhân viên smoke", null, "Part-time", 0, 35000, "CV002"), null, id);
    var row = await employees.GetAsync(session, code) ?? throw new InvalidOperationException("Employee mapping failed.");
    if (row.HourlyRate != 35000 || row.Phone is not null || (await employees.ReceiptAsync(session, id, "EmployeeCreate"))?.Code != code) throw new InvalidOperationException("Employee/receipt mapping failed.");
    var pw = Guid.NewGuid().ToString("N"); await accounts.WriteAsync(session, "Create", new(code, null, "smoke.account", "CASH", true), pw, pw, Guid.NewGuid());
    row = await employees.GetAsync(session, code) ?? throw new InvalidOperationException("Account mapping failed.");
    if (row.AccountId is null || row.Role != "CASH" || row.AccountStamp is null) throw new InvalidOperationException("Account metadata failed.");
    if (int.Parse(args[3]) >= 23)
    {
        var attendance = new AttendanceService(new AttendanceRepository(factory), permissions);
        var day = DateTime.Today.AddDays(-1);
        var batchId = Guid.NewGuid();
        await attendance.SaveAsync(session, [new(1,"Insert",code,day,"CA001",day.AddHours(8),day.AddHours(16),"Di lam",null)],batchId);
        var saved = (await attendance.ListAsync(session,new(day.Month,day.Year,code))).Single();
        var receipt = await attendance.ReceiptAsync(session,batchId);
        evidence["AttendanceActual"] = new { saved.Hours, saved.Incomplete, saved.PeriodLocked, ReceiptMatches = string.Equals(receipt?.Code,batchId.ToString(),StringComparison.OrdinalIgnoreCase) };
        if(saved.Hours!=8 || saved.Incomplete || saved.PeriodLocked || !string.Equals(receipt?.Code,batchId.ToString(),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Attendance dependency mapping failed.");
        evidence["AttendanceHours"] = saved.Hours;
        if (int.Parse(args[3]) >= 24)
        {
            var payroll = new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
            var input = new PayrollRequest(code,day.Month,day.Year,8,0,0,0,0,0);
            var preview = await payroll.PreviewAsync(session,input);
            evidence["PayrollPreviewActual"]=preview.Snapshot;
            if(preview.Snapshot.Amount!=280000 || preview.Snapshot.Issues.Count!=0) throw new InvalidOperationException("Payroll preview dependency failed.");
            var payrollId = int.Parse(await payroll.CommitAsync(session,input,preview.Token,Guid.NewGuid()));
            var payrollRow = await payroll.GetAsync(session,payrollId) ?? throw new InvalidOperationException("Payroll mapping failed.");
            if(payrollRow.Status!="Unpaid" || payrollRow.Snapshot?.Attendance.Count!=1) throw new InvalidOperationException("Payroll snapshot dependency failed.");
            await Task.Delay(1200);
            var now = DateTime.Now; now=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerSecond));
            await payroll.PayAsync(session,payrollRow,now,Guid.NewGuid());
            payrollRow = await payroll.GetAsync(session,payrollId) ?? throw new InvalidOperationException("Paid payroll missing.");
            if(payrollRow.Status!="Paid" || payrollRow.ExpenseCode is null) throw new InvalidOperationException("Payroll expense dependency failed.");
            evidence["PayrollId"]=payrollId; evidence["PayrollAmount"]=payrollRow.Amount; evidence["ExpenseCode"]=payrollRow.ExpenseCode;
            var finance=new FinanceService(new FinanceRepository(factory),permissions);
            var links=new CafeManager.WinForms.UI.DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),finance,new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll);
            evidence["UI"]=UiSmoke.Run(employees,accounts,attendance,payroll,session,links,context,payrollRow);
        }
    }
    evidence["Schema"] = context.SchemaVersion; evidence["Employee"] = code; evidence["Runtime"] = "G06Runtime";
    evidence["Status"] = "Succeeded";
}
catch (Exception e) { evidence["Status"] = "Failed"; evidence["Error"] = e.ToString(); if(e is UserOperationException u) evidence["ErrorNumber"] = u.Error.Number; Environment.ExitCode = 1; }
finally { File.WriteAllText(args[2], JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true })); }


