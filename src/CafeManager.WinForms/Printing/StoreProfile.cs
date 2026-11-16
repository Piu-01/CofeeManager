namespace CafeManager.WinForms.Printing;

// Thông tin quán chưa có bảng cấu hình trong DB; cho phép override khi đóng gói mà không đổi schema.
internal static class StoreProfile
{
    public static string Name => Read("CAFEMANAGER_STORE_NAME") ?? "COFFEE MANAGER";
    public static string? Address => Read("CAFEMANAGER_STORE_ADDRESS");
    public static string? Phone => Read("CAFEMANAGER_STORE_PHONE");

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
