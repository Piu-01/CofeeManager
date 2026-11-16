namespace CafeManager.Models.Dtos.Assets;

public sealed record AssetRow(string Code, string Name, DateOnly PurchasedOn, decimal Value, int Months, byte[] Revision);
public sealed record AssetPage(int TotalCount, DateTime ReadAt, List<AssetRow> Rows);
public sealed record AssetExpense(string Code, DateTime Date, string Category, decimal Amount, string? Note);
public sealed record AssetExpensePage(int TotalCount, decimal TotalAmount, DateTime ReadAt, List<AssetExpense> Rows);
public sealed record AssetReceipt(string Code, string Operation, byte[]? Revision);
public sealed record AssetContext(string DatabaseId, string ServerName, string DatabaseName)
{
    public string Scope => DatabaseId + "|" + ServerName.ToUpperInvariant() + "|" + DatabaseName.ToUpperInvariant();
}
