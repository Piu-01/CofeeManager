# Regression G03-B03 — 20/10/2026

**Cập nhật sau người dùngLưu:** yêu cầu tạm gácgiao diện. [DBsauLưu](../runs/g03-b03-fix/db-after-manual-save.json): recipe2dòngNVL01=0.0200Kg/NVL02=0.0180Lit, khác thứtự lượngkịchbảngốc. Chờxácnhận inputmanual để phânbiệtinputkhác/mappingbug; khôngđổiexpected. ReceiptReplaceRecipe1lần, tồnNVL01/02khôngđổi:PASSphầnkhôngtrừtồn/ghi1lần củaR06; phầnđúngpayload/reopenchưachốt. R04/R05/R07vẫnnativeresultchưađủ, khôngđóngB03từbuild/Lưuthànhcông.

Người dùng đãduyệtđợtsửa+regression. Scope2fileUI, giữcontract/migrations/BLL/SQL. Skillkarpathygiữphạmvi hẹp; đọc bộguideOrbit theoREADME. Source saufixđượcđóngbăng ở [source-frozen.csv](../runs/g03-b03-fix/source-frozen.csv).

| Ca | Expected | Actual | Status |
|---|---|---|---|
| B03-R01 Debugbuild | Compile0warning/0error | dotnet build CafeManager.slnx --no-restore -v minimal, đúng | PASS |
| B03-R02 Releasebuild | Compile0warning/0error | đúng, [log](../runs/g03-b03-fix/build-release.log) | PASS |
| B03-R03 UnitG03 | Khôngregressionrules | 20PASS/0FAIL/0skip, [TRX](../runs/g03-b03-fix/g03-b03-unit.trx), [log](../runs/g03-b03-fix/unit.log) | PASS |
| B03-R04 Gridnative | Combo/edit/placeholder/quantity/unit rõ, không trắng/tràn; unitngay | Bảnappmới mở, chờngười dùng kiểm | NOT RUN |
| B03-R05 Xóadòng | ADMINxóadòngnhầm/đangedit/newrow/hàngcuối, STOREkhôngmutate | Chờmanual, không suyPASStừsource | NOT RUN |
| B03-R06 Save2dòng/reopen | Đúng0.0180Kg/0.0200Lit, receipt1lần, tồnkhôngđổi | [Baseline DB](../runs/g03-b03-fix/db-before-manual.json) recipe0/receipt1, stockNVL01/02đãlưu | NOT RUN |
| B03-R07 Validation/keyboard/DPI | Saiquantity/trùngNVL giữinput; Tab/Delete; resize/DPI đủmatrix | Chờmanual | NOT RUN |

Đãđóng đúngPIDtestcũ25536đểbuild và mởbảnmới dùngcùngDBtest/marker. [Manifestphiên](../runs/manual-g03-20261019/manifest.json) cậpnhậtPIDmới; giữđến21/10/2026vàcleanupđúngtên/marker. Khôngcommit/push. 47IDkhôngtựnângPASSvàM17UXFAILgiữlịch sửtới khiđủregression. Khônggọiunit20PASS là20testUI.

# Round B04 — UI editor công thức — 21/11/2026

Người dùng duyệt đợt sửa. Phạm vi: `ProductsControl.Recipe` + nhánh cột `Delete` trong `OrbitManagementTheme.DialogGrid`; không đổi SQL/BLL/quyền/contract; không ghi DB test.

| Ca | Expected | Actual | Status |
|---|---|---|---|
| B04-R01 Debug build | Compile 0 error | `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -p:OutDir=$TEMP/cm-verify/` → 0 error (build thẳng vào bin bị chặn vì app đang chạy khóa DLL) | PASS |
| B04-R02 Combo khớp dòng | Chỉ chọn từ danh sách, không ô edit lồng; dropdown rộng bằng cột; chữ thẳng hàng | Chờ người dùng kiểm native | NOT RUN |
| B04-R03 Thùng rác từng dòng | Icon mỗi dòng, hover đỏ, xóa đúng dòng giữa/dòng mới/dòng cuối; không còn nút xóa hàng loạt | Chờ người dùng kiểm native | NOT RUN |
| B04-R04 Bố cục | Dialog ôm nội dung, không khoảng trống vô nghĩa; quá 8 dòng thì cuộn | Chờ người dùng kiểm native | NOT RUN |
| B04-R05 Đơn vị/Lưu | Đổi nguyên liệu cập nhật đơn vị ngay; Lưu rồi mở lại đúng; không ghi DB trước Lưu | Chờ manual | NOT RUN |
| B04-R06 STORE | Không có cột xóa, read-only | Chờ manual | NOT RUN |
| B04-R07 DPI 125% | Bố cục/combo/icon đúng ở 125%; 100/150 không chạy theo quy định phân công | Chờ manual | NOT RUN |

Theo AGENTS: hình thức do người dùng tự kiểm; agent không suy PASS native từ build. Chưa đóng bug B04 tới khi các ca trên có kết quả người dùng.
