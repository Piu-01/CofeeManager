namespace CafeManager.Models.States;

// Chỉ dịch metadata do SQL của app tạo; tên và ghi chú nghiệp vụ không đi qua hàm này.
public static class ReportLabels
{
    public static string Text(string value) => value switch
    {
        "Ngày thanh toán hóa đơn" => L.T("Ngày thanh toán hóa đơn"),
        "Tiền món theo giá snapshot, trước giảm toàn bill; tên món là tên hiện tại." => L.T("Tiền món theo giá snapshot, trước giảm toàn bill; tên món là tên hiện tại."),
        "Công nợ tại cuối ngày đến; ngày nguồn nhập và ngày phiếu chi trả nợ" => L.T("Công nợ tại cuối ngày đến; ngày nguồn nhập và ngày phiếu chi trả nợ"),
        "Thu bán: ngày thanh toán; thu/chi khác: ngày phiếu" => L.T("Thu bán: ngày thanh toán; thu/chi khác: ngày phiếu"),
        "Dòng tiền quản trị, đối tác chưa đối soát; không phải số dư ngân hàng hoặc lợi nhuận chính xác. Dữ liệu LegacyUnknown chưa đủ liên kết nguồn." => L.T("Dòng tiền quản trị, đối tác chưa đối soát; không phải số dư ngân hàng hoặc lợi nhuận chính xác. Dữ liệu LegacyUnknown chưa đủ liên kết nguồn."),
        "Giờ: NgayLam; lương chốt: NgayChot; thực chi: ngày phiếu" => L.T("Giờ: NgayLam; lương chốt: NgayChot; thực chi: ngày phiếu"),
        "Kỳ công, kỳ bảng lương và thời điểm chốt/chi là các nguồn độc lập. Giờ tăng ca theo snapshot New; LegacyUnknown không suy từ hồ sơ hiện tại." => L.T("Kỳ công, kỳ bảng lương và thời điểm chốt/chi là các nguồn độc lập. Giờ tăng ca theo snapshot New; LegacyUnknown không suy từ hồ sơ hiện tại."),
        "Có bảng lương LegacyUnknown; giờ snapshot chỉ gồm New" => L.T("Có bảng lương LegacyUnknown; giờ snapshot chỉ gồm New"),
        "Giờ snapshot chỉ gồm New" => L.T("Giờ snapshot chỉ gồm New"),
        "Tháng mua và kỳ khấu hao; chi theo ngày phiếu" => L.T("Tháng mua và kỳ khấu hao; chi theo ngày phiếu"),
        "Khấu hao THAM KHẢO theo hồ sơ hiện tại, không tự ghi phiếu chi hoặc tính như giá vốn/dòng tiền." => L.T("Khấu hao THAM KHẢO theo hồ sơ hiện tại, không tự ghi phiếu chi hoặc tính như giá vốn/dòng tiền."),
        "Thời điểm ghi kho sau mốc mở sổ; ngày nguồn chứng từ hiển thị riêng" => L.T("Thời điểm ghi kho sau mốc mở sổ; ngày nguồn chứng từ hiển thị riêng"),
        "Không dựng tồn trước mở sổ. Closing tại ngày đến khác Current đọc hiện tại; không cộng lại lịch sử legacy đã nằm trong mốc." => L.T("Không dựng tồn trước mở sổ. Closing tại ngày đến khác Current đọc hiện tại; không cộng lại lịch sử legacy đã nằm trong mốc."),
        "Incomplete: chưa mở sổ" => L.T("Incomplete: chưa mở sổ"),
        "Incomplete: kỳ bắt đầu trước mốc mở sổ" => L.T("Incomplete: kỳ bắt đầu trước mốc mở sổ"),
        "Incomplete: thiếu thời điểm ghi kho" => L.T("Incomplete: thiếu thời điểm ghi kho"),
        "Incomplete: có timestamp fallback" => L.T("Incomplete: có timestamp fallback"),
        "Incomplete: nguồn sau mốc chưa khớp tồn hiện tại" => L.T("Incomplete: nguồn sau mốc chưa khớp tồn hiện tại"),
        "Complete" => L.T("Đủ nguồn từ mốc mở sổ"),
        _ => value
    };
}
