using CafeManager.Business.Services;
using CafeManager.DataAccess;
using Microsoft.Data.SqlClient;

internal static class FailureProbe
{
    // Chỉ chấp nhận lỗi nghiệp vụ xác định, không coi timeout/cancel bất kỳ là từ chối hợp lệ.
    internal static async Task ExpectRejected(int number, Func<Task> action)
    {
        try { await action(); }
        catch (UserOperationException e) when (e.Error.Number == number) { return; }
        catch (DataAccessException e) when (e.Error.Number == number) { return; }
        catch (SqlException e) when (e.Number == number) { return; }
        throw new InvalidOperationException("Expected business rejection not observed.");
    }
}
