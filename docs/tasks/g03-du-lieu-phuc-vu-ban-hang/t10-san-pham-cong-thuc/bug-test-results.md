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
