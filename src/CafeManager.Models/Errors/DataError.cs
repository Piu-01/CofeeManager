namespace CafeManager.Models.Errors;

public enum DataErrorKind { BusinessRule, Constraint, Duplicate, Permission, Connection, Timeout, Cancelled, Unknown, Schema }

public sealed record DataError(DataErrorKind Kind, int Number, string Message, bool RequiresReconciliation = false);
