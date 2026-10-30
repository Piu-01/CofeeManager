# Đợt bổ sung sau regression24/10/2026 — chờ duyệt

Năm lỗi bộ kiểm thử được người dùng duyệt ngày24/10 đã sửa trong v2 và chạy regression.
Các lỗi/phát hiện mới dưới đây chưa thuộc đợt đó, chưa sửa:

| Mã | Bằng chứng | Phần đề nghị duyệt |
|---|---|---|
| G04-TEST-06 | [T18 bug](t18-giao-hang/bug-report.md), raw102/102 nhưng PASS | Sửa fixture revision typed và validator Pair; negative control phải FAIL; chạy lại toàn bộ cặp concurrency |
| G04-CONC-01 | [T15 bug](t15-checkout/bug-report.md), deadlock graph1205 giữa khóa kho/HOADON | Điều tra actual plan/lock rồi sửa tối thiểu lock scope nếu xác nhận; migration016 khi cần, regression tiền/kho/điểm/closed protection |

Không sửa checksum001–015, không thay expected để nhận mọi lỗi, không NOLOCK hoặc
retry tiền tự động. Giữ nguyên v1/v2, ghi run mới và dọn DB có marker. Điểm/số dư hiện
chưa thấy sai; vấn đề mới là deadlock trong ca vốn expected thiếu kho. Nếu chấp nhận
rollback1205 như contract concurrency, cần quyết định rõ thay cho kết luận giả52203.

GUI tiếp tục BLOCKED ở click/focus dù accessibility/SetValue đã hoạt động sau cấp
quyền. Không có lỗi GUI sản phẩm đã chứng minh để sửa. Chuẩn bị kiểm tiếp bằng công
cụ khi thao tác được hoặc người dùng thao tác chức năng còn thiếu; hình thức vẫn
do người dùng tự kiểm. Không đổi Windows Scale, không dùng TEST login làm chứng cứ.

Gate bổ sung theo [bug-investigation-workflow](../../../.agents/skills/bug-investigation-workflow/SKILL.md):
“bug phát sinh mới chưa được duyệt và đổi scope/contract phải xin duyệt bổ sung.”
