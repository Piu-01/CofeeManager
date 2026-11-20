# G07 — Đường chạy development và dùng thử

**Cập nhật09/11/2026:** người dùng đã duyệt và development đã nâng021→024→027, bảo toàn hash dữ liệu, giữ hai backup đã VERIFYONLY CHECKSUM.16 kiểm đọc runtime và native Tổng quan PASS theo phạm vi; xem [biên bản](t28-bao-cao-doi-chieu/development-upgrade-results-20261109.md). Các dòng “chưa nâng/chưa thực hiện” bên dưới là lịch sử31/10; không suy đã nghiệm thu toàn G07.

31/10/2026. **Chưa chạy nâng DB development, chưa nghiệm thu G07.** Source025 cung cấp tài sản,026 cung cấp báo cáo;027_asset_write_lock sửa khóa theo đợt G07-B01 đã duyệt, không phải index tối ưu. Không coi F5 với schema cũ là đã dùng được G07.

## Đường chạy cố định

Mở `CafeManager.slnx`, startup `CafeManager.WinForms`, profile `CoffeeManager.Development`; SDK10.0.401 theo global.json. Exe Debug ở `src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe`; dùng Release tương ứng khi đã build.

DB development cố định `CoffeeManager_Development` trên `localhost`. Profile đọc connection DPAPI của Windows user hiện tại; `CAFEMANAGER_APP_CONNECTION` nếu đã đặt vẫn có ưu tiên. Không ghi/copy credential vào hồ sơ này; tài khoản demo theo hướng dẫn local hiện có, không reset chỉ vì nâng G07.

## Chuẩn bị nâng schema — chưa thực hiện

Công cụ: `tools/database/Update-DevelopmentG07.ps1 -Apply`. Chỉ chạy khi đã được giao nâng development. Script yêu cầu marker đúng, app đã đóng và schema tối thiểu024; không tự gộp nâng G06 còn chưa được giao. Kiểm checksum trước, backup COPY_ONLY/CHECKSUM tại đường backup SQL Server, áp migration kế tiếp tới026 hoặc027 đã chuẩn bị, rồi so hash dữ liệu gốc gồm tài sản, tài khoản, công/lương, tiền/kho/điểm/nợ/hóa đơn. Lưu manifest và đường backup; không seed lại hoặc ghi fixture vào development.

Đường backup thuộc máy SQL Server; không tự xóa backup vận hành. Nếu migration/preflight hoặc đối chiếu thất bại, giữ bằng chứng và backup, không tiếp tục sử dụng app như đã nâng thành công. Cần ghi server/DB/schema trước/sau và vị trí bản build khi bàn giao dùng thử thực tế.

## Kịch bản sau khi nâng đúng schema

1. ADMIN → Tài sản → Thêm: nhập tên/ngày/nguyên giá/số tháng, lưu và tìm lại mã TS…; không có chi tự sinh. Sửa/reload dùng revision, xung đột yêu cầu tải lại.
2. Xem hồ sơ: khấu hao **tham khảo** từ tháng mua, phần dư dồn tháng cuối; xem chi theo kỳ/pager. Ước tính theo hồ sơ hiện tại, không phải snapshot kế toán.
3. Thu chi → Ghi phiếu: danh mục chi ghi tay, chọn tài sản qua picker; ghi một phiếu, xem MaTS và mở hồ sơ. Từ hồ sơ mở lại phiếu chi, quay về giữ kỳ/selection. Hồ sơ có chứng từ không xóa được.
4. Khi kết quả ghi chưa rõ: giữ request, chọn **Kiểm tra kết quả**; không tạo yêu cầu mới ngay sau timeout. Yêu cầu chờ tài sản scoped theo DB và actor; mở Tài sản → Yêu cầu chờ để đối chiếu sau restart.
5. Báo cáo: chọn loại → kỳ/filter tương ứng → **Xem báo cáo**. Đổi filter hiện Cần tải lại và khóa nguồn cũ; chọn dòng → Chứng từ nguồn → Xem chi tiết → Quay lại. Pager dùng bộ lọc đã áp dụng.
6. Doanh thu theo ngày trả; món trước giảm toàn bill; tiền khách/ship/giảm/phí đối tác tách riêng. Nợ tại ngày dùng cutoff. Lương chốt/thực chi/giờ làm là chỉ tiêu độc lập; khấu hao không phải dòng tiền. Không gọi thu−chi là lợi nhuận chính xác.
7. Tồn kho: đọc mốc mở sổ, phạm vi nguồn và thời điểm ghi kho. Kỳ trước mốc/thiếu nguồn có nhãn Incomplete, không diễn giải là số0 hoặc chênh lệch0. Closing tại ngày quá khứ khác Current đọc hiện tại.
8. Home ADMIN → Tải lại đọc tiền khách đã trả hôm nay/đơn mở/nợ thật. Lỗi kết nối có thông báo và thao tác tải lại, không biến thành tổng0. CASH/STORE không vào tài sản/báo cáo tài chính; quyền kho/nợ hiện có giữ theo contract.

Agent chỉ kiểm chức năng/layout kỹ thuật125% theo phạm vi được duyệt; người dùng tự kiểm hình thức. Native và DPI100/150% chưa chạy phải ghi đúng trạng thái. Không dùng reference HTML làm chứng cứ WinForms.
