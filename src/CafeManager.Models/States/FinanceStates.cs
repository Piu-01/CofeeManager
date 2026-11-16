namespace CafeManager.Models.States;

public static class FinanceStates
{
    public static string TypeLabel(string value) => value switch { "Thu" => L.T("Thu"), "Chi" => L.T("Chi"), _ => value };
    public static string IssueLabel(string value) => value switch { "Huy do hong" => L.T("Hủy do hỏng"), "Tra hang" => L.T("Trả hàng"), _ => value };
    public static string DebtLabel(string value) => value switch
    {
        "Chua tra" => L.T("Chưa trả"), "Tra mot phan" => L.T("Trả một phần"), "Da tra" => L.T("Đã trả"), "Qua han" => L.T("Quá hạn"), _ => value
    };
}
