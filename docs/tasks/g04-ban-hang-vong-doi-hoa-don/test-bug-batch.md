Cập nhật24/10/2026: người dùng đã duyệt năm lỗi bên dưới; đã sửa trong v2 và chạy regression.
Xem report.md; hai vấn đề mới nằm ở additional-bug-batch.md, không xin duyệt lại đợt cũ.

# Đề nghị duyệt đợt sửa bộ kiểm thử G04

Ngày23/10/2026. Đợt test đã được duyệt và đã chạy; đây là gate sửa lỗi bộ kiểm thử,
không hỏi duyệt lại requirements/plan/đợt test ban đầu. Chưa sửa năm lỗi sau:

| ID | Task/hồ sơ | Phần sửa cụ thể |
|---|---|---|
| G04-TEST-01 | [T15](t15-checkout/bug-report.md) | Fixture điểm đặt net999/1.000/1.999 và expected độc lập; giữ FAIL cũ |
| G04-TEST-02 | [T15](t15-checkout/bug-report.md) | Expected ngày tương lai52205 theo contract; thêm snapshot/transaction hậu lỗi |
| G04-TEST-03 | [T16](t16-pos-huy-don/bug-report.md) | Import namespace AccountIdentity, sửa lifetime PreviewPrintController, build/đóng băng fixture GUI rồi kiểm handler/keyboard thật |
| G04-TEST-04 | [T17](t17-tra-cuu-in/bug-report.md) | Dựng fixture ngày trước chốt, prefix riêng/tách dependency; không tắt trigger hoặc sửa lịch sử đã trả |
| G04-TEST-05 | [T14](t14-tao-sua-don/bug-report.md) | Giữ đầy đủ exception chain/chọn SqlException; kiểm lại timeout trước/sau commit và receipt |

Chỉ thay bộ kiểm thử ở phiên bản chạy mới; artifacts/script/hash/result đã chạy giữ
nguyên. Không đổi sản phẩm, SQL nghiệp vụ hoặc expected để né lỗi. Nếu chạy bù chứng
minh lỗi sản phẩm mới, ghi bug mới và xin duyệt bổ sung trước sửa.

Sau sửa: regression năm lỗi và chạy bù các nhánh BLOCKED/subcase NOT RUN trong năm
test-results.md theo test-plan đã duyệt. Mỗi lượt đóng băng riêng, test DB thực có
marker/dọn allowlist; không ghi vào development. Bằng chứng GUI chỉ claim đúng
fixture/principal/session được dùng; login/revocation cần session chuẩn. Nếu cần
người dùng thao tác đăng nhập sẽ chuẩn bị đến bước đó trước. Không dùng TEST bypass
để chứng nhận đăng nhập. Hình thức SKIPPED;100/150%NOT RUN; chỉ chức năng125%.

Lý do phải chờ: [skill bug-investigation-workflow](../../../.agents/skills/bug-investigation-workflow/SKILL.md)
quy định: “Chờ duyệt cả đợt bug một lần rồi debug lần lượt và regression theo ảnh hưởng.”
AGENTS.md cũng quy định không tự sửa lỗi nội bộ trong lượt test. Áp dụng cả fixture/
test script, nên không sửa ngay để đổi kết quả của đợt đã chạy.

