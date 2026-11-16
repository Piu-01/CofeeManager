namespace CafeManager.WinForms.UI;

// Metadata gắn đúng kết quả query, kể cả trang rỗng; query trả muộn không đổi tổng trang mới.
internal sealed class OrbitPageRows<T>(IEnumerable<T> rows,int totalCount) : List<T>(rows)
{
    public int TotalCount {get;}=totalCount;
}
