using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Errors;

namespace CafeManager.Business.Services;

public sealed record TableStatusResult(IReadOnlyList<TableStatusDto>? Rows, DataError? Error);

public sealed class TableStatusService(TableStatusRepository repository)
{
    public async Task<TableStatusResult> LoadAsync(string? areaCode, CancellationToken cancellationToken)
    {
        try { return new(await repository.GetAsync(areaCode, cancellationToken), null); }
        catch (DataAccessException exception) { return new(null, exception.Error); }
    }
}
