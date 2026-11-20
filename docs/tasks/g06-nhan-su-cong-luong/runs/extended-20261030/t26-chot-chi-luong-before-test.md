# T26 — Kết quả nghiệm thu

30/10/2026. Source đã triển khai; **chờ người dùng duyệt đợt test G06**. Không lấy smoke kỹ thuật làm PASS ca gốc. SDK10.0.401; migrations022–024; nguồn chưa commit. Môi trường/source hash/lệnh/dữ liệu/bằng chứng của đợt nghiệm thu sẽ ghi khi thực sự chạy. Hình thức do người dùng kiểm (SKIPPED); DPI100/150%NOT RUN, không đổi Windows Scale.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T26-K01 | [I] SQL tính full-time = lương tháng + phụ cấp/tăng ca/thưởng − khấu trừ; part-time = giờ thường×giá + khoản còn lại; đối chiếu dữ liệu kỳ mẫu bằng kết quả tính tay. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K02 | [I] SQL không tính giờ tăng ca đồng thời như giờ thường; preview SQL và ThucLinh DB làm tròn khớp. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K03 | [I] Ca thiếu giờ ra/overlap phải xử lý trước chốt; không tự im lặng tính 0 rồi chốt. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K04 | [I] Tháng 0/13, năm sai, tiền/giờ âm hoặc khấu trừ vượt tổng bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K05 | [I] Hai lần/2 session chốt cùng nhân viên/kỳ chỉ có một BANGLUONG. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K06 | [I] Snapshot mức lương/loại hợp đồng/giờ/duyệt giữ nguyên sau đổi hồ sơ nhân viên. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K07 | [I] UPDATE/DELETE bảng lương chốt và sửa CC kỳ chốt qua app bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K08 | [I] Chi lương đúng thực lĩnh tạo một phiếu chi gắn MaBL; chi lần hai bị chặn ở DB. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K09 | [I] Lỗi giữa ghi chi/liên kết rollback, không phát sinh chi rác hoặc trạng thái đã trả giả. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K10 | [I] Thực lĩnh 0 không tạo phiếu SoTien=0; UI hiển thị không phát sinh chi theo quy tắc. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K11 | [I] Bảng lương tạo nhưng chưa chi không được gọi là đã trả; báo cáo không cộng lương snapshot và phiếu chi hai lần. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T26-K12 | [U/M] Chỉ ADMIN chốt/chi; timeout đối chiếu MaBL/phiếu chi trước thử lại, reload đúng. | Chưa chạy đợt nghiệm thu | NOT RUN |
