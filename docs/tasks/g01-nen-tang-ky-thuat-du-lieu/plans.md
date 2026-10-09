# G01 — Bộ plan để duyệt một lần

**Trạng thái:** 45/45 ca nguyên bản PASS kế thừa; runner-failure và regression B09/B10 (Designer/DPI/keyboard) PASS. Đủ kiểm chứng kỹ thuật, chờ cho phép commit bàn giao.  
**Duyệt:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Nguồn:** Kế hoạch 3.2, G01; requirements nhóm đã duyệt.  
**Cập nhật quy trình:** Theo yêu cầu người dùng ngày 08/10/2026.

## 1. Chỉ mục

| Task | Plan | Hiện trạng | Ca nghiệm thu |
|---|---|---|---|
| T01 — Môi trường/repository | [plan](t01-moi-truong-repository/plan.md) | Đã duyệt, kế thừa 7/7 PASS | T01-K01–K07 |
| T02 — SQL baseline | [plan](t02-sql-baseline/plan.md) | Đã duyệt, kế thừa 12/12 PASS (80 subcase) | T02-K01–K12 |
| T03 — Contract/migration/cấp mã | [plan](t03-contract-migration-cap-ma/plan.md) | 9/9 PASS, runtime app principal riêng đã xác nhận | T03-K01–K09 |
| T04 — Solution/Models | [plan](t04-solution-models/plan.md) | 7/7 PASS; Designer sau B07 đã regression | T04-K01–K07 |
| T05 — DAL/nền kiểm thử | [plan](t05-dal-nen-kiem-thu/plan.md) | 10/10 PASS sau B08; bổ sungUI/runner còn NOT RUN | T05-K01–K10 |

Đủ **45 ca nguyên bản**; giữ nguyên phạm vi, đầu ra, thứ tự và phụ thuộc từng
task. T01/T02 và lịch sử bug/test được kế thừa, không thực hiện lại chỉ vì đổi
đường dẫn. Nếu T03–T05 ảnh hưởng kết quả cũ, regression liên quan nằm trong đợt
test được duyệt, không ghi đè lịch sử. Commit bàn giao vẫn chờ cho phép.
Lần sắp xếp hồ sơ chỉ cập nhật vị trí và tham chiếu đường dẫn; không tạo lượt
test mới. Runner T02 cũng được đổi nơi lưu kết quả sang thư mục T02 trong G01.

Sau duyệt bộ plan, đã viết migration/contract/quyền và runner T03; solution
bốn tầng cùng Form tối thiểu T04; DAL, async query/service/UI và fixture/xUnit/
runner T05. Đã restore/build/chạy app/unit và runner SQL; setup seed bị chặn.
Hồ sơ: [test-plan.md](test-plan.md), [report.md](report.md). Chưa sửa source trong
lượt test; sau duyệt đã sửa B01–B06 và regression PASS tự động, còn manual.

## 2. Trình tự và các điểm duyệt

1. **Duyệt bộ plan G01 một lần**: cho phép triển khai T03 → T04 → T05.
2. **Triển khai từng plan**: viết migration/contract T03 → cấu trúc T04 → DAL
   và test fixture/runner T05. Đầu ra trước dùng cho task sau; test nghiệm thu,
   chạy DB/migration, build/run kiểm chứng và GUI nằm ở đợt test sau duyệt.
   Blocker thực sự ngăn triển khai tiếp thì ghi rõ và hỏi quyết định.
3. **Chờ duyệt đợt test G01 một lần** sau triển khai: nêu source/schema snapshot,
   các ca chạy mới T03–T05 (26 ca), ca T01/T02 kế thừa và regression do ảnh hưởng.
4. **Test từng task** T03 → T04 → T05; giữ source cố định trong lượt. FAIL không
   chặn ca độc lập; ca phụ thuộc thiếu đầu ra ghi BLOCKED. Biên bản ở từng task,
   báo cáo tổng hợp ở `report.md` sau hết đợt, kể cả khi còn bug.
5. **Duyệt cả đợt bug một lần**: mỗi bug ghi tại task sở hữu, có mã bug, ca FAIL,
   bằng chứng, nguyên nhân/giả thuyết, hướng sửa, ảnh hưởng và regression dự kiến.
   Báo cáo nhóm liên kết các bug; chỉ debug sau khi người dùng duyệt đợt.
6. **Debug và regression** lần lượt các bug đã duyệt; lưu lượt mới, giữ FAIL cũ.
   Duyệt debug bao gồm regression đúng ảnh hưởng; bug mới ngoài đợt cần duyệt bổ
   sung. Cập nhật report nhóm; chỉ chốt nghiệm thu khi đủ tiêu chí và không còn
   lỗi trọng yếu.

## 3. Cấu trúc hồ sơ

```text
g01-nen-tang-ky-thuat-du-lieu/
  requirements.md
  plans.md
  report.md                    # tạo sau đợt test, cập nhật sau debug
  t01-moi-truong-repository/
  t02-sql-baseline/
  t03-contract-migration-cap-ma/
  t04-solution-models/
  t05-dal-nen-kiem-thu/
    plan.md
    test-results.md            # tạo khi đã test
    bug-report.md              # tạo nếu phát hiện bug
```

Test/bug của task nào nằm trong thư mục task đó. Bug xuyên task có một task sở
hữu theo nguyên nhân, tham chiếu các task ảnh hưởng trong report, không nhân bản
bug thành nhiều hồ sơ mâu thuẫn. Không tạo kết quả giả cho task chưa chạy.
