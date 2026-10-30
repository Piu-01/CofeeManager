using CafeManager.Models.Dtos;
namespace CafeManager.Business.Services;

public interface IPendingOperationStore
{
    PendingOperation? Read(int actor, string operation);
    void Save(PendingOperation operation);
    void Clear(int actor, string operation);
}
