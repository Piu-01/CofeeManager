# T07 — Liên kết bug và giới hạn evidence

**Trạng thái cuối17/10/2026:** cácbugliênkếtB01/B04/B05/B06 đãsửa/regressionPASS;
mọisubcaseT07đãđốichiếuPASS,khôngcònblockerhiệnhành. Lịch sửFAIL vàgiới hạn
củalượtcũ dướiđây vẫngiữ; xem`../acceptance-matrix.md`.

Ca loading T07-K05.a FAIL do CatalogsControl thuộc T08. Bug sở hữu:
[G02-B01](../t08-danh-muc-nen/bug-report.md). Người dùng đã duyệt đợt debug
B01/B02; B01 đã sửa và5/5ca native regressionPASS ngày11/10/2026.
Chứng cứFAIL trước sửa vẫn giữ. Các ca chưa đủ chứng cứ ở dưới không tự nângPASS.

Các raw FAIL khác ở native harness (content chưa ready, sys.partitions count,
exact cancel timeout/modal) được giữ nguyên trong runs. Không tự kết luận là
bug ứng dụng khi chưa phân biệt thao tác/đo; ca liên quan giữ BLOCKED và ghi
chứng cứ bổ sung/điều kiện còn thiếu trong test-results.md.

## Đợt bug bổ sung 11–16/10/2026

Ca T07-K04.c đã thao tác được Designer đúngsolution, không còn coi là chỉ
blockerautomation. Lỗi được ghi tại task sở hữu source:

- [T06/B04](../t06-bootstrap-dang-nhap-phan-quyen/bug-report.md): LoginSave
  thêmAbsolute20row, bảnclone sauSave chạy thật bịclipping.
- [T06/B05](../t06-bootstrap-dang-nhap-phan-quyen/bug-report.md): Bootstrap
  parseerror tạiinitializer dòng38.
- [T08/B06](../t08-danh-muc-nen/bug-report.md): editor/catalogsparseerror dòng47.

ĐợtB03–B06 đã được duyệt vàsửa; regressioncuốiPASS ngày16/10/2026.
Modal/DPI/keyboard/GC và
các nhánh đãPASS trong lượt bổ sung được cập nhật ở test-results.md; giữraw
FAIL trước đó, không coi count/captionDesign là nghiệm thuDesigner thànhcông.

B06cólượt1FAILgrid150%5px; đãkếtthúclượt vàdebug phầnColumnStyle tựthêm,
giữAutoSize nhưsemantics cũ. Lượt cuối binary sauSave36/36UI+DPI và
23/23CRUDPASS; bằngchứngFAILtrước vẫn lưu tại taskT08. Xemtest-results.md.
