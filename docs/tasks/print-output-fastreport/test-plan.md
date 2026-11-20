# Test plan & kết quả sơ bộ — Đầu ra in FastReport + PDF

**Ngày chạy:** 16/11/2026. **Môi trường:** Windows, .NET SDK 10.0.401, build Debug
`net10.0-windows`; không kết nối SQL cho các ca này.

## A. Ca tự động (xUnit)

| Mã | Nội dung | Kỳ vọng | Kết quả |
|---|---|---|---|
| PR-01 | Probe FastReport trên net10: export hóa đơn A4 | File `%PDF`, > 1 KB, MediaBox A4 595pt | PASS |
| PR-02 | Hóa đơn 80mm | Magic `%PDF`, MediaBox 226pt (80mm) | PASS |
| PR-03 | Hóa đơn 60 dòng | Sang ≥ 2 trang, không lỗi | PASS |
| PR-04 | `PrintFormat` | Tiền `81.000 ₫`, số lượng `1,5`, ngày `16/11/2026 09:30`, null `—` | PASS |
| PR-05 | CSV | Có BOM EF BB BF, `;`, escape `;`/`"`/xuống dòng, giữ số thô | PASS |
| PR-06 | PDF báo cáo | Magic `%PDF`, A4 ngang 841pt | PASS |
| PR-07 | Báo cáo 0 dòng | Vẫn export được, không crash | PASS |
| PR-08 | `ReportColumns` | Cả 9 `ReportKind` có cột, header khác rỗng, weight > 0, field không trùng | PASS |

Lệnh: `dotnet test tests/CafeManager.Tests/CafeManager.Tests.csproj -c Debug --filter "FullyQualifiedName~Printing"`.
Kết quả: **8/8 PASS**. File test: `tests/CafeManager.Tests/Printing/{PdfSmokeTests,ReportOutputTests}.cs`.

## B. Kiểm tra thủ công trong phiên agent (không phải nghiệm thu)

Đã trích ảnh trang từ PDF sinh thử và xem nội dung: hóa đơn A4, hóa đơn 80mm,
báo cáo A4 ngang hiển thị đúng cột, căn phải tiền, ghi chú xuống dòng, nhãn IN LẠI,
footer `Trang 1/1`, tổng in đậm. Đây là kiểm tra kỹ thuật nội bộ, **không** thay
phần hình thức do người dùng nghiệm thu.

## C. SKIPPED / chờ người dùng

| Mã | Nội dung | Lý do |
|---|---|---|
| PR-S1 | In thật ra Microsoft Print to PDF / máy in A4 | Cần thao tác người dùng |
| PR-S2 | In trên máy in nhiệt 80mm thật | Agent không có thiết bị; ghi SKIPPED, không suy PASS |
| PR-S3 | Hình thức cuối, DPI 100/125/150 | Theo phân công, người dùng tự kiểm |
| PR-S4 | Hộp in Edge/WebView2 với PDF trên máy đích | Cần chạy app thật; đã có fallback mở trình đọc ngoài |
| PR-S5 | In báo cáo/CSV với dữ liệu SQL thật nhiều dòng | Cần DB test và đợt test được duyệt |

## D. Ghi chú

- Không chạy đợt nghiệm thu đầy đủ khi chưa được duyệt.
- Hóa đơn đã chốt không bị thay đổi bởi thao tác in/hủy in; `DemandPrintAsync`
  vẫn kiểm quyền trước khi mở preview.
