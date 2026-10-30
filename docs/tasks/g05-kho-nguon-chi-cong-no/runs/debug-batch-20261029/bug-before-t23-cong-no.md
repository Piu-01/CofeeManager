# G05-B03 — Nợ chưa có lần trả làm hỏng danh sách Công nợ

27/10/2026. **Chờ duyệt đợt bug; chưa sửa.**

ADMIN fixture mở Công nợ: hộp Unhandled exception `Data is Null`, danh sách không tải xong. Stack trace: `SupplierDebtRepository.Map`, dòng17, `SqlDataReader.GetString`.

Bằng chứng `../runs/acceptance-full-20261027_110730/gui-debt.txt`, `gui-debt-stack.txt`, `debt-null-evidence.json`. Nguồn mới hợp lệ PN/NCC được tạo bằng `sp_LapPhieuNhap`, trả ngay0; nợ CN20250429 có Original10000, Paid0, Remaining10000 và HistoryJson SQL NULL. Đây là trạng thái nghiệp vụ bình thường, không chỉ dữ liệu âm.

Nguyên nhân: mapper gọi `r.GetString(9)` trực tiếp cho HistoryJson. `Deserialize(...) ?? []` chỉ xử lý kết quả deserialize, không xử lý DBNull. Lỗi thoát qua LoadRows/event async. SQL trả nợ, quyền và số dư vẫn đạt các kiểm độc lập; chúng không thay bằng chứng native FAIL.

Đề xuất: kiểm DBNull và trả lịch sử rỗng, giữ nguyên Original/Paid/Remaining/trạng thái và các quyền. Đọc ngày cutoff có lịch sử rỗng cũng phải an toàn. Không ghi một lần trả giả, không trả trước tiền để né lỗi, không bắt mọi exception rồi trả số dư0.

Regression: nợ mới chưa trả; nhiều lần trả; đã trả hết; hiện tại/tại ngày; ADMIN trả và link phiếu chi; STORE chỉ xem; CASH bị chặn; timeout đọc receipt. T23-K11/K12 hiện FAIL, cần kiểm lại native sau sửa. Rủi ro: không được đổi NULL tiền/ngày thành0 giả; chỉ chuẩn hóa tập lịch sử rỗng.
