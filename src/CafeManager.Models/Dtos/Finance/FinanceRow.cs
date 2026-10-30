namespace CafeManager.Models.Dtos.Finance;

public sealed record FinanceCategory(string Code, string Name, string Type)
{
    public override string ToString() => $"{Name} ({Type})";
}
public sealed record FinanceRow(string Code, DateTime Date, string Category, string Type, decimal Amount,
    string? Note, string Origin, string? PurchaseCode, int? PayrollId, string? AssetCode, string? DebtCode,
    decimal TotalIncome, decimal TotalExpense, int TotalRows);
