namespace CafeManager.Models.Requests;

public sealed record PaymentAllocation(string MaPT, decimal SoTien, string? MaThamChieu = null);
