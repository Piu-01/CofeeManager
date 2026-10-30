namespace CafeManager.Models.States;

public static class SalesStates
{
    public static string Label(string? value) => value switch
    {
        "Tai quan" => "Tại quán", "Mang di" => "Mang đi", "Giao hang" => "Giao hàng",
        "Chua thanh toan" => "Chưa thanh toán", "Da thanh toan" => "Đã thanh toán", "Huy" => "Đã hủy",
        "Cho giao" => "Chờ giao", "Dang giao" => "Đang giao", "Da giao" => "Đã giao", "Huy giao" => "Hủy giao",
        "Promotion" => "Khuyến mãi", "Tier" => "Hạng thành viên", "None" => "Không ưu đãi", _ => value ?? ""
    };
}
