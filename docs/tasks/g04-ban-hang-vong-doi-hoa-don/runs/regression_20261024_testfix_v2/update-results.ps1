$ErrorActionPreference='Stop'
$group=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$updates=@{
'T14-K01'=@('PASS','Ba kênh đúng; sai bàn/ship và7biên người nhận/tên101/SDT16/chữ/địa chỉ251/trắng bị52101, không header/receipt, transaction0.')
'T14-K03'=@('PASS','Trigger test quan sát header ban đầu tổng/giảm0 trong transaction; sau món đầu tổng25.000 đúng.')
'T14-K07'=@('PASS','Add/tăng món ngừng bị chặn; giảm/xóa được. Thiếu recipe: add52105,checkout52204, toàn snapshot không đổi.')
'T14-K11'=@('BLOCKED','Fault header/line/receipt rollback đạt; timeout sau commit ghi đúng-2, đối chiếu1receipt/1header/transaction0. Trước commit/mất kết nối khác chưa chạy đủ.')
'T15-K02'=@('PASS','Token đổi do KM/policy/khách/đối tác kể cả tiền giữ nguyên:52201; không ghi thanh toán, toàn snapshot rollback. Hai thứ tự policy/recipe đồng thời chưa chạy bổ sung.')
'T15-K06'=@('PASS','Thiếu NVL/ngừng bán/thiếu recipe đều chặn checkout, snapshot và transaction rollback, đơn mở còn nguyên.')
'T15-K07'=@('PASS','HaiSP chung2NVL,hai dòng cùngSP: tiêu hao1.2885/4.7035, tồn9998.7115/9995.2965; đổi recipe sau chốt không đổi snapshot.')
'T15-K08'=@('PASS','Fixture KM0% thay hạng, net999/1000/1999→0/1/1; ship không tính điểm; overflow52205 rollback; replay không nhân history. G04-TEST-01 đã sửa.')
'T15-K11'=@('FAIL','Raw v2:0/1205 thay0/52203; Pair ghi PASS sai, audit hạ FAIL. Đồ thị deadlock khóa kho↔HOADON đã lưu; invariants một paid/tồn không âm/đơn thua không payment vẫn đạt. Chờ G04-CONC-01.')
'T15-K14'=@('PASS','Ngày tương lai52205 đúng contract; toàn snapshot không đổi,transaction0. Expected51108 cũ giữ FAIL lịch sử; không sửa sản phẩm.')
'T16-K01'=@('BLOCKED','POS tạo/mở lại ba kênh/reload app chưa thao tác. Fixture compile đã sửa; công cụ click/focus chưa hoạt động.')
'T16-K02'=@('BLOCKED','SQL dòng món đạt; handler/binding POS sửa/xóa/refresh chưa thao tác đầy đủ.')
'T16-K03'=@('BLOCKED','Control thật SetValue100.000 cho bill81.000 hiển thị thối19.000; chưa click commitDB nên chưa chứng nhận lưu phân bổ từGUI.')
'T16-K04'=@('BLOCKED','Control thật báo thiếu khi80.000, báo nguyênVND khiabc; click Confirm trả geometry unavailable, chưa đủ biên/handler/đóng dialog.')
'T16-K05'=@('BLOCKED','SQL replay đạt. Return không tạo thay đổi; Tab không đổi focus nên không đủ bằng chứng phím đến app; không đánh PASS keyboard/doubleclick.')
'T16-K06'=@('BLOCKED','SQL stale/closed chặn đúng; POS cũ tải lại chưa kiểm.')
'T16-K07'=@('BLOCKED','Timeout sau commit-2 và receipt đạt; UI unknown/reconcile/khóa gửi lại chưa kiểm.')
'T16-K08'=@('BLOCKED','SQL hủy mở/paid refusal đạt; handler hủy và bàn refresh chưa kiểm.')
'T16-K09'=@('BLOCKED','SQL empty/thiếu kho/recipe đạt; thông báo/giữ input POS chưa kiểm.')
'T16-K10'=@('BLOCKED','Unit/SQL STORE bị chặn; menu/PT010 trên POS chưa thao tác.')
'T16-K11'=@('BLOCKED','Native DeviceDpi120=125%,1row/3columns; chưa đủ resize/tìm/nhiều bàn/dòng. Không đổi Scale; hình thức SKIPPED.')
'T16-K12'=@('BLOCKED','SQL không nhân kho/điểm đạt; refresh bàn/khách/tồn từ POS chưa thao tác.')
'T17-K01'=@('PASS','Fixture ngày trước chốt, actual ngàyTT; Created/Paid2/1,cuối ngày23:59:59 có mặt,filter kết hợp1,paging51đơn50/1, raw IDs không trùng.')
'T17-K03'=@('BLOCKED','Preview trong bộ nhớ100dòng/210entry;9trang đầu, không kiểm hình thức. Chưa đủ UI preview/in thật và đối chiếu nội dung theo trang.')
'T17-K07'=@('BLOCKED','Hai Print() trong bộ nhớ hoàn tất, cursor210; controller tái dùng trả9rồi18previewpages tích lũy. Chưa thao tác UI in lại và đối chiếuDB, không claim18là trang mới.')
'T18-K05'=@('FAIL','Fixture nội suy rowversion sai→SQL102/102, Pair ghi PASS sai. Audit hạ FAIL; chưa chứng nhận concurrency delivery. G04-TEST-06 chờ duyệt.')
'T18-K06'=@('BLOCKED','Unit5case đạt; SQL ADMIN/CASH đổi được, STORE51605 và không receipt. Quyền/menuUI chưa kiểm đầy đủ.')
}
$summary=@()
foreach($file in Get-ChildItem $group -Filter test-results.md -Recurse | Where-Object {$_.Directory.Name-match'^t(14|15|16|17|18)-'}){
 $old=Get-Content $file.FullName -Raw;$task=[regex]::Match($file.Directory.Name,'^t(\d+)').Groups[1].Value
 Set-Content (Join-Path $PSScriptRoot "previous-test-results-t$task.md") $old -Encoding utf8
 $rows=[regex]::Matches($old,'(?m)^\| T\d\d-K\d\d \|[^\r\n]+')
 $body=@"
# Biên bản T$task — regression24/10/2026

Người dùng đã duyệt sửa5lỗi bộ kiểm thử và chạy bù ngày24/10. V1 giữ nguyên lịch sử;
source sản phẩm/schema015 không đổi. Các nhánh mới dưới đây thay trạng thái hiện
tại theo evidence, không sửa expected/checklist T01–T32.

[Manifest v2](../runs/regression_20261024_testfix_v2/manifest.json),
[SQL extended](../runs/regression_20261024_testfix_v2/extended-results.json),
[audit raw concurrency](../runs/regression_20261024_testfix_v2/concurrency-audit.json),
[SQL còn lại](../runs/regression_20261024_testfix_v2/remaining-results.json),
[SQL chạy bù](../runs/regression_20261024_testfix_v2/supplement-results.json),
[52unit TRX](../runs/regression_20261024_testfix_v2/unit/g04-regression.trx),
[GUI control actions](../runs/regression_20261024_testfix_v2/gui-actions.json).
SQL/script/JSON lưu đầy đủ dữ liệu, lệnh, expected và actual của subcase. Chỉ dựa
raw actual để kết luận, không dựa PASS sai của Pair. Kết quả kế thừa v1 xem
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t$task.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
"@
 foreach($row in $rows){$parts=$row.Value.Split('|');$id=$parts[1].Trim();$expected=$parts[2].Trim();$actual=$parts[3].Trim();$state=$parts[4].Trim();if($updates.ContainsKey($id)){$state=$updates[$id][0];$actual=$updates[$id][1]};$body+="`r`n| $id | $expected | $actual | $state | v2JSON/script Id tương ứng hoặc biên bản kế thừa |";$summary+=[pscustomobject]@{Id=$id;Status=$state}}
 $body+="`r`n`r`nChức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout`r`ntrước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN`r`nở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.`r`nHình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.`r`nCác vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.`r`n"
 Set-Content $file.FullName $body -Encoding utf8
}
if($summary.Count-ne56){throw 'Checklist must remain56'}
$summary|Export-Csv (Join-Path $PSScriptRoot 'checklist-results.csv') -NoTypeInformation
$summary|Group-Object Status|Select-Object Name,Count|ConvertTo-Json|Set-Content (Join-Path $PSScriptRoot 'checklist-counts.json') -Encoding utf8
