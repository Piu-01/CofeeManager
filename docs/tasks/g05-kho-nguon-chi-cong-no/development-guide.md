# G05 — Dùng thử development

27/10/2026: **localhost/CoffeeManager_Development đã nâng016→021** sau duyệt target và dừng đúng PID23728 được người dùng cho phép. Không seed/reset tài khoản. So sánh đủ8 nhóm hash với backup trước nâng đều khớp; xem [báo cáo](report.md). Cập nhật29/10: B01–B04 đã sửa và regression PASS; B05 footer đã duyệt/sửa và regression theo phạm vi; B06 grid nhập/xuất đã duyệt/sửa và regression chính PASS; còn nhánh nghiệm thu chưa đủ, G05 chưa nghiệm thu hoàn tất.

Lệnh đã dùng cho lần nâng được duyệt (không cần chạy lại để dùng thử):

```powershell
./tools/database/Update-DevelopmentG05.ps1
```

Script yêu cầu appđãđóng và DevelopmentMarker đúng; không tự dừngprocess. BackupCOPY_ONLY CHECKSUM trước nâng, áp dụng001–021 qua runner/checksum, đối chiếu kho/điểm/hóađơn/tài khoản và nguồnchi/nợ trước/sau; khôngseed, khôngfixture, khôngresetmật khẩu. Giữbackup đến sau nghiệmthuG05/người dùng chấpnhận, không tự xóa.

Mở CoffeeManager.slnx, startup src/CafeManager.WinForms, profileCoffeeManager.Development, F5; exeDebug dự kiến src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe. Xác minh schema021 qua Thông tin hệ thống, kết nối lấy profileDPAPI hiện có; không lưu mật khẩu vào repo/chat/log.

- ADMIN: Thu chi → ghi phiếu thường → detailchỉđọc. Kho → Nhập → previewSQL/trả ngay/nợ → chốt; Xuất → hủy/trả; Kiểm kê → snapshot/lưu đếm/chốt. Công nợ → hiện tại/tại ngày → trả nợ và xem chi nguồn.
- STORE: Home là kho thật, nhập với trả ngay0, xuất/kiểm kê, xem nợ và lịch sử; không mởFinance hoặc trả nợ.
- CASH: giữPOS/tồn phục vụ bán; không lối vào ghi kho/nợ/tài chính. Quyền vẫn kiểm ởBLL/SQL.

Mỗi formcon qua OrbitDialogHost là modalWindows cóowner/titlebar/X native, không embeddedpanel. Inputdùng OrbitField.SelectAllOnEntry chung. Sautimeout dùng Kiểm tra kết quả và request đã lưu, không tạo phiếu mới. PendingDPAPI tại LocalApplicationData/CoffeeManager/Pending theoactor/operation; không xóa file để né đối chiếu. File.lock không chứa payload, giữ làm khóa giữaprocess.

Dữ liệu legacy có nguồn thiếu được ghi đúng giới hạn, không suy chi ban đầu từ tổngPN−nợ. Bộplan/testplan quy định đầy đủ nghiệmthu; hướngdẫn này không là bằngchứng đãchạyGUI.



## Cập nhật30/10/2026 — Native follow-up và B07

STORE nhập21000/trả ngay0, xem nợ chỉ đọc và xuất trả2 đơn vị PASS; ADMIN trả10000+11000, lịch sử và hai link phiếu chi đúng nguồn PASS. Hai subcase runtime T19-K02.Name51/T20-K02.ActorMissing PASS, rollback0:0/dữ liệu không đổi. Đối chiếu tiền/kho/nợ cuối PASS. Khi đăng xuất Công nợ phát sinh B07 CTS đã dispose, chờ duyệt bổ sung trước sửa; CASH native tiếp theo BLOCKED, native keyboard chưa đủ. Chi tiết [biên bản](runs/native-followup-20261029/test-results.md), [bug B07](t23-cong-no/bug-report.md). Source cố định; đã dọn1/1DB và credentials, development không bị ghi fixture. Không đổi55ID lịch sử hay chốt nghiệm thu toàn nhóm.

## B07 — Đã duyệt, sửa và regression30/10/2026

Người dùng duyệt bổ sung B07; đã sửa vòng đời CTS trong đúng năm control G05, không đổi SQL/quyền/ghi nghiệp vụ. Debug/Release0warning/0error,22/22unit,47/47lifecycle và5/5CASH-BLL PASS. Native chuyển năm màn, nháp modal, đóng lịch sử nợ→logout, CASH login/logout, ADMIN login lại/đóng MainForm PASS theo phạm vi. Counts/receipt/tồn không đổi; source cố định;1DB test dọn1/còn0, credentials đã xóa. [Biên bản B07](runs/b07-20261030/test-results.md), [bug](t23-cong-no/bug-report.md). Nội dung “B07 chờ duyệt” ở cập nhật trước là lịch sử trước phê duyệt. CASH native không còn bị chặn bởi B07; native keyboard vẫn chưa đủ do công cụ. Không đổi55ID lịch sử, G05 Đang làm do các nhánh nghiệm thu còn thiếu.
