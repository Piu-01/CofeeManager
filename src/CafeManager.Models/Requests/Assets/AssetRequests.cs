namespace CafeManager.Models.Requests.Assets;

public sealed record AssetRequest(string Name, DateOnly PurchasedOn, decimal Value, int Months);
public sealed record AssetFilter(string Search = "", int Skip = 0);
public sealed record AssetWriteRequest(Guid RequestId, string Action, string? Code, byte[]? ExpectedRevision, AssetRequest? Value);
