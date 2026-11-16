using CafeManager.Models.Errors;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

internal static class SqlErrorMapper
{
    private static readonly Dictionary<int, Func<string>> BusinessMessages = new()
    {
        [53401] = () => L.T("Thông tin tài sản hoặc bộ lọc không hợp lệ."),
        [53402] = () => L.T("Tài sản đã có chứng từ liên quan, không thể xóa."),
        [53403] = () => L.T("Tài sản đã thay đổi; tải lại trước khi lưu."),
        [53405] = () => L.T("Đã hết miền mã tài sản TS001–TS999; cần quản trị kiểm tra trước khi tạo thêm."),
        [53407] = () => L.T("Hồ sơ tài sản cũ cần đối soát trước nâng cấp."),
        [53501] = () => L.T("Loại báo cáo hoặc bộ lọc không hợp lệ."),
        [53502] = () => L.T("Nguồn báo cáo chưa đầy đủ để đối chiếu."),
        [53505] = () => L.T("Tổng tiền vượt miền số ứng dụng có thể hiển thị."),
        [53301] = () => L.T("Nhân viên, kỳ, giờ hoặc khoản duyệt lương không hợp lệ."),
        [53302] = () => L.T("Công chưa hoàn tất, bảng lương đã chốt/chi hoặc dữ liệu cũ cần đối soát."),
        [53303] = () => L.T("Preview lương đã thay đổi; tải và xác nhận lại."),
        [53305] = () => L.T("Lương vượt giới hạn hoặc khấu trừ vượt tổng."),
        [53307] = () => L.T("Dữ liệu lương cũ cần đối soát trước nâng cấp."),
        [53201] = () => L.T("Thông tin công hoặc batch không hợp lệ."),
        [53202] = () => L.T("Kỳ đã chốt lương, nhân viên đã nghỉ hoặc ca không tồn tại."),
        [53203] = () => L.T("Công bị chồng giờ/trùng key hoặc đã thay đổi; tải lại trước khi lưu."),
        [53207] = () => L.T("Công cũ cần đối soát trước nâng cấp."),
        [53101] = () => L.T("Thông tin nhân viên/tài khoản không hợp lệ."),
        [53102] = () => L.T("Hồ sơ có lịch sử, nhân viên đã nghỉ hoặc cần giữ ADMIN hoạt động cuối."),
        [53103] = () => L.T("Hồ sơ/tài khoản đã thay đổi; tải lại trước khi lưu."),
        [53107] = () => L.T("Dữ liệu nhân sự cũ cần đối soát trước nâng cấp."),
        [52201] = () => L.T("Preview đã thay đổi; tải và xác nhận lại số mới."),
        [52202] = () => L.T("Phân bổ chưa đủ/không đúng hoặc phương thức chưa được hỗ trợ."),
        [52203] = () => L.T("Không đủ nguyên liệu. Đơn chưa chốt; kiểm tra tồn và sửa đơn."),
        [52204] = () => L.T("Đơn có món ngừng bán hoặc thiếu công thức."),
        [52205] = () => L.T("Tiền/điểm/tiêu hao vượt giới hạn hoặc ngày thanh toán không hợp lệ."),
        [52207] = () => L.T("Snapshot/dữ liệu nâng cấp checkout không hợp lệ."),
        [52501] = () => L.T("Đơn giao không có hoặc đã hủy."),
        [52502] = () => L.T("Chỉ chuyển bước giao kế tiếp; không nhảy/lùi trạng thái."),
        [52503] = () => L.T("Thông tin người nhận chưa hợp lệ."),
        [52101] = () => L.T("Thông tin đơn/số lượng vượt giới hạn hoặc không phù hợp loại đơn."),
        [52102] = () => L.T("Đơn đã chốt/hủy hoặc không cho phép sửa thông tin hiện tại."),
        [52103] = () => L.T("Đơn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục."),
        [52104] = () => L.T("Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món."),
        [52105] = () => L.T("Món/công thức/bàn/khách/chương trình không còn hợp lệ; tải lại và sửa lựa chọn."),
        [52106] = () => L.T("Đang xử lý ở phiên khác; đối chiếu kết quả trước khi thử lại."),
        [52107] = () => L.T("Dữ liệu hoặc phiên bản schema chưa phù hợp để nâng cấp bán hàng."),
        [52108] = () => L.T("Đơn cần có món trước khi yêu cầu thanh toán."),
        [52109] = () => L.T("Chỉ mở lại được đơn đang chờ thanh toán."),
        [52110] = () => L.T("Bàn còn đơn chưa chốt; thu hoặc hủy trước khi trả bàn."),
        [52111] = () => L.T("Phiên bàn đã đóng hoặc không còn mở."),
        [51001] = () => L.T("Không được sửa món của hóa đơn đã chốt hoặc hủy."),
        [51002] = () => L.T("Không được sửa thanh toán của hóa đơn đã chốt hoặc hủy."),
        [51003] = () => L.T("Phân bổ thanh toán vượt số phải trả."),
        [51004] = () => L.T("Không được sửa hoặc xóa hóa đơn đã hủy."),
        [51005] = () => L.T("Hóa đơn cần có món, đủ thanh toán và tiêu hao trước khi chốt."),
        [51006] = () => L.T("Chỉ hủy đơn chưa thu tiền và chưa trừ nguyên liệu."),
        [51007] = () => L.T("Tổng hóa đơn không khớp chi tiết món."),
        [51008] = () => L.T("Thay đổi làm số đã thu vượt số phải trả."),
        [51009] = () => L.T("Không được sửa hoặc xóa lịch sử tiêu hao."),
        [51010] = () => L.T("Chỉ ghi tiêu hao khi thanh toán đơn đang mở."),
        [51011] = () => L.T("Không được sửa hoặc xóa lịch sử điểm."),
        [51012] = () => L.T("Hóa đơn tích điểm cần đã thanh toán và đúng khách hàng."),
        [51013] = () => L.T("Không được sửa công nợ đã có thanh toán."),
        [51014] = () => L.T("Công nợ vượt giá trị phiếu nhập."),
        [51015] = () => L.T("Tổng phiếu nhập không khớp chi tiết."),
        [51016] = () => L.T("Không được giảm phiếu nhập dưới công nợ."),
        [51017] = () => L.T("Không được sửa hoặc xóa lịch sử trả nợ."),
        [51018] = () => L.T("Trả nợ cần liên kết phiếu chi."),
        [51019] = () => L.T("Trả nợ vượt số nợ gốc."),
        [51020] = () => L.T("Không được sửa hoặc xóa phiếu chi đã liên kết trả nợ."),
        [51021] = () => L.T("Không được đổi loại danh mục của phiếu trả nợ."),
        [51022] = () => L.T("Hóa đơn đã trả chỉ được chuyển trạng thái giao hàng."),
        [51101] = () => L.T("Hóa đơn không tồn tại hoặc đã chốt/hủy."),
        [51102] = () => L.T("Hóa đơn phải có món."), [51103] = () => L.T("Đơn đã có dòng thanh toán; cần đối chiếu trước khi gửi."),
        [51104] = () => L.T("Tổng phân bổ không bằng số phải trả."), [51105] = () => L.T("Khuyến mãi không hiệu lực tại ngày lập đơn."),
        [51106] = () => L.T("Hóa đơn có món đã ngừng bán."), [51107] = () => L.T("Món chưa có công thức."),
        [51108] = () => L.T("Ngày thanh toán không được trước ngày lập."), [51109] = () => L.T("Chỉ hủy hóa đơn đang mở."),
        [51110] = () => L.T("Công nợ không tồn tại."), [51111] = () => L.T("Số tiền trả nợ không hợp lệ."),
        [51112] = () => L.T("Chỉ chốt phiếu kiểm kê hôm nay chưa điều chỉnh."), [51113] = () => L.T("Phiếu kiểm kê chưa có chi tiết."),
        [51114] = () => L.T("Tồn đã thay đổi sau khi đếm; cần lấy lại snapshot."), [51115] = () => L.T("Không sửa chi tiết kiểm kê đã chốt."),
        [51301] = () => L.T("Nguồn cấp mã chưa được hỗ trợ."), [51302] = () => L.T("Đã hết giới hạn cấp mã; liên hệ quản trị."),
        [51401] = () => L.T("Thiết lập quản trị đã hoàn tất hoặc không còn được phép."),
        [51402] = () => L.T("Cần xác nhận admin hiện tại trước khi hoàn tất thiết lập."),
        [51403] = () => L.T("Dữ liệu hoặc nhân viên được chọn không hợp lệ."),
        [51404] = () => L.T("Tên đăng nhập không hợp lệ hoặc dữ liệu cũ cần xử lý xung đột."),
        [51405] = () => L.T("Thông tin tài khoản đã thay đổi; tải lại hoặc đăng nhập lại."),
        [51406] = () => L.T("Không đủ quyền thiết lập."), [51407] = () => L.T("Cấu hình thiết lập xung đột; cần người cài kiểm tra."),
        [51501] = () => L.T("Dữ liệu danh mục không hợp lệ; kiểm tra trường nhập."),
        [51502] = () => L.T("Danh mục hệ thống hoặc có lịch sử được bảo vệ; không thể xóa/đổi loại."),
        [51503] = () => L.T("Danh mục đã thay đổi hoặc bị xóa ở phiên khác; tải lại trước khi lưu."),
        [51504] = () => L.T("Phương thức này chưa được hỗ trợ; PT010 không được phép dùng."),
        [51505] = () => L.T("Tài khoản hoặc quyền đã thay đổi; đăng nhập lại."),
        [51506] = () => L.T("Mã danh mục không hợp lệ hoặc đã tồn tại."),
        [51507] = () => L.T("Dữ liệu danh mục cũ xung đột; cần người cài kiểm tra."),
        [51701] = () => L.T("Thông tin sản phẩm/công thức không hợp lệ."),
        [51702] = () => L.T("Món cần công thức trước bật bán; sản phẩm có lịch sử không thể xóa."),
        [51704] = () => L.T("Công thức đang được sử dụng; đối chiếu trước khi thử lại."),
        [51707] = () => L.T("Dữ liệu menu cũ xung đột; cần người cài kiểm tra."),
        [51801] = () => L.T("Thông tin bàn/khu vực không hợp lệ."),
        [51807] = () => L.T("Dữ liệu bàn/khu vực cũ xung đột; cần người cài kiểm tra."),
        [51901] = () => L.T("Thông tin khách hoặc điểm mở sổ không hợp lệ."),
        [51902] = () => L.T("Khách đã có mốc hoặc giao dịch; không thể mở sổ lại."),
        [51904] = () => L.T("Chính sách đang thay đổi; đối chiếu trước khi thử lại."),
        [51907] = () => L.T("Dữ liệu hạng/khách cũ xung đột; cần người cài kiểm tra."),
        [52001] = () => L.T("Thông tin ưu đãi/chính sách/điểm không hợp lệ hoặc chương trình không hiệu lực."),
        [52002] = () => L.T("Không đủ điểm hoặc số dư vượt giới hạn."),
        [52004] = () => L.T("Chính sách đang được sử dụng; đối chiếu trước khi thử lại."),
        [52007] = () => L.T("Dữ liệu ưu đãi cũ xung đột; cần người cài kiểm tra."),
        [51601] = () => L.T("Dữ liệu nguyên liệu hoặc lượng không hợp lệ."),
        [51602] = () => L.T("Đã có mốc mở sổ, công thức hoặc giao dịch; không thể thực hiện thao tác này."),
        [51603] = () => L.T("Dữ liệu đã thay đổi hoặc bị xóa; tải lại trước khi lưu."),
        [51605] = () => L.T("Bạn không có quyền thực hiện thao tác này."),
        [51607] = () => L.T("Dữ liệu chuyển đổi xung đột; cần người cài kiểm tra."),
        [51608] = () => L.T("Mã yêu cầu đã dùng với nội dung khác; cần đối chiếu."),
        [53001] = () => L.T("Số tiền, ngày nguồn hoặc phương thức trả nợ không hợp lệ."),
        [53002] = () => L.T("Nợ hoặc nguồn chi trả nợ không hợp lệ."),
        [53003] = () => L.T("Số trả vượt còn nợ; tải lại số dư và sửa số tiền."),
        [53007] = () => L.T("Lịch sử nợ cũ cần được đối chiếu trước nâng cấp."),
        [52901] = () => L.T("Số đếm, lý do hoặc phạm vi kiểm kê không hợp lệ."),
        [52902] = () => L.T("Chỉ chốt phiếu hôm nay chưa điều chỉnh; phiếu đã chốt chỉ đọc."),
        [52903] = () => L.T("Phiếu hoặc kho đã thay đổi; tải lại và xác nhận đếm lại."),
        [52907] = () => L.T("Dữ liệu kiểm kê cũ cần được đối chiếu."),
        [52801] = () => L.T("Thông tin hoặc dòng xuất không hợp lệ."),
        [52802] = () => L.T("Phiếu xuất đã chốt chỉ đọc."),
        [52803] = () => L.T("Không đủ nguyên liệu; toàn phiếu chưa chốt."),
        [52807] = () => L.T("Dữ liệu xuất cũ cần được đối chiếu trước nâng cấp."),
        [52701] = () => L.T("Thông tin nhập hoặc dòng nguyên liệu không hợp lệ."),
        [52702] = () => L.T("Chứng từ nhập và nợ đã chốt không được sửa/xóa."),
        [52703] = () => L.T("Preview hoặc phần trả ngay thay đổi; kiểm tra và xác nhận lại."),
        [52705] = () => L.T("Tổng nhập vượt giới hạn lưu trữ."),
        [52707] = () => L.T("Dữ liệu nhập cũ cần được đối chiếu trước nâng cấp."),
        [52601] = () => L.T("Ngày, số tiền nguyên VND hoặc thông tin phiếu không hợp lệ."),
        [52602] = () => L.T("Nguồn hoặc trạng thái thu chi không hợp lệ; phiếu đã ghi chỉ đọc."),
        [52603] = () => L.T("Yêu cầu đang xử lý; kiểm tra kết quả trước khi gửi lại."),
        [52607] = () => L.T("Dữ liệu thu chi cũ xung đột; cần người cài kiểm tra."),
        [51508] = () => L.T("Mã yêu cầu đã được dùng với dữ liệu khác; cần đối chiếu.")
    };

    public static DataError Map(SqlException exception, bool writing)
    {
        // Select the actionable error rather than a secondary batch-aborted message.
        foreach (SqlError error in exception.Errors)
            if (BusinessMessages.ContainsKey(error.Number) || error.Number is 547 or 2601 or 2627 or 229 or -2 or 208 or 2812 or 2715)
                return MapNumber(error.Number, writing);
        return MapNumber(exception.Number, writing);
    }

    internal static DataError MapNumber(int number, bool writing)
    {
        if (BusinessMessages.TryGetValue(number, out var message))
            return new(DataErrorKind.BusinessRule, number, message());
        return number switch
        {
            547 => new(DataErrorKind.Constraint, number, L.T("Dữ liệu không thỏa ràng buộc hoặc không đủ tồn; kiểm tra nguồn liên quan.")),
            2601 or 2627 => new(DataErrorKind.Duplicate, number, L.T("Mã hoặc dữ liệu duy nhất đã tồn tại; tải lại để đối chiếu.")),
            1205 => new(DataErrorKind.BusinessRule, number, L.T("Giao dịch gặp xung đột khóa và đã rollback; tải lại/đối chiếu trước thử lại.")),
            229 => new(DataErrorKind.Permission, number, L.T("Tài khoản kết nối không có quyền thao tác này.")),
            // Thiếu đối tượng do DB chưa áp đủ migration: lời gọi thiếu procedure/kiểu hỏng trước khi
            // thực thi nên không còn kết quả ghi chưa xác định. 208 có thể bật ra giữa procedure đã ghi
            // dở (deferred name resolution) nên giữ đối chiếu khi ghi.
            2812 or 2715 => new(DataErrorKind.Schema, number, L.T("CSDL chưa áp migration cần thiết nên thiếu procedure hoặc kiểu dữ liệu; liên hệ người triển khai rồi thử lại.")),
            208 => new(DataErrorKind.Schema, number, L.T("CSDL chưa áp migration cần thiết nên thiếu bảng hoặc đối tượng; liên hệ người triển khai rồi thử lại."), writing),
            -2 => new(DataErrorKind.Timeout, number, writing ? L.T("Chưa xác định kết quả ghi; đối chiếu mã nguồn trước khi gửi lại.") : L.T("Truy vấn quá thời gian; thử tải lại."), writing),
            -2146893019 => new(DataErrorKind.Connection, number, L.T("Không xác thực được chứng chỉ SQL Server; kiểm tra chứng chỉ và tên máy chủ."), writing),
            2 or 20 or 53 or 64 or 233 or 258 or 10060 or 10061 or 11001 or 18456 or 4060 =>
                new(DataErrorKind.Connection, number, L.T("Không kết nối được dữ liệu; kiểm tra kết nối và thử lại."), writing),
            _ => new(DataErrorKind.Unknown, number, L.T("Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục."), writing)
        };
    }
}
