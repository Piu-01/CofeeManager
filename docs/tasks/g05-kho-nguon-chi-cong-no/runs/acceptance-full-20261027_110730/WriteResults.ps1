$ErrorActionPreference='Stop';$group='D:/Workspace/Nam_3/HK1/CoffeeManager/docs/tasks/g05-kho-nguon-chi-cong-no';$run=Split-Path $PSCommandPath
$records=@{
't19-thu-chi'=@'
K01;PASS;SQL Thu3456/Chi1234 đúng; native ADMIN ghi TC20250521 NV991 số1234 và reload;sql-supplement-v2-results.json, gui-finance-after.txt
K02;NOT RUN;7 subcase tiền/ghi chú201/FK PASS; nhánh tên quá dài chưa kiểm đủ trong lượt này;sql-extended-v2-results.json
K03;PASS;PN/BL FK và unique bị chặn, nguồn hợp lệ lưu được; MaBL là fixture chưa thay tích hợp T26;sql-extended-v2-results.json
K04;PASS;Nguồn hệ thống sai loại bị chặn, tiền/nguồn không đổi sau lỗi;sql-extended-v2-results.json
K05;BLOCKED;SQL ADMIN/CASH/STORE và cấm helper/rawDML PASS; chưa hoàn tất dropdown/role native do hạn chế thao tác hộp con;sql-extended-v2-results.json
K06;PASS;Nguồn trùng và fault sau ghi chi rollback, snapshot không đổi và transaction0:0;sql-extended-v2-results.json
K07;BLOCKED;SQL sửa/xóa phiếu và đổi loại danh mục bị chặn; nhánh detail chỉ đọc native chưa hoàn tất;sql-extended-v2-results.json
K08;PASS;Tổng Thu3456/Chi47702 tại thời điểm kiểm khớp aggregate trực tiếp PHIEUTHUCHI, không cộng lịch sử trả nợ lần hai;sql-supplement-v2-results.json
K09;PASS;Checkout fixture SP991 trả1000; số phiếu PHIEUTHUCHI không tăng, thanh toán đúng1000;sql-supplement-v4-results.json
K10;BLOCKED;Ghi thường/reload được; bộ lọc và nhóm thao tác thiếu, source links chưa kiểm đủ native;gui-finance-after.txt, ../t20-nhap-kho/bug-report.md
'@
't20-nhap-kho'=@'
K01;BLOCKED;SQL nhập nhiều dòng/làm tròn từng dòng PASS; không mở được editor từ workspace để kiểm nháp/đóng;sql-extended-v2-results.json, ../t20-nhap-kho/bug-report.md
K02;NOT RUN;SQL NCC/qty/price/precision/trùng/NVL FK bị chặn; chưa gọi riêng actor/nhân viên không tồn tại nên chưa PASS toàn ID;sql-extended-v2-results.json
K03;PASS;Thiếu quyền và fault sau ghi kho/chi rollback header/CT/kho/chi/nợ/receipt, transaction0:0;sql-extended-v2-results.json
K04;PASS;Trả0 có nợ10000 không chi0; trả đủ10000 một chi không nợ0;sql-extended-v2-results.json
K05;PASS;Tổng10000/trả4000/nợ6000, nguồn chi và nợ đúng PN;sql-extended-v2-results.json
K06;PASS;Âm/vượt tổng/precision bị chặn; nguồn chi sai loại bị bảo vệ bởi invariant hệ thống;sql-extended-v2-results.json
K07;PASS;Replay cùng request không đổi dữ liệu; timeout trước commit receipt0/snapshot giữ nguyên, sau commit receipt1/replay không đổi;sql-extended-v2-results.json, timeout-v4-results.json
K08;PASS;UPDATE/DELETE header và chi tiết đã chốt bị chặn, snapshot giữ nguyên;sql-extended-v2-results.json
K09;PASS;10 PN legacy đều DaChot1; so sánh đủ hash với backup schema016 xác nhận kho và nguồn không đổi qua021;sql-final-results.json, development-backup-verification.json
K10;PASS;Hai SQL session cùng NVL88, hai thứ tự, cả hai commit và tồn tăng đúng3;concurrency-v2-results.json
K11;FAIL;SQL STORE trả0 được, STORE trả tiền/CASH bị chặn; native ADMIN thiếu Lập phiếu nhập nên không thao tác theo luồng;gui-inventory.txt, ../t20-nhap-kho/bug-report.md
K12;BLOCKED;SQL rounding/source khớp; preview/reload nguồn trong editor native bị chặn bởi G05-B01;sql-extended-v2-results.json, ../t20-nhap-kho/bug-report.md
'@
't21-xuat-kho'=@'
K01;BLOCKED;SQL xuất giảm tồn đã kiểm; nháp/editor native không truy cập được do thiếu nút lập phiếu;gui-issue.txt, ../t20-nhap-kho/bug-report.md
K02;PASS;Xuat ra quay bị từ chối52801, không đổi kho/chứng từ;sql-extended-v2-results.json
K03;PASS;Qty0/âm/quáprecision, FK và trùng NVL bị chặn, snapshot giữ nguyên;sql-extended-v2-results.json
K04;PASS;Xuất đúng lượng tồn về0; thiếu tồn bị chặn và rollback;sql-extended-v2-results.json
K05;PASS;Dòng đầu đủ và dòng sau thiếu vẫn rollback toàn bộ, transaction0:0;sql-extended-v2-results.json
K06;PASS;Hai xuất và checkout/xuất tranh tồn cuối qua hai thứ tự, barrier/DMV thấy cả hai chờ khóa; chỉ giao dịch đủ tồn commit;concurrency-v2-results.json
K07;PASS;Replay/chốt lại và sửa/xóa bị chặn; timeout trước/sau commit không trừ hai lần;sql-extended-v2-results.json, timeout-v4-results.json
K08;BLOCKED;SQL hủy đơn mở rồi xuất hủy đúng1, không có tiêu hao bán của đơn hủy; chưa chạy đủ luồng nháp/native do G05-B01;sql-supplement-v4-results.json
K09;PASS;Workspace thật hiển thị chữ Hủy do hỏng / Trả hàng; chưa tự hoàn tiền hoặc cấn nợ;gui-issue.txt
K10;FAIL;SQL ADMIN/STORE/CASH đúng quyền; native thiếu nút lập và nhóm thao tác nên không hoàn tất nghiệp vụ/reload theo luồng;gui-issue.txt, ../t20-nhap-kho/bug-report.md
'@
't22-kiem-ke'=@'
K01;BLOCKED;SQL snapshot đúng tồn, countNULL, chưa đổi kho PASS; native lấy snapshot bị chặn bởi UI/exception;sql-supplement-v2-results.json, ../t22-kiem-ke/bug-report.md
K02;PASS;NULL phân biệt0, đếm0 và bằng tồn chốt đúng; âm/precision/trùng bị chặn;sql-extended-v2-results.json, sql-supplement-v2-results.json
K03;BLOCKED;SQL thiếu lý do khi lệch bị chặn; nhánh validation native chưa chạy do lỗi danh sách;sql-extended-v2-results.json, ../t22-kiem-ke/bug-report.md
K04;PASS;Phiếu hôm nay khớp snapshot chốt đúng một lần, replay giữ dữ liệu;sql-extended-v2-results.json, sql-supplement-v2-results.json
K05;PASS;Chốt lại/reset/header/ngày/nhân viên/CT sau chốt bị chặn, transaction0:0;sql-extended-v2-results.json
K06;PASS;Header ngày cũ/tương lai/rỗng do owner chuẩn bị cho ca âm đều bị từ chối chốt; không sửa ngày/expected;sql-extended-v2-results.json
K07;PASS;Mua rồi xuất về cùng lượng nhưng Revision khác: ABA vẫn bị từ chối, không đè tồn;sql-extended-v2-results.json
K08;PASS;Một trong nhiều NVL stale khiến cả phiếu rollback;sql-extended-v2-results.json
K09;PASS;Hai session cùng KK có một commit/một52902; KK và checkout hai thứ tự giữ đúng biến động, không lost update;concurrency-v2-results.json, concurrency-kk-results.json
K10;BLOCKED;Chưa kiểm đủ native thời gian đếm và lời nhắc tạm dừng vì không mở được luồng; không suy PASS từ source;../t20-nhap-kho/bug-report.md, ../t22-kiem-ke/bug-report.md
K11;FAIL;SQL STORE/ADMIN được và CASH bị chặn; native danh sách gặp LinesJson NULL của header rỗng thì Unhandled exception;gui-stocktake-stack.txt, ../t22-kiem-ke/bug-report.md
'@
't23-cong-no'=@'
K01;PASS;Nợ đúng NCC qua PN, nguồn bất biến và unique một PN một nợ bị chặn;sql-supplement-v2-results.json, sql-extended-v2-results.json
K02;PASS;Nguồn độc lập không có nợ: INSERT0 bị CHECK chặn và vượt tổng bị trigger chặn; hạnNULL hợp lệ;sql-supplement-v4-results.json, sql-supplement-v2-results.json
K03;PASS;Trả một phần/nhiều lần/hết khớp số dư và trạng thái;sql-extended-v2-results.json
K04;PASS;0/âm/vượt/precision/method sai và PT010 bị chặn; fault liên kết không để chi rác;sql-extended-v2-results.json
K05;PASS;Trước ngày nguồn bị chặn; ngày hợp lệ lưu đúng;sql-extended-v2-results.json
K06;PASS;Tạm thiếu mapping SupplierDebt trong DB test: lỗi52602 rõ, rollback không đổi tiền; finally khôi phục đúng mapping fixture;sql-supplement-v2-results.json
K07;PASS;Hai session tranh nợ cuối qua hai thứ tự: một trả10000, một53003, một lịch sử và còn0;concurrency-v2-results.json
K08;PASS;Sửa/xóa lịch sử/chi/nợ/nguồn chốt bị chặn, không chuyển NCC;sql-extended-v2-results.json
K09;PASS;Hiện tại quá hạn đúng; as-of bỏ lần trả sau X, trước nguồn không có dòng và ngàymax không tràn;sql-extended-v2-results.json, sql-final-results.json
K10;PASS;Trả ngay giảm nợ gốc và không được tính thêm vào lịch sử trả nợ;sql-extended-v2-results.json
K11;FAIL;SQL quyền ADMIN/STORE/CASH và timeout PASS; native mở Công nợ crash khi có nợ chưa trả, không tiếp tục luồng trả được;gui-debt-stack.txt, ../t23-cong-no/bug-report.md
K12;FAIL;Reload không tải được toàn danh sách khi HistoryJson NULL; chưa thể xem mọi lịch sử/link nguồn trong UI;gui-debt-stack.txt, ../t23-cong-no/bug-report.md
'@
}
$summary=@();$total=0
foreach($folder in $records.Keys|Sort-Object){
 $path=Join-Path $group "$folder/test-results.md";$old=Get-Content $path -Raw
 if(-not(Test-Path (Join-Path $run "pre-test-$folder.md"))){Copy-Item -LiteralPath $path -Destination (Join-Path $run "pre-test-$folder.md")}
 $expected=@{};foreach($line in ($old -split '\r?\n')){if($line -match '^\| (T\d{2}-K\d{2}) \| (.*?) \|'){$expected[$matches[1]]=$matches[2]}}
 $task=$folder.Substring(0,3).ToUpperInvariant();$out=@("# $task — Biên bản đợt test G05",'', '27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.','', 'Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.','', 'Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.','', '| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |','|---|---|---|---|---|')
 $states=@();foreach($line in ($records[$folder] -split '\r?\n'|Where-Object{$_})){if($line -notmatch '^(K\d{2});(PASS|FAIL|BLOCKED|NOT RUN);(.*);([^;]+)$'){throw "Invalid row $line"};$v=@($matches[1],$matches[2],$matches[3],$matches[4]);$id="$task-$($v[0])";if(-not $expected.ContainsKey($id)){throw "Missing original $id"};$out+="| $id | $($expected[$id]) | $($v[2]) | $($v[1]) | $($v[3]) |";$states+=$v[1];$total++}
 if($states.Count -ne $expected.Count){throw "Checklist count mismatch $task"}
 $counts=[ordered]@{Task=$task;PASS=@($states|Where-Object{$_ -eq 'PASS'}).Count;FAIL=@($states|Where-Object{$_ -eq 'FAIL'}).Count;BLOCKED=@($states|Where-Object{$_ -eq 'BLOCKED'}).Count;NOT_RUN=@($states|Where-Object{$_ -eq 'NOT RUN'}).Count};$summary+=[pscustomobject]$counts
 $out+=@('',"Kết quả: $($counts.PASS) PASS / $($counts.FAIL) FAIL / $($counts.BLOCKED) BLOCKED / $($counts.NOT_RUN) NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.")
 $out|Set-Content $path -Encoding utf8
}
if($total -ne 55){throw "Expected55, actual$total"}
$summary|ConvertTo-Json -Depth 5|Set-Content "$run/checklist-summary.json" -Encoding utf8
$summary|Format-Table


