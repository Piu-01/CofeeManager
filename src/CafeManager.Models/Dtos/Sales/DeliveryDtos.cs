namespace CafeManager.Models.Dtos.Sales;
public sealed record DeliveryRow(string Code, DateTime Created, string Recipient, string Phone, string Address,
    string? Partner, string Delivery, string PaymentStatus, byte[] Revision, int TotalCount);
