using CafeManager.Models.Errors;

namespace CafeManager.DataAccess;

public sealed class DataAccessException(DataError error) : Exception(error.Message)
{
    public DataError Error { get; } = error;
}
