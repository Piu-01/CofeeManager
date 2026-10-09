---
name: bug-investigation-workflow
description: >
  Dùng skill này khi người dùng báo rằng kết quả sau khi thực hiện task bị lỗi,
  chạy không đúng, hoặc không như mong đợi. KHÔNG tự ý sửa code ngay khi nghe báo lỗi —
  phải điều tra và xin thêm thông tin nếu thiếu. Lỗi độc lập cần duyệt hướng sửa;
   bug thuộc nhóm ghi vào task tương ứng, tổng hợp cả đợt để người dùng duyệt
   một lần trước debug/regression, không tự sửa trong đợt test.
---

# Quy trình điều tra lỗi trước khi sửa

## Lỗi nội bộ trong nhóm CoffeeManager G01–G08

Theo quy trình người dùng chốt lại ngày 08/10/2026, bug phát hiện trong nhóm
ghi tại `docs/tasks/gXX-<slug>/tXX-<slug>/bug-report.md`. Điều tra bằng source/log
có sẵn, ghi hiện tượng/nguyên nhân đã chứng minh hoặc giả thuyết/hướng sửa/ca
regression. Giữ source cố định và kết quả FAIL trong đợt test; tiếp tục ca độc
lập, ghi BLOCKED nếu phụ thuộc bug. Báo cáo chung sau đợt test, liên kết mọi bug.
Chờ duyệt cả đợt bug một lần rồi debug lần lượt và regression theo ảnh hưởng.
Không xin duyệt từng fix đã thuộc đợt; bug phát sinh mới chưa được duyệt và đổi
scope/contract phải xin duyệt bổ sung. Reproduction có chạy test mới cũng cần
thuộc đợt test/debug đã duyệt. Không vá khi chưa rõ nguyên nhân.

## Nguyên tắc

Khi người dùng báo lỗi, KHÔNG được:
- Tự ý đoán nguyên nhân rồi sửa ngay.
- Sửa nhiều chỗ "cho chắc" dù chưa xác định rõ nguyên nhân.
- Tự bịa ra thông tin còn thiếu (log, giá trị cụ thể, bước tái hiện) để lấp chỗ trống
  cho việc phân tích — nếu thiếu thì phải hỏi.

## Bước 1 — Điều tra

Dùng những gì đang có sẵn để xác định nguyên nhân trước khi hỏi thêm:
- Đọc lại code vừa thay đổi liên quan đến phần bị lỗi.
- Đọc log/console output nếu có sẵn.
- Thử tái hiện lỗi nếu có thể tự làm được (chạy test, chạy lệnh, gọi tool đọc trạng thái...).

## Bước 2 — Xin thêm thông tin nếu còn thiếu

Nếu chưa đủ để xác định chính xác nguyên nhân, dừng lại và hỏi người dùng cung cấp
cụ thể, ví dụ:
- Log/error message đầy đủ, ảnh chụp màn hình.
- Bước tái hiện lỗi (làm gì, theo thứ tự nào, kết quả mong đợi vs thực tế).
- Input/giá trị cụ thể gây ra lỗi.
- Môi trường/điều kiện xảy ra (luôn luôn hay thỉnh thoảng, ở đâu).

Chỉ hỏi những gì thực sự cần để xác định nguyên nhân — không hỏi tràn lan.
Sau khi có thêm thông tin, quay lại Bước 1 nếu cần, lặp lại tới khi xác định
được nguyên nhân rõ ràng, có dẫn chứng cụ thể (dòng code, dòng log...).

## Bước 3 — Viết báo cáo

Không sửa code ở bước này. Viết file:
- Nếu lỗi thuộc task trong nhóm: thêm file
  `/docs/tasks/gXX-<slug>/tXX-<slug>/bug-report.md`.
- Nếu là lỗi độc lập, không gắn với task cụ thể: tạo
  `/docs/bugs/<slug-mo-ta-loi>/report.md`.

Nội dung báo cáo gồm:
- **Hiện tượng**: lỗi biểu hiện ra sao, khi nào xảy ra.
- **Nguyên nhân**: root cause cụ thể, kèm dẫn chứng (trích dòng code/log liên quan).
- **Cách sửa đề xuất**: sửa gì, ở đâu, phạm vi ảnh hưởng bao nhiêu.
- **Tối ưu (nếu có)**: nếu trong lúc điều tra phát hiện chỗ có thể tối ưu nhưng
  không phải nguyên nhân gây lỗi, ghi riêng mục này và nêu rõ đây là optional,
  không bắt buộc phải làm chung với fix lỗi.
- **Rủi ro**: cách sửa này có ảnh hưởng gì tới phần khác không.

## Bước 4 — Chờ duyệt rồi mới sửa

- Gửi báo cáo, dừng lại chờ người dùng xem và xác nhận.
- Người dùng yêu cầu chỉnh báo cáo/hướng sửa → cập nhật lại, gửi lại, chờ duyệt tiếp.
- Người dùng đồng ý → chỉ sửa đúng phạm vi đã nêu trong báo cáo, không lan sang
  chỗ khác không liên quan.
- Nếu sau khi sửa vẫn còn lỗi hoặc phát sinh lỗi mới → quay lại Bước 1, không tự
  vá chồng thêm nhiều lớp sửa khi chưa rõ nguyên nhân mới.
