namespace CafeManager.Models.Requests.Tables;
public sealed record TableRequest(string Code,string Name,string? Area);
public sealed record CloseSessionRequest(int SessionCode,byte[] Revision,Guid RequestId);
