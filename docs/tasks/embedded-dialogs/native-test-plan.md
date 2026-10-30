# Đợt test — Form modal Windows có owner

Trạng thái: ĐỢT TEST NATIVE ĐÃ ĐƯỢC USER DUYỆT26/10/2026, đang chạy. Requirements/plan sửa
đã duyệt; build/smoke kỹ thuật không thay nghiệm thu. Source/DLL tại
runs/native-implementation-20261026/{source-final.csv,dll-hashes.json}.

Giữ ED01–ED13 và119component/18unit của bản host cũ nguyên lịch sử. Các ca ND
dưới đây có expected native đúng yêu cầu mới, chưa chạy nghiệm thu và không
tự kế thừa PASS từ card/container.

| ID | Phạm vi và expected |
|---|---|
| ND01 | Mọi editor/picker/detail/preview có title/X native, owner đúng, không taskbar riêng; không Form độc lập |
| ND02 | Windows khóa cha; chuột/sidebar/phím không xuyên; focus/tab trong hộp hoạt động |
| ND03 | Cha→con→confirm đúng owner chain, input/selection giữ, result đúng một lần, focus trở lại |
| ND04 | X/Esc/Hủy, dirtyNo/Yes, busy/unknown và đóngMainForm; không mất nháp hoặc treo task |
| ND05 | Error/timeout/reconcile/sessionrevoke/logout, queuedShow/abort và callbackmuộn giữ đúng trạng thái |
| ND06 | CRUD các module qua cơ chế chung; Save/Cancel/Error giữ typedresult/refresh/quyền và snapshot |
| ND07 | F3/UI06 đúng ô/focus; thêm/chọn khách lồng nhau, hủy giữ/bỏ và lỗi lookup |
| ND08 | F4 tìm/paging/Enter/doubleclick mở đúng mã, chỉ một hộp; history/giao→POS đúng |
| ND09 | F8invalid/Enter/doubleclick,success/conflict/unknown; một payment và đúng kho/điểm/receipt |
| ND10 | Paid→No/Yes→hủy/PDF/lỗisaucommit,preview/inlại; nativeprinter owner đúng,POSreset,khôngthu lại |
| ND11 | ADMIN/CASH/STORE, nhiều vòng mở/đóng, không orphan/ròhandles hoặc giữ session cũ |
| ND12 |125%,resize/WorkingArea/focus/footer/scroll,nhiềudòng;100/150%NOT RUN,hìnhthứcuser |
| ND13 | Input chọn hết khi click/Tab/vào lại,caretclicktiếp; tiền/SL/ghi chú/gridreused/combogõ/paste/IME;readonlykhôngđổi |

Test giao dịch trên SQL Server thật, DBtest riêng marker đã kiểm. Tái dùng DB
checkout hiện có theo ownership, không tạo DBthứ4. Trước thao tác đọc trạng thái
475–478 và lưu baseline mới; user chưa xác nhận checkout475 nên không giả là
đã chốt hoặc tự thu lại. Đối chiếu tiền/kho/điểm/snapshot/receipt, hai session cho
ca cạnh tranh bị ảnh hưởng. Không sửa profiledevelopment hoặc source trong test.

Agent kiểm chức năng qua control thật khi phù hợp; phản ánh đúng component hay
input native, không suy PASS từ source. Phần hình thức user kiểm. Gom bước cần
user, lưu actual từng subcase; gặpFAIL ghi điều tra rồi tiếp tục phần độc lập,
duyệt đợt bug trước sửa, giữ source cố định. Chưa chạy các ca ND toàn phần.

