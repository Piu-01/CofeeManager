# Kết quả kiểm thử — customer-point-history (CP-01..CP-09)

## Môi trường

- Ngày chạy: 21/11/2026, một lượt liên tục (đợt test gộp CP + BR-01 đã được duyệt).
- Source: worktree trên commit `f35a894` (chưa commit; `OperationEditorDialog`, `CustomersControl`,
  `LoyaltyControl`, `Program`, các file BLL/DAL/Models của CP đã áp dụng trong worktree).
- Build: `dotnet build src/CafeManager.WinForms -c Debug`, **0 error** (1 warning có sẵn),
  output `C:\Users\Lenovo\.qoder\tmp\cm-build-cph`.
- DB: SQL Server `localhost`, DB test riêng `CoffeeManager_Test_RG_20261121_b_ui`,
  schema/procedure của baseline 008 + migrations đến 032.
- Tài khoản: `admin/Admin@123`, `thungan/Cash@123`. DPI 125%.
- Điều khiển UI: UIA (`tools/database/G04Ui.ps1`, bản tmp `G04Ui2.ps1` lọc cửa sổ hiển thị)
  và đọc text Win32 (`Dump-RawChildren.ps1`) vì tên UIA bị `AccessibleName` che.
- Bằng chứng: `runs/2026-11-21/cph-*.txt` (dump UIA/Win32 của từng bước).

## Kết quả

| Mã | Nội dung | Kết quả | Ghi chú / bằng chứng |
|---|---|---|---|
| CP-01 | Tab loyalty ADMIN chỉ còn Khuyến mãi + Hạng thành viên; không còn tab Điểm khách hàng | PASS | `cph-cp01-admin-tabs.txt`, `cph-loyalty.txt` |
| CP-02 | CASH không thấy màn Ưu đãi / Điểm, không có tab hạng | PASS | `cph-cp02-cash.txt`, `cph-cash-main.txt` |
| CP-03 | Màn Khách hàng ADMIN có action “Lịch sử điểm”, không còn “Hóa đơn” | PASS | `cph-cp03-admin-customers.txt` |
| CP-04 | “Lịch sử điểm” của KH003 mở dialog chỉ đọc, note mã/điểm/hạng, danh sách 50 dòng/trang, thứ tự MaGD giảm dần khớp DB (1380, 1350, 1331) | PASS | `cph-cp04-history-dialog.txt`, `cph-cp04b.txt`, `cph-cp04c.txt` |
| CP-05 | Khách chưa có giao dịch điểm hiển thị trạng thái rỗng, không lỗi | PASS (một phần) | Tạo KH023 “KH Test CP” (receipt `CustomerCreate`), dialog lịch sử mở được. Trạng thái rỗng là control vẽ `OrbitEmptyState`, không đọc được text qua UIA — không xác nhận được câu chữ hiển thị. `cph-cp05-*.txt` |
| CP-06 | Không chọn dòng thì action Lịch sử điểm không làm gì | NOT RUN (không đạt được trạng thái) | Màn Khách hàng tự chọn dòng đầu sau khi tải (hành vi có từ trước, giống action Hóa đơn cũ); không tạo được trạng thái “không chọn dòng”. `cph-cp06-*.txt` |
| CP-07 | Khách hàng CASH không có action Lịch sử điểm / Mở sổ điểm, chỉ Thêm | PASS | `cph-cp07-cash-customers.txt` |
| CP-08 | Không còn mã nguồn chức năng điểm khách hàng / điều chỉnh điểm | PASS | `grep` toàn `src/`: không còn `PointList`, `PointPreviewAsync`, `AdjustAsync`, `PointAdjustmentRequest`, `CustomerOrderList`, `OrdersAsync`, chuỗi “Điểm khách hàng”; build 0 error |
| CP-09 | Các action còn lại của màn Khách hàng không hồi quy | PASS | Sửa khách mở đúng dữ liệu, Hủy đóng sạch; Mở sổ điểm mở dialog, Hủy đóng sạch. `cph-cp09-*.txt` |

## Ghi chú dữ liệu sau lượt

- Hạng: `DONG 0/0% | BAC 2000/3% | PLAT 6000/6% | KIM 30000/8%` (trở về trạng thái trước đợt
  sau khi BR01-R4 xóa hạng TST tạo bởi BR01-R1..R3).
- Dữ liệu do đợt test tạo và **giữ lại** trong DB test: khách `KH023` (0 điểm),
  khuyến mãi `KM20261121`, nguyên liệu `NVL99`.
- Receipt mới trong lượt (17:00 trở đi): `CustomerCreate KH023`, 4 × `PolicyReplace POLICY`,
  `PromotionCreate KM20261121`, `MaterialCreate NVL99`; tổng bảng 3527 dòng.

## Hạn chế

- CP-05: câu chữ trạng thái rỗng không kiểm được qua UIA (control tự vẽ); mới xác nhận
  danh sách 0 dòng và không lỗi.
- CP-06: app tự chọn dòng đầu, trạng thái không-chọn không tái hiện được.
- Trong lượt, tiến trình app tự đóng 3 lần khi thao tác tự động (không có sự kiện crash
  CafeManager trong Event Log); đã mở lại và chạy tiếp, không ca nào phải chạy lại vì dữ liệu sai.
- Hình thức giao diện: SKIPPED theo phân công (người dùng tự kiểm).
