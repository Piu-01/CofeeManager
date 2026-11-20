using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.Requests.Inventory;
using CafeManager.Models.Requests.HumanResources;
using Microsoft.Data.SqlClient;

internal static class E2EProbe
{
    internal static async Task Run(SqlConnection owner, string connection, Dictionary<string, object?> evidence)
    {
        var cases = new List<object>();
        evidence["E2ESubcases"] = cases;
        var factory = OperationProbe.RuntimeFactory(connection);
        var auth = new AuthRepository(factory);
        var permissions = new PermissionService(auth);
        var actor = Convert.ToInt32(await StateSnapshot.Scalar(owner, "SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'"));
        using var admin = new SessionContext(await auth.ReadIdentityAsync(actor, default) ?? throw new InvalidOperationException("Actor missing."));
        var sales = new SalesService(new SalesRepository(factory), permissions);
        var purchases = new PurchaseService(new PurchaseRepository(factory), permissions);
        var failed = 0;
        async Task Check(string id, string expected, Func<Task<object>> run)
        {
            try { cases.Add(new { Id = id, Expected = expected, Actual = await run(), Status = "PASS" }); }
            catch (Exception e) { failed++; cases.Add(new { Id = id, Expected = expected, Actual = e.GetType().Name,
                Number = e switch { UserOperationException u => u.Error.Number, SqlException sql => sql.Number, _ => 0 }, Status = "FAIL" }); }
        }
        void Require(bool valid) { if (!valid) throw new InvalidOperationException("E2E assertion failed."); }
        DateTime Now() { var now = DateTime.Now; return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond)); }
        if (await StateSnapshot.Scalar(owner, "SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP001') THEN 1 ELSE 0 END") is not int demo || demo != 1)
        {
            evidence["E2ENative"] = "NOT RUN: clean fixture chỉ bootstrap; E2E SQL dùng nhánh Demo";
            return;
        }
        await Check("T31-K02.sql-purchase-debt", "Nhập10×1000, trả ngay2000, trả nợ3000+5000; tồn+10, còn0; receipts đúng", async () =>
        {
            var inventory = Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));
            var id = Guid.NewGuid();
            var code = await purchases.CreateAsync(admin, new(id, "NCC01", Now(), null, 2000, 10000, [new("NVL01", 10, 1000)]));
            var debts = new SupplierDebtService(new SupplierDebtRepository(factory), permissions);
            var debt = (await debts.ListAsync(admin, "", null, 0)).Single(d => d.PurchaseCode == code);
            Require(debt.Original == 8000 && debt.Remaining == 8000 && await purchases.ReconcileAsync(admin, id) == code);
            var first = Guid.NewGuid(); var second = Guid.NewGuid();
            await Task.Delay(1100);
            var p1 = await debts.PayAsync(admin, new(first, debt.Code, "PT001", 3000, Now()));
            var p2 = await debts.PayAsync(admin, new(second, debt.Code, "PT002", 5000, Now()));
            var after = (await debts.GetAsync(admin, debt.Code))!;
            Require(after.Remaining == 0 && after.History.Count == 2 && await debts.ReconcileAsync(admin, first) == p1
                && await debts.ReconcileAsync(admin, second) == p2
                && Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'")) == inventory + 10);
            return new { Purchase = code, Debt = debt.Code, Payments = new[] { p1, p2 }, Remaining = after.Remaining };
        });
        await Check("T31-K01.sql-two-lines-two-payments", "SP0012dòng khác ghi chú:50000;TT20000+30000;tồnNVL01−.036,NVL03−.020;replaykhôngthu2lần", async () =>
        {
            var before = Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));
            var sugar = Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03'"));
            var code = await sales.AllocateAsync(admin);
            var created = await sales.CreateAsync(admin, new(code, Guid.NewGuid(), new("Tai quan", "B0001"), "SP001", 1, "Ít đường"));
            await sales.LineAsync(admin, new(code, created.Revision, Guid.NewGuid(), "Add", Product: "SP001", Quantity: 1, Note: "Không đá"));
            var order = (await sales.GetAsync(admin, code))!;
            var preview = await sales.PreviewAsync(admin, order.Header);
            Require(order.Lines.Count == 2 && order.Lines.Select(l => l.LineId).Distinct().Count() == 2 && preview.Payable == 50000);
            var request = new CheckoutRequest(code, preview.Revision, Guid.NewGuid(), preview.Token, [new("PT001", 20000), new("PT002", 30000)]);
            await sales.CheckoutAsync(admin, request);
            await sales.CheckoutAsync(admin, request);
            var paid = (await sales.GetAsync(admin, code))!;
            Require(paid.Header.Status == "Da thanh toan" && paid.Payments.Count == 2 && paid.Payments.Sum(p => p.Amount) == 50000
                && Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'")) == before - .036m
                && Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03'")) == sugar - .020m);
            return new { Code = code, Total = paid.Header.Payable, Payments = paid.Payments.Count, Native = "NOT RUN:ưuđãi/điểm/in/bànquaUI kiểm riêng" };
        });
        await Check("T31-K03.sql-stale-stocktake", "Xuấthủy1;snapshotcũbịchặn;snapshotmớiđếmgiảm1/chốtmộtlần", async () =>
        {
            var issues = new StockIssueService(new StockIssueRepository(factory), permissions);
            var stocktake = new StocktakeService(new StocktakeRepository(factory), permissions);
            await issues.CreateAsync(admin, new(Guid.NewGuid(), Now(), "Huy do hong", "E2E G08", [new("NVL01", 1)]));
            var code = await stocktake.WriteAsync(admin, new(Guid.NewGuid(), "StocktakeCreate", null, null, ["NVL01"], []));
            var row = (await stocktake.GetAsync(admin, code))!;
            var count = row.Lines[0].Expected - 1;
            await stocktake.WriteAsync(admin, new(Guid.NewGuid(), "StocktakeSave", code, row.Revision, [], [new("NVL01", count, "Đếm thực tế")]));
            row = (await stocktake.GetAsync(admin, code))!;
            await issues.CreateAsync(admin, new(Guid.NewGuid(), Now(), "Huy do hong", "Phát sinh sau snapshot", [new("NVL01", 1)]));
            await FailureProbe.ExpectRejected(52903, () => stocktake.WriteAsync(admin, new(Guid.NewGuid(), "StocktakeCommit", code, row.Revision, [], [])));
            var fresh = await stocktake.WriteAsync(admin, new(Guid.NewGuid(), "StocktakeCreate", null, null, ["NVL01"], []));
            var updated = (await stocktake.GetAsync(admin, fresh))!;
            var expected = updated.Lines[0].Expected - 1;
            await stocktake.WriteAsync(admin, new(Guid.NewGuid(), "StocktakeSave", fresh, updated.Revision, [], [new("NVL01", expected, "Đếm thực tế")]));
            updated = (await stocktake.GetAsync(admin, fresh))!;
            var commit = new StocktakeRequest(Guid.NewGuid(), "StocktakeCommit", fresh, updated.Revision, [], []);
            await stocktake.WriteAsync(admin, commit);
            await stocktake.WriteAsync(admin, commit);
            Require((await stocktake.GetAsync(admin, fresh))!.Closed && Convert.ToDecimal(await StateSnapshot.Scalar(owner, "SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'")) == expected);
            return new { Stale = code, Committed = fresh, Inventory = expected };
        });
        await Check("T31-K04.sql-overnight-payroll", "Nhânviênmới35000/h,ca22h→6h=8h,thựclĩnh280000;chốt/chireplaymộtnguồn", async () =>
        {
            var employees = new EmployeeService(new EmployeeRepository(factory), permissions);
            var worker = await employees.SaveAsync(admin, new("Công qua đêm G08", null, "Part-time", 0, 35000, "CV003"), null, Guid.NewGuid());
            var day = DateTime.Today.AddDays(-2);
            await new AttendanceService(new AttendanceRepository(factory), permissions).SaveAsync(admin,
                [new(1, "Insert", worker, day, "CA003", day.AddHours(22), day.AddDays(1).AddHours(6), "Di lam", null)], Guid.NewGuid());
            var payroll = new PayrollService(new PayrollRepository(factory), new EmployeeRepository(factory), permissions);
            var request = new PayrollRequest(worker, day.Month, day.Year, 8, 0, 0, 0, 0, 0);
            var preview = await payroll.PreviewAsync(admin, request);
            var id = Guid.NewGuid();
            var code = await payroll.CommitAsync(admin, request, preview.Token, id);
            Require(await payroll.CommitAsync(admin, request, preview.Token, id) == code);
            var row = (await payroll.GetAsync(admin, int.Parse(code)))!;
            Require(row.Amount == 280000);
            await Task.Delay(1100);
            var payId = Guid.NewGuid(); var date = Now();
            var expense = await payroll.PayAsync(admin, row, date, payId);
            Require(await payroll.PayAsync(admin, row, date, payId) == expense);
            var paid = (await payroll.GetAsync(admin, row.Id))!;
            Require(paid.Status != "Unpaid" && paid.Amount == 280000);
            return new { Employee = worker, Payroll = code, Expense = expense, Amount = paid.Amount };
        });
        await Check("T31-K05.sql-delivery-finance", "Giaođãtrả:Chogiao→Đanggiao→Đãgiao;tiền/kho/điểmkhôngđổi", async () =>
        {
            var code = await sales.AllocateAsync(admin);
            await sales.CreateAsync(admin, new(code, Guid.NewGuid(), new("Giao hang", Recipient: "Khách G08", Phone: "0900000000", Address: "Địa chỉ kiểm thử", Ship: 5000), "SP001", 1, null));
            var order = (await sales.GetAsync(admin, code))!;
            var preview = await sales.PreviewAsync(admin, order.Header);
            Require(preview.Payable == 30000);
            await sales.CheckoutAsync(admin, new(code, preview.Revision, Guid.NewGuid(), preview.Token, [new("PT001", 30000)]));
            var before = await StateSnapshot.FinancialHash(owner);
            var delivery = new DeliveryService(new DeliveryRepository(factory), permissions);
            foreach (var (current, target) in new[] { ("Cho giao", "Dang giao"), ("Dang giao", "Da giao") })
            {
                order = (await sales.GetAsync(admin, code))!;
                await delivery.ChangeAsync(admin, new(code, order.Header.Revision, Guid.NewGuid(), current, target));
            }
            Require((await sales.GetAsync(admin, code))!.Header.Delivery == "Da giao" && before == await StateSnapshot.FinancialHash(owner));
            return new { Code = code, Delivery = "Da giao", FinanceUnchanged = true, Limitation = "Chỉ lưu trạng thái hiện tại, không timeline" };
        });
        await Check("T31-K07.sql-session-roles", "CASH/STORE login thật; Finance bị chặn; khóa fixture thu hồi phiên cũ", async () => await SessionProbe.Run(owner, factory, admin));
        await Check("T31-K09.transaction-state", "@@TRANCOUNT/XACT_STATE=0:0 sau các luồng", async () =>
        {
            var state = await StateSnapshot.Scalar(owner, "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)");
            Require((string?)state == "0:0"); return new { State = state };
        });
        evidence["E2ENative"] = "NOT RUN: SQL subcases không thay native E2E, ưu đãi/điểm/report/print/fault kiểm theo guide";
        evidence["E2EFailed"] = failed;
        if (failed != 0) throw new InvalidOperationException("Có subcase E2E FAIL.");
    }
}
