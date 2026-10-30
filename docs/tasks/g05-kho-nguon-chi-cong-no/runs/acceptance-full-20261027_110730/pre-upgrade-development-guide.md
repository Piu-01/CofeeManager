# G05 — Dùng thử development

Source/migrations017–021 đã triển khai. **DBdevelopment chưa được nâng**: bộduyệt tựđộng từchối thao tác26/10 vì cần xác nhận rõ target. Không chạy chức năngG05 trên schema016 rồi coi lỗi thiếuprocedure là kết quả nghiệm thu.

Sau khi được phê duyệt đúng targetlocalhost/CoffeeManager_Development:

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
