# G06 — Đường chạy development và schema024

**Cập nhật09/11/2026:** người dùng đã duyệt nâng development021→024 rồi027. Chặng G06 Succeeded, hash dữ liệu gốc khớp, backup đã VERIFYONLY CHECKSUM; EmployeeContext/List đọc runtime PASS sau nâng027. [Bằng chứng chặng024](runs/development024_20261109_214928_9d110509/manifest.json), [biên bản chung](../g07-tai-san-bao-cao-hieu-nang-du-lieu/t28-bao-cao-doi-chieu/development-upgrade-results-20261109.md). Ghi chú chưa nâng phía dưới là lịch sử; không chốt nghiệm thu G06 từ việc nâng DB.

30/10/2026. **Chỉ chuẩn bị tài liệu/script, chưa nâng development.** Source G06 cần migrations022–024 sau001–021. Không dùng source có procedure mới như bằng chứng DB development đã có schema024.

Giữ solution CoffeeManager.slnx, startup CafeManager.WinForms, profile CoffeeManager.Development và cấu hình DPAPI hiện có. Module Nhân sự chỉ ADMIN; DB thiếu procedure sẽ báo chưa tải được/cần schema mới. Không fallback sang DB khác, không bỏ qua đăng nhập.

Khi được giao nâng development:

1. Đóng app bình thường. Xác minh đúng localhost/CoffeeManager_Development, marker CoffeeManager.Development.v1 và schema021–024. Không dùng DB test thay development hoặc cấp lại account để vào màn.
2. Review rồi chạy `tools/database/Update-DevelopmentG06.ps1 -Apply`. Script kiểm checksum các migration cũ, backup SQL COPY_ONLY CHECKSUM vào đường của SQL service, rồi áp dụng phần022–024 còn thiếu. Không xóa backup hoặc tự restore khi có lỗi.
3. Script hash toàn bộ nguồn gốc phía SQL trước/sau: hồ sơ, account, công, tiền bảng lương cũ, thu chi, nợ/lịch sử, tồn, điểm và hóa đơn. Metadata mới không dùng để giả lịch sử. Nếu preflight dừng hoặc hash khác: giữ backup/manifest, dừng dùng app và điều tra; không seed lại hay sửa tiền/tồn để hợp thức hóa.
4. Mỗi migration là một transaction riêng. Nếu022 đã commit nhưng023/024 dừng thì manifest/migrations.jsonl là bằng chứng phiên bản trung gian; không báo đã lên024, không tự reset DB. Chỉ tiếp tục sau quyết định xử lý nguyên nhân.
5. Mở đúng profile và login ADMIN bình thường. Nhân sự gồm Nhân viên/Tài khoản/Chấm công/Bảng lương. Chức vụ và ca dùng danh mục có sẵn. Không dùng dữ liệu thật để chạy kịch bản nghiệm thu; dùng DB test riêng.

Lương legacy giữ số tiền/giờ cũ và gắn LegacyUnknown; không backfill hợp đồng/giờ duyệt từ hồ sơ hiện tại. Legacy chưa đối soát không chi mới; nguồn đã liên kết hợp lệ vẫn đọc được. Bảng mới chốt/chi theo preview SQL và chỉ một lần. Tăng ca không tự lấy hệ số từ ca/chức vụ.

Request chưa rõ kết quả: mở Yêu cầu chờ hoặc Đối chiếu chốt/chi trên cùng DB và account; không tạo request mới khi request cũ chưa đối chiếu. Sau self-reset/đổi quyền/khóa, đăng nhập lại theo trạng thái mới hoặc ADMIN khác tra request metadata. File không chứa mật khẩu/hash. Không xóa file pending chỉ để thử ghi lại tiền.

Script nâng development chưa chạy; chưa có bằng chứng bảo toàn DB development của G06. Việc được duyệt bộ plan hoặc đợt test DB riêng không tự cho phép chạy script nâng development. Bằng chứng kỹ thuật hiện có nằm ở [report.md](report.md).
