# Đề xuất thiết kế Tổng quan hoạt động quán

09/11/2026. **Người dùng đã duyệt thiết kế và ràng buộc mô hình hiện có bằng “duyệt”.** [Plan kỹ thuật](dashboard-analytics-plan.md) đã được duyệt và triển khai source; build Debug/Release đạt, [chờ duyệt đợt test riêng](dashboard-analytics-test-plan.md). Chưa áp028 development. Yêu cầu mới thay hướng lấy “Truy cập nhanh” làm nội dung chính. UI02 là lịch sử sửa bố cục tối thiểu, không phải nghiệm thu thiết kế phân tích này. Đây là phần mở rộng T28;giữ nguyên T01–T32 và325ID.

## Ràng buộc bắt buộc người dùng bổ sung

Thiết kế và triển khai **chỉ dựa trên các thực thể, thuộc tính đang có**:

- Không thêm bảng, cột, thuộc tính lưu trữ, computed column, bảng snapshot/thống kê hoặc entity nghiệp vụ mới. Không đổi định nghĩa/thêm property vào các entity hiện có để phục vụ dashboard.
- Số liệu, %, bucket giờ/ngày và chuỗi biểu đồ là **kết quả truy vấn/tính toán từ dữ liệu nguồn**, không phải thuộc tính mới để ghi vào DB. Chỉ đọc dữ liệu; không ghi lại các số thống kê vào thực thể nguồn.
- Ưu tiên tái sử dụng DTO/model báo cáo hiện có. Đề xuất này không cấp quyền tự thêm DTO/model/property mới. Nếu kế hoạch kỹ thuật thấy thiếu cấu trúc truyền dữ liệu, phải nêu rõ trước để người dùng quyết định; không tự tạo rồi coi là ngoài ràng buộc.
- Chỉ tiêu không chứng minh được nguồn thì loại hoặc ghi rõ chưa đủ cơ sở. Không thêm trường mục tiêu doanh số, giá vốn, ngày tạo khách, mức tồn tối thiểu hoặc hạn trả nợ để làm dashboard phong phú hơn.
- Có thể cần điều chỉnh truy vấn aggregate/luồng đọc để lấy kết quả mong muốn. Mọi thay đổi procedure/quyền đọc phải nêu cụ thể ở plan; không mặc định các procedure hiện tại đã hỗ trợ sẵn, cũng không được dùng đó làm lý do thêm thực thể/thuộc tính.

### Truy vết từng chỉ tiêu về thuộc tính đang có

| Nội dung thiết kế | Thực thể/view và thuộc tính nguồn hiện có | Phép suy ra khi đọc |
|---|---|---|
| Doanh thu đã thanh toán | HOADON:MaHD,TrangThai,NgayThanhToan,ThanhTien | SUM ThanhTien của đơn đã trả trong kỳ; ThanhTien chỉ đọc, không sửa computed column |
| Số hóa đơn đã trả | HOADON:MaHD,TrangThai,NgayThanhToan | COUNT theo header hóa đơn, không nhân bởi dòng món/thanh toán |
| Bình quân/hóa đơn | Cùng tập HOADON ở hai dòng trên | Tổng tiền chia số hóa đơn khi mẫu khác0 |
| Thực chi | PHIEUTHUCHI:MaTC,NgayLap,SoTien,MaDMTC → DANHMUC_THUCHI:MaDMTC,Loai | SUM SoTien của phiếu loại Chi trong kỳ, mỗi MaTC một lần |
| % tăng giảm | Chính các nguồn KPI trên ở hai khoảng thời gian | So sánh hai kết quả aggregate; không thêm trường TangTruong/DoanhThuThang |
| Xu hướng theo giờ/ngày | HOADON:NgayThanhToan,ThanhTien,TrangThai | Nhóm theo giờ/ngày lấy từ NgayThanhToan; không thêm cột giờ/tháng |
| Donut kênh bán | HOADON:LoaiDon,ThanhTien,TrangThai,NgayThanhToan | Nhóm LoaiDon, chia tổng từng kênh cho tổng cùng kỳ |
| Món bán chạy/tổng số ly | CHITIET_HD:MaHD,MaSP,SoLuong → HOADON:MaHD,TrangThai,NgayThanhToan; SANPHAM:MaSP,TenSP | SUM SoLuong theo MaSP trên đơn đã trả; sắp Top5 theo lượng |
| Đơn đang mở | HOADON:MaHD,TrangThai | COUNT đơn Chua thanh toan hiện tại |
| Giao chưa hoàn tất | HOADON:MaHD,LoaiDon,TrangThai,TrangThaiGiao | Đếm đơn giao đã trả còn Cho giao/Dang giao |
| Nguyên liệu hết tồn | NGUYENVATLIEU:MaNVL,SoLuongTon | Đếm nguyên liệu có SoLuongTon=0; không tự đặt mức “sắp hết” |
| Công nợ hiện tại | vw_CongNoNCC:ConNo, từ nguồn công nợ/nhập/trả nợ đã có | Tổng ConNo dương hiện tại; không tạo thuộc tính TrangThai/HanTra mới |

Đã đối chiếu baseline DB_Schema.sql, migrations004/026 và model/report hiện có; đây là truy vết thiết kế, không phải kết quả test engine mới hoặc xác nhận đã triển khai dashboard phân tích.

## 1. Mục tiêu: đăng nhập là nắm tình hình

Trang phải trả lời được bốn câu hỏi: hôm nay/tháng này đạt bao nhiêu; tăng hay giảm so với cùng kỳ; kết quả đến từ đâu; hiện có việc gì cần xử lý. Thông tin có biến động theo kỳ được ưu tiên hơn danh sách nút đi tới các tab.

Đề xuất trước mắt cho **ADMIN**, đúng màn trong ảnh và quyền báo cáo tài chính hiện có. CASH vẫn vào POS; STORE vẫn vào tổng quan kho. Không đưa doanh thu/chi phí tài chính cho role không được phép. Thiết kế dashboard riêng cho hai role đó là phạm vi khác nếu người dùng yêu cầu.

## 2. Bố cục ưu tiên

```text
TỔNG QUAN QUÁN                   [Hôm nay | Tháng này]   [Tải lại]
Khoảng thời gian · so với kỳ nào · cập nhật lúc ...

[Doanh thu]   [Hóa đơn đã trả]   [Bình quân/hóa đơn]   [Thực chi]
 giá trị       giá trị            giá trị               giá trị
 tăng/giảm%    tăng/giảm%         tăng/giảm%             tăng/giảm%

┌───────────────────────────────────┬────────────────────────────┐
│ XU HƯỚNG DOANH THU                │ CƠ CẤU THEO KÊNH          │
│ Đường kỳ này + đường kỳ trước     │ Donut: tại quán/mang đi/   │
│ Ngày: theo giờ · Tháng: theo ngày │ giao hàng, số tiền và %    │
└───────────────────────────────────┴────────────────────────────┘

┌───────────────────────────────────┬────────────────────────────┐
│ TOP5 MÓN BÁN CHẠY                 │ CẦN CHÚ Ý · HIỆN TẠI      │
│ Tên món · số ly · thanh ngang     │ Đơn mở / giao chưa xong /  │
│ Tổng số ly trong kỳ               │ nguyên liệu hết / nợ NCC   │
└───────────────────────────────────┴────────────────────────────┘
```

Màn hình đầu tập trung vào KPI và hai biểu đồ. Các phần bên dưới tiếp cận bằng cuộn dọc khi chiều cao không đủ. Bốn KPI cùng hàng khi đủ rộng; thu còn2 hoặc1 cột khi hẹp. Biểu đồ2cột chuyển thành1cột, giữ chữ/trục/legend đọc được. Không ép nội dung vào một màn bằng cách thu nhỏ font hoặc cắt nút.

Giữ màu/font/tokens Orbit. Caramel nhấn giá trị chính, lilac cho series đối chiếu, trạng thái có chữ/icon. Biểu đồ phẳng2D, chú thích ngắn, không trang trí3D/gauge hoặc thêm biểu đồ chỉ để lấp chỗ trống. Không có khối nút “Truy cập nhanh” lớn; lối mở chi tiết nằm ngay ở chỉ tiêu/biểu đồ liên quan.

## 3. Bộ lọc và thời gian

- **Hôm nay** là mặc định mỗi lần đăng nhập ADMIN. **Tháng này** tính từ ngày1 đến thời điểm đọc hiện tại.
- Ngày và giờ nghiệp vụ theo thời gian SQL Server như contract hiện có. Tất cả phần theo kỳ cùng dùng một mốc ReadAt và khoảng lọc, không để KPI dùng tháng trong khi biểu đồ vẫn là ngày.
- Tải khi vào màn, đổi kỳ hoặc bấm Tải lại. Giữ lựa chọn kỳ khi mở chi tiết rồi quay về; lấy lại ngày thực khi qua nửa đêm/sang tháng. Chưa đưa chọn kỳ tùy ý hoặc tự refresh theo timer vào phạm vi đầu tiên.
- Hiện rõ “09/11/2026 đến22:00 · so với08/11 đến22:00”, hoặc “01–09/11 đến22:00 · so với01–09/10 đến22:00”. Đây là ví dụ cách ghi, không phải dữ liệu phát sinh thật.
- Các mục **Cần chú ý · Hiện tại** là số dư/trạng thái lúc đọc, không phải số phát sinh trong kỳ. Nhãn này giữ rõ khi chuyển Hôm nay/Tháng này.

## 4. Bốn KPI chính

| KPI | Ý nghĩa/nguồn chính thức | So sánh |
|---|---|---|
| **Doanh thu đã thanh toán** | Tổng ThanhTien của hóa đơn đã trả, theo NgayThanhToan. Kế thừa định nghĩa báo cáo hiện tại: đã trừ giảm, gồm phí ship; phí đối tác tách riêng, chưa đối soát. Có chú thích khi xem chi tiết. | Kỳ này và cùng khoảng kỳ trước |
| **Hóa đơn đã thanh toán** | COUNT hóa đơn đã trả cùng tập ngày thanh toán. Không cộng đơn mở/hủy hoặc đếm theo số dòng món/phương thức thanh toán. | Như trên |
| **Bình quân/hóa đơn** | Doanh thu đã thanh toán chia số hóa đơn đã trả, tính ở nguồn aggregate. Số hóa đơn bằng0 thì hiện “—”, không suy bình quân bằng0. | Hai giá trị bình quân có mẫu hợp lệ |
| **Thực chi** | Tổng phiếu thuộc loại Chi theo ngày phiếu trong kỳ, gồm các nguồn chi đã ghi nhận; không lấy giá trị phiếu nhập/chốt lương/khấu hao cộng lại. Là tiền chi, không gọi là giá vốn hoặc lỗ. | Cùng khoảng kỳ trước, mô tả tăng/giảm trung tính |

Không đưa tổng nhân viên, tổng danh mục, tổng tài sản hoặc tổng khách đã có vào hàng KPI vì phần lớn là số tĩnh và ít giúp đánh giá hoạt động trong ngày. Chưa đưa “lợi nhuận” vì hệ thống chưa có giá vốn lịch sử đầy đủ. Không gọi Thực chi tăng là tự động tốt/xấu.

Tổng số ly nằm ở phần món bán chạy, không tạo thêm một hàng KPI dài. Công nợ là số dư hiện tại ở phần Cần chú ý, không trộn với doanh thu của kỳ.

## 5. Quy tắc % và kỳ so sánh

`(Kỳ này − Kỳ trước) / Kỳ trước ×100%`, chỉ khi mẫu số và độ phủ dữ liệu hợp lệ. Ví dụ minh họa1.000.000 so với800.000 là tăng25%; không phải số của DB development.

- Hôm nay đang chạy: so với hôm qua **đến cùng giờ/phút**, không với cả ngày hôm qua.
- Tháng này đang chạy: so từ ngày1 tháng trước đến **cùng ngày và giờ/phút**. Không với cả tháng trước.
- Nếu tháng trước thiếu ngày tương ứng (ví dụ31/03 so với tháng02), giữ tổng tháng này và ghi “Không có mốc so sánh tương ứng”, không tự âm thầm so31ngày với28ngày. Không trộn % từ khoảng30ngày vào một KPI tổng31ngày.
- Kỳ trước=0, kỳ này>0: “Mới phát sinh” kèm chênh lệch tiền/số lượng, không hiện tăng100% hoặc vô cực.
- Cả hai=0: ghi “Không thay đổi”; không cần huy hiệu tăng0% gây nhiễu.
- Kỳ này=0, kỳ trước>0: giảm100% nếu hai kỳ đầy đủ nguồn.
- Bình quân/hóa đơn thiếu mẫu ở một kỳ: “Chưa đủ cơ sở so sánh”. Dữ liệu không đầy đủ/lỗi không dùng làm mẫu số0.
- Hiện dấu/mũi tên và chữ, không chỉ màu; tỷ lệ làm tròn1chữ số thập phân, tooltip có giá trị kỳ trước/chênh lệch tuyệt đối/khoảng ngày. Thực chi dùng diễn giải trung tính.

Đếm dòng0 trong một kỳ đã truy vấn thành công khác với “không biết/thiếu dữ liệu”. Nguồn LegacyUnknown/coverage không đầy đủ phải có trạng thái riêng khi ảnh hưởng chỉ tiêu.

## 6. Biểu đồ xu hướng — phần lớn nhất

**Hôm nay:** doanh thu theo giờ; **Tháng này:** doanh thu theo ngày. Đường kỳ này rõ, đường kỳ trước nét đứt; tooltip có ngày/giờ thật và số tiền của cả hai series.

Đây là xu hướng doanh thu thực tế; % tăng trưởng nằm ở KPI để dễ đọc. Chưa thêm hai trục hoặc biểu đồ% riêng làm trang khó hiểu. Thời gian chưa tới không vẽ thành số0. Khoảng đã qua có dữ liệu đầy đủ nhưng không phát sinh thì có điểm0; dữ liệu chưa biết để khuyết, không nối xuyên như có giá trị.

Tổng các bucket của kỳ này khớp KPI doanh thu. Điểm so sánh của bucket đang chạy cũng cắt tại cùng mốc giờ/phút; không so bucket hôm nay chưa đủ một giờ/ngày với bucket kỳ trước đã trọn.

Khi trỏ/chọn điểm có thể mở doanh thu của khoảng đó trong màn báo cáo với bộ lọc đúng; không mở màn trắng rồi bắt chọn lại kỳ. Legend phân biệt bằng chữ/nét bên cạnh màu.

## 7. Donut cơ cấu doanh thu theo kênh

Ba nhóm **Tại quán / Mang đi / Giao hàng**, theo số tiền hóa đơn đã trả trong kỳ. Không lấy số đơn làm tử số rồi ghi là cơ cấu doanh thu. Trung tâm hiển thị tổng tiền khớp KPI; legend ghi tên kênh, số tiền và tỷ trọng.

Nếu một nhóm=0 vẫn đọc được trong legend; tổng=0 thì hiển thị trạng thái “Chưa có hóa đơn đã thanh toán trong kỳ”, không vẽ vòng100% giả. Tỷ lệ làm tròn có quy tắc nhất quán, không âm thầm sửa số tiền để làm tổng% tròn100.

Chọn một kênh mở báo cáo đúng kênh/kỳ; không tự thay tất cả KPI khi người dùng chỉ chọn một lát để xem chi tiết. Biểu đồ phương thức thanh toán chưa đưa thêm vì dễ lặp thông tin và tăng số khối trên trang.

## 8. Món bán chạy

Top5 theo **số ly đã bán từ các hóa đơn đã thanh toán** trong kỳ, có tên món, số ly, thanh ngang và tổng số ly toàn kỳ. Thứ tự hòa số ly có tie-break ổn định. Không lấy đơn mở/hủy.

Không hiển thị doanh thu từng món cạnh tổng hóa đơn như thể có thể cộng ngang: tiền món theo snapshot trước giảm toàn bill khác tổng tiền đã trả/gồm ship. Phạm vi đầu chỉ cần lượng bán; giá/ưu đãi đổi sau đó không làm đổi lượng lịch sử. Chọn món có thể mở báo cáo món đúng kỳ; tên là tên hiện tại theo contract report hiện có, không bịa snapshot tên nếu DB không lưu.

## 9. Cần chú ý — trạng thái hiện tại

Một panel gọn, mỗi dòng có số lượng/số tiền và lối mở danh sách được lọc:

| Dòng | Quy tắc đề xuất |
|---|---|
| Đơn đang mở | Hóa đơn Chua thanh toan hiện tại, không giới hạn ngày tạo để bỏ sót đơn từ hôm trước |
| Giao hàng chưa hoàn tất | Đơn giao đã thanh toán, trạng thái Cho giao/Dang giao; bỏ Da giao/Huy giao theo contract hiện tại |
| Nguyên liệu hết tồn | Nguyên liệu có SoLuongTon=0. Không cộng lượng khác đơn vị vào một con số chung; chỉ đếm nguyên liệu và mở danh sách tương ứng |
| Nợ nhà cung cấp | Tổng ConNo dương theo vw_CongNoNCC hiện tại. View đã có HanTra, nhưng dashboard chỉ dùng số dư hiện tại theo phạm vi đã duyệt, không mở thêm chỉ tiêu nợ quá hạn |

Không thêm cảnh báo “sắp hết” vì schema nguyên liệu hiện chưa có mức tồn tối thiểu được cấu hình; chỉ có cảnh báo hết tồn theo thuộc tính sẵn có. Không đưa lương/khấu hao vào panel đầu tiên: ưu tiên các việc vận hành thường xuyên, tránh biến Tổng quan thành tất cả báo cáo ghép lại.

## 10. Hành vi và trạng thái cần có

- Đổi ngày/tháng: tải toàn bộ phần theo kỳ cùng nhau, chặn phản hồi cũ ghi đè, không lẫn các kỳ giữa card và biểu đồ. Hiện ngày/kỳ đã áp dụng và thời điểm cập nhật.
- Loading dùng placeholder/skeleton hoặc “—”; thành công không phát sinh mới hiện0/list rỗng. Lỗi không biến biểu đồ thành0 hoặc KPI thành dữ liệu mới giả.
- Nếu giữ dữ liệu lần thành công trước khi refresh lỗi, phải có nhãn “Dữ liệu lúc…” và trạng thái cũ rõ; tuyệt đối không ghép phần cũ/phần mới như một snapshot. Lựa chọn đơn giản cho bản đầu là làm mờ/khóa toàn khối theo kỳ trong lúc tải và cập nhật nguyên bộ khi thành công.
- Tooltip/chú thích/legend đọc được bằng bàn phím; màu không là tín hiệu duy nhất. Không dùng modal cho mỗi tooltip.
- Chỉ tiêu và biểu đồ là đường vào nguồn liên quan; không chốt tiền/kho/nợ từ Tổng quan. Giữ phiên/quyền/selection khi quay lại theo UX.
- Màn hình đọc aggregate; không tải mọi bill/dòng món về UI để tự tính. Không giữ transaction trong thời gian người dùng xem biểu đồ.

## 11. Đối chiếu với phần đã có

| Dữ liệu | Hiện trạng | Phần cần mở rộng để đáp ứng thiết kế |
|---|---|---|
| DashboardRow/sp_ReportDashboard | Chỉ hôm nay: tiền đã trả, đơn mở, nợ; một snapshot | Thêm lựa chọn kỳ, chỉ tiêu, mốc so sánh và series; không giả định procedure hiện tại đã trả đủ |
| Reports SalesDay/SalesChannel | Có aggregate theo ngày/kênh, đếm bill và tiền theo NgayThanhToan | Tổng thể dashboard nhất quán, bucket theo giờ, bucket đang chạy/cùng kỳ, so sánh% |
| Products | Có số ly theo hóa đơn đã trả, giá snapshot | Top5 đúng toàn tập, tổng số ly toàn kỳ; không lấy5dòng đầu của trang sắp theo mã |
| CashFlow/PHIEUTHUCHI | Có phân nguồn thu/chi | Aggregate Thực chi đúng kỳ và nguồn, không đếm lại chi nhập/trả nợ/chi lương |
| Kho/nợ/giao | Có số dư/trạng thái | Snapshot vận hành và lối lọc đúng nguồn, nhãn “Hiện tại” |
| Điều hướng hiện tại | OpenWorkspaceAsync chủ yếu nhận tên module | Truyền ngữ cảnh kỳ/kênh/điểm đã chọn; cần plan cụ thể, không coi sẵn có |

SQL→DAL→BLL→UI vẫn theo kiến trúc dự án. Số tiền/aggregate/% chính thức và điều kiện coverage cần được chốt trong truy vấn đọc trước UI. UI định dạng và vẽ chart. Không có kế hoạch mở rộng bảng/cột/entity để phục vụ dashboard; cấu trúc DTO/model hiện có được ưu tiên và không tự thêm property mới. Cách tổ chức truy vấn và render biểu đồ sẽ được nêu ở plan kỹ thuật, kiểm tính phù hợp .NET10/offline/license; không tự thêmWebView/server hoặc dependency chỉ để vẽ mockup.

## 12. Tiêu chí chốt thiết kế và nghiệm thu sau này

1. Đăng nhập ADMIN thấy ngay4KPI, xu hướng, cơ cấu kênh, món bán chạy và trạng thái vận hành theo phân cấp đã nêu; không lấy nhóm nút làm nội dung chính.
2. Hôm nay/Tháng này có nhãn kỳ, so sánh cùng mốc rõ; đối chiếu% bằng dữ liệu tính tay, bao gồm0/thiếu kỳ/tháng ngắn.
3. Biểu đồ theo giờ/ngày và donut khớp KPI;2món×2phương thức thanh toán không nhân tổng. Tiền/kho/điểm/nợ/lương không đổi từ thao tác xem.
4. Top5 đúng theo lượng từ toàn tập, không lẫn phân trang; tồn/nợ/đơn hiện tại được phân biệt với phát sinh kỳ.
5. Không phát sinh thì trạng thái rỗng có ý nghĩa. Mất kết nối, query muộn, đổi kỳ nhanh, logout/revoke và click mở nguồn giữ đúng luồng/quyền.
6. Agent kiểm chức năng và layout kỹ thuật tại125%, SQLServer thật/DBtest riêng; người dùng kiểm hình thức. Giữ checklist T28 và bổ sung subcase, không thay325ID hoặc nghiệm thu hồi tố.
7. Hình minh họa/dataset test nếu có phải ghi rõ tính chất; không tự chèn hóa đơn vào development để biểu đồ có dữ liệu. Development hiện không có doanh thu ngày hiện tại vẫn phải hiển thị trạng thái thật.
8. Mỗi KPI/điểm biểu đồ truy được về trường nguồn trong bảng truy vết; đối chiếu schema/entity trước/sau chứng minh không thêm thực thể/thuộc tính lưu trữ. Không tự bổ sung property/DTO/model mới ngoài ràng buộc đã chốt.

## 13. Những điểm đề xuất để chốt

- ADMIN là phạm vi đầu tiên; mặc định Hôm nay.
- Bốn KPI: Doanh thu đã thanh toán/Hóa đơn đã thanh toán/Bình quân hóa đơn/Thực chi.
- Hai chart chính: xu hướng doanh thu và donut kênh; Top5 dùng thanh ngang.
- So sánh hôm qua/cùng mốc tháng trước; chưa thêm bộ chọn tùy ý, mục tiêu doanh số, dự báo hoặc ngưỡng cảnh báo tồn mới.
- Đủ nguồn mới tính so sánh; không đưa lợi nhuận giả hoặc dữ liệu tĩnh vào cho đầy trang.

Sau khi chốt đề xuất này mới viết plan kỹ thuật cho truy vấn/UI, fixture và regression của phần phát sinh T28, bám mô hình hiện có. Chưa thay code, DB hoặc các hồ sơ nghiệm thu cũ từ việc lập tài liệu này. Mọi yêu cầu cần thực thể/thuộc tính mới đều nằm ngoài phạm vi và không được tự triển khai.
