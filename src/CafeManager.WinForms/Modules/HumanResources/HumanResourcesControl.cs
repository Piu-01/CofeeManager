using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.HumanResources;

public sealed class HumanResourcesControl : UserControl
{
    private bool initialized;
    public HumanResourcesControl(EmployeeService employees, EmployeeAccountService accounts, SessionContext session, AttendanceService attendance, PayrollService payroll, UI.DocumentLinks links)
    {
        Dock = DockStyle.Fill; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        AppTheme.Apply(this);
        var state = new Label { Dock = DockStyle.Fill, Text = L.T("Đang tải nhân sự…"), AutoSize = false }; Controls.Add(state);
        Load += async (_, _) =>
        {
            if (initialized) return; initialized = true;
            try
            {
                var context = await employees.ContextAsync(session, session.Token); if (IsDisposed || session.IsRevoked) return;
                var pending = new PendingOperationFile(context.Scope);
                var tabs = new OrbitTabControl { Dock = DockStyle.Fill, FlatManagementTabs = true };
                // Theme chrome trước khi gắn vào cây đang hiện; không áp lại theme mọi trang con.
                AppTheme.Apply(tabs);
                tabs.AddLazyPage(L.T("Nhân viên"), () => new EmployeesControl(employees, accounts, session, pending, false));
                tabs.AddLazyPage(L.T("Tài khoản"), () => new EmployeesControl(employees, accounts, session, pending, true));
                if(context.SchemaVersion>=23) tabs.AddLazyPage(L.T("Chấm công"), () => new AttendanceControl(attendance,session,pending));
                if(context.SchemaVersion>=24) tabs.AddLazyPage(L.T("Bảng lương"), () => new PayrollControl(payroll,employees,session,pending,links));
                SuspendLayout();
                try { Controls.Remove(state); state.Dispose(); Controls.Add(tabs); }
                finally { ResumeLayout(true); }
            }
            catch (Exception e) when (e is UserOperationException or OperationCanceledException or UnauthorizedAccessException) { if (!IsDisposed) state.Text = L.T("Chưa tải được nhân sự; cần schema022 trở lên. ") + e.Message; }
        };
    }
}


