namespace CafeManager.Models.Dtos;

// Chỉ giữ nhận dạng và payload nghiệp vụ; không lưu thông tin đăng nhập.
public sealed record PendingOperation(int Actor, Guid RequestId, string Operation, string Payload, DateTime CreatedAt);
