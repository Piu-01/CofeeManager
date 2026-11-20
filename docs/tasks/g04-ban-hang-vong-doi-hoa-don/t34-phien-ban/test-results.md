# Kết quả test T34 — Phiên bàn và trả bàn

Lượt test ngày 17/11/2026. Không sửa source trong lượt test.

## Môi trường và bằng chứng

- SQL Server `localhost`, schema **034**, commit nền `8a38f86` + thay đổi chưa commit.
- Acceptance: `runs/acceptance_20261117_003501_600081b1/manifest.json`
  — `acceptance-table-sessions.sql` **PASS**, `SourceUnchanged=true`.
- Technical: run `technical_20261117_001148_f2533692` — typed smoke **FAIL** (bug T34-01);
  native smoke **NOT RUN** vì runner dừng ở typed smoke.
- Chẩn đoán bổ sung: DB demo tạm dựng lại độc lập (technical-v17 + table-sessions)
  xác nhận kịch bản PASS và số phiên `base+4`, 0 phiên mở trên B0005–B0008.
- Bug: [bug-report.md](bug-report.md) (T34-01).
- **Sau sửa (17/11/2026, migration 035, schema 035):** technical run
  `runs/technical_20261117_020349_e6dedcdf` — typed + native smoke `Success=true`;
  regression `runs/acceptance_20261117_020633_de5483fe` — 5/5 SQL subset PASS,
  typed smoke `Success=true`, unit 23/23 PASS, `SourceUnchanged=true`.
- Concurrency TS04: `runs/concurrency_20261118_173648_529b4d1e/manifest.json` (DB tạm
  `CoffeeManager_Test_G04_*_ts04`, tự dọn sau PASS).
- UI 18/11/2026: app thật trên DB clone `CoffeeManager_Test_G04_20261118_174233_3319aa73_ui`
  (schema 35, DPI 120 = 125%); bằng chứng trong `runs/ui_20261118_174233_3319aa73/`.

## Kết quả theo mã

| Mã | Kết quả | Ghi chú |
|---|---|---|
| TS01 | PASS | Đơn tại quán mở đúng 1 phiên, `MaNVMo=NV001`; đơn mang đi không tạo phiên. |
| TS02 | PASS | Flush nháp tại quán mở phiên trong transaction; nháp chưa Giữ không đụng bàn. |
| TS03 | PASS | Thu tiền vẫn giữ phiên (`Dang phuc vu`, `Retained=1`, `MaHD NULL`); gọi thêm nối đúng phiên, 2 hóa đơn riêng, không gộp; trả bàn → `Trong`. |
| TS04 | PASS | Run `runs/concurrency_20261118_173648_529b4d1e/manifest.json`: hai lệnh tạo đơn chồng thực sự (A=False, B=False trước khi nhả gate); một thắng `HD20250411`, một bị 52104 (`HD20250412`); 1 đơn/1 phiên mở, link `1:B0007:NV001:open`, bên thua 0 dòng, 0 thanh toán, kho không đổi, transaction `0:0`; DB tạm đã dọn. |
| TS05 | PASS | Trả bàn khi còn đơn → 52110, `GioRa` NULL, không receipt, đơn không đổi; thu xong phiên vẫn giữ. |
| TS06 | PASS | Trả bàn phiên sạch ghi `GioRa`/`MaNVTra`, receipt `TableClose`, bàn `Trong`; replay cùng RequestId không đóng lần hai. |
| TS07 | PASS | RequestId khác → 52111; revision cũ → 52103, phiên không đổi. |
| TS08 | PASS (UI, 125%) / NOT RUN (nhánh lỗi UI) | Checkout `HD26093444` xong hiện hộp "Trả bàn ngay?" (Có/Không). Chọn **Không**: DB giữ phiên 1 mở, `Retained=1`, bàn `Dang phuc vu`. Sau đó trả bàn thủ công (TS11): phiên đóng `GioRa=2026-11-18 21:32:05`, `MaNVTra=NV001`, bàn `Trong`, receipt `TableClose`. Nhánh "đóng phiên lỗi mà tiền đã chốt" không gieo lỗi được trên UI — độ bền giao dịch do TS06/TS07 SQL phủ. Hình thức **SKIPPED**; DPI 100/150 **NOT RUN**. |
| TS09 | PASS (UI, 125%) | Tạo nháp trên Ban 02, flush qua hộp "Giữ đơn nháp?" → `HD26093445` (25.000, phiên 2). "Hủy đơn mở" + xác nhận → hiện đúng hộp "Trả bàn ngay?"; chọn **Có**: đơn `Huy`, bàn `Trong`, phiên 2 đóng `GioRa=2026-11-18 21:34:44`, `MaNVTra=NV001`, dòng món giữ nguyên (1), kho không đổi. Hình thức **SKIPPED**; DPI 100/150 **NOT RUN**. |
| TS10 | PASS | Đơn cũ `MaPhien NULL` vẫn giữ bàn bận, không backfill/sinh phiên giả; thu xong bàn `Trong`. |
| TS11 | PASS (UI, 125%) | "Trả bàn" disabled khi phiên còn đơn mở (`Chua`/`Cho`) và khi đơn vừa thu còn trên bàn; enabled khi `Retained=1` không còn đơn mở (bàn `Ban 07 · Đã trả · giữ bàn`) kèm dòng giải thích thêm món sẽ tạo hóa đơn mới trong phiên. Hộp xác nhận "Trả bàn Ban 07? Bàn sẽ trống cho khách mới." (modal có owner, nhận focus); xác nhận → `Trong`, phiên đóng, receipt `TableClose`. Ảnh/bố cục hình thức **SKIPPED**; DPI 100/150 **NOT RUN**. |
| TS12 | PASS (SQL) / NOT RUN (in lại UI) | Đếm đơn mở/dashboard/doanh thu không lệch do phiên; tổng phiên `base+4`; không phiên mở trên B0005–B0008. Sau 035, chi tiết hóa đơn trong phiên đã đọc được (`sp_InvoiceDetail` trả 27 cột, typed smoke PASS); in lại trên UI chưa chạy trong lượt. |

## Chỉnh sửa script test trong lượt (không phải source sản phẩm)

- `technical-v17.sql`: guard `filter_definition` khớp đúng dạng SQL Server lưu
  `([GioRa] IS NULL)` — trước đó so `%GioRa IS NULL%` nên báo thiếu index sai.
- `acceptance-table-sessions.sql`:
  - TS03: bỏ điều kiện vô lý "hóa đơn gốc không còn dòng" (luôn đúng → luôn THROW);
    thay bằng đếm dòng mỗi hóa đơn = 1 (chống gộp). Đã xác nhận hành vi thật trên DB
    scratch: hóa đơn gọi thêm nối phiên, 2 hóa đơn, mỗi hóa đơn 1 dòng.
  - TS12: baseline doanh thu theo cửa sổ hôm nay như `sp_ReportDashboard`;
    kiểm phiên sạch theo 4 bàn T34 thay vì toàn bộ bảng (script khác trong đợt còn
    phiên mở hợp lệ); đơn mang đi TS01 để lại cộng +1 khi đối chiếu đơn mở.

Các script test cần đưa vào commit cùng T33–T35 khi người dùng duyệt.

Kết luận: T34 **đủ điều kiện nghiệm thu** — T34-01 đã sửa và regression PASS (migration
035); TS04 PASS bằng hai connection chồng thực (18/11/2026); TS08/TS09/TS11 PASS trên
UI thật 125% (18/11/2026). Còn lại đúng một phần: nhánh "lỗi đóng phiên sau thu" không
gieo lỗi được trên UI (độ bền do TS06/TS07 SQL phủ) và phần in lại của TS12 mới PASS ở
mức typed smoke, chưa bấm in lại trên UI.

## Đợt kiểm T34-02 — nút "Trả bàn" bị che (20/11/2026)

Môi trường: Windows 11, DPI hệ thống 125% (`DeviceDpi=120`), source worktree sau fix
`OrderPanel.OnLayout`; app CafeManager (PID 2552) và Visual Studio đang mở nên bin
Debug bị khóa — build/probe dùng `BaseOutputPath` tạm.

| Ca | Kết quả | Bằng chứng |
|---|---|---|
| T34-02-B | PASS (compile) | `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -p:BaseOutputPath=<temp>`: 0 error; chỉ cảnh báo WebView2/WindowsBase có sẵn. Build solution vào bin chuẩn bị chặn bởi file khóa của app/VS. |
| T34-02-L345 | PASS | Probe layout thật `OrderPanel` (reflection, CreateControl + PerformLayout) rộng 345: nút đầu `[0,55]`, "Trả bàn" `[65,169]`, "Hủy đơn mở" `[179,315]` = inner; không chồng, nút cuối sát phải. |
| T34-02-L420 | PASS | Rộng 420: `[0,130]`, `[140,244]`, `[254,390]`; không chồng, nút cuối sát phải. |
| T34-02-L300 | PASS | Rộng 300 (biên): `[0,10]`, `[20,124]`, `[134,270]`; không chồng (nút đầu co còn 10 px). |
| T34-02-UI | NOT RUN | Không thể chạy app bản mới: bin Debug đang bị Visual Studio + CafeManager.WinForms (PID 2552) giữ. Cần đóng app, build/publish lại rồi kiểm thủ công: nút "Trả bàn" hiện cạnh "Hủy đơn mở". |
| T34-02-REG | NOT RUN | Regression checkout/hủy + hộp "Trả bàn ngay?" trên UI chưa chạy vì cùng lý do khóa bin; logic `CloseTable`/`FinishCheckout` không đổi trong lượt này. |

Bằng chứng: `runs/t34-02-layout-20261120/probe.json`, `build.log`.
Probe: `C:\Users\Lenovo\.qoder\tmp\cm-layout-probe` (ngoài repo, chỉ đọc source).

Ghi nhận (không phải FAIL): ở bề rộng panel cố định 345 logic, nút "F3 · Khách / Ưu đãi"
bị co còn ~55 px (ellipsis). Chấp nhận được nhưng là điểm cần cân nhắc nếu người dùng
muốn giữ đủ nhãn.
