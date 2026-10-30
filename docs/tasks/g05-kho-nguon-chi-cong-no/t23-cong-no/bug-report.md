# T23 — Hồ sơ bug B03 và B07

## G05-B07 — Đăng xuất từ Công nợ gọi Cancel trên CTS đã dispose

30/10/2026. **Đã duyệt, sửa và regression PASS theo phạm vi.** Nội dung điều tra/đề xuất dưới đây được giữ lịch sử; B03 bên dưới là lịch sử đã sửa.

Kết quả B07: đúng năm control dùng StopLoading đặt loading=null trước Cancel/Dispose, tăng generation để bỏ kết quả cũ; chặn tải/cập nhật khi disposing/disposed hoặc session revoked. Debug/Release0warning/0error,22unit/47lifecycle/5CASH-BLL PASS; native đóng lịch sử nợ→logout→CASH login/logout→ADMIN login lại và đóng Thu chi không lỗi. Counts/receipt/tồn giữ nguyên, DB test dọn1/1; source cố định. [Biên bản](../runs/b07-20261030/test-results.md). Không suy PASS native keyboard hay55ID toàn nhóm.

Hiện tượng: trong phiên native ADMIN, trả CN20250405 hai lần10000 và11000, xem lịch sử và hai phiếu chi nguồn, đóng các dialog rồi Đăng xuất. Login xuất hiện cùng hộp Unhandled exception: `System.ObjectDisposedException: The CancellationTokenSource has been disposed.` Stack chỉ đúng `SupplierDebtsControl.cs:21`, handler VisibleChanged gọi CancellationTokenSource.Cancel. Bằng chứng: `../runs/native-followup-20261029/logout-exception-details.txt`; các bước trước đó trong `test-results.md` cùng run.

Nguyên nhân có bằng chứng: Dispose(bool) của control tăng generation, Cancel rồi Dispose loading nhưng giữ tham chiếu; sau đó base.Dispose làm control đổi Visible. Handler ẩn ở dòng21 gọi lại loading?.Cancel() trên đối tượng đã dispose. LoadRows cũng Cancel/Dispose tham chiếu hiện tại trước try. Chỉ Công nợ đã tái hiện lỗi native; bốn control Thu chi/Nhập kho/Xuất kho/Kiểm kê có cùng mẫu source, chưa ghi FAIL native cho chúng.

Phạm vi đề xuất cho đợt B07: sửa vòng đời CTS trong đúng năm control G05 `FinanceControl`, `SupplierDebtsControl`, `PurchasesControl`, `StockIssuesControl`, `StocktakesControl`. Tách tham chiếu loading về null trước Cancel/Dispose để teardown lặp an toàn, chặn tải khi Disposing/IsDisposed hoặc phiên đã hủy; giữ generation chống kết quả cũ. Kiểm event khi base.Dispose đổi Visible. Không nuốt ObjectDisposedException toàn app, không sửa SQL/DAL/quyền/contract hoặc refactor MainForm khi chưa có bằng chứng cần thiết.

Regression thuộc đề xuất: năm màn tải xong rồi chuyển màn/đăng xuất/đóng; dispose lặp và đọc đang chờ; kết quả cũ không ghi vào UI đã dispose. Đăng nhập lại ADMIN/CASH, quyền menu/BLL; giữ nháp, modal và chặn gửi lặp. Component kiểm vòng đời và native tại125%, DB test riêng; đối chiếu không phát sinh ghi tiền/kho/nợ khi đóng màn. Rủi ro cần kiểm: hủy nhầm lần tải mới hoặc bỏ kết quả refresh hợp lệ. Không thay đổi dữ liệu đã commit: SQL đối chiếu hiện tại PASS (nhập21000, xuất trả2 đơn vị, trả nợ10000+11000; còn nợ0).

## G05-B03 — Nợ chưa có lần trả làm hỏng danh sách Công nợ

> Cập nhật29/10/2026: bug trong đợt G05-B01–B04 đã duyệt, đã sửa và PASS regression theo phạm vi tại phần cuối. Nội dung27/10 dưới đây là lịch sử điều tra.

27/10/2026. **Chờ duyệt đợt bug; chưa sửa.**

ADMIN fixture mở Công nợ: hộp Unhandled exception `Data is Null`, danh sách không tải xong. Stack trace: `SupplierDebtRepository.Map`, dòng17, `SqlDataReader.GetString`.

Bằng chứng `../runs/acceptance-full-20261027_110730/gui-debt.txt`, `gui-debt-stack.txt`, `debt-null-evidence.json`. Nguồn mới hợp lệ PN/NCC được tạo bằng `sp_LapPhieuNhap`, trả ngay0; nợ CN20250429 có Original10000, Paid0, Remaining10000 và HistoryJson SQL NULL. Đây là trạng thái nghiệp vụ bình thường, không chỉ dữ liệu âm.

Nguyên nhân: mapper gọi `r.GetString(9)` trực tiếp cho HistoryJson. `Deserialize(...) ?? []` chỉ xử lý kết quả deserialize, không xử lý DBNull. Lỗi thoát qua LoadRows/event async. SQL trả nợ, quyền và số dư vẫn đạt các kiểm độc lập; chúng không thay bằng chứng native FAIL.

Đề xuất: kiểm DBNull và trả lịch sử rỗng, giữ nguyên Original/Paid/Remaining/trạng thái và các quyền. Đọc ngày cutoff có lịch sử rỗng cũng phải an toàn. Không ghi một lần trả giả, không trả trước tiền để né lỗi, không bắt mọi exception rồi trả số dư0.

Regression: nợ mới chưa trả; nhiều lần trả; đã trả hết; hiện tại/tại ngày; ADMIN trả và link phiếu chi; STORE chỉ xem; CASH bị chặn; timeout đọc receipt. T23-K11/K12 hiện FAIL, cần kiểm lại native sau sửa. Rủi ro: không được đổi NULL tiền/ngày thành0 giả; chỉ chuẩn hóa tập lịch sử rỗng.

## Sửa và regression29/10 — B03 PASS theo phạm vi

Mapper kiểm IsDBNull(9) trước deserialize. Typed SQL integration: nợ10000/Paid0/Remaining10000 có lịch sử0; trả1000 còn9000/lịch sử1; as-of trước lần trả vẫn lịch sử0/nợ10000; trả hết có hai dòng/còn0. STORE/CASH gọi trả đều51605. Native ADMIN mở danh sách72 nguồn và lịch sử nợ chưa trả CN20250467 không crash; link PN20250564 mở dialog lồng đúng cây owner, dữ liệu đúng và đóng về hộp cha. Bằng chứng `../runs/debug-batch-20261029/db-regression.json`, `native-debt-list.txt`, `native-debt-detail.txt`, `native-source-link.txt`. Chưa chạy lại đầy đủ T23-K11/K12; footer B05 đang chặn phần tiếp cận nhiều hành động và cần duyệt riêng.
