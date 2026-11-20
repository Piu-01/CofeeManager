namespace CafeManager.Models.Dtos.Customers;
public sealed record CustomerRow(string Code,string Name,string? Phone,int Points,string Tier,int Discount,byte[] Revision,DateTime ReadAt);
public sealed record CustomerOrder(string Code,DateTime CreatedAt,string Status);
