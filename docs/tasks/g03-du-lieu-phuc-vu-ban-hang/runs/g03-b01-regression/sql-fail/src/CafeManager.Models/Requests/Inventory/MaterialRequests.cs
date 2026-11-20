namespace CafeManager.Models.Requests.Inventory;

public sealed record MaterialRequest(string Code, string Name, string Unit);
public sealed record MaterialOpeningRequest(string Code, decimal Quantity, string Source);
