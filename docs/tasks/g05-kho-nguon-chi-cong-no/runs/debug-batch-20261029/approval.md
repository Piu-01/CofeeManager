# Duyệt debug G05-B01–B04 — 29/10/2026

Người dùng trả lời “duyệt” sau đề nghị sửa cả bốn bug và regression theo ảnh hưởng trong report.md ngày27/10. Cho phép sửa workspace G05 và mapper NULL, công cụ đối chiếu nguồn tài chính; không đổi nghiệp vụ/SQL migration017–021. Regression dùng SQL Server/DB test riêng, không ghi fixture vào development; giữ backup. Không commit. Nguồn trước sửa tại source-before-debug.csv. Các lần probe nguyên nhân là pha debug; lượt regression source cố định bắt đầu sau khi sửa xong và lưu source-after-debug.csv.

Áp dụng các skill cục bộ bug-investigation-workflow, karpathy-guidelines, post-change-test-workflow. Duyệt debug đã bao gồm regression bốn bug; không hỏi lại từng ca.

B05 phát hiện trong regression: footer dialog cắt nút, đã ghi tại bug-report T20. Chưa sửa B05; chờ duyệt bổ sung, không suy duyệt B01–B04 thành duyệt bug mới.

