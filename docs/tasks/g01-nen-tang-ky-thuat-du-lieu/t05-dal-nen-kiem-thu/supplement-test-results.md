# G01 — Kiểm chứng bổ sung UI/runner09/10/2026

**Duyệt:** User “ok xác nhận” sau B08 và đề nghị kiểm nốt phần bổ sung.
**Run:** G01_Supplement_20261009_120403; source/schema/fixture/migration fixed,
source-stability.json count0. SDK10.0.401, Windows, SQLlocalhost17.0.1000.7.

## DPI thật và focus

Windows Settings → Display → Scale đổi100%/150%; GetDpiForWindow xác nhận96/144.
Sau test đã trả125% (Recommended), restored-scale.json xác nhận. Không fake Scale
control để thay display DPI. Host tạm ngoài repo mở đúng StartupForm compiled
mới, bind4 DTO demo; không dùng demo để chứng minh SQL/principal.

| Subcase | Expected | Actual | Status |
|---|---|---|---|
|100%1280×720|Window đúng kích thước/control chính nhìn được|Bounds1280×720; PNG đầu black vì capture, không dùng chứng minh hình thức|PASS bounds, hình thức chưa đủ ảnh |
|100%1366×768|Window/grid/buttons không bị che|Bounds đúng, PNG native có grid4 DTO và controls|PASS |
|150%1280×720|Window thu đúng kích thước mục tiêu|Actual1280×812, không về720|**FAIL B09**|
|150%1366×768|Window thu đúng kích thước mục tiêu|Actual1366×812, không về768|**FAIL B09**|
|Tab tiến ở100%|Input → tải → grid, bỏ nút hủy disabled|FocusedElement đúng app, nút tải rồi cellgrid|PASS subcase|
|Shift+Tab/keyboard150%|Focus vẫn trongapp/thứ tự đảo đúng|Focus chuyển shell/không có element; không đủ bằng chứng nguyên nhân|INCONCLUSIVE, cần manual|

Không kết luận Shift+Tab là bug sản phẩm từ focus ngoài app. Không báo PASS đầy
đủ keyboard; cần người dùng thực hiện trong app hoặc harness ổn định foreground.
Ảnh/JSON ở runs nhóm supplement: dpi-keyboard.json, dpi100/150-*.png,
dpi-harness.txt, restored-scale.json. B09 source liên quan MinimumSize800×560
ở StartupForm.Designer.cs, AutoScaleMode.Dpi; chưa sửa trong lượt test.

## B04 — Runner khi test FAIL thật (fault fixture chủ động)

Chạy nguyên Test-DataAccess.ps1 trên DBtest **mới**. Khi progressXUNIT, session
fixture riêng đổi test-only T05_Unknown từ THROW52905 sang51109; expected của
test giữ nguyên, nên test UnknownAndTimeout...FAIL có chủ đích. Không đổi source/
migration. Cuối lượt khôi phục test-only procedure, giữ DB/JSON/TRX failure.

Run20261009_120705_f58ecf52, DBCoffeeManager_Test_T05_20261009_120705_f58ecf52:
**20 kết quả được thu:19 PASS/1 FAIL chủ động**, dotnetExit1, runnerExit1,
SetupErrornull, đầy đủ stdout/stderr/TRX, FixtureRestoredtrue.
**Kiểm chứng capture-failure B04 PASS**. Đây không phải regressionFAIL của DAL
với fixture chuẩn hoặc bug mới; kết quả20/20 chuẩn trước được giữ lịch sử.
Nhánh lỗi không bị nhầm setupError và runner không dừng trước khi lưu TRX.

Attempt1 cùng fault có đủ20 results/dotnetExit1 nhưng observer Process.ExitCode
null; harness giữ process Handle rồi chạy lại trên DBmới, đọc runnerExit1 được.
Giữ attempt1-* và JSON; không sửa runner để né thiếu observer. Latest task
test-results.json là raw lượt fault, không thay bảng nghiệm thu chuẩn ở Markdown.
Original accepted Markdown giữ tại runs/b08-accepted-before-runner-fault.md;
raw20/20 chuẩn trước ở runs/automated-pass-before-runner-fault.json.

Bằng chứng runner-failure-check.json, runner-fault.trx, runner-fault-output/error.txt,
runner-failure-harness.txt trong runs nhóm; cả2 DB giữ để tra cứu, không drop/reset.

## Kết luận

Ca45 mã nguyên bản đã PASS ở các lượt trước, nhưng **planUI bổ sung150%FAIL**:
G01 chưa nghiệm thu, B09 chờ duyệt sửa/regression. Keyboard/full ảnh100%720
còn cần hoàn thiện. Không đổi expected để chấp nhận window812px thay720/768.
