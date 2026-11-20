# G08 — Báo cáo hiện tại sau sửa G08-B01

02/11/2026 · **16 PASS / 0 FAIL / 13 BLOCKED trên 29 ID gốc. G08 vẫn Đang làm.** Requirements, bộ plan, đợt test và đợt sửa G08-B01 kèm regression đã được người dùng duyệt. G08-B01 đã đóng theo phạm vi regression; còn các ca native/fault và gate bàn giao chưa đủ bằng chứng.

| Task | PASS | FAIL | BLOCKED | Biên bản |
|---|---:|---:|---:|---|
| T30 — Vận hành/cài sạch | 9 | 0 | 0 | [test-results](t30-backup-restore-cai-sach/test-results.md) |
| T31 — Nghiệm thu xuyên suốt | 1 | 0 | 10 | [test-results](t31-nghiem-thu-regression/test-results.md) |
| T32 — Publish/bàn giao | 6 | 0 | 3 | [test-results](t32-publish-ban-giao/test-results.md) |
| G08 | 16 | 0 | 13 | Giữ nguyên 29 ID G08/325 ID toàn dự án |

## G08-B01 đã sửa và kiểm lại

Mapper nay nhận258 là lỗi kết nối và−2146893019 là lỗi chứng chỉ, có thông báo Việt hướng dẫn kiểm tra phù hợp. Chỉ đổi `SqlErrorMapper.cs` và thêm14case trong `ParameterAndErrorTests.cs`. Không đổi SQL/migration, quyền/TLS hoặc UI. `writing && sent` của DAL giữ nguyên: trước gửi không yêu cầu đối chiếu; mapper nhận cờ ghi sau gửi vẫn giữ bảo vệ kết quả chưa biết.

- **142/142 unit PASS**, gồm các quy tắc cũ và14case mới về Connection/timeout/Unknown/quyền/deadlock/đối chiếu.
- **4/4 kiểm DAL/BLL trước gửi PASS** trên DLL package thật: đọc và đường ghi thất bại tại OpenAsync cho offline/TLS, đúng Number/Kind/message/flag. DB fixture không có bill, thanh toán hoặc phiếu thu-chi phát sinh.
- **7/7 ca native PASS:** thiếu cấu hình, cấu hình sai, offline, chứng chỉ, mật khẩu sai, đăng nhập đúng vào ADMIN, logout trả Login trống. Ca chứng chỉ vẫn từ chối TLS, không fallback hoặc hướng dẫn bỏ kiểm chứng chỉ.
- Build Debug/Release/publish thành công; inventory0issues. Có NU1900 do feed NuGet audit không truy cập được, không tuyên bố vulnerability audit sạch.
- Source460file và archive được giữ nguyên trong regression. **1DB/1SQLlogin/DPAPI tạm đã dọn**, không truy cập development trong lượt này.

[Biên bản sửa/regression](t30-backup-restore-cai-sach/bug-regression-results.md), [bug và lịch sử điều tra](t30-backup-restore-cai-sach/bug-report.md), [manifest regression](runs/regression_b01_20261102_171805_94f20337/manifest.json), [native](runs/regression_b01_20261102_171805_94f20337/native.json), [142unitTRX](runs/regression_b01_20261102_171805_94f20337/b01-unit.trx).

Lệnh kiểm DAL/BLL đầu có2FAIL do đặt expected sai cho lỗi *trước gửi*. Giữ nguyên file/log; đối chứng hash của guard DAL từ acceptance xác nhận guard đã có từ trước và không đổi. Lượt scenario mới chạy lại thật theo giai đoạn trước gửi,4/4PASS; các unit mapper `writing=true` vẫn yêu cầu đối chiếu. Không ghi đè FAIL của lệnh cũ, không sửa implementation để thỏa expected sai. Chi tiết trong biên bản regression.

## Kết quả được kế thừa và phần còn thiếu

Lượt acceptance cũ có15PASS/1FAIL/13BLOCKED được giữ nguyên tại [report-acceptance-20261102.md](report-acceptance-20261102.md) và test-results-acceptance của T30. Lượt đó đã kiểm backup development021/restore45bảng, upgrade restore021/026→027, checksum/rerun, bootstrap/identity thật,7SQL/BLLscenario,7nhóm đối chiếu tiền-kho-điểm-nợ-lương/nguồn và128unit.13lần tạo DB test/13lần dọn,3backup/2SQLlogin/DPAPI đã dọn, development không đổi.

T30-K07 được nâng PASS bằng regression mới. Các kết quả không bị ảnh hưởng được kế thừa, **không gọi29ID đã chạy lại đầy đủ trên package mới**. T31-K01–K08/K10/K11 và T32-K03/K05/K09 vẫn BLOCKED: native qua ownedmodal chưa đi trọn, in/keyboard/IME/latequery/DPI125% còn thiếu, fault trước/saucommit đủ7usecase chưa có bằng chứng an toàn. Blocker công cụ nhập modal không bị coi là bug sản phẩm. Không dùng replay/staleKK thay fault saucommit.

Gate cuối gỡ Login TEST vẫn BLOCKED: source/DLL không helper và native Login/logout đã kiểm; native đủ3role/thu hồi/callback/unknown và các cờ môi trường legacy chưa đủ. Không suy PASS từ hồ sơ gỡ24/10. Các nhóm G04/G05/G06/G07 chưa được nghiệm thu thay. [325ID](acceptance-matrix.md) và [44UX-ID](ux-matrix.md) giữ lịch sử; bằng chứng report rỗng/lỗi/thử lại của G08 đã liên kết về T28.

Hình thức **SKIPPED theo phân công người dùng**;100/150%DPI **NOT RUN**, không đổi Scale. Chưa commit/push.

## Phiên bản package

Package được chạy native trong regression: `artifacts/g08/20261102_171805_94f20337/candidate.zip`, SHA256 **DBE592A647904A4FF2F06AA6FB29E14D7292C487F86D722AFE33B0EEDFD3C436**. Win-x64/framework-dependent, schema027, SDK10.0.401. HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3+workingtree; source thực theo SHA256 trong manifest, không suy toàn bộ source từ HEAD.

Bản đóng gói sau regression cập nhật hồ sơ hiện tại, được đối chiếu mọi file app/SQL/công cụ với package đã test. Xem [release-candidate.json](t32-publish-ban-giao/release-candidate.json) cho đường dẫn/hash cuối và bằng chứng parity. Package vẫn là **ứng viên**, G08 chưa Hoàn tất.

Bản đóng gói cuối: artifacts/g08/20261102_215232_0a0b72a5/candidate.zip;SHA256 **61AD7FC48DF29E79398F9CD7706F9283594BB5A4FBA83295B772771201E4AE12**.87file app/SQL/công cụ giống nguyên byte với packageDBE592…đãtest;chỉcậpnhậthồsơsau regression. Xem [parity](runs/regression_b01_20261102_171805_94f20337/handoff-package-parity.json).

