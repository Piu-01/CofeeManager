namespace CafeManager.Models.Dtos.Inventory;
public sealed record InventoryLookup(string Code,string Name,string? Unit)
{
    public override string ToString()=>Unit is null?$"{Code} — {Name}":$"{Code} — {Name} ({Unit})";
}
public sealed record MaterialPurchaseLookup(string Code,string Name,string? Unit,decimal Stock,decimal? LastPrice)
{
    public override string ToString()=>Unit is null?$"{Code} — {Name}":$"{Code} — {Name} ({Unit})";
}
public sealed record PurchaseDetailLine(string Code,string Name,string Unit,decimal Quantity,decimal? Received,decimal? Diff,string? Reason,decimal Price);
public sealed record PurchaseRow(string Code,DateTime Date,string Supplier,decimal Total,string CutoverKind,decimal? Immediate,string? FinanceCode,string? DebtCode,decimal? Debt,byte[] Revision,
 string Status,decimal PlannedImmediate,DateTime? DueDate,DateTime? CountedAt,string? CountedBy,string? CancelReason,List<PurchaseDetailLine> Lines,int TotalRows)
{
    public string StatusText=>Status switch
    {
        "ChoNhan"=>L.T("Chờ nhận"),"DaKiem"=>L.T("Đã kiểm"),"DaChot"=>L.T("Đã chốt"),"DaHuy"=>L.T("Đã huỷ"),_=>Status
    };
}
