using CafeManager.Business.Security;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.Business.Services;

public sealed partial class SalesService
{
    public static IReadOnlyList<PaymentAllocation> NormalizeAllocations(IReadOnlyList<PaymentAllocation> rows)
    {
        return rows.Select(r =>
        {
            LoyaltyService.Money(r.SoTien);
            var code = Code(r.MaPT, 5);
            if (r.SoTien <= 0 || code == "PT010") throw new ArgumentException(L.T("Phân bổ dương; PT010 chưa hỗ trợ."));
            return r with { MaPT = code, MaThamChieu = Optional(r.MaThamChieu, 100) };
        }).ToArray();
    }
    public static decimal CashChange(decimal allocatedCash, decimal tendered)
    {
        LoyaltyService.Money(allocatedCash); LoyaltyService.Money(tendered);
        if (tendered < allocatedCash) throw new ArgumentException(L.T("Tiền đưa chưa đủ phần tiền mặt."));
        return tendered - allocatedCash;
    }
    public Task<CheckoutPreview> PreviewAsync(SessionContext s, SalesOrderHeader h, CancellationToken t = default)
    {
        Revision(h.Revision);
        return Run(s, ct => repository.PreviewAsync(s.Identity, h.Code, h.Revision, ct), t);
    }
    public Task<SalesWriteResult> CheckoutAsync(SessionContext s, CheckoutRequest r, CancellationToken t = default)
    {
        Revision(r.Revision);
        if (r.PreviewToken.Length != 32 || r.RequestId == Guid.Empty) throw new ArgumentException(L.T("Thiếu preview/mã yêu cầu."));
        r = r with { Code = Code(r.Code, 10), Allocations = NormalizeAllocations(r.Allocations) };
        return Run(s, ct => repository.CheckoutAsync(s.Identity, r, ct), t);
    }
    public Task<List<SalesLookup>> PaymentMethodsAsync(SessionContext s, CancellationToken t = default) =>
        Run(s, ct => repository.PaymentMethodsAsync(s.Identity, ct), t);
}
