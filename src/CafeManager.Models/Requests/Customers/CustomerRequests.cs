namespace CafeManager.Models.Requests.Customers;
public sealed record CustomerRequest(string Name,string? Phone);
public sealed record CustomerOpeningRequest(string Code,int Points,string Source);
