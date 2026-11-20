using CafeManager.Models.Errors;

namespace CafeManager.Business.Services;

public sealed class UserOperationException(DataError error) : Exception(error.Message)
{
    public DataError Error { get; } = error;
}
