# G08 — Báo cáo nghiệm thu ngày 02/11/2026

**15 PASS / 1 FAIL / 13 BLOCKED trên 29 ID gốc. G08 vẫn Đang làm.** Người dùng đã duyệt requirements, cả bộ plan và đợt test riêng. Chưa sửa bug, chưa thay package, chưa commit/push. Bộ đếm ID không đồng nhất với số unit/subcase.

| Task | PASS | FAIL | BLOCKED | Biên bản |
|---|---:|---:|---:|---|
| T30 — Vận hành/cài sạch | 8 | 1 | 0 | [test-results](t30-backup-restore-cai-sach/test-results.md) |
| T31 — Nghiệm thu xuyên suốt | 1 | 0 | 10 | [test-results](t31-nghiem-thu-regression/test-results.md) |
| T32 — Publish/bàn giao | 6 | 0 | 3 | [test-results](t32-publish-ban-giao/test-results.md) |
| G08 | 15 | 1 | 13 | 29 ID, giữ nguyên 325 ID toàn dự án |

## Lỗi cần duyệt sửa

**G08-B01, T30-K07 FAIL:** khi DB không truy cập được hoặc chứng chỉ SQL không được tin cậy, Login báo “Có lỗi dữ liệu chưa xác định…” thay vì nêu nguyên nhân kết nối/chứng chỉ. Đã tái hiện trên package thật và đối chứng DLL của đúng package: SqlClient trả258 và−2146893019; SqlErrorMapper chưa ánh xạ hai mã này nên trả Kind=Unknown. Không có bằng chứng fallback DB, bypass TLS hoặc sai tiền/kho.

[Báo cáo nguyên nhân, bằng chứng, phạm vi sửa và regression](t30-backup-restore-cai-sach/bug-report.md). Đây là **một bug** gây hai tình huống FAIL, không tự tách thành hai bug hoặc sửa trong đợt test. Đợt sửa đề xuất chỉ chạm mapper/test liên quan, giữ quyền/TLS/policy đối chiếu giao dịch; tạo package mới sau sửa. Duyệt test hiện tại không đồng nghĩa duyệt debug.

## Những phần đã kiểm được

- SQL Server thật localhost 17.0.1000.7 Developer. Backup development thực ở schema 021 bằng COPY_ONLY/CHECKSUM/VERIFYONLY; restore sang DB test mới khớp hash45 bảng và11 nhóm tiền/kho/điểm/nợ/lương/trạng thái/nguồn. Development không bị ghi hoặc nâng schema.
- Restore các fixture021 và026 rồi nâng027: hash nghiệp vụ/liên kết giữ nguyên;027rerunAlreadyApplied; bản sao checksum sai bị chặn. Clean có3 role/2 phương thức/3 danh mục chi hệ thống, không dữ liệu demo; Demo chạy seed đúng một lần trước migrations.
- Setup và runtime bằng hai SQL login test thật, tách identity operator. Bootstrap hoàn tất một lần; thu hồi setup có hiệu lực. Runtime chỉCafeManagerApp, khôngsysadmin/db_owner/backup/DDL, DML bảng được bảo vệ và setup trái phép bị chặn. Không tạo Windows user hoặc đổiMixedMode.
- Runner đã duyệt chạy7 subcase SQL/BLL. Lượt đối chiếu trực tiếp cuối kiểm thêm7 nhóm: bill/dòng/TT/tiêuhao, mọi nguyên liệu, điểm, chi/lương, liên kết và bàn, lỗi52903 trên cùng runtimeconnection với transaction0:0. Expected được tính trước; không lấy actual làm expected.
- 128/128 unit test hiện có PASS. Chỉ chạy unit/rule phù hợp; suiteSQLIntegration/G02 cần fixture riêng không được giả thành PASS. Lượt unfiltered trong sandbox đã ngắt và giữ log, không coi là bug sản phẩm.
- Native trên exe được giải nén ở thư mục khác: Login sai/đúng, ADMIN ở DB demo restore và DB clean, tổng quan đúng số SQL, POS/dialog món, report rỗng/lỗi/thử lại, logout trả Login trống. Runtime thực tải coreclr/System.Windows.Forms10.0.1326.50504. Không dùng probe thay các bước native chưa làm được.
- Chính script trong package đã cài clean/demo trên hai DB mới và kiểm027/seed counts đúng. Package có94payload file theo hash và một package-manifest, tổng95 file; inventory/metadata9 assembly Debug/Release/publish0 issues, license còn, không credential thật/localconfig/backup/DPAPI hoặc đường dẫn PDB máy tác giả.

## Phần còn BLOCKED và giới hạn

T31-K01–K08/K10/K11 vàT32-K03/K05/K09 chưa đủ điều kiện PASS. Các luồng ghi đầu cuối qua modal, in thực tế, native3 role/thu hồi/callback, keyboard/IME/DPI125%, latequery và fault trước/saucommit đủ7usecase chưa đi trọn. Công cụ nhập modal báo chỉ mục note không ở cache và không xác minh được đổi focus sau recovery; đây là blocker điều khiển, chưa phải bug sản phẩm.

Không dùng replay SQL hoặc lỗi stale kiểm kê để suy đã kiểm timeout saucommit. Không tự thêm hook/fault vào source đang cố định, không tắt SQLservice hoặc đổi mạng/Windows Scale. Hình thức **SKIPPED theo phân công người dùng**;100/150%DPI **NOT RUN**, không dùng làm blocker hình thức.

[Ma trận325ID](acceptance-matrix.md) và [44UX-ID](ux-matrix.md) giữ lịch sử. Các nhóm G04/G05/G06/G07 chưa được nghiệm thu thay; report native rỗng/lỗi/thử lại bổ sung bằng chứng choT28-K06 nhưng không tự chuyển toàn bộ G07 sangPASS. Gate cuối gỡ Login TEST vẫn **BLOCKED**: audit source/DLL và native Login/logout đã có, nhưng chưa đầy đủ native phiên/quyền/callback và cờ môi trường legacy. Không bịa tên cờ còn thiếu hoặc suy PASS từ bản gỡ24/10.

## Phiên bản và bằng chứng

Lượt cha [acceptance_full_20261102_160135_579468c5](runs/acceptance_full_20261102_160135_579468c5/manifest.json). HEAD8a38f8691d37089feec149b71a8f5718d37167f3+workingtree; [460 hash source trước/sau](runs/acceptance_full_20261102_160135_579468c5/freeze-audit.json) xác nhận không đổi. Package vẫn là artifacts/g08/20261102_155435_c6b76b42/candidate.zip, SHA256D1F7A782FB0C083DE02D30D0597DE4EC1AF68F138D17C36BF3143EBF88D8C54A. Không patch/rebuild package trong nghiệm thu. NU1900 do nguồn NuGet audit không truy cập được vẫn công khai, không tuyên bố vulnerability audit sạch.

- [Vận hành/restore](runs/acceptance_full_20261102_160135_579468c5/t30-operations.json), [identity](runs/acceptance_full_20261102_160135_579468c5/t30-identities.json), [native](runs/acceptance_full_20261102_160135_579468c5/native-transcript.json).
- [RunnerSQL/BLL7 subcase](runs/acceptance_20261102_160708_43592f44/Demo-probe.json), [đối chiếu trực tiếp7 nhóm](runs/acceptance_full_20261102_160135_579468c5/reconciliation_r3_20261102_165150/t31-direct-reconciliation.json), [128unitTRX](runs/acceptance_full_20261102_160135_579468c5/g08-unit-filtered.trx).
- [Điều tra lỗi kết nối/chứng chỉ](runs/acceptance_full_20261102_160135_579468c5/connection-error-investigation.json), [cài từ package](runs/acceptance_full_20261102_160135_579468c5/packaged-install-results.json), [inventory](runs/acceptance_full_20261102_160135_579468c5/release-inventory-acceptance.json).

Hai lệnh đối chiếu thủ công không hoàn tất được giữ nguyên: lần đầu lỗi suffixdecimalPowerShell; R2query nhầm cột số tiền của lịch sử nợ. Đã kết thúc/dọn từng lượt, chạy lượt mới với script hash riêng; R3join đúngPHIEUTHUCHI.SoTien quaMaTC, không đổi expected8000 hoặc các delta. Không biến lỗi lệnh kiểm thành bug sản phẩm, không xóa log và không sửa source/test/migration gốc trong lượt.

## Tài nguyên và quyền tạm

[Audit cuối](runs/acceptance_full_20261102_160135_579468c5/cleanup-audit-final.json): **13 lần tạo DB test /13 lần dọn /0 DB còn**,3 backup mới của lượt đã xóa đúng tên/manifest,2 SQL login test đã dọn,DPAPI tạm đã xóa; không có tài nguyên test giữ lại ngoài evidence/package trong thư mục đã chỉ định. Development snapshot cuối không đổi. Không xóa backup/DB có trước.

Auto-review từng từ chối fixtureDENY vì chưa thấy duyệt cụ thể và lệnh chưa thu hồi ngay. Sau khi người dùng trả lời **“Cho phép fixture tạm”**, ca được thực hiện chỉ trênG08NativeRuntime/DBclean test; [finallyREVOKE/CanExecute1](runs/acceptance_full_20261102_160135_579468c5/report-fault-fixture.json) xác nhận khôi phục sau30.23giây. Report đã hiển thị lỗi thay0giả và thử lại thành công; không còn quyền tạm hoặc yêu cầu quyền đang chờ.

Báo cáo triển khai trước nghiệm thu được giữ tại [report-implementation-20261102.md](report-implementation-20261102.md). Bước tiếp theo đang chờ là duyệt đợt sửa **G08-B01 kèm regression**; các blocker native/fault còn lại vẫn được công khai, không tự chốt G08 từ việc sửa một bug.

