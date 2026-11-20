# G07-DA02 — Kết quả sửa hiệu năng Tổng quan

**Ngày:** 10/11/2026
**Phạm vi:** chỉ `src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs`.
Không đổi SQL/migration/Models/contract, không thêm cache thống kê, không đổi
bố cục/logic nghiệp vụ.

## Thay đổi

| # | Thay đổi | Lý do |
|---|---|---|
| 1 | Thay cặp `Panel track` + `Panel fill` (mỗi dòng Top 5) bằng control `Bar` tự vẽ, bỏ `SizeChanged` handler gán `fill.Width` | Đo trước sửa: 76 sự kiện layout của `Panel` trong một lượt Render; mỗi dòng tạo thêm control lồng và handler phát layout |
| 2 | `WrapLabels` chỉ đặt `MaximumSize` khi bề rộng đích thay đổi | Trước đó mỗi lần `SizeChanged` đặt lại `MaximumSize` cho mọi Label, gây đo lại văn bản và layout lặp |
| 3 | `UpdateSnapshot` giữ nguyên cơ chế treo/cho layout toàn cây quanh một lượt cập nhật, có guard `closed/IsDisposed` khi resume | Bảo đảm luôn resume kể cả render lỗi; không đổi hành vi bất đồng bộ/`await` |

Không đổi: `ReadAsync`/generation/cancellation, `ClearSnapshotCore`, `RenderCore`
nội dung, `Arrange`, trạng thái loading/lỗi, điều hướng, quyền, DTO.

## Đo trước/sau (cùng harness, cùng máy, dev DB, 1366×768, DPI 125%)

Harness: [LayoutBench](LayoutBench/Program.cs) — dựng `MainForm` thật, gắn
`AdminDashboardControl`, chờ `ready`, giữ nguyên `pages/indicators/period`, gọi
private method và đếm `Layout` event. Chỉ đọc DB; không ghi.

| Mẫu | Trước (before-results.json) | Sau lần 1 (after6) | Sau lần 2 (after7) |
|---|---:|---:|---:|
| Mở màn đến ready | 1.531 ms | 1.260 ms | 1.012 ms |
| Render (có treo layout) | 122 ms / 17 layout | 131 ms / 17 | 132 ms / 17 |
| ClearSnapshot | 814 ms / 39 layout | 242 ms / 39 | 217 ms / 39 |
| Render sau Clear | 628 ms / 37 layout | 348 ms / 37 | 350 ms / 37 |
| RenderCore gọi trực tiếp (chẩn đoán) | 11.678 ms / 312 | 4.622 ms / 312 | 4.838 ms / 312 |
| ClearSnapshotCore gọi trực tiếp (chẩn đoán) | 5.891 ms / 179 | 2.538 ms / 179 | 4.912 ms / 179 |

- Hai mẫu cuối cố ý gọi **không** qua cơ chế treo layout nên chậm; app không dùng
  đường này. Giữ lại để thấy chi phí layout lặp đã bị loại khỏi đường chạy thật.
- Kiểm đúng đắn layout: snapshot hình học (bounds scroll/page/metrics/charts/bottom/
  products, số control con, chiều cao 4 hàng Cần chú ý, giá trị 4 KPI) sau render
  nhanh phải **trùng khít** snapshot sau `RenderCore` đầy đủ.
  Kết quả: `FastStable=true`, `FastMatchesRaw=true` ở cả hai lượt.
- Thử 2 phương án gộp layout khác đã bị loại vì sai hình học:
  `ResumeLayout(false) + PerformLayout()` để lại bounds mặc định cho dòng Top 5
  mới (`FastMatchesRaw=false`); resume từ trên xuống làm chậm lại 1,5–6 s.

## Kiểm khác

| Mã | Nội dung | Kết quả |
|---|---|---|
| P01 | Build Release (WinForms + bench) | PASS — 0 warning/0 error |
| P02 | Build Debug | BLOCKED — app đang chạy (PID 33760) và VS (36512) khóa `bin\Debug` |
| P03 | `dotnet test` lọc `FullyQualifiedName~Localization` (Release) | PASS 11/11 |
| P04 | `tools/localization/audit.ps1` | PASS — 0 violation |
| P05 | `dotnet test` toàn bộ (trừ Integration) | FAIL 367 do fixture G02 cần DB test riêng (`Use Test-G02.ps1 with an isolated marked DB`) — không liên quan thay đổi này |

## Regression 10.000 hóa đơn (10/11/2026)

Đợt test đã được duyệt và chạy trên DB test riêng schema 29; chi tiết và bằng chứng ở
[da02-regression-results.md](da02-regression-results.md).

| Nhóm | Kết quả |
|---|---|
| Timing UI 10.000 bill (startup/tháng/hôm nay) | PASS 3/3 — 582,6 / 645,2 / 661,5 ms (<3.000 ms) |
| Perf gồm KPI khớp SQL, Top 5, ẩn/dispose/lỗi, query muộn, nguồn phân trang | PASS 12/12 |
| Trạng thái rỗng (ngày/tháng, DB sạch) | PASS 3/3 |
| Ma trận bố cục 1366/1280/1920/600 tại 125% | PASS — 2/2, 2/2, 4/2, 1/1 chi tiết/chart |
| Source cố định trong lượt | PASS — 469 file, diff 0 dòng |

## Chưa chạy (NOT RUN)

- Hình thức/cảm giác thanh Top 5 sau đổi `Bar` — người dùng tự kiểm.
- DPI 100%/150% NOT RUN, không đổi Windows Scale.
- Dialog nguồn chi tiết mở trực quan trên app đang mở (dữ liệu/phân trang đã kiểm).

## Rủi ro còn lại

- `Bar` là control mới chỉ trong file này; màu/độ dày lấy từ theme và DPI như
  panel cũ. Nếu người dùng thấy khác hình thức cũ thì chỉnh trong `Bar`.
- `WrapLabels` có guard bề rộng; khi cửa sổ đổi kích thước, MaximumSize vẫn được
  cập nhật như cũ nên nhãn vẫn wrap đúng.
- Hiệu năng thực tế với 10.000 hóa đơn đã đo lại: UI <700 ms, đạt mục tiêu <3 s
  ([biên bản regression](da02-regression-results.md)); chỉ còn phụ thuộc tốc độ SQL
  khi kỳ/dữ liệu lớn hơn nhiều so với dataset đo.
