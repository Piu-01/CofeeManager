namespace CafeManager.Models.Dtos.Inventory;
public sealed record InventoryLookup(string Code,string Name,string? Unit)
{
    public override string ToString()=>Unit is null?$"{Code} — {Name}":$"{Code} — {Name} ({Unit})";
}
public sealed record PurchaseDetailLine(string Code,string Name,string Unit,decimal Quantity,decimal Price);
public sealed record PurchaseRow(string Code,DateTime Date,string Supplier,decimal Total,string CutoverKind,decimal? Immediate,string? FinanceCode,string? DebtCode,decimal? Debt,byte[] Revision,List<PurchaseDetailLine> Lines,int TotalRows);
