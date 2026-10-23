# Đợt kiểm tra cửa sổ con Orbit

22/10/2026 — người dùng đã duyệt: “test đi, giao điện thì mình tự nhìn không cần test”. Áp dụng phạm vi **chức năng**; bỏ kiểm hình thức, ảnh, resize/DPI/Designer/hover theo yêu cầu, không coi phần bỏ qua là PASS. Các yêu cầu chụp ảnh/DPI bên dưới là kế hoạch gốc, được miễn trong lượt này. Source đóng băng ở runs/source-implementation.csv; nếu phát hiện FAIL giữ source, ghi bug, không vá trong lượt test. Đây là đợt độc lập, không cấp PASS cho toàn bộ G03 hoặc T01–T32.

## Môi trường và dữ liệu

- Bản Debug: `runs/build/Debug/CafeManager.WinForms.exe`; Release ở thư mục tương ứng. Build cả hai đã sạch. Không dùng exe cũ đang mở để kiểm source mới.
- WinForms .NET10, Windows; ảnh app thật, ghi DPI thực và kích thước client. Các ảnh fixture/control probe ghi riêng, không thay thế kiểm native.
- Đọc/ghi chỉ DB test có marker; kiểm tra manifest/marker trước khởi chạy, không dùng DB development đang sử dụng. Có thể dùng DB test riêng đã có nếu marker hợp lệ; không chạy lại seed.
- Tài khoản ADMIN đăng nhập thủ công; CASH/STORE kiểm readonly/quyền bằng fixture cấu trúc và manual nếu có phiên hợp lệ. Không gửi mật khẩu hoặc tự thao tác xác thực.
- Fixture tạm có tên theo runid, mã hợp lệ chọn sau khi xác minh chưa tồn tại. Không sửa dữ liệu demo có lịch sử/điểm/tồn; không thanh toán hoặc sửa số dư hệ thống để tạo tình huống lỗi.

## Ma trận

| ID | Thao tác cụ thể | Expected |
|---|---|---|
| D01a | Mở Thêm/Sửa khách hàng, khu vực và nguyên liệu, rồi Hủy | Nhãn/input/footer cùng theme; chiều cao theo nội dung; không ghi DB khi Hủy |
| D01b | Mở editor sản phẩm, bàn và khuyến mãi | Combo chọn đúng giá trị đã tải; input typed không mất; không mất nút |
| D02a | Mở thêm/sửa đủ7CatalogKind | Trường ẩn không chiếm chỗ; mã readonly/tự sinh, PT010 và checkbox theo đúng trạng thái cũ |
| D02b | Ca làm/chức vụ/khuyến mãi ở chiều cao thấp, mở lịch | Giờ/ngày nhập đúng, popup không bị che; body cuộn, footer ở ngoài |
| D03a | Công thức: chọn nguyên liệu, nhập lượng, xóa dòng nháp rồi Hủy | Đơn vị cập nhật; input dễ nhận biết; xóa nháp không xóa nguyên liệu/không ghi trước Lưu |
| D03b | Công thức với STORE; chính sách hạng với ADMIN, thay nháp rồi preview/Hủy | Readonly đúng role, grid không kéo giãn từng dòng; preview còn bắt buộc trước ghi |
| D04a | Hóa đơn khách có dữ liệu và rỗng; thử cuộn/phân trang | Cột/ngày/trạng thái đúng, khung gọn không heading lặp, paging50 giữ nguyên |
| D04b | Picker Points tìm theo tên/SDT, chọn KH001, mở lại rồi Đóng/Escape | Không quản lý/vãng lai; đúng KH001/điểm/hạng/lịch sử; hủy giữ lựa chọn |
| D04c | Picker trong Preview ưu đãi, chọn khách/vãng lai rồi Đóng | Có vãng lai, không action quản lý; trả kết quả hoặc hủy đúng context |
| D05a | Bỏ trống trường bắt buộc, số âm/quá precision/ngày sai; bấm Lưu | ErrorProvider đúng trường, focus đúng input trong wrapper, giữ nháp, không ghi |
| D05b | Preview với tên/lý do dài, tiền lớn; thay input sau preview | Wrap không cắt/chồng; kết quả cũ bị vô hiệu theo code hiện có |
| D06a | Xác nhận YesNo/OKCancel bằng nút từ chối, Escape, X và Enter mặc định | Mapping đúng No/Cancel; Enter mặc định không thực hiện hành động ghi |
| D06b | Nhập nháp rồi rời/hủy editor; trạng thái writing/unknown qua fixture | Hỏi bỏ nháp đúng cũ; đang ghi/unknown chặn đóng, giữ Đối chiếu; không gửi lặp |
| D07a | Từng nhóm editor ngắn/dài/grid/picker/confirm tại100/125/150%, client1280×720 và1366×768 | Header/status/footer không chồng/cắt; nativecontrol rõ, không autoscale kép; giới hạn WorkingArea |
| D07b | Hover, nhấn/thả ngoài, Tab/ShiftTab/Space/Escape, resize liên tục | Visual reset, focus đúng, không action kép, không stardust |
| D08 | Quay các màn chính, mở/đóng dialog nhiều lần, kiểm thứ tự nút và quyền | Không đổi trang chính/cột/logic; không nhãn đếm hoặc ghi chú tĩnh bị đưa lại; picker không có quản lý |
| D09a | Ghi hợp lệ bản ghi fixture bằng editor đại diện cho Catalog, Material, Product, Table/Area, Customer, Promotion; đọc lại qua service | Payload đúng; lưu một lần/receipt đúng, giá trị/trạng thái không đổi ngoài request; không ghi DB thật |
| D09b | Hủy mở sổ/điều chỉnh/chính sách ở bước xác nhận; đối chiếu snapshot trước/sau | Tồn/điểm/chính sách không đổi khi hủy; không tự retry/ghi do Enter |

Các thao tác ghi cần fixture được xác định trong biên bản trước chạy; mã trường và expected theo BLL/contract hiện có. Không mở rộng sửa SQL hoặc tạo procedure giả. Nhóm lỗi/unknown có thể cần fixture trong process; ghi rõ giới hạn khác app thật. Không gọi kiểm quyền backend PASS từ việc ẩn nút.

## Bằng chứng và thứ tự

1. Xác minh hash source/exe và marker DB; ghi viewport/DPI. Mở bản mới, một lần đăng nhập admin cho nhiều ca cùng nhóm.
2. Kiểm form ngắn/dài, danh mục và bảng con, picker/preview, xác nhận; thu kết quả trước đổiDPI. Không tự chốt giao dịch chỉ để chụp ảnh.
3. Kiểm100/125/150% theo điều kiện thực; ghi trạng thái WindowsScale cuối. Nếu cần người dùng nhập vào modal do công cụ không route phím, gom hướng dẫn thành một lần; ca chưa làm giữ BLOCKED/NOT RUN.
4. Lưu screenshot thật cho các nhóm trên cùng tree/trạng thái; SQL/service snapshot không chứa credentials. Tổng hợp PASS/FAIL/BLOCKED/NOT RUN trong test-results/report, ghi bug theo task sở hữu.

Đã chạy đợt chức năng; xem test-results.md và bug-report.md. Không thay expected để bỏ qua lỗi; lỗi SP005 và roundingfooter125% của đợt cũ giữ hồ sơ riêng, không tự vá. Harness in-process chạy control WinForms thật với message loop và handler hiện có; đây không phải nghiệm thu thao tác người dùng trên app đăng nhập đầy đủ.
