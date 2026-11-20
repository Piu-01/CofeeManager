namespace CafeManager.Models.Dtos.Tables;
public sealed record AreaRow(string Code,string Name,byte[] Revision);
public sealed record TableRow(string Code,string Name,string Area,string Status,string? OrderCode,byte[] Revision,DateTime ReadAt);
