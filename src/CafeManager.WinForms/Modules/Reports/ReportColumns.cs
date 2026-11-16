using CafeManager.Models.Requests.Reports;

namespace CafeManager.WinForms.Modules.Reports;

internal enum ReportValueKind { Text, Money, Quantity, Hours, Integer, Date }

internal sealed record ReportColumn(string Field, string Header, ReportValueKind Kind, float Weight, bool Right = false);

// Một nguồn mô tả cột cho grid, PDF và CSV để nhãn/độ rộng không lệch giữa các đầu ra.
internal static class ReportColumns
{
    public static IReadOnlyList<ReportColumn> For(ReportKind kind) => kind switch
    {
        ReportKind.SalesDay or ReportKind.SalesChannel =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 3f),
            new("Bills", Header("Bills"), ReportValueKind.Integer, 0.8f, true),
            new("Gross", Header("Gross"), ReportValueKind.Money, 1.4f, true),
            new("Discount", Header("Discount"), ReportValueKind.Money, 1.2f, true),
            new("AfterDiscount", Header("AfterDiscount"), ReportValueKind.Money, 1.4f, true),
            new("Ship", Header("Ship"), ReportValueKind.Money, 1f, true),
            new("CustomerPaid", Header("CustomerPaid"), ReportValueKind.Money, 1.4f, true),
            new("PartnerFee", Header("PartnerFee"), ReportValueKind.Money, 1.2f, true),
        ],
        ReportKind.Products =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 3f),
            new("Bills", Header("Bills"), ReportValueKind.Integer, 0.8f, true),
            new("Quantity", Header("Quantity"), ReportValueKind.Quantity, 1f, true),
            new("BeforeBillDiscount", Header("BeforeBillDiscount"), ReportValueKind.Money, 1.5f, true),
        ],
        ReportKind.Payments =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 3f),
            new("Bills", Header("Bills"), ReportValueKind.Integer, 0.8f, true),
            new("Amount", Header("Amount"), ReportValueKind.Money, 1.5f, true),
        ],
        ReportKind.Debts =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 3f),
            new("SourceDate", Header("SourceDate"), ReportValueKind.Date, 1.3f, true),
            new("Original", Header("Original"), ReportValueKind.Money, 1.4f, true),
            new("Paid", Header("Paid"), ReportValueKind.Money, 1.4f, true),
            new("Remaining", Header("Remaining"), ReportValueKind.Money, 1.4f, true),
        ],
        ReportKind.CashFlow =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 3f),
            new("Inflow", Header("Inflow"), ReportValueKind.Money, 1.4f, true),
            new("Outflow", Header("Outflow"), ReportValueKind.Money, 1.4f, true),
            new("Net", Header("Net"), ReportValueKind.Money, 1.4f, true),
        ],
        ReportKind.Labor =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 2.4f),
            new("IncompleteShifts", Header("IncompleteShifts"), ReportValueKind.Integer, 1f, true),
            new("Hours", Header("Hours"), ReportValueKind.Hours, 1f, true),
            new("RegularSnapshotHours", Header("RegularSnapshotHours"), ReportValueKind.Hours, 1.2f, true),
            new("OvertimeSnapshotHours", Header("OvertimeSnapshotHours"), ReportValueKind.Hours, 1.2f, true),
            new("Committed", Header("Committed"), ReportValueKind.Money, 1.4f, true),
            new("Paid", Header("Paid"), ReportValueKind.Money, 1.4f, true),
            new("Coverage", Header("Coverage"), ReportValueKind.Text, 1.5f),
        ],
        ReportKind.Assets =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 2.4f),
            new("PurchasedOn", Header("PurchasedOn"), ReportValueKind.Date, 1.2f, true),
            new("Value", Header("Value"), ReportValueKind.Money, 1.4f, true),
            new("RelatedExpenses", Header("RelatedExpenses"), ReportValueKind.Money, 1.3f, true),
            new("PeriodDepreciation", Header("PeriodDepreciation"), ReportValueKind.Money, 1.4f, true),
            new("Accumulated", Header("Accumulated"), ReportValueKind.Money, 1.4f, true),
            new("Remaining", Header("Remaining"), ReportValueKind.Money, 1.3f, true),
        ],
        _ =>
        [
            new("Code", Header("Code"), ReportValueKind.Text, 1.2f),
            new("Name", Header("Name"), ReportValueKind.Text, 2.2f),
            new("OpenedAt", Header("OpenedAt"), ReportValueKind.Date, 1.2f, true),
            new("Opening", Header("Opening"), ReportValueKind.Quantity, 1f, true),
            new("Purchases", Header("Purchases"), ReportValueKind.Quantity, 1f, true),
            new("Issues", Header("Issues"), ReportValueKind.Quantity, 1f, true),
            new("Consumption", Header("Consumption"), ReportValueKind.Quantity, 1f, true),
            new("Adjustment", Header("Adjustment"), ReportValueKind.Quantity, 1f, true),
            new("Closing", Header("Closing"), ReportValueKind.Quantity, 1f, true),
            new("Current", Header("Current"), ReportValueKind.Quantity, 1f, true),
            new("Coverage", Header("Coverage"), ReportValueKind.Text, 1.5f),
        ],
    };

    public static string Header(string name) => name switch
    {
        "Code" => L.T("Mã / Nhóm"),
        "Name" => L.T("Nội dung"),
        "Bills" => L.T("Số đơn"),
        "Gross" => L.T("Tiền món / Nợ gốc"),
        "Discount" => L.T("Giảm"),
        "AfterDiscount" => L.T("Món sau giảm"),
        "Ship" => L.T("Phí giao"),
        "CustomerPaid" => L.T("Khách trả"),
        "PartnerFee" => L.T("Phí đối tác"),
        "Quantity" => L.T("Số lượng"),
        "BeforeBillDiscount" => L.T("Trước giảm toàn bill"),
        "Amount" => L.T("Số tiền"),
        "Original" => L.T("Nợ gốc"),
        "Paid" => L.T("Đã chi / Đã trả"),
        "Remaining" => L.T("Còn lại"),
        "Inflow" => L.T("Thu"),
        "Outflow" => L.T("Chi"),
        "Net" => L.T("Thu − Chi"),
        "Hours" => L.T("Giờ theo ngày làm"),
        "RegularSnapshotHours" => L.T("Giờ thường chốt (New)"),
        "OvertimeSnapshotHours" => L.T("Giờ tăng ca chốt (New)"),
        "Committed" => L.T("Lương chốt trong kỳ"),
        "IncompleteShifts" => L.T("Ca chưa đủ giờ"),
        "Coverage" => L.T("Phạm vi nguồn"),
        "Value" => L.T("Nguyên giá"),
        "RelatedExpenses" => L.T("Chi liên quan"),
        "PeriodDepreciation" => L.T("KH kỳ (tham khảo)"),
        "Accumulated" => L.T("KH lũy kế (tham khảo)"),
        "PurchasedOn" => L.T("Ngày mua"),
        "OpenedAt" => L.T("Mốc mở sổ"),
        "Opening" => L.T("Tồn đầu kỳ"),
        "Purchases" => L.T("Nhập"),
        "Issues" => L.T("Xuất"),
        "Consumption" => L.T("Tiêu hao"),
        "Adjustment" => L.T("Chênh kiểm kê"),
        "Closing" => L.T("Tồn cuối kỳ"),
        "Current" => L.T("Tồn hiện tại"),
        "SourceDate" => L.T("Ngày nguồn"),
        _ => name,
    };
}
