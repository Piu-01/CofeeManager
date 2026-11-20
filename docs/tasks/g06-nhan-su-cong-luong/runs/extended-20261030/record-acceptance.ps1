$ErrorActionPreference='Stop'
$group=$PSScriptRoot
$run=Join-Path $group 'runs/extended-20261030'
$original=(Get-Content (Join-Path $run 'results.json') -Raw|ConvertFrom-Json).Cases
$supplement=Get-Content (Join-Path $run 'supplement/results.json') -Raw|ConvertFrom-Json
$maps=@{
'T24-K01'=@('FAIL','Full/part CRUD service đúng ở mức thông thường; biên tiền hợp lệ overflow và text grid sai (G06-B01/B02). Native editor chưa nhập được.');
'T24-K02'=@('FAIL','Tên/SDT/FK/NULL/trùng/âm/lẻ/quámax bị chặn đúng; ca biên mở rộng Name50Phone15MaxMoney FAIL overflow; lỗi SqlWideName của harness được kiểm bổ sung, không sửa expected.');
'T24-K03'=@('PASS','Unique employee/account, username/canonical login được chặn đúng; hai session tạo account cùng nhân viên:2waiters,1success/1uniqueerror.');
'T24-K04'=@('BLOCKED','BLL reset/login cũ-mới và DTO không có secret PASS; biên mật khẩu8/128,7/129 và confirmation đã kiểm. Native reset dialog/grid/log thao tác chưa nghiệm thu đủ.');
'T24-K05'=@('PASS','Nghỉ việc khóa account, session cũ bị từ chối; hóa đơn/công lịch sử còn nguyên.');
'T24-K06'=@('BLOCKED','Race haiADMIN tự hạ quyền2waiters/1success; admin cuối lock/retire/delete/demote bị chặn; bootstrap bị chặn qua SQL followup. Native thao tác còn BLOCKED.');
'T24-K07'=@('PASS','Đổi chức vụ không đổi role; CASH/STORE đang hoạt động không đổi hồ sơ/account qua BLL/SQL.');
'T24-K08'=@('PASS','Mức lương snapshot legacy và snapshot mới không đổi sau cập nhật hồ sơ; đối chiếu T26-K06.');
'T24-K09'=@('PASS','NV có account/hóa đơn bị chặn xóa; NV mới không tham chiếu xóa được, không cascade.');
'T24-K10'=@('BLOCKED','CASH/STORE service/SQL từ chối đúng; filter typed/phiên bản/receipt đúng. Reload/bàn phím native chưa chạy được.');
'T25-K01'=@('PASS','Cùng ngày8h, qua đêm8h; typed giờ và tổng thời lượng khớp.');
'T25-K02'=@('PASS','Ra thiếu vào, bằng/trước vào, khác NgayLam và subsecond đều bị từ chối, không ghi.');
'T25-K03'=@('PASS','Nghỉ/vắng có giờ bị chặn; nghỉ/vắng không giờ lưu được; thiếu giờra phân biệt Incomplete, chặn chốt.');
'T25-K04'=@('PASS','Trùngkey và overlap rollback; bổ sung interval chứa/một phần/qua ngày/mở đều bị chặn.');
'T25-K05'=@('PASS','Ranh giới liền kề được lưu; chồng1giây bị chặn.');
'T25-K06'=@('PASS','Hai session có barrier thật, quan sát2waiters: đúng1success,1conflict; chỉ1dòng cuối.');
'T25-K07'=@('PASS','NVnghỉ/ca không tồn tại không ghi mới; dữ liệu trước nghỉ vẫn đọc được.');
'T25-K08'=@('PASS','Kỳ legacy và snapshot mới đều chặn insert/update/delete; kiểm trực tiếp SQL/quyền runtime và service.');
'T25-K09'=@('PASS','Ca30/09 qua01/10: tháng9=8h, tháng10=0dòng; không cộng hai lần.');
'T25-K10'=@('BLOCKED','Filter tháng/NV/status và role bằng typedservice đúng; native bộ lọc/reload chưa thao tác được.');
'T25-K11'=@('PASS','Batch nhiềuNV hợp lệ commit; FK/overlap/trùngkey một dòng rollback toàn batch và receipt.');
'T26-K01'=@('PASS','SQL fulltime10.800.000, parttime1.600.000 đúng tính tay; no-prorating và NV đã nghỉ kỳ lịch sử có kiểm bổ sung. Phạm vi tiền lớn bị bug chung, không suy PASS cho biên đó.');
'T26-K02'=@('PASS','Thường36/tăngca4 không nhân lặp;0,5VND→1; cộng36giây một lần→0,01h→1VND, không làm tròn từng dòng.');
'T26-K03'=@('PASS','Thiếu cảhai/thiếu ra và overlap dữ liệu fixture đều hiện issue, không commit được.');
'T26-K04'=@('PASS','Tháng0/13,năm1999,âm/lẻ/precisiongiờ/khấu trừvượt tổng bị chặn; SQLmonth13 kiểm riêng.');
'T26-K05'=@('PASS','Hai session chốt cùng kỳ:2waiters/đúng1success/1BANGLUONG; replay request và mismatch được kiểm.');
'T26-K06'=@('PASS','Sau đổi tên/hợp đồng/lương, snapshot cũ giữ nguyên; owner update/delete snapshot bị trigger chặn.');
'T26-K07'=@('PASS','Runtime DML bị từ chối; kỳ snapshot mới chặn service insert/update/delete công; owner không sửa/xóa BANGLUONG đã chốt. RawError harness kiểm lại theo baseline transaction.');
'T26-K08'=@('PASS','Hai session chi cùngMaBL:1phiếu/đúng thực lĩnh,1success/1reject; replay không tạo thêm chi.');
'T26-K09'=@('PASS','Fault trigger sau ghi expense trước receipt: toàn bộ rollback; không phiếu rác/trạng thái đãchi giả.');
'T26-K10'=@('BLOCKED','Service/SQL từ chối chi0, không phiếuSoTien0. Text/trạng thái và khóa action native chưa kiểm được.');
'T26-K11'=@('PASS','Unpaid/Paid tách biệt, SUMphiếu cho hai bảng lương chỉ10.800.000, không cộng snapshot lầnhai.');
'T26-K12'=@('BLOCKED','Role SQL và BLL từ chối đúng; timeout trướccommit khôngghi/khôngreceipt; timeout saucommit đối chiếu thấy1phiếu rồi replay antoàn. PendingDPAPI/restart/scopedDB/corrupt kiểm được; native unknown/reload/ownerlồng chưa đủ.');
}
foreach($task in @('t24-nhan-vien-tai-khoan','t25-cham-cong','t26-chot-chi-luong')){
 $p=Join-Path $group "$task/test-results.md";$text=Get-Content $p -Raw
 Copy-Item $p (Join-Path $run "$task-before-test.md")
 $lines=@("# $($task.Substring(0,3).ToUpper()) — Kết quả nghiệm thu G06",'', 'Đợt được duyệt30/10/2026, tổng hợp31/10/2026. SDK10.0.401; SQL Server localhost, baseline+seed/migrations001–024; DB test riêng có marker. Source chưa commit, SHA256 trước/sau không đổi (src/database/tests/tools). Không nâng development. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN; app component thực DeviceDpi120.','', 'Bằng chứng/lệnh/input/expected/actual chi tiết: [runner acceptance](../runs/acceptance_20261030_155806_b10d6581/manifest.json), [extended](../runs/extended-20261030/results.json), [bổ sung](../runs/extended-20261030/supplement/results.json), [SQL followup](../runs/extended-20261030/transaction-followup-v2.json), [native trace](../runs/extended-20261030/native-trace.json), [audit](../runs/extended-20261030/final-audit.json). Các script/project đặt cạnh kết quả để tái hiện. Expected gốc dưới đây giữ nguyên.','', '| ID | Expected gốc | Actual | Kết quả |','|---|---|---|---|')
 foreach($line in ($text -split "`r?`n")){if($line -match '^\| (T\d+-K\d+) \| (.*?) \|'){$id=$Matches[1];$expected=$Matches[2];$m=$maps[$id];$lines+="| $id | $expected | $($m[1]) | $($m[0]) |"}}
 $lines+=@('','Lịch sử trước nghiệm thu được giữ tại runs/extended-20261030; không đổi expected để né lỗi. PASS là phạm vi ca gốc đã kiểm ở tầng được nêu; không suy native/hình thức PASS từ service/component. Các nhánh native chưa chạy giữ BLOCKED. Xem [báo cáo nhóm](../report.md) để biết giới hạn và đợt bug đề nghị.');$lines|Set-Content $p -Encoding utf8
}
Copy-Item (Join-Path $group 'report.md') (Join-Path $run 'report-implementation-before-acceptance.md')
