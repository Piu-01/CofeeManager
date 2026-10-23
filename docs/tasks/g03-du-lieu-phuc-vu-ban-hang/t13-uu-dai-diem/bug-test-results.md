## Xác nhận của người dùng — 20/10/2026

Người dùng phản hồi sau khi nhận bản sửa: “ok test vậy mình thấy đủ ok”. Ghi nhận chấp nhận giao diện lịch/nút lịch bản mới trong phiên manual hiện tại, kết hợp bằng chứng bounds125% đã PASS. Phần lỗi visual B04 được người dùng chấp nhận đóng. Không suy rộng phản hồi này thành đã chạy bàn phím, Designer hoặc DPI100/150%; các ca đó giữ NOT RUN. Không thay đổi kết quả nghiệm thu toàn nhóm G03 hay các ca nghiệp vụ còn lại. Không sửa source hoặc dữ liệu DB trong lượt cập nhật này.
## Regression bổ sung sau duyệt

Xem [biên bản B04 follow-up](../runs/g03-b04-followup/test-results.md). Build0warning0error;20unit,11logicngày,2bounds125%PASS. Native visual/keyboard editor và DPI100/150% NOT RUN; không đóng B04 từ các phép đo. Giữ kết quả FAIL của lượt trước bên dưới.

# Regression G03-B04 — 20/10/2026

Đãduyệtcùngđợtdebug. .NET10.0.401/Windows,DBtestđúngmarkerđanggiữmanual; sourceđóngbăngsaufixở[source-frozen.csv](../runs/g03-b04-fix/source-frozen.csv), [audit](../runs/g03-b04-fix/audit.json)0fileđổitrongregression.

| Ca | Expected | Actual | Status |
|---|---|---|---|
| B04-R01 Debug/Release | Compile0warning0error | Đúng; [Releaselog](../runs/g03-b04-fix/build-release.log), Debugtrongtooltranscript | PASS |
| B04-R02 rulesunit | KhôngregressionG03 | 20/20PASS, [TRX](../runs/g03-b04-fix/g03-b04-unit.trx) | PASS |
| B04-R03 datefieldinprocessSTA | Dateonlyroundtrip/leap/rejectinvalid/keepinput/invalidatepreview/correctagain/calendarselection | 11/11PASS, [results](../runs/g03-b04-fix/date-results.json), [harness](../runs/g03-b04-fix/DateProbe/Program.cs) | PASS |
| B04-R04 date/popupnative | CùngthemeOrbit, keyboardTab/AltDown/Escape, focus/bounds/resize/DPI | Ảnh người dùng: lịch bị cắt, nút “Lịc…”; keyboard/DPI chưa chạy đủ | FAIL |
| B04-R05 Edit/reopen/save | Ngày20–21/10giữkhôngđổi, invaliddategiữinputkhôngghi | [BaselineSQL](../runs/g03-b04-fix/db-before-manual.json) chưađổi, chờmanual | NOT RUN |

LượtbuildharnessđầulỗirelativeProjectReference(khôngphảisourcebug); logdate-probe.loggiữnguyên, sửađườngdẫnharnessrồichạydate-probe-v2.log. KhôngsửatestexpectedđểnéFAIL. Native-calendar-selectionchỉkiểmproperties/inprocess, khôngđạidiệnảnhpopuplịchhoặcuserclick.

ĐãđóngđúngPIDtestcũ15728đểbuild, mởbảnmớivàgiữcùngDBtest/marker, [manifest](../runs/manual-g03-20261019/manifest.json)cóPIDmới/cáchcleanup. Mốcgiữ21/10/2026. TỷlệKM0%làbaselinechưaxácnhậninput, khôngtựđổi20. B04chưađóng; G03vẫnĐanglàm, khôngcommit/push.



