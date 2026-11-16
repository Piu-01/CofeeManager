namespace CafeManager.Models.States;

public static class SalesStates
{
    public static string Label(string? value) => value switch
    {
        "Tai quan" => L.T("Tại quán"), "Mang di" => L.T("Mang đi"), "Giao hang" => L.T("Giao hàng"),
        "Chua thanh toan" => L.T("Chưa thanh toán"), "Cho thanh toan" => L.T("Chờ thanh toán"),
        "Da thanh toan" => L.T("Đã thanh toán"), "Huy" => L.T("Đã hủy"),
        "Cho giao" => L.T("Chờ giao"), "Dang giao" => L.T("Đang giao"), "Da giao" => L.T("Đã giao"), "Huy giao" => L.T("Hủy giao"),
        "Promotion" => L.T("Khuyến mãi"), "Tier" => L.T("Hạng thành viên"), "None" => L.T("Không ưu đãi"), _ => value ?? ""
    };
}
