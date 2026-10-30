namespace CafeManager.Models.Dtos.Inventory;
public sealed record StockIssueDetailLine(string Code,string Name,string Unit,decimal Quantity);
public sealed record StockIssueRow(string Code,DateTime Date,string Kind,string? Reason,string CutoverKind,byte[] Revision,List<StockIssueDetailLine> Lines,int TotalRows);
