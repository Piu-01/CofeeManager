using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Finance;
using CafeManager.Models.Requests.Finance;
namespace CafeManager.Business.Services;

public sealed class FinanceService(FinanceRepository repository, PermissionService permissions)
{
    public static FinanceRequest Normalize(FinanceRequest r)
    {
        if (r.RequestId == Guid.Empty || r.Amount <= 0 || r.Amount > 999999999999999999m || decimal.Truncate(r.Amount) != r.Amount)
            throw new ArgumentException(L.T("Số tiền phải là nguyên VND dương, tối đa18 chữ số."), "Amount");
        var category = MaterialService.Required(r.Category,5,"Category");
        var note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        var asset = string.IsNullOrWhiteSpace(r.Asset) ? null : r.Asset.Trim();
        if (note?.Length > 200) throw new ArgumentException(L.T("Ghi chú tối đa200 ký tự."), "Note");
        if (asset?.Length > 5) throw new ArgumentException(L.T("Mã tài sản tối đa5 ký tự."), "Asset");
        return r with { Category=category, Note=note, Asset=asset };
    }
    private async Task<T> Run<T>(SessionContext session, Func<CancellationToken,Task<T>> action, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        try { await permissions.DemandAsync(session, AppPermission.Finance, linked.Token); var result = await action(linked.Token); session.EnsureCurrent(); return result; }
        catch(UnauthorizedAccessException e){throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Permission,session.IsRevoked?51505:51605,e.Message));} catch(DataAccessException e) { if (e.Error.Number==51505) session.Revoke(); throw new UserOperationException(e.Error); }
        catch (OperationCanceledException) when (!session.IsRevoked)
        { throw new UserOperationException(new(CafeManager.Models.Errors.DataErrorKind.Cancelled,0,L.T("Đã dừng chờ; kiểm tra kết quả trước khi gửi lại."),true)); }
    }
    public Task<List<FinanceCategory>> CategoriesAsync(SessionContext s, CancellationToken t=default) => Run(s, ct=>repository.CategoriesAsync(s.Identity,ct),t);
    public Task<List<FinanceRow>> ListAsync(SessionContext s, FinanceFilter f, CancellationToken t=default)
    {
        if (f.From.Date>f.To.Date || f.Skip<0 || f.Search.Length>100) throw new ArgumentException(L.T("Bộ lọc không hợp lệ."));
        return Run(s,ct=>repository.ListAsync(s.Identity,f,ct),t);
    }
    public Task<FinanceRow?> GetAsync(SessionContext s,string code,CancellationToken t=default) => Run(s,ct=>repository.GetAsync(s.Identity,code,ct),t);
    public Task<string> CreateAsync(SessionContext s,FinanceRequest r,CancellationToken t=default) => Run(s,ct=>repository.CreateAsync(s.Identity,Normalize(r),ct),t);
    public Task<string?> ReconcileAsync(SessionContext s,Guid request,CancellationToken t=default) => Run(s,ct=>repository.ReceiptAsync(s.Identity,request,ct),t);
}

