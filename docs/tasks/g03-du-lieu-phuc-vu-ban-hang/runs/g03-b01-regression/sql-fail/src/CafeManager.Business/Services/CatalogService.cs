using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;

namespace CafeManager.Business.Services;

public sealed class CatalogService(CatalogRepository repository, PermissionService permissions)
{
    public static bool IsSystemExpense(string code) => code is "TC005" or "TC011" or "TC012";
    public static CatalogKind KindOf(CatalogRequest request) => request switch
    {
        ProductCategoryRequest => CatalogKind.ProductCategory, SupplierRequest => CatalogKind.Supplier,
        PaymentMethodRequest => CatalogKind.PaymentMethod, DeliveryPartnerRequest => CatalogKind.DeliveryPartner,
        ExpenseCategoryRequest => CatalogKind.ExpenseCategory, PositionRequest => CatalogKind.Position,
        ShiftRequest => CatalogKind.Shift, _ => throw new ArgumentException("Loại danh mục không hợp lệ.")
    };
    public static CatalogRequest Normalize(CatalogRequest request, bool creating)
    {
        var kind = KindOf(request);
        var name = request.Name?.Trim() ?? ""; var code = request.Code?.Trim(' ').ToUpperInvariant() ?? "";
        var max = kind == CatalogKind.Supplier ? 100 : kind == CatalogKind.Shift ? 30 : 50;
        if (name.Length == 0 || name.Length > max || request.Name?.Any(char.IsControl) == true)
            throw new ArgumentException($"Tên bắt buộc, tối đa {max} ký tự; không chứa ký tự điều khiển.", "Name");
        if (creating && kind != CatalogKind.ProductCategory)
        {
            var pattern = kind switch { CatalogKind.Supplier => "NCC[0-9]{2}", CatalogKind.PaymentMethod => "PT[0-9]{3}",
                CatalogKind.DeliveryPartner => "DT[0-9]{3}", CatalogKind.ExpenseCategory => "TC[0-9]{3}", CatalogKind.Position => "CV[0-9]{3}", _ => "CA[0-9]{3}" };
            if (!Regex.IsMatch(code, "\\A" + pattern + "\\z") || code.EndsWith(kind == CatalogKind.Supplier ? "00" : "000", StringComparison.Ordinal))
                throw new ArgumentException("Mã phải đúng prefix và phạm vi số (NCC01–99; PT/DT/TC/CV/CA001–999).", "Code");
        }
        else if (!creating && (code.Length is < 1 or > 5 || code.Any(c => c > 127 || char.IsControl(c))))
            throw new ArgumentException("Mã danh mục không hợp lệ.", "Code");
        var normalized = request with { Name = name, Code = code };
        switch (normalized)
        {
            case ProductCategoryRequest p when creating && p.RequestId == Guid.Empty: throw new ArgumentException("Cần mã yêu cầu tạo danh mục.");
            case SupplierRequest s:
                var phone = s.Phone?.Trim(' ') ?? "";
                if (phone.Length is < 1 or > 15 || phone.Any(c => c is < '0' or > '9')) throw new ArgumentException("SDT bắt buộc, 1–15 chữ số ASCII.", "Phone");
                normalized = s with { Phone = phone }; break;
            case DeliveryPartnerRequest d: Number(d.Ratio, 0, 100, 2, "Ratio"); break;
            case PositionRequest p: Number(p.Allowance, 0, 999999999999999999m, 0, "Allowance"); Number(p.Factor, 1, 999.99m, 2, "Factor"); break;
            case ShiftRequest s:
                if (s.StartTime < TimeSpan.Zero || s.EndTime < TimeSpan.Zero || s.StartTime >= TimeSpan.FromDays(1) || s.EndTime >= TimeSpan.FromDays(1)
                    || s.StartTime.Ticks % TimeSpan.TicksPerMinute != 0 || s.EndTime.Ticks % TimeSpan.TicksPerMinute != 0 || s.StartTime == s.EndTime)
                    throw new ArgumentException("Giờ ca theo phút, trong 00:00–23:59; giờ bắt đầu và kết thúc phải khác nhau.", "StartTime");
                break;
            case ExpenseCategoryRequest e:
                if (!Enum.IsDefined(e.Type)) throw new ArgumentException("Loại phải là Thu hoặc Chi.", "Type");
                if (IsSystemExpense(code) && (creating || e.Type != ExpenseType.Chi)) throw new ArgumentException("Mã Chi hệ thống chỉ được sửa tên.", "Type");
                break;
            case PaymentMethodRequest p:
                if (creating && (p.Code == "PT010" || p.Supported)) throw new ArgumentException("Phương thức mới chưa hỗ trợ; lưu rồi xác nhận riêng. PT010 được dành riêng.", "Supported");
                if (p.Code == "PT010" && p.Supported) throw new ArgumentException("PT010 không được hỗ trợ.", "Supported");
                break;
        }
        return normalized;
    }
    private static void Number(decimal value, decimal min, decimal max, int scale, string field)
    {
        if (value < min || value > max || decimal.Round(value, scale) != value)
            throw new ArgumentException($"Giá trị từ {min} đến {max}, tối đa {scale} chữ số thập phân; không tự làm tròn.", field);
    }
    private async Task<T> Run<T>(SessionContext session, Func<CancellationToken, Task<T>> operation, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, AppPermission.CatalogAdmin, linked.Token);
        try { var result = await operation(linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
    public Task<List<CatalogRow>> ListAsync(SessionContext session, CatalogKind kind, string search, int skip, CancellationToken token = default)
    {
        if (!Enum.IsDefined(kind) || search.Length > 100 || skip < 0) throw new ArgumentException("Bộ lọc hoặc trang không hợp lệ.");
        return Run(session, ct => repository.ListAsync(session.Identity, kind, search, skip, ct), token);
    }
    public Task<CatalogRow?> GetAsync(SessionContext session, CatalogKind kind, string code, CancellationToken token = default) =>
        Run(session, ct => repository.GetAsync(session.Identity, kind, code, ct), token);
    public async Task<CatalogRow?> ReconcileAsync(SessionContext session, CatalogRequest request, bool creating, CancellationToken token = default)
    {
        var normalized = Normalize(request, creating);
        var code = normalized.Code;
        if (creating && normalized is ProductCategoryRequest p)
        {
            code = await Run(session, ct => repository.ReceiptAsync(session.Identity, p.RequestId, ct), token);
            if (code is null) return null;
        }
        return await GetAsync(session, KindOf(normalized), code, token);
    }
    public Task<CatalogWriteResult> SaveAsync(SessionContext session, CatalogRequest request, byte[]? revision, CancellationToken token = default)
    {
        var normalized = Normalize(request, revision is null);
        return Run(session, ct => repository.SaveAsync(session.Identity, KindOf(normalized), normalized, revision, ct), token);
    }
    public Task<CatalogWriteResult> DeleteAsync(SessionContext session, CatalogKind kind, CatalogRow row, CancellationToken token = default)
    {
        if (kind == CatalogKind.ExpenseCategory && IsSystemExpense(row.Code)) throw new ArgumentException("Không được xóa mã Chi hệ thống.");
        return Run(session, ct => repository.DeleteAsync(session.Identity, kind, row.Code, row.Revision, ct), token);
    }
    private async Task<T> Lookup<T>(SessionContext session, AppPermission permission, Func<CancellationToken, Task<T>> read, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, permission, linked.Token);
        try { var result = await read(linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
    public Task<List<NameLookup>> ProductCategoriesAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.MenuRead, t => repository.NamesAsync(s.Identity, CatalogKind.ProductCategory, t), ct);
    public Task<List<SupplierLookup>> SuppliersAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.Inventory, t => repository.SuppliersAsync(s.Identity, t), ct);
    public Task<List<NameLookup>> DeliveryPartnersAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.Sales, t => repository.NamesAsync(s.Identity, CatalogKind.DeliveryPartner, t), ct);
    public Task<List<ExpenseLookup>> ExpensesAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.CatalogAdmin, t => repository.ExpensesAsync(s.Identity, t), ct);
    public Task<List<NameLookup>> PositionsAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.CatalogAdmin, t => repository.NamesAsync(s.Identity, CatalogKind.Position, t), ct);
    public Task<List<ShiftLookup>> ShiftsAsync(SessionContext s, CancellationToken ct = default) => Lookup(s, AppPermission.CatalogAdmin, t => repository.ShiftsAsync(s.Identity, t), ct);
}
