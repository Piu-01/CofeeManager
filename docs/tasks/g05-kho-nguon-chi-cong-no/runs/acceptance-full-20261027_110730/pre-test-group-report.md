# G05 — Đã triển khai source, chờ nghiệm thu

26/10/2026. Requirements và bộplan đã được duyệt. Đã triển khai tuần tựT19→T23 theo SQL→DAL→BLL→UI; **G05 Đang làm**, chưa nghiệmthu/Hoàn tất.

| Task | Source/migration | Nghiệm thu gốc |
|---|---|---|
| T19 |017, thu chi nguồn và bất biến |10NOT RUN |
| T20 |018, nhập/trả ngay/nợ nguyên tử |12NOT RUN |
| T21 |019, xuất hủy/trả và khóa tồn |10NOT RUN |
| T22 |020, snapshot/countNULL/chốt một lần |11NOT RUN |
| T23 |021, trả nợ/lịch sử/cutoff |12NOT RUN |

Tổng **0PASS/0FAIL/55NOT RUN** cho đợt nghiệmthu. Build/typed smoke ở dưới là kiểm kỹ thuật riêng. G04 giữ trạng thái kếthừa40PASS/0FAIL/16BLOCKED; không xóa hoặc suyPASS16ca.

## Đầu ra

- Migrations017–021 trong database/migrations/, contracttyped Models, repositorySqlClient và servicequyền/normalize trongsrc. Helperghi chi nội bộ không cấpEXECruntime; nguồnTC005/011/012 được giữ. Không sửa001–016/baseline/seed.
- UI Thu chi, Kho(Tồn/Nhập/Xuất/Kiểm kê), Công nợ; STOREHome vào kho thật, CASH giữPOS/tồn phục vụ bán. PN↔CN↔chi có link đúngrole, formconmodalnativeowner quaOrbitDialogHost. InputSelectAll dùnghelperchung; footerresponsive chỉopt-inG05.
- Pendingactor/request được bảo vệDPAPI, requesthash/receipt tạiSQL; không tự tạo phiếu mới sautimeout. Không tạo lại tài chính sau in hoặc ghi thêmThu checkout.
- Unitcases có giá trị và fixture/subcaseSQL đã soạn, compile project test sạch; chưa chạy. Runner Test-G05.ps1 chỉ làsubset, không tự chứng nhận55ID/native/concurrency.
- [Bộplan](plans.md), [đợttest](test-plan.md), [dùng thử](development-guide.md), các technical-checks/test-results từngtask và source-implementation.csv.

## Kiểm kỹ thuật thực tế

- Debug vàRelease sản phẩm:0warning/0error. ProjectCafeManager.Tests compile:0warning/0error. Kết quả từtooloutput của lệnhdotnetbuild tươngứng, không mô tả thànhunitPASS.
- SQL Server clean/demo qua001–021 thành công; typed dependency smoke năm luồng thành công tại [evidence](runs/technical_20261026_231552_1d5d908b/typed-smoke.json). Quyềnconnectionowner ghi rõ, không thay kiểmruntimeprincipal. Source cố định từnglượt, tiền/kho mới chỉ phát sinh trongDBtest của smoke.
- Tổng tài nguyên qua10lượt kỹ thuật: tạo17DB/dọn17/còn0, xem [audit](resource-audit.json). Giữloglỗi compile/runner/sandbox banđầu, không xóa lịch sử. Không giữDBtest hoặc đổiSQLservice/authmode/WindowsScale.
- GUI/55ID/concurrency/fault/legacy-invalid/unit execution:NOT RUN, chờ duyệt đợttest. Hình thứcSKIPPED theo phâncông; DPI100/150%NOT RUN. Chưa cónghiệmthuGUI125% mới từ việcbuild.

## Phần còn chặn và bước tiếp

Nâng **localhost/CoffeeManager_Development** lên021 bị bộduyệt tựđộng từchối trước thựcthi vì cần người dùng xác nhận đúngtarget. Khôngbackup/nângDB/seed/resetaccount. Scriptđãchuẩnbị tại tools/database/Update-DevelopmentG05.ps1, cómarker/appclosed/preflight/COPY_ONLY CHECKSUM/đối chiếu tiền-kho-điểm-nợ-account. Cần duyệt rõtarget trước ápdụng; khôngchuyển sangDBkhác đểnéchặn.

Đợt test55ID cần người dùng duyệt riêng theo [test-plan](test-plan.md). Khôngcommit/push. Nếu phát hiệnbug trong đợttest, ghi tại task sởhữu và trình cảđợt trước debug; không sửa giữa lượt test.

Build cuối sau định dạng vi-VN riêngG05: [Debug](runs/final-build-20261026/debug.log), [Release](runs/final-build-20261026/release.log), cả hai0warning/0error. Không thayculture/luồng POS. Source-implementation.csv đã cập nhật sau build cuối.
