namespace CafeManager.WinForms.Modules.Sales;

// Dòng nháp chỉ sống trong bộ nhớ; Key là khóa cục bộ, không phải MaDong.
internal sealed class SalesDraftLine
{
    public required int Key { get; init; }
    public required string Product { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public decimal Amount => Quantity * Price;
}
