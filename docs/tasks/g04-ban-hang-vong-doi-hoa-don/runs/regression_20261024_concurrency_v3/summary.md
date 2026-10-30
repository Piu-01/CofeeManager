# Bàn giao G04-TEST-06 / G04-CONC-01 —24/10/2026

Đã được người dùng duyệt cả đợt bổ sung. Không có thay đổi C#/UI/API, không commit.

- Test v3 đọc rowversion typed trên SQL, so mảng int/count rõ ràng; negative102/102
  không được PASS. Giữ v1/v2/raw audit, không sửa expected để nhận mọi lỗi.
- Actual plan schema015: Clustered Index Scan9rows trong TR_CTTT_KiemTra. Điều tra
  tái hiện0/1205, graph v2 đã có vòng chờ kho↔HOADON. Before/manifest giữ Aborted
  do instrumentation/summary của lượt điều tra; interpretation.md phân biệt rõ.
- Migration016 chỉ đổi trigger: affected MaHD theo thứ tự, khóa PK seek trước kiểm
  trạng thái rồi tổng phân bổ; giữ51002/51003 và rollback cả batch. Actual plan sau
  sửa Seek1row. Không sửa/checksum lại001–015, khôngNOLOCK hoặc auto retry.
- After **68/68PASS**, gồm18cặp/36session state0:0:10vòng tranh tồn hai chiều đều
  0/52203;4vòng giao0/52103; cùng request0/0;khác request0/52102;sửa dòng0/52103;
  tranh bàn0/52104. Tiền/kho/điểm/snapshot/receipt và paid/multiorder guards đạt.
- Empty **3/3PASS**: baselineclean001–016 không seed;rerun016AlreadyApplied;
  checksum mismatch bị từ chối,trigger không đổi.
- Development015→016 sau backup COPY_ONLY CHECKSUM; tồn,điểm/hạng,hóa đơn,tài khoản
  không đổi. Manifest ở ../development016_20261024_132735_d5f3a71d/manifest.json.
  Không seed/fixture development;backup giữ đến nghiệm thu/chấp nhận development.

Checklist56ID hiện **28PASS,0FAIL,28BLOCKED**. Toàn G04 chưaPASS; các nhánh GUI/
keyboard/POS/phiên/in,timeout trước commit,policy/recipe hai thứ tự và legacy-invalid
chưa đủ bằng chứng. Hình thứcSKIPPED theo phân công,100/150%NOT RUN; không đổiScale.
52unit ở v2 được kế thừa vì không đổi C#; không gọi đó là một lượt unit mới.
