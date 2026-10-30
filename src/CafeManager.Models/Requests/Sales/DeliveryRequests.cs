namespace CafeManager.Models.Requests.Sales;
public sealed record DeliveryRequest(string Code, byte[] Revision, Guid RequestId, string ExpectedStatus, string TargetStatus);
