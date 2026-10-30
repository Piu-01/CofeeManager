using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed partial class SalesRepository
{
    public Task<CheckoutPreview> PreviewAsync(AccountIdentity a, string code, byte[] revision, CancellationToken t) =>
        Call("sp_SalesCheckoutPreview", [.. Actor(a), P("@Code", SqlDbType.VarChar, code, -1), P("@ExpectedRevision", SqlDbType.Binary, revision, 8)],
            async (r, ct) =>
            {
                if (!await r.ReadAsync(ct)) throw new InvalidDataException("Thiếu preview SQL.");
                return new CheckoutPreview(Text(r, 0), (byte[])r[1], r.GetDecimal(2), r.GetDecimal(3), r.GetDecimal(4),
                    r.GetDecimal(5), Text(r, 6), r.GetInt32(7), r.GetDecimal(8), r.GetDecimal(9), r.GetInt64(10), (byte[])r[11]);
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
