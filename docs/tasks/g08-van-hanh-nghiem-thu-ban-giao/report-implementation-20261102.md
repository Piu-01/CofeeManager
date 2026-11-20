# G08 — Trạng thái triển khai

02/11/2026 · **Đã viết đủ đầu ra triển khai theo bộ plan; G08 vẫn Đang làm, chờ duyệt đợt nghiệm thu riêng.** Requirements và cả bộ plan T30–T32 đã được người dùng duyệt. Không có T30–T32-Kxx nào được ghi PASS từ triển khai.

## Đầu ra

- T30: Install-Database.ps1, OperationsTools.ps1, Backup-Database.ps1, Restore-TestDatabase.ps1; hướng dẫn docs/operations/install.md, backup-restore.md, security.md. Không thay source sản phẩm/migration001–027 hoặc DB development. [Kiểm kỹ thuật](t30-backup-restore-cai-sach/technical-checks.md).
- T31: [ma trận325ID](acceptance-matrix.md), [44UX-ID](ux-matrix.md), G08Probe cho bootstrap/login/quyền, chuẩn bị subcase SQL bán/nhập-nợ/kiểm kê/công-lương/giao và phiên; [hướng dẫn native](t31-nghiem-thu-regression/native-test-guide.md). SQL subcase không thay native E2E hoặc fault saucommit.
- T32: Publish-G08.ps1/Test-ReleaseInventory.ps1, hướng dẫn release/demo, [guide kiểm package](t32-publish-ban-giao/native-publish-guide.md). Package chỉ là ứng viên trước nghiệm thu; chưa phát hành chính thức.

## Kiểm chứng kỹ thuật đã chạy

Lượt [technical_20261102_153236_d51d6525](runs/technical_20261102_153236_d51d6525/manifest.json): clean/demo baseline→001–027, bootstrap/BLL/login ADMIN và Demand FinancialReports qua user CafeManagerApp. Succeeded, SourceUnchanged=true,2DBtạo/2dọn/0giữ. Không kiểm runtime identity kết nối thật bằng user WITHOUT LOGIN. Build probe thành công, có NU1900 do NuGet vulnerability feed không truy cập được; không coi audit sạch. Các lượt kỹ thuật lỗi trước đó giữ nguyên trong runs, không tạo DB.

Lượt kỹ thuật cuối [technical_20261102_155211_7e943c93](runs/technical_20261102_155211_7e943c93/manifest.json): clean/demo027, bootstrap/BLL/login; COPY_ONLY/CHECKSUM/VERIFYONLY, restore test sang file mới, parity hash **48bảng**, SourceStable=true/Parity=true/SourceUnchanged=true; **3DBtạo/3dọn/0giữ**, file backup của lượt đã dọn. Đã chạy tương tự ở154507_ba9f74ef; không backup hoặc nâng development. [T30technical-checks](t30-backup-restore-cai-sach/technical-checks.md) ghi các lượt và khắc phục lifecycle của harness, không viết lại lịch sử.

Build Debug/Release và publish win-x64/framework-dependent thành công. Ứng viên154615_7a524f5b được thay bằng bản có PathMap vì DLL đầu chứa CodeView/PDB path máy tác giả. Bản155027_19c8013d:94file, inventory/metadata9assembly Debug/Release/publish Succeeded/0issues, positive LoginForm/AuthService/AuthRepository còn, không helper bypass; CodeView publish là đường dẫn trung tính. **NU1900 vẫn có do không truy cập NuGet vulnerability feed**, không coi audit sạch. Kiểm PE/inventory là kỹ thuật, không thay native Login/phiên/quyền hoặc chứng nhận chạy package.

Package cuối được tạo lại từ cùng công cụ/hồ sơ đã chốt; đường dẫn/hash/bằng chứng tại [T32technical-checks](t32-publish-ban-giao/technical-checks.md). Chưa chạy native, fullE2E/fault saucommit, backup nguồn development hoặc xUnit nghiệm thu trong G08; tất cả29ID vẫn NOT RUN. Không commit/push.

## Nghiệm thu còn chờ

| Task | ID gốc | Trạng thái |
|---|---:|---|
| T30 | 9 | NOT RUN — chờ duyệt đợt test |
| T31 | 11 | NOT RUN — chờ duyệt đợt test |
| T32 | 9 | NOT RUN — chờ duyệt đợt test |
| G08 | 29 | Chưa nghiệm thu |

Các nhóm trước chưa được nghiệm thu thay: G07còn9IDnativeBLOCKED; G04/G05/G06 và ngoại lệG03 theo từng report hiện hành. Ma trận hiện đánh dấu NOT RUN trong lượtG08, không xóa PASS/FAIL/lịch sử cũ. T31-K10 phải kiểm tính phù hợp source/schema/bằng chứng từng ca, không suy325PASS.

Tiện ích Login TEST đã gỡ24/10 theo hồ sơ lịch sử; audit build/publish và regression native trên bản cuối vẫn bắt buộc. G08 chưa PASS nếu chưa có bằng chứng này. Hình thứcSKIPPED theo phân công; chức năng/layout kỹthuật chỉ125%;100/150%NOTRUN, không đổi Scale.

Bug phát hiện trong nghiệm thu phải ghi task sở hữu và duyệt cả đợt trước debug/regression. Source/migration cố định từng lượt. Đợt test dự kiến tại [test-plan.md](test-plan.md), cần duyệt riêng sau triển khai.

## Đường dẫn và tài nguyên cuối

Ứng viên cuối: artifacts/g08/20261102_155435_c6b76b42/candidate.zip, schema027/win-x64/framework-dependent. Inventory0issues và94/94filehash khớp sau giải nén; chưa chạy exe/native/nghiệm thu. Manifest/hash tại t32-publish-ban-giao/release-candidate.json.

Audit SQL cuối:13DBtest thực tạo/13dọn/0còn;0filebackup G08 còn. Nguồn: runs/resource-audit-20261102.json; lượt154357 có cleanup-recovery riêng và giữ manifest lỗi gốc. Chỉ giữ package/buildlog/unzipintegrity trong artifacts cùng hồ sơ kỹ thuật trong docs; không còn principal SQL/Windows mới do G08 (probe chỉ tạo DB user WITHOUT LOGIN được dọn cùng DB). Chưa nâng development/commit/push.

