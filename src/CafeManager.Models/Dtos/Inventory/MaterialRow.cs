namespace CafeManager.Models.Dtos.Inventory;

public sealed record MaterialRow(string Code, string Name, string Unit, decimal Quantity, string? OpeningKind, byte[] Revision, DateTime ReadAt);
public sealed record OperationReceipt(string Code, byte[]? Revision, string Operation, DateTime CompletedAt);
public sealed record OperationResult(string Code, byte[]? Revision, int? AffectedCount = null);

