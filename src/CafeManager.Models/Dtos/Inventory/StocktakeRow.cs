namespace CafeManager.Models.Dtos.Inventory;
public sealed record StocktakeLine(string Code,string Name,string Unit,decimal Expected,decimal? Actual,string? Reason);
public sealed record StocktakeRow(string Code,DateTime Date,bool Closed,DateTime? SnapshotAt,string CutoverKind,byte[] Revision,List<StocktakeLine> Lines,int TotalRows);
