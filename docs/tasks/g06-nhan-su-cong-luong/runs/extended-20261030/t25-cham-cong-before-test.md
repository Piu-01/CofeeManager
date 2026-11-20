# T25 — Kết quả nghiệm thu

30/10/2026. Source đã triển khai; **chờ người dùng duyệt đợt test G06**. Không lấy smoke kỹ thuật làm PASS ca gốc. SDK10.0.401; migrations022–024; nguồn chưa commit. Môi trường/source hash/lệnh/dữ liệu/bằng chứng của đợt nghiệm thu sẽ ghi khi thực sự chạy. Hình thức do người dùng kiểm (SKIPPED); DPI100/150%NOT RUN, không đổi Windows Scale.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T25-K01 | [I/U] Giờ vào/ra hợp lệ cùng ngày và qua đêm cho thời lượng đúng. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K02 | [I] Giờ ra không có giờ vào, ra≤vào, ngày vào khác NgayLam bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K03 | [I] Nghỉ phép/Vắng có giờ bị chặn; đang làm chưa ra được lưu nhưng không tự tính thành kỳ hoàn tất. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K04 | [I] Cùng nhân viên trùng MaCa/NgayLam bị PK chặn; khác ca chồng khoảng giờ bị procedure chặn. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K05 | [I] Ca kết thúc đúng lúc ca sau bắt đầu được phép, không coi là overlap. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K06 | [I] Hai session cùng nhập ca chồng giờ không cùng vượt kiểm tra. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K07 | [I] Nhân viên đã nghỉ/ca không tồn tại bị chặn thao tác mới; dữ liệu cũ đọc được. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K08 | [I] Chỉnh chấm công kỳ đã chốt lương bị chặn theo quy trình. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K09 | [U/I] Phân kỳ theo ngày làm đã chọn, qua đêm cuối tháng không bị cộng hai lần. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K10 | [M] Bộ lọc tháng/nhân viên/trạng thái và reload đúng; quyền ADMIN, không quyền kế thừa từ chức vụ. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T25-K11 | [I] Một batch nhiều nhân viên có dòng lỗi rollback theo phạm vi batch đã công bố. | Chưa chạy đợt nghiệm thu | NOT RUN |
