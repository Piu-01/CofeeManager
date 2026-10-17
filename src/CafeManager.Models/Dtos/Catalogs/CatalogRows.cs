namespace CafeManager.Models.Dtos.Catalogs;

public enum CatalogKind { ProductCategory, Supplier, PaymentMethod, DeliveryPartner, ExpenseCategory, Position, Shift }
public enum ExpenseType { Thu, Chi }
public abstract record CatalogRow(string Code, string Name, byte[] Revision);
public sealed record ProductCategoryRow(string Code, string Name, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record SupplierRow(string Code, string Name, string Phone, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record PaymentMethodRow(string Code, string Name, bool Supported, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record DeliveryPartnerRow(string Code, string Name, decimal Ratio, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record ExpenseCategoryRow(string Code, string Name, ExpenseType Type, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record PositionRow(string Code, string Name, decimal Allowance, decimal Factor, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record ShiftRow(string Code, string Name, TimeSpan StartTime, TimeSpan EndTime, byte[] Revision) : CatalogRow(Code, Name, Revision);
public sealed record CatalogWriteResult(string Code, byte[]? Revision);
public sealed record NameLookup(string Code, string Name);
public sealed record SupplierLookup(string Code, string Name, string Phone);
public sealed record ExpenseLookup(string Code, string Name, ExpenseType Type);
public sealed record ShiftLookup(string Code, string Name, TimeSpan StartTime, TimeSpan EndTime);
public enum PaymentPurpose { Checkout, Inventory }
