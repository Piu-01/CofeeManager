using CafeManager.Models.Dtos.Catalogs;

namespace CafeManager.Models.Requests.Catalogs;

public abstract record CatalogRequest(string Code, string Name);
public sealed record ProductCategoryRequest(string Code, string Name, Guid RequestId) : CatalogRequest(Code, Name);
public sealed record SupplierRequest(string Code, string Name, string Phone) : CatalogRequest(Code, Name);
public sealed record PaymentMethodRequest(string Code, string Name, bool Supported = false) : CatalogRequest(Code, Name);
public sealed record DeliveryPartnerRequest(string Code, string Name, decimal Ratio) : CatalogRequest(Code, Name);
public sealed record ExpenseCategoryRequest(string Code, string Name, ExpenseType Type) : CatalogRequest(Code, Name);
public sealed record PositionRequest(string Code, string Name, decimal Allowance, decimal Factor) : CatalogRequest(Code, Name);
public sealed record ShiftRequest(string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) : CatalogRequest(Code, Name);
