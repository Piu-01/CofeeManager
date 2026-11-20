# T26 — Kết quả nghiệm thu G06

Đợt được duyệt30/10/2026, tổng hợp31/10/2026. SDK10.0.401; SQL Server localhost, baseline+seed/migrations001–024; DB test riêng có marker. Source chưa commit, SHA256 trước/sau không đổi (src/database/tests/tools). Không nâng development. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN; app component thực DeviceDpi120.

Bằng chứng/lệnh/input/expected/actual chi tiết: [runner acceptance](../runs/acceptance_20261030_155806_b10d6581/manifest.json), [extended](../runs/extended-20261030/results.json), [bổ sung](../runs/extended-20261030/supplement/results.json), [SQL followup](../runs/extended-20261030/transaction-followup-v2.json), [native trace](../runs/extended-20261030/native-trace.json), [audit](../runs/extended-20261030/final-audit.json). Các script/project đặt cạnh kết quả để tái hiện. Expected gốc dưới đây giữ nguyên.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T26-K01 | [I] SQL tính full-time = lương tháng + phụ cấp/tăng ca/thưởng − khấu trừ; part-time = giờ thường×giá + khoản còn lại; đối chiếu dữ liệu kỳ mẫu bằng kết quả tính tay. | SQL fulltime10.800.000, parttime1.600.000 đúng tính tay; no-prorating và NV đã nghỉ kỳ lịch sử có kiểm bổ sung. Phạm vi tiền lớn bị bug chung, không suy PASS cho biên đó. | PASS |
| T26-K02 | [I] SQL không tính giờ tăng ca đồng thời như giờ thường; preview SQL và ThucLinh DB làm tròn khớp. | Thường36/tăngca4 không nhân lặp;0,5VND→1; cộng36giây một lần→0,01h→1VND, không làm tròn từng dòng. | PASS |
| T26-K03 | [I] Ca thiếu giờ ra/overlap phải xử lý trước chốt; không tự im lặng tính 0 rồi chốt. | Thiếu cảhai/thiếu ra và overlap dữ liệu fixture đều hiện issue, không commit được. | PASS |
| T26-K04 | [I] Tháng 0/13, năm sai, tiền/giờ âm hoặc khấu trừ vượt tổng bị chặn. | Tháng0/13,năm1999,âm/lẻ/precisiongiờ/khấu trừvượt tổng bị chặn; SQLmonth13 kiểm riêng. | PASS |
| T26-K05 | [I] Hai lần/2 session chốt cùng nhân viên/kỳ chỉ có một BANGLUONG. | Hai session chốt cùng kỳ:2waiters/đúng1success/1BANGLUONG; replay request và mismatch được kiểm. | PASS |
| T26-K06 | [I] Snapshot mức lương/loại hợp đồng/giờ/duyệt giữ nguyên sau đổi hồ sơ nhân viên. | Sau đổi tên/hợp đồng/lương, snapshot cũ giữ nguyên; owner update/delete snapshot bị trigger chặn. | PASS |
| T26-K07 | [I] UPDATE/DELETE bảng lương chốt và sửa CC kỳ chốt qua app bị chặn. | Runtime DML bị từ chối; kỳ snapshot mới chặn service insert/update/delete công; owner không sửa/xóa BANGLUONG đã chốt. RawError harness kiểm lại theo baseline transaction. | PASS |
| T26-K08 | [I] Chi lương đúng thực lĩnh tạo một phiếu chi gắn MaBL; chi lần hai bị chặn ở DB. | Hai session chi cùngMaBL:1phiếu/đúng thực lĩnh,1success/1reject; replay không tạo thêm chi. | PASS |
| T26-K09 | [I] Lỗi giữa ghi chi/liên kết rollback, không phát sinh chi rác hoặc trạng thái đã trả giả. | Fault trigger sau ghi expense trước receipt: toàn bộ rollback; không phiếu rác/trạng thái đãchi giả. | PASS |
| T26-K10 | [I] Thực lĩnh 0 không tạo phiếu SoTien=0; UI hiển thị không phát sinh chi theo quy tắc. | Service/SQL từ chối chi0, không phiếuSoTien0. Text/trạng thái và khóa action native chưa kiểm được. | BLOCKED |
| T26-K11 | [I] Bảng lương tạo nhưng chưa chi không được gọi là đã trả; báo cáo không cộng lương snapshot và phiếu chi hai lần. | Unpaid/Paid tách biệt, SUMphiếu cho hai bảng lương chỉ10.800.000, không cộng snapshot lầnhai. | PASS |
| T26-K12 | [U/M] Chỉ ADMIN chốt/chi; timeout đối chiếu MaBL/phiếu chi trước thử lại, reload đúng. | Role SQL và BLL từ chối đúng; timeout trướccommit khôngghi/khôngreceipt; timeout saucommit đối chiếu thấy1phiếu rồi replay antoàn. PendingDPAPI/restart/scopedDB/corrupt kiểm được; native unknown/reload/ownerlồng chưa đủ. | BLOCKED |

Lịch sử trước nghiệm thu được giữ tại runs/extended-20261030; không đổi expected để né lỗi. PASS là phạm vi ca gốc đã kiểm ở tầng được nêu; không suy native/hình thức PASS từ service/component. Các nhánh native chưa chạy giữ BLOCKED. Xem [báo cáo nhóm](../report.md) để biết giới hạn và đợt bug đề nghị.
