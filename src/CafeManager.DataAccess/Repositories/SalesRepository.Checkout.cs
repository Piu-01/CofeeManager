using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed partial class SalesRepository
{
    public Task<CheckoutPreview> PreviewAsync(AccountIdentity a, string code, byte[] revision, CancellationToken t) =>
        Call("sp_SalesCheckoutPreview", [.. Actor(a), P("@Code", SqlDbType.VarChar, code, -1), P("@ExpectedRevision", SqlDbType.Binary, revision, 8)],
            async (r, ct) =>
            {
                if (!await r.ReadAsync(ct)) throw new InvalidDataException(L.T("Thiếu preview SQL."));
                var code = Text(r, 0); var revision = (byte[])r[1]; var total = r.GetDecimal(2); var ship = r.GetDecimal(3);
                var discount = r.GetDecimal(4); var payable = r.GetDecimal(5); var source = Text(r, 6);
                var percent = r.GetInt32(7); var tierPercent = r.GetInt32(8); var promotionPercent = r.GetInt32(9);
                var tierDiscount = r.GetDecimal(10); var promotionDiscount = r.GetDecimal(11); var promotionCount = r.GetInt32(12);
                var partnerPercent = r.GetDecimal(13); var partnerFee = r.GetDecimal(14); var points = r.GetInt64(15);
                var token = (byte[])r[16];
                var promotions = new List<AppliedPromotion>();
                if (await r.NextResultAsync(ct))
                    while (await r.ReadAsync(ct))
                        promotions.Add(new AppliedPromotion(Text(r, 0), Text(r, 1), r.GetInt32(2), r.GetDecimal(3), r.GetDecimal(4)));
                return new CheckoutPreview(code, revision, total, ship, discount, payable, source, percent, tierPercent,
                    promotionPercent, partnerPercent, partnerFee, points, token, promotionCount, tierDiscount,
                    promotionDiscount, promotions);
            }, t);
    public async Task<SalesWriteResult> CheckoutAsync(AccountIdentity a, CheckoutRequest request, CancellationToken t)
    {
        using var table = PaymentTableBuilder.Build(request.Allocations);
        return await Write(a, "sp_ThanhToanHoaDon", request.Code, request.RequestId,
        [P("@ExpectedRevision", SqlDbType.Binary, request.Revision, 8),
            P("@ExpectedPreviewToken", SqlDbType.Binary, request.PreviewToken, 32), PaymentTableBuilder.Parameter(table)], t);
    }
    public async Task<List<SalesLookup>> PaymentMethodsAsync(AccountIdentity a, CancellationToken t) =>
        (await new PaymentMethodRepository(factory).SupportedAsync(a, PaymentPurpose.Checkout, t))
            .Select(x => new SalesLookup(x.Code, x.Name)).ToList();
}
