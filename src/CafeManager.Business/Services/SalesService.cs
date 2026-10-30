using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.Business.Services;

public sealed partial class SalesService(SalesRepository repository, PermissionService permissions)
{
    internal async Task<T> Run<T>(SessionContext session, Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, AppPermission.Sales, linked.Token);
        try { var result = await action(linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException ex)
        {
            if (ex.Error.Number == 51505) session.Revoke();
            throw new UserOperationException(ex.Error);
        }
    }
    public static string? Optional(string? text, int length)
    {
        text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (text?.Length > length) throw new ArgumentException($"Nội dung tối đa {length} ký tự.");
        return text;
    }
    public static string Code(string text, int length) => Optional(text, length) ?? throw new ArgumentException("Thiếu mã nguồn.");
    public static void Revision(byte[] revision) { if (revision.Length != 8) throw new ArgumentException("Cần tải lại phiên bản đơn."); }
    public static OrderHeaderInput Normalize(OrderHeaderInput h)
    {
        LoyaltyService.Money(h.Ship);
        h = h with { TableCode = Optional(h.TableCode, 5), Customer = Optional(h.Customer, 5), Promotion = Optional(h.Promotion, 10),
            Partner = Optional(h.Partner, 5), Recipient = Optional(h.Recipient, 100), Phone = Optional(h.Phone, 15), Address = Optional(h.Address, 250) };
        if (h.Channel is not ("Tai quan" or "Mang di" or "Giao hang")) throw new ArgumentException("Loại đơn không hợp lệ.");
        if (h.Channel == "Tai quan" && h.TableCode is null || h.Channel != "Tai quan" && h.TableCode is not null)
            throw new ArgumentException("Tại quán cần bàn; kênh khác không gán bàn.");
        if (h.Channel != "Giao hang" && (h.Partner is not null || h.Recipient is not null || h.Phone is not null || h.Address is not null || h.Ship != 0))
            throw new ArgumentException("Thông tin giao chỉ dành cho đơn giao hàng.");
        if (h.Channel == "Giao hang" && (h.Recipient is null || h.Phone is null || h.Address is null || h.Phone.Any(c => c is < '0' or > '9')))
            throw new ArgumentException("Cần tên, số điện thoại và địa chỉ giao hợp lệ.");
        return h;
    }
    public Task<string> AllocateAsync(SessionContext s, CancellationToken t = default) => Run(s, repository.AllocateCodeAsync, t);
    public Task<SalesOrderDto?> GetAsync(SessionContext s, string code, CancellationToken t = default) =>
        Run(s, ct => repository.GetAsync(s.Identity, Code(code, 10), ct), t);
    public Task<SalesWriteResult> CreateAsync(SessionContext s, CreateOrderRequest r, CancellationToken t = default)
    {
        if (r.Quantity <= 0 || r.RequestId == Guid.Empty) throw new ArgumentException("Số lượng dương và mã yêu cầu bắt buộc.");
        r = r with { Code = Code(r.Code, 10), Header = Normalize(r.Header), Product = Code(r.Product, 5), Note = Optional(r.Note, 100) };
        return Run(s, ct => repository.CreateAsync(s.Identity, r, ct), t);
    }
    public Task<SalesWriteResult> LineAsync(SessionContext s, OrderLineRequest r, CancellationToken t = default)
    {
        Revision(r.Revision);
        if (r.Action is not ("Add" or "Update" or "Delete") || r.Action != "Delete" && (r.Quantity is null or <= 0)
            || r.Action != "Add" && r.LineId is null || r.RequestId == Guid.Empty) throw new ArgumentException("Dòng món không hợp lệ.");
        r = r with { Code = Code(r.Code, 10), Product = r.Action == "Add" ? Code(r.Product ?? "", 5) : null, Note = Optional(r.Note, 100) };
        return Run(s, ct => repository.LineAsync(s.Identity, r, ct), t);
    }
    public Task<SalesWriteResult> HeaderAsync(SessionContext s, OrderHeaderRequest r, CancellationToken t = default)
    {
        Revision(r.Revision); r = r with { Code = Code(r.Code, 10), Header = Normalize(r.Header) };
        return Run(s, ct => repository.HeaderAsync(s.Identity, r, ct), t);
    }
    public Task<SalesReceipt?> ReconcileAsync(SessionContext s, Guid id, string code, string operation, CancellationToken t = default) =>
        Run(s, ct => repository.ReconcileAsync(s.Identity, id, Code(code, 10), operation, ct), t);
    public Task<List<SalesOrderRow>> OpenAsync(SessionContext s, string q, string? channel, int skip, CancellationToken t = default) =>
        Run(s, ct => repository.OpenAsync(s.Identity, Optional(q, 100) ?? "", channel, skip, ct), t);
    public Task<List<SalesMenuItem>> MenuAsync(SessionContext s, string q, string? category, int skip, CancellationToken t = default) =>
        Run(s, ct => repository.MenuAsync(s.Identity, Optional(q, 100) ?? "", Optional(category, 5), skip, ct), t);
    public Task<List<SalesLookup>> CategoriesAsync(SessionContext s, CancellationToken t = default) => Run(s, ct => repository.CategoriesAsync(s.Identity, ct), t);
    public Task<List<SalesLookup>> PartnersAsync(SessionContext s, CancellationToken t = default) => Run(s, ct => repository.PartnersAsync(s.Identity, ct), t);
}
