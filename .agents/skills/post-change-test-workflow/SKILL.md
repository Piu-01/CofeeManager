---
name: post-change-test-workflow
description: >
  Dùng skill này ngay sau khi hoàn thành một task (theo task-approval-workflow)
   hoặc sau một fix. Với backlog G01–G08, chờ duyệt đợt test một lần sau triển
   khai nhóm; test từng task, lưu kết quả trong task thuộc nhóm và báo cáo chung.
   Lỗi cần duyệt cả đợt bug trước debug/regression; source cố định trong lượt test.
---

# Quy trình test sau khi thực hiện/sửa lỗi

## Khi nào áp dụng

- Ngay sau khi hoàn thành 1 task theo `task-approval-workflow`.
- Ngay sau khi áp dụng 1 fix theo `bug-investigation-workflow`.
- Không áp dụng cho các thay đổi cực nhỏ không cần verify (typo, đổi 1 giá trị không ảnh hưởng logic).

## Bước 1 — Tổng hợp phần cần test

Sau khi code hoặc migration đã được áp dụng xong trong dự án WinForms, liệt kê ra:
- **Phần nào cần test**: chức năng/luồng nào bị ảnh hưởng bởi thay đổi vừa rồi.
- **Test như thế nào**: build Debug/Release khi phù hợp, chạy xUnit, integration
  trên SQL Server thật với DB test riêng, hoặc thao tác app WinForms; ghi rõ dữ
  liệu, thứ tự thao tác và kết quả kỳ vọng. Dùng mã Txx-Kxx/UX-Kxx liên quan.
- Nếu có phần rủi ro cao hơn (ví dụ ảnh hưởng tới luồng khác không nằm trong yêu
  cầu ban đầu), nêu riêng để người dùng chú ý.

## Bước 2 — Hỏi trước khi test

**Backlog CoffeeManager G01–G08:** sau triển khai toàn bộ plan nhóm, chờ người
dùng duyệt đợt test một lần rồi test từng task theo plan. Duyệt plan không là
duyệt test. Duyệt đợt debug bao gồm regression đúng phạm vi các bug đã duyệt,
không hỏi lại từng ca. Thiếu GUI/dữ liệu/quyền thì gom yêu cầu manual và hỏi;
không báo PASS khi chưa chạy.

Trình bày danh sách ở Bước 1, hỏi người dùng có muốn tiến hành test ngay không.
Dừng lại, không tự động chạy test khi chưa có xác nhận.

- Người dùng từ chối/muốn sửa lại phạm vi test → điều chỉnh theo ý người dùng.
- Người dùng đồng ý → sang Bước 3.

## Bước 3 — Thực hiện test

Test đầy đủ các phần đã liệt kê. Trong lúc test:

- Dùng lệnh build/test, test runner, công cụ thao tác/quan sát WinForms, đọc log,
  chụp màn hình và truy vấn đối chiếu SQL. Fixture được tạo/dọn dữ liệu đúng phạm
  vi đã duyệt trong DB test riêng; không chạy thử ghi dữ liệu vào DB sử dụng thật.
- **Tuyệt đối không sửa source, migration hoặc test để né lỗi trong lúc test**.
  Không vá tạm dữ liệu để biến FAIL thành PASS. Nếu phát hiện cần sửa gì, ghi
  nhận lại và chuyển sang điều tra lỗi, không sửa ngay tại chỗ.
- Test lần lượt hết các phần đã liệt kê, không dừng giữa chừng khi gặp lỗi nhỏ —
  ghi nhận lỗi rồi tiếp tục các phần còn lại (trừ khi lỗi chặn hẳn không test
  tiếp được).

## Bước 4 — Xử lý kết quả

- Sau đợt test, lưu kết quả và báo cáo chung của nhóm kể cả có FAIL/BLOCKED;
  không gửi báo cáo hoàn thành task con riêng.
- Lưu kết quả tại `docs/tasks/gXX-<slug>/tXX-<slug>/test-results.md` hoặc cạnh báo cáo bug độc
  lập: mã ca, môi trường/schema, dữ liệu, lệnh/thao tác, expected/actual và
  PASS/FAIL/BLOCKED/NOT RUN. Thiếu SDK, SQL instance, Designer hoặc khả năng thao
  tác GUI thì ghi rõ phần chưa chạy; không đánh dấu PASS từ việc đọc source.
- **Nếu phát hiện lỗi**: ghi FAIL, điều tra theo `bug-investigation-workflow`,
  ghi bug vào task sở hữu và tổng hợp đợt bug trong báo cáo nhóm. Chờ người dùng
  duyệt cả đợt trước debug/regression. Không tự sửa lỗi nội bộ trong lượt test;
  bug mới ngoài đợt đã duyệt cần duyệt bổ sung. Tối ưu không phục vụ nguyên nhân
  không làm kèm.
