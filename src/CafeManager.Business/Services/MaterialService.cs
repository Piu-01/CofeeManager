using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;

namespace CafeManager.Business.Services;

public sealed class MaterialService(MaterialRepository repository, PermissionService permissions)
{
    public static string Required(string? value, int maximum, string field)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > maximum || text.Any(char.IsControl)) throw new ArgumentException($"{field}: bắt buộc, tối đa {maximum} ký tự, không chứa ký tự điều khiển.", field);
        return text;
    }
    public static MaterialRequest Normalize(MaterialRequest request)
    {
        var code = Required(request.Code, 5, "Mã").ToUpperInvariant();
        if (!Regex.IsMatch(code, "^NVL(0[1-9]|[1-9][0-9])$")) throw new ArgumentException("Mã nguyên liệu từ NVL01 đến NVL99.", "Mã");
        return new(code, Required(request.Name, 50, "Tên"), Required(request.Unit, 20, "Đơn vị"));
    }
    public static void Quantity(decimal value)
    {
        if (value < 0 || value > 99999999999999.9999m || decimal.Round(value, 4) != value) throw new ArgumentException("Lượng không âm, tối đa 14 số nguyên và 4 số thập phân.", "Lượng");
    }
    private async Task<T> Run<T>(SessionContext session, AppPermission permission, Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, permission, linked.Token);
        try { var result = await action(linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
    public Task<List<MaterialRow>> ListAsync(SessionContext session, string search, int skip, CancellationToken token = default)
    {
        if (search.Length > 100 || skip < 0) throw new ArgumentException("Bộ lọc không hợp lệ.");
        return Run(session, AppPermission.MaterialRead, t => repository.ListAsync(session.Identity, search, skip, t), token);
    }
    public Task<MaterialRow?> GetAsync(SessionContext session, string code, CancellationToken token = default) =>
        Run(session, AppPermission.MaterialRead, t => repository.GetAsync(session.Identity, code, t), token);
    public Task<OperationResult> SaveAsync(SessionContext session, MaterialRequest request, byte[]? revision, Guid requestId, CancellationToken token = default)
    {
        var normalized = Normalize(request);
        return Run(session, AppPermission.MaterialManage, t => repository.WriteAsync(session.Identity, revision is null ? "Create" : "Update", requestId, normalized.Code, revision, normalized.Name, normalized.Unit, null, null, t), token);
    }
    public Task<OperationResult> DeleteAsync(SessionContext session, MaterialRow row, Guid requestId, CancellationToken token = default) =>
        Run(session, AppPermission.MaterialManage, t => repository.WriteAsync(session.Identity, "Delete", requestId, row.Code, row.Revision, null, null, null, null, t), token);
    public Task<OperationResult> OpenAsync(SessionContext session, MaterialOpeningRequest request, byte[] revision, Guid requestId, CancellationToken token = default)
    {
        Quantity(request.Quantity); var source = Required(request.Source, 100, "Nguồn");
        return Run(session, AppPermission.MaterialOpen, t => repository.WriteAsync(session.Identity, "Open", requestId, request.Code, revision, null, null, request.Quantity, source, t), token);
    }
    public Task<OperationReceipt?> ReconcileAsync(SessionContext session, Guid requestId, CancellationToken token = default) =>
        Run(session, AppPermission.MaterialRead, t => repository.ReceiptAsync(session.Identity, requestId, t), token);
}
