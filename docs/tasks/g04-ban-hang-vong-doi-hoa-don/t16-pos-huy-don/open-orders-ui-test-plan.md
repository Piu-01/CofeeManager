# Test Đơn đang mở mới — đã duyệt

Người dùng đã duyệt test bằng “duyệt”; đã chạy component/SQL và đang chờ xác nhận
thao tác native/hình thức từ người dùng. Xem open-orders-ui-test-results.md. Giữ nguyên
56IDG04/325ID toàn dự án. Đóng băng source/DLL mới trước test, không sửa sản phẩm
trong lượt test. Bug mới phải ghi/duyệt đợt riêng trước debug.

| Ca bổ sung | Cách kiểm / expected |
|---|---|
| OO01 Mã đầy đủ |Font/DPI120 thật, đoHD99999999 và mã thực; cellcontentwidth saupadding đủtext, không bịcut; footer nguyên mã, Frozen đúng khi cuộn ngang |
| OO02 Tải/resize |Default,1280×720,1366×768,sizehẹp; đo sauShown/settled, rebind vàpage; code minimum/width giữ, toolbar/footer/bảng trong ancestor; cuộn đúng vùng |
| OO03 Nội dung |Ngàydd/MM/yyyy HH:mm,81.000 ₫/tiền lớn căn phải, kênh tiếng Việt/badge, kháchnull/dài; dữ liệu typed không đổi |
| OO04 Tìm/chọn/mở |Tìm thấy/không thấy, Entersearch, đổi dòng, doubleclick/Entergrid/Mởđơn trả đúngSelectedCode; Esc/đóngkhông chọn không sửa/hủyDB |
| OO05 Phân trang |0/1/>50kếtquả,50/1trang, total/page/nút đúng; pagecuối hếtđơn fallbackpage0; không mở dữ liệu cũ khiloading/lỗi |
| OO06 Lỗi/responsecũ |Giữinput và nhãnchưa cập nhật, không0giả, Mởđơnkhóa; nhiềuquery generation khôngghiđè; đóng lúcquery/Dispose antoàn |
| OO07 Shared regression |Inventory/Catalogs/Payment vàCheckout editable editor, binding/money không đổi vì thêmprofile; không thay luồng hỏiin/reset |

Ưu tiên component thực để kiểm metrics/bounds, dùng fixtureSQLtest riêng để
kiểmservice/query/phântrang và ảnhhưởngDB. Ghi rõ component/SQL/Usermanual,
không suyPASSnative từreflection hoặcgọiBLL. Công cụ modal có thể cần người dùng
bấm phần chọn/mở; agent chủđộng phần engine/layout kỹthuật.

Agent chỉ kiểm chức năng/layout kỹthuật125%, không đổiScale100/150% vàkhông
chụpảnhđối chiếu. Hình thức SKIPPED theo phân công người dùng;100/150%NOT RUN.
Không tạo DBtestthứ4 khi3DBcònđanggiữ; ưu tiên fixture đãcó đúngmarker hoặcdọn
DBcủađợtnày khiđãhếtcần, không tự xóaDBG03/manualcũ ngoài phạmvi.

Lưu test-results cạnhplan rồireportnhóm, kể cảFAIL/PARTIAL/BLOCKED; khôngtick
T16-K01/K11 hoặcG04hoàntất chỉvìcácOOsubsetđạt.
