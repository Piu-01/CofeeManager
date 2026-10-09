---
name: task-approval-workflow
description: >
  Duyệt requirements nhóm G01–G08; viết đủ plan rồi duyệt cả bộ một lần,
  triển khai tuần tự, chờ duyệt đợt test riêng. Bug đặt trong task thuộc nhóm,
  duyệt cả đợt bug trước debug/regression. Giữ nguyên checklist T01–T32.
---

# Quy trình 2 bước duyệt trước khi làm

## Quy trình CoffeeManager theo nhóm — áp dụng từ 08/10/2026

Quy trình này thay các điểm duyệt theo task riêng bên dưới cho backlog T01–T32:

1. Đọc kế hoạch, dùng bảng nhóm G01–G08. Không đổi nội dung, phạm vi, phụ thuộc,
   đầu ra hoặc checklist của task con.
2. Viết `docs/tasks/gXX-<slug>/requirements.md` cho nhóm lớn; chờ duyệt một lần.
   Requirements tham chiếu các task con nguyên bản, không rút gọn tiêu chí nghiệm thu.
3. Viết đủ `docs/tasks/gXX-<slug>/tXX-<slug>/plan.md` của nhóm; `plans.md` là
   chỉ mục. Mỗi plan có bước/file/contract/migration/fixture/ca nghiệm thu.
   Chờ duyệt cả bộ plan một lần, không xin duyệt plan task con riêng.
4. Duyệt bộ plan → triển khai từng task theo thứ tự phụ thuộc. Đầu ra task trước
   dùng cho task sau; chưa ghi PASS từ việc triển khai. Blocker phụ thuộc phải báo.
5. Triển khai xong → chờ duyệt đợt test một lần → test từng task với source cố
   định → lưu test-results cạnh plan → báo cáo chung ở `report.md` của nhóm,
   gồm PASS/FAIL/BLOCKED/NOT RUN. Không sửa bug trong đợt test.
6. Bug đặt tại `tXX-<slug>/bug-report.md`, liên kết trong report nhóm; chờ duyệt
   cả đợt bug một lần trước debug. Duyệt debug bao gồm regression theo ảnh hưởng.
   Bug mới ngoài đợt đã duyệt cần duyệt bổ sung. Không báo cáo hoàn thành từng task.
7. Giữ phạm vi/checklist, hỏi khi thiếu dữ liệu/manual hoặc đổi scope. T01/T02
   đã có kết quả được kế thừa; không chạy lại chỉ vì đổi cách nhóm/đường dẫn.

Hồ sơ requirements task con đã tồn tại được giữ làm lịch sử/tham chiếu, không
còn là điểm duyệt riêng. Không xin lại duyệt chỉ vì đổi sang quy trình nhóm.

Các bước dưới đây chỉ dùng cho yêu cầu lớn độc lập chưa có nhóm/backlog tương ứng.

## Khi nào áp dụng

- Task lớn/phức tạp: nhiều bước, ảnh hưởng nhiều file, tính năng mới, refactor,
  hoặc yêu cầu còn mơ hồ ở chi tiết triển khai.
- Không áp dụng cho: sửa lỗi 1 dòng, đổi 1 tham số/giá trị, typo, câu hỏi thuần
  không cần code. Những việc này làm luôn, không cần file requirements/plan.

Nếu không chắc task thuộc loại nào, hỏi người dùng 1 câu ngắn thay vì tự đoán.

## Bước 1 — Viết file requirements

1. Tạo thư mục `/docs/tasks/<slug>/` với `<slug>` là tên ngắn gọn kebab-case mô tả task
   (ví dụ task "thêm hệ thống inventory cho player" → `/docs/tasks/inventory-system/`).
2. Viết file `/docs/tasks/<slug>/requirements.md` gồm:
   - **Mục tiêu**: task này để làm gì, giải quyết vấn đề gì.
   - **Phạm vi**: cụ thể sẽ làm gì, và nêu rõ KHÔNG làm gì (out of scope) nếu có.
   - **Giả định**: những điểm người dùng nói mơ hồ mà AI đang tự hiểu theo cách nào.
   - **Câu hỏi mở** (nếu có điểm thực sự cần người dùng trả lời mới làm được).
   - **Tiêu chí hoàn thành**: làm sao biết task đã xong/đúng.
3. Dừng lại, báo cho người dùng biết file đã sẵn sàng ở đâu, và chờ xác nhận.
   KHÔNG viết plan hay code ở bước này.

## Bước 2 — Chờ xác nhận requirements

- Nếu người dùng yêu cầu sửa → sửa lại file requirements.md, gửi lại, chờ xác nhận tiếp.
- Nếu người dùng xác nhận đúng ý → sang bước 3.
- Không tự suy diễn "chắc đúng rồi" để bỏ qua bước chờ này.

## Bước 3 — Viết file plan

1. Viết file `/docs/tasks/<slug>/plan.md` dựa trên requirements đã được duyệt, gồm:
   - Danh sách các bước thực hiện theo thứ tự, mỗi bước ngắn gọn cụ thể.
   - Với mỗi bước: nêu rõ file/khu vực nào sẽ bị động tới.
   - Với mỗi bước: tiêu chí verify (làm sao biết bước đó xong đúng).
2. Dừng lại, chờ người dùng duyệt plan. KHÔNG bắt đầu code ở bước này.

## Bước 4 — Chờ duyệt plan, rồi mới thực hiện

- Người dùng yêu cầu sửa plan → sửa lại, gửi lại, chờ duyệt tiếp.
- Người dùng duyệt → bắt đầu thực hiện đúng theo plan đã duyệt.
- Nếu trong lúc làm phát sinh thứ ngoài plan (cần đổi hướng, thêm bước mới phát
  sinh không nhỏ) → dừng lại, cập nhật plan.md, báo người dùng duyệt lại phần
  phát sinh đó trước khi tiếp tục — không tự ý mở rộng phạm vi.
