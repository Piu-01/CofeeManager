namespace CafeManager.Models.Requests.Sales;

public sealed record CheckoutRequest(string Code, byte[] Revision, Guid RequestId, byte[] PreviewToken,
    IReadOnlyList<PaymentAllocation> Allocations);
