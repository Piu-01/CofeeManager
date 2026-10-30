using CafeManager.Models.Errors;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

internal static class SqlErrorMapper
{
    private static readonly Dictionary<int, string> BusinessMessages = new()
    {
        [52201] = "Preview đã thay đổi; tải và xác nhận lại số mới.",
        [52202] = "Phân bổ chưa đủ/không đúng hoặc phương thức chưa được hỗ trợ.",
        [52203] = "Không đủ nguyên liệu. Đơn chưa chốt; kiểm tra tồn và sửa đơn.",
        [52204] = "Đơn có món ngừng bán hoặc thiếu công thức.",
        [52205] = "Tiền/điểm/tiêu hao vượt giới hạn hoặc ngày thanh toán không hợp lệ.",
        [52207] = "Snapshot/dữ liệu nâng cấp checkout không hợp lệ.",
        [52501] = "Đơn giao không có hoặc đã hủy.",
        [52502] = "Chỉ chuyển bước giao kế tiếp; không nhảy/lùi trạng thái.",
        [52503] = "Thông tin người nhận chưa hợp lệ.",
        [52101] = "Thông tin đơn/số lượng vượt giới hạn hoặc không phù hợp loại đơn.",
        [52102] = "Đơn đã chốt/hủy hoặc không cho phép sửa thông tin hiện tại.",
        [52103] = "Đơn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục.",
        [52104] = "Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.",
        [52105] = "Món/công thức/bàn/khách/chương trình không còn hợp lệ; tải lại và sửa lựa chọn.",
        [52106] = "Đang xử lý ở phiên khác; đối chiếu kết quả trước khi thử lại.",
        [52107] = "Dữ liệu hoặc phiên bản schema chưa phù hợp để nâng cấp bán hàng.",
        [51001] = "Không được sửa món của hóa đơn đã chốt hoặc hủy.",
        [51002] = "Không được sửa thanh toán của hóa đơn đã chốt hoặc hủy.",
        [51003] = "Phân bổ thanh toán vượt số phải trả.",
        [51004] = "Không được sửa hoặc xóa hóa đơn đã hủy.",
        [51005] = "Hóa đơn cần có món, đủ thanh toán và tiêu hao trước khi chốt.",
        [51006] = "Chỉ hủy đơn chưa thu tiền và chưa trừ nguyên liệu.",
        [51007] = "Tổng hóa đơn không khớp chi tiết món.",
        [51008] = "Thay đổi làm số đã thu vượt số phải trả.",
        [51009] = "Không được sửa hoặc xóa lịch sử tiêu hao.",
        [51010] = "Chỉ ghi tiêu hao khi thanh toán đơn đang mở.",
        [51011] = "Không được sửa hoặc xóa lịch sử điểm.",
        [51012] = "Hóa đơn tích điểm cần đã thanh toán và đúng khách hàng.",
        [51013] = "Không được sửa công nợ đã có thanh toán.",
        [51014] = "Công nợ vượt giá trị phiếu nhập.",
        [51015] = "Tổng phiếu nhập không khớp chi tiết.",
        [51016] = "Không được giảm phiếu nhập dưới công nợ.",
        [51017] = "Không được sửa hoặc xóa lịch sử trả nợ.",
        [51018] = "Trả nợ cần liên kết phiếu chi.",
        [51019] = "Trả nợ vượt số nợ gốc.",
        [51020] = "Không được sửa hoặc xóa phiếu chi đã liên kết trả nợ.",
        [51021] = "Không được đổi loại danh mục của phiếu trả nợ.",
        [51022] = "Hóa đơn đã trả chỉ được chuyển trạng thái giao hàng.",
        [51101] = "Hóa đơn không tồn tại hoặc đã chốt/hủy.",
        [51102] = "Hóa đơn phải có món.", [51103] = "Đơn đã có dòng thanh toán; cần đối chiếu trước khi gửi.",
        [51104] = "Tổng phân bổ không bằng số phải trả.", [51105] = "Khuyến mãi không hiệu lực tại ngày lập đơn.",
        [51106] = "Hóa đơn có món đã ngừng bán.", [51107] = "Món chưa có công thức.",
        [51108] = "Ngày thanh toán không được trước ngày lập.", [51109] = "Chỉ hủy hóa đơn đang mở.",
        [51110] = "Công nợ không tồn tại.", [51111] = "Số tiền trả nợ không hợp lệ.",
        [51112] = "Chỉ chốt phiếu kiểm kê hôm nay chưa điều chỉnh.", [51113] = "Phiếu kiểm kê chưa có chi tiết.",
        [51114] = "Tồn đã thay đổi sau khi đếm; cần lấy lại snapshot.", [51115] = "Không sửa chi tiết kiểm kê đã chốt.",
        [51301] = "Nguồn cấp mã chưa được hỗ trợ.", [51302] = "Đã hết giới hạn cấp mã; liên hệ quản trị.",
        [51401] = "Thiết lập quản trị đã hoàn tất hoặc không còn được phép.",
        [51402] = "Cần xác nhận admin hiện tại trước khi hoàn tất thiết lập.",
        [51403] = "Dữ liệu hoặc nhân viên được chọn không hợp lệ.",
        [51404] = "Tên đăng nhập không hợp lệ hoặc dữ liệu cũ cần xử lý xung đột.",
        [51405] = "Thông tin tài khoản đã thay đổi; tải lại hoặc đăng nhập lại.",
        [51406] = "Không đủ quyền thiết lập.", [51407] = "Cấu hình thiết lập xung đột; cần người cài kiểm tra.",
        [51501] = "Dữ liệu danh mục không hợp lệ; kiểm tra trường nhập.",
        [51502] = "Danh mục hệ thống hoặc có lịch sử được bảo vệ; không thể xóa/đổi loại.",
        [51503] = "Danh mục đã thay đổi hoặc bị xóa ở phiên khác; tải lại trước khi lưu.",
        [51504] = "Phương thức này chưa được hỗ trợ; PT010 không được phép dùng.",
        [51505] = "Tài khoản hoặc quyền đã thay đổi; đăng nhập lại.",
        [51506] = "Mã danh mục không hợp lệ hoặc đã tồn tại.",
        [51507] = "Dữ liệu danh mục cũ xung đột; cần người cài kiểm tra.",
        [51701] = "Thông tin sản phẩm/công thức không hợp lệ.",
        [51702] = "Món cần công thức trước bật bán; sản phẩm có lịch sử không thể xóa.",
        [51704] = "Công thức đang được sử dụng; đối chiếu trước khi thử lại.",
        [51707] = "Dữ liệu menu cũ xung đột; cần người cài kiểm tra.",
        [51801] = "Thông tin bàn/khu vực không hợp lệ.",
        [51807] = "Dữ liệu bàn/khu vực cũ xung đột; cần người cài kiểm tra.",
        [51901] = "Thông tin khách hoặc điểm mở sổ không hợp lệ.",
        [51902] = "Khách đã có mốc hoặc giao dịch; không thể mở sổ lại.",
        [51904] = "Chính sách đang thay đổi; đối chiếu trước khi thử lại.",
        [51907] = "Dữ liệu hạng/khách cũ xung đột; cần người cài kiểm tra.",
        [52001] = "Thông tin ưu đãi/chính sách/điểm không hợp lệ hoặc chương trình không hiệu lực.",
        [52002] = "Không đủ điểm hoặc số dư vượt giới hạn.",
        [52004] = "Chính sách đang được sử dụng; đối chiếu trước khi thử lại.",
        [52007] = "Dữ liệu ưu đãi cũ xung đột; cần người cài kiểm tra.",
        [51601] = "Dữ liệu nguyên liệu hoặc lượng không hợp lệ.",
        [51602] = "Đã có mốc mở sổ, công thức hoặc giao dịch; không thể thực hiện thao tác này.",
        [51603] = "Dữ liệu đã thay đổi hoặc bị xóa; tải lại trước khi lưu.",
        [51605] = "Bạn không có quyền thực hiện thao tác này.",
        [51607] = "Dữ liệu chuyển đổi xung đột; cần người cài kiểm tra.",
        [51608] = "Mã yêu cầu đã dùng với nội dung khác; cần đối chiếu.",
        [53001] = "Số tiền, ngày nguồn hoặc phương thức trả nợ không hợp lệ.",
        [53002] = "Nợ hoặc nguồn chi trả nợ không hợp lệ.",
        [53003] = "Số trả vượt còn nợ; tải lại số dư và sửa số tiền.",
        [53007] = "Lịch sử nợ cũ cần được đối chiếu trước nâng cấp.",
        [52901] = "Số đếm, lý do hoặc phạm vi kiểm kê không hợp lệ.",
        [52902] = "Chỉ chốt phiếu hôm nay chưa điều chỉnh; phiếu đã chốt chỉ đọc.",
        [52903] = "Phiếu hoặc kho đã thay đổi; tải lại và xác nhận đếm lại.",
        [52907] = "Dữ liệu kiểm kê cũ cần được đối chiếu.",
        [52801] = "Thông tin hoặc dòng xuất không hợp lệ.",
        [52802] = "Phiếu xuất đã chốt chỉ đọc.",
        [52803] = "Không đủ nguyên liệu; toàn phiếu chưa chốt.",
        [52807] = "Dữ liệu xuất cũ cần được đối chiếu trước nâng cấp.",
        [52701] = "Thông tin nhập hoặc dòng nguyên liệu không hợp lệ.",
        [52702] = "Chứng từ nhập và nợ đã chốt không được sửa/xóa.",
        [52703] = "Preview hoặc phần trả ngay thay đổi; kiểm tra và xác nhận lại.",
        [52705] = "Tổng nhập vượt giới hạn lưu trữ.",
        [52707] = "Dữ liệu nhập cũ cần được đối chiếu trước nâng cấp.",
        [52601] = "Ngày, số tiền nguyên VND hoặc thông tin phiếu không hợp lệ.",
        [52602] = "Nguồn hoặc trạng thái thu chi không hợp lệ; phiếu đã ghi chỉ đọc.",
        [52603] = "Yêu cầu đang xử lý; kiểm tra kết quả trước khi gửi lại.",
        [52607] = "Dữ liệu thu chi cũ xung đột; cần người cài kiểm tra.",
        [51508] = "Mã yêu cầu đã được dùng với dữ liệu khác; cần đối chiếu."
    };

    public static DataError Map(SqlException exception, bool writing)
    {
        // Select the actionable error rather than a secondary batch-aborted message.
        foreach (SqlError error in exception.Errors)
            if (BusinessMessages.ContainsKey(error.Number) || error.Number is 547 or 2601 or 2627 or 229 or -2)
                return MapNumber(error.Number, writing);
        return MapNumber(exception.Number, writing);
    }

    internal static DataError MapNumber(int number, bool writing)
    {
        if (BusinessMessages.TryGetValue(number, out var message))
            return new(DataErrorKind.BusinessRule, number, message);
        return number switch
        {
            547 => new(DataErrorKind.Constraint, number, "Dữ liệu không thỏa ràng buộc hoặc không đủ tồn; kiểm tra nguồn liên quan."),
            2601 or 2627 => new(DataErrorKind.Duplicate, number, "Mã hoặc dữ liệu duy nhất đã tồn tại; tải lại để đối chiếu."),
            1205 => new(DataErrorKind.BusinessRule, number, "Giao dịch gặp xung đột khóa và đã rollback; tải lại/đối chiếu trước thử lại."),
            229 => new(DataErrorKind.Permission, number, "Tài khoản kết nối không có quyền thao tác này."),
            -2 => new(DataErrorKind.Timeout, number, writing ? "Chưa xác định kết quả ghi; đối chiếu mã nguồn trước khi gửi lại." : "Truy vấn quá thời gian; thử tải lại.", writing),
            2 or 20 or 53 or 64 or 233 or 10060 or 10061 or 11001 or 18456 or 4060 =>
                new(DataErrorKind.Connection, number, "Không kết nối được dữ liệu; kiểm tra kết nối và thử lại.", writing),
            _ => new(DataErrorKind.Unknown, number, "Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục.", writing)
        };
    }
}








