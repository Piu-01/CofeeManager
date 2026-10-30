$ErrorActionPreference='Stop'
$group=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$run='acceptance-wave_20261023_195243_f9876e48'
$results=@{
14=@(
@('BLOCKED','Ba kênh tạo đúng; tổ hợp sai bàn/ship rollback. Chưa đủ biên tên/SDT/địa chỉ.'),
@('BLOCKED','NULL và FK khách sai đã kiểm; actor/stamp/nghỉ việc chưa phủ đủ.'),
@('BLOCKED','Tổng sau thêm đúng; chưa quan sát header0 trước trigger trong transaction.'),
@('PASS','Hai MaDong khác ghi chú giữ riêng; foreign MaDong bị chặn52101.'),
@('PASS','Sửa giữ25.000; thêm sau đổi giá lấy30.000.0/âm/intMax+1 bị chặn.'),
@('PASS','Giảm99%: bớt từ50.000→25.000, giảm24.750; xóa về0/0, transaction thành công.'),
@('BLOCKED','Ngừng bán: add/tăng bị chặn, giảm/xóa được, checkout bị chặn. Thiếu recipe chưa chạy riêng.'),
@('PASS','Sửa đơn paid/canceled bị52102; snapshot DB không đổi; stale version52103.'),
@('BLOCKED','Hai kết nối:0/52104; đúng một header/một dòng. Chưa kiểm loser tải mã thật qua TableSelectCurrent.'),
@('PASS','Hai kết nối cùng revision:52103/0; không ghi đè im lặng.'),
@('FAIL','Ba fault header/line/receipt rollback toàn snapshot, transaction0. Timeout classifier mất mã exception: G04-TEST-05.'),
@('PASS','Xóa hết giữ đơn mở0; checkout51102; hủy thành công không tiêu hao.')
)
15=@(
@('PASS','Một payment và hai dòng cùng PT chốt đúng; replay không tăng kho/điểm/payment.'),
@('BLOCKED','KM đổi revision dù giữ tỷ lệ:52201, không payment. Policy/khách/đối tác thay đồng thời chưa đủ.'),
@('PASS','Thiếu/dư1₫, unknown/PT010 bị52202; snapshot tiền/kho/điểm/receipt không đổi.'),
@('PASS','KM100% ship0 chốt được bằng TVP rỗng; có tiêu hao, không payment.'),
@('PASS','KM100% ship15.000: thu15.000, không lịch sử điểm.'),
@('BLOCKED','Thiếu NVL rollback toàn snapshot; ngừng bán52204. Nhánh thiếu recipe chưa chạy riêng.'),
@('BLOCKED','Ba ly ở hai dòng group theo recipe đúng, tồn trừ một lần. Chưa đủ nhiềuSP chungNVL và định mức1.2345.'),
@('FAIL','Fixture so điểm theo gross sai với hạng10%; điều tra net900→0 điểm đúng. G04-TEST-01, không sửa FAIL.'),
@('PASS','Phí0%/100% và50%×25.001 làm tròn12.501; ship không trừ; đổi tỷ lệ không đổi đơn paid.'),
@('PASS','Hai kết nối cùng đơn:0/52102; chỉ một payment/history. Replay cùng RequestId không ghi lặp.'),
@('PASS','Hai đơn tranh tồn cuối:0/52203; đúng một paid, tồn không âm, đơn thua không có payment.'),
@('PASS','Fault sau tiêu hao/payment/điểm: toàn header/dòng/tiền/kho/điểm/snapshot/receipt như trước, transaction0.'),
@('PASS','User WITHOUT LOGIN thuộc CafeManagerApp bị229 khi DML6bảng; STORE sales bị51605. Không là bằng chứng login runtime.'),
@('FAIL','Ngày tương lai bị chặn đúng52205 nhưng test đòi51108: G04-TEST-02; nhánh hậu điều kiện chưa phủ đủ.'),
@('BLOCKED','Clean/demo001–015 apply được, LegacyUnknown không bịa tỷ lệ; rerun/checksum/preflight lỗi chưa chạy nghiệm thu.')
)
16=@(
@('BLOCKED','Chưa thao tác tạo/mở lại qua POS; fixture GUI compile lỗi G04-TEST-03.'),
@('BLOCKED','SQL dòng món đạt subcase; chưa kiểm binding/handler POS. G04-TEST-03.'),
@('BLOCKED','Unit CashChange đạt; chưa nhập100.000 qua dialog và commit81.000. G04-TEST-03.'),
@('BLOCKED','Unit biên tiền đạt; GUI invalid/đóng dialog chưa chạy. G04-TEST-03.'),
@('BLOCKED','Replay SQL đạt; double-click/Enter/F8 và enable nút chưa thao tác. G04-TEST-03.'),
@('BLOCKED','SQL stale/closed chặn đúng; POS cũ refresh chưa kiểm. G04-TEST-03.'),
@('BLOCKED','Receipt/readback có dữ liệu; timeout fixture FAIL05, UI unknown chưa kiểm.'),
@('BLOCKED','SQL hủy mở/paid refusal đạt; UI/bàn refresh chưa kiểm. G04-TEST-03.'),
@('BLOCKED','SQL empty/thiếu kho/ngừng bán đạt; thông báo/giữ input UI chưa kiểm.'),
@('BLOCKED','Unit/SQL STORE bị chặn; menu/PT010 trên UI chưa thao tác.'),
@('BLOCKED','Native125%1row/3columns, CTA trong client; chưa đủ resize/tìm/bàn/nhiều dòng. Hình thức SKIPPED.'),
@('BLOCKED','SQL trừ/cộng một lần đạt; refresh bàn/khách/tồn từ POS chưa thao tác.')
)
17=@(
@('FAIL','Fixture cố UPDATE ngày đơn paid bị chặn; ca ngày/paging phụ thuộc FAIL, chưa chứng nhận filter. G04-TEST-04.'),
@('PASS','Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot.'),
@('BLOCKED','Native preview100dòng/210entry→13trang exit0. Chưa nghiệm thu UI preview/in bản thật, không kiểm hình thức.'),
@('BLOCKED','Có nội dung print từ snapshot; chưa thao tác/đối chiếu output nghiệp vụ đầy đủ.'),
@('BLOCKED','Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu.'),
@('BLOCKED','Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy.'),
@('BLOCKED','Chưa thao tác in lại nhiều lần và đối chiếu DB; probe một lần không thay ca này.'),
@('BLOCKED','SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm.'),
@('BLOCKED','SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy.')
)
18=@(
@('BLOCKED','SQL DeliveryGet non-delivery bị52501; màn hình/biên người nhận chưa đủ.'),
@('PASS','Cho giao→Dang giao→Da giao được; nhảy bước/lùi bị52502.'),
@('PASS','Đơn paid chuyển giao giữ tổng/giảm/ship/phí/ngàyTT; không tạo payment mới.'),
@('BLOCKED','SQL hủy mở→Huy/Huy giao không tiêu hao; paid cancel52102. UI chưa kiểm.'),
@('BLOCKED','Stale revision bị52103 trong SQL; hai kết nối delivery riêng chưa chạy.'),
@('BLOCKED','5unit transition/quyền đạt; chưa đủ ADMIN/CASH/STORE trên UI/runtime SQL delivery.'),
@('BLOCKED','SQL lọc mã/SDT/Da giao trả đúng1dòng; refresh/reload màn hình chưa chạy.'),
@('BLOCKED','Chưa thao tác hướng dẫn giao thất bại paid; không suy PASS từ đọc source.')
)
}
$summary=@();$folders=@{14='t14-tao-sua-don';15='t15-checkout';16='t16-pos-huy-don';17='t17-tra-cuu-in';18='t18-giao-hang'}
foreach($task in 14..18){
 $file=Join-Path $group ($folders[$task]+'/test-results.md');$old=Get-Content $file -Raw
 $rows=@([regex]::Matches($old,'(?m)^\| T\d\d-K\d\d \|[^\r\n]+')|ForEach-Object{$_.Value})
 if($rows.Count-ne$results[$task].Count){throw "Original checklist count mismatch T$task"}
 $body="# Biên bản nghiệm thu T$task — đợt23/10/2026`r`n`r`nNgười dùng đã duyệt cả đợt bằng 'duyệt'. SQL Server localhost thật, DB test riêng`r`nschema001–015; SDK10.0.401. Source đóng băng tại [manifest](../runs/$run/manifest.json),`r`nkhông sửa sản phẩm/migration/test đã chạy. Chưa nghiệm thu hoàn tất.`r`n`r`nEvidence: [SQL subset +18unit](../runs/acceptance_20261023_195253_7d540db5/manifest.json),`r`n[SQL mở rộng](../runs/$run/extended-results.json), [phần còn lại/điều tra](../runs/$run/remaining-results.json),`r`n[native binding/print](../runs/$run/native-checkout-print.json), [34unit regression](../runs/$run/regression/regression.trx).`r`nSQL và dữ liệu/expected/actual từng subcase lưu nguyên trong JSON và script cùng thư mục.`r`nOperator Windows identity không thay bằng chứng login/quyền runtime; ca DML dùng user WITHOUT LOGIN riêng.`r`n`r`nPASS dưới đây chỉ cho checklist gốc đã đủ bằng chứng; subcase mở rộng trong plan chưa chạy vẫn nêu rõ.`r`nFAIL fixture không tự chuyển thành PASS của sản phẩm. BLOCKED là phần chưa đủ do bộ kiểm thử/luồng GUI.`r`n`r`n| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |`r`n|---|---|---|---|---|`r`n"
 for($i=0;$i-lt$rows.Count;$i++){$parts=$rows[$i].Split('|');$id=$parts[1].Trim();$expected=$parts[2].Trim();$state=$results[$task][$i][0];$actual=$results[$task][$i][1];$body+="| $id | $expected | $actual | $state | JSON/script liên kết trên; lọc Id tương ứng |`r`n";$summary+=[pscustomobject]@{Id=$id;Status=$state}}
 $body+="`r`nHình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.`r`nNative probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.`r`nCác nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe`r`nđồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.`r`nKhông tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.`r`n"
 Set-Content $file $body -Encoding utf8
}
$summary|Export-Csv (Join-Path $PSScriptRoot 'checklist-results.csv') -NoTypeInformation
$summary|Group-Object Status|Select-Object Name,Count|ConvertTo-Json|Set-Content (Join-Path $PSScriptRoot 'checklist-counts.json') -Encoding utf8
