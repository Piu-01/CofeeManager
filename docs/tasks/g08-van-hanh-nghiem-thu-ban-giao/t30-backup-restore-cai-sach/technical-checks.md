# T30 — Kiểm tra kỹ thuật triển khai

02/11/2026. Không thay nghiệm thu T30-Kxx. Source sản phẩm và migrations001–027 không đổi.

- Lượt technical_20261102_153236_d51d6525: SQL Server localhost, clean/demo baseline→001–027, bootstrap qua BLL, hash ngẫu nhiên trong bộ nhớ, login ADMIN và Demand FinancialReports qua DB user G08Runtime/CafeManagerApp. Cả hai probe Succeeded, SourceUnchanged=true,2DB dự kiến/tạo/dọn2, giữ0. [Manifest](../runs/technical_20261102_153236_d51d6525/manifest.json); clean/demo-install.json chứa checksum từng migration; clean/demo-probe.json chứa kết quả thật.
- Chưa sao lưu development, chưa chạy ma trận backup/restore/quyền identity thật/chứng chỉ/native. DB user WITHOUT LOGIN chỉ là smoke quyền DB, không chứng nhận runtime identity kết nối thật.
- Build probe -m:1 -nr:false thành công. NU1900 do không truy cập NuGet vulnerability feed, không coi là audit sạch. Build song song mặc định từng trả Failed không diagnostics; runner giới hạn một luồng, không đổi SDK.
- Giữ các lượt kỹ thuật không thành công:152842(build),153138(builder PowerShell),153200(kết nối sandbox). Chưa tạo DB ở cả ba lượt;153236 chạy SQL ngoài sandbox thành công. Không viết lại lịch sử hoặc tick PASS T30-Kxx từ smoke.

## Backup/restore engine và lifecycle

- technical_20261102_154211_506f857f dừng do alias SQL RowCount chưa quote trong snapshot helper. Chưa tạo backup;2DBtest đã dọn. Chỉ sửa harness giữa các lượt kỹ thuật, không thay source sản phẩm hoặc expected nghiệm thu.
- technical_20261102_154357_0cd6667f: BACKUP/VERIFYONLY và RESTORE/parity thành công nhưng finally không lưu manifest cuối do Windows user không được truy cập thư mục backup SQL. Giữ manifest gốc; [cleanup-recovery.json](../runs/technical_20261102_154357_0cd6667f/cleanup-recovery.json) đối chứng source-before/source-after bằng nhau,0DB còn/0backup còn sau dọn exact file bằng SQL service. Không đổi ACL/service.
- technical_20261102_154507_ba9f74ef và lượt cuối [155211_7e943c93](../runs/technical_20261102_155211_7e943c93/manifest.json): clean/demo001–027/bootstrap/login thành công, COPY_ONLY/CHECKSUM/VERIFYONLY, FILELIST/MOVE restore mới, snapshot48bảng trước/sau nguồn ổn định và đích khớp. SourceUnchanged=true;3DBtạo/3dọn/0giữ,backup file mới của từng lượt đã dọn. Manifest backup/restore chỉ lưu hash/count, không dòng credential/password.
- Backup cleanup qua SQL service với exact path, guard root/name/runID và manifest NewFileVerified; không xóa backup có trước hoặc DROPwildcard. Partial restore thiếu marker được báo giữ để operator kiểm, không tự drop thiếu ownership.

Chỉ dùng DB test của lượt, chưa sao lưu/nâng development, chưa kiểm runtime identity kết nối thật/config/chứng chỉ/native. Đầu ra kỹ thuật dùng được cho T31/T32, **không tick T30-Kxx**. Các ca nghiệm thu vẫn chờ duyệt đợt test.
