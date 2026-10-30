namespace CafeManager.Models.Requests.Sales;
public sealed record InvoiceFilter(string DateBasis, DateOnly From, DateOnly To, string? Status = null,
    string? Channel = null, string? Customer = null, string Search = "", int Skip = 0);
