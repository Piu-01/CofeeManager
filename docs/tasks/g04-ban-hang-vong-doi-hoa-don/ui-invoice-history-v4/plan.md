# Plan bổ sung T17 — Lịch sử hóa đơn OrbitV4

Phạm vi và plan (kèm làm rõ hành vi kínhlúp/bộlọc) đã được user duyệt26/10/2026.
Trạng thái: ĐÃ TRIỂN KHAI, CHỜ DUYỆT ĐỢT TEST RIÊNG. Theo requirements nhóm,
không tạo requirements
task con hoặc thay checklist325ID. Sau triển khai duyệt một đợt test riêng.

## 1. Hướng triển khai

Giữ `InvoicesControl : OrbitRecordListControl`, query qua InvoiceService như hiện
tại. Bổ sung tùy chọn trình bày/toolbar/total cho control chung, chỉ màn history
bật; các module khác mặc định giữ nguyên. Không tạo bản sao framework danh sách,
theme mới hoặc WebView. Giữ MainForm/sidebar và cơ chế Form modal Windows.

Nguồn mẫu là docs/ui/invoice-history-v4/HTML+PNG/manifest; nội dung demo không
được dùng làm data/logic. README trong ZIP không tự mở rộng quy tắc cho toànapp.

**Làm rõ đã duyệt:** kínhlúp/Enter chỉ áp dụng từ khóa ô tìm kiếm; loại ngày,
khoảng ngày, trạng thái và kênh tự áp dụng ngay khi đổi. Filter/paging/refresh
dùng từ khóa đã submit gần nhất, không lấy nội dung đang gõ dở. Không còn nút
chữÁp dụng bộ lọc; giữ riêng draftsearch/appliedsearch ở UI, không đổiSQL.

## 2. Các bước triển khai tuần tự

### Bước1 — Bố cục card/toolbar dùng chung theo tùy chọn

Files: `UI/OrbitListWorkspace.cs`, `UI/OrbitRecordListControl.cs`,
`UI/OrbitSearchBox.cs` chỉ khi cần hỗ trợ kích thước/icon, `Theme/OrbitManagementTheme.cs`
chỉ khi tái sử dụng style hiện có cần helper nhỏ.

- Thêm chế độ toolbar hai hàng opt-in: hàng1 Search co giãn + hai nút icon
  kínhlúp/làm mới; hàng2 nhãnLọctheo, loại ngày, cụm từ–đến, trạng thái và kênh.
- Giữ event/query/loading/cancellation/generation sẵn có. NútApply bằng kínhlúp
  có AccessibleName/tooltip tìm kiếm; Enter tìm, không liveTextChanged. Các filter
  tự tải riêng, dùng từ khóa đã áp dụng như phần làm rõ, không submit draftsearch.
  Refresh giữ đường tải thật và query hiện hành; không gắn handler trùng.
- Header INVOICE WORKSPACE /Lịch sử hóa đơn/mô tả theo mẫu, vẫn lấy role/session
  từ shell hiện tại. Dời Chi tiết/In sang hàng hành động riêng, căn phải/icon in.
- Không thêm summary số lượng/khoảng ngày phía trên bảng. `Count` hiện đã bịẩn
  trong workspace chung: giữ trạng thái module khác, không đổi hide/show toànapp.
- Card chia filter/action/body/footer rõ. Toolrow wrap theo client nhỏ, không
  dùng minwidth làm control chui ra ngoài card. Chỉ bodygrid cuộn; footer/status
  luôn tiếp cận được ở1280×720/1366×768 tại125%.

Kiểm kỹ thuật triển khai: build và bounds/layout tối thiểu, không gọi đây là
nghiệm thu hình thức; hình thức user kiểm sau triển khai.

### Bước2 — Cụm khoảng ngày tái sử dụng

Files: `UI/OrbitDateRangeField.cs` mới nếu chưa có helper tương đương;
`UI/OrbitDateField.cs` chỉ thay khi cần style của composite; `InvoicesControl.cs`.

- Ghép hai OrbitDateField hiện có thành một cụm từ→đến có icon lịch; tái dùng
  parser/value/selection và popup native đã có, không dựng parserngày riêng.
- Giữ AccessibleName Từ ngày/Đến ngày, Tab theo thứ tự và chọn hết khi mới vào ô.
  Khi wrap, cụm ngày đi cùng nhau nếu vừa; ở client rất hẹp cho hai trường xuống
  hàng trong composite mà không mất giá trị/nhãn hoặc footer.
- Mặc địnhPaid/hômnay, giữ hai lựa chọnNgày lập/Ngày thanh toán và eventfilter
  hiện có. Khoảng typed inclusive theo contractT17. Input sai/range đảo phải
  báo rõ và không giả success/empty, không hardcode ngày của mẫu.

### Bước3 — Profile grid Lịch sử hóa đơn và badge

Files: `UI/OrbitDataGridView.cs` (enum/profile/painting),
`Modules/Sales/InvoicesControl.cs`. Dùng helper paintbadge/theme có sẵn.

- ProfileInvoiceHistory opt-in, bảng readonly/một dòng selected/radio marker
  theo grid hiện có; không cho sửa tài chính trong grid.
- Cột Code cố định đủ rộng mã10ký tự, đo text theo font/DPI khi cần và ưu tiên
  freezeCode; Created đủdd/MM/yyyyHH:mm, Channel badge, Customer fill, Payable
  căn phải vi-VN81.000 ₫, Status badge có chữ (đãtrả/chưatrả/hủy).
- Không để header/format ngày/tiền bị đổi bởi profile mặc định. Giá/tiền lấyDTO,
  không parse chuỗi đãformat để query hoặc tính lại snapshot.
- Nếu viewport nhỏ hơn tổngminwidth thì cuộn ngang grid; không thu font hoặc
  cắt mã để giả vừa. Chi tiết/In vẫn footeraction ngoài vùng cuộn và trạng thái
  enable theo lựa chọn/busy/error hợp lệ.

### Bước4 — Pager dùng TotalCount thật

Files: `UI/OrbitRecordListControl.cs`, `UI/OrbitPagination.cs`, `InvoicesControl.cs`.

- Thêm hook totalcount tùy chọn cho danh sách biết tổng; history lấy TotalCount
  từ SalesOrderRow do SQL hiện trả, không tạo migration/query mới. Các module
  chưa bật tiếp tục SetServerPage theo rowcount như trước.
- KnownTotal điều khiển cả Range/pageindicator/nútPrevious/Next và actionstate,
  không để UpdateActions bậtNext trên trang cuối đủ50dòng.0/1/50/51+ đều đúng.
- Nới indicator theo text khi nhiều trang; noun hóa đơn đúng, không hiện literal
  “nguyên liệu” từ overloadSetPage cũ. Giữ API mặc định cho inventory hiện dùng.
- Khi concurrent data khiến trang hiện tại rỗng, với history fallback về trang
  đầu/reload có generation bảo vệ để lấy tổng đúng. Chưa có rows thì không suy
  tổng từ dữ liệu trang trước; error giữ trạng thái rõ và không render0giả.
- Không fake1/1 hoặc đếm rowCount làm tổng. Search/filter mới vềpage0, query chậm
  cũ không ghi đè kết quảfiltermới, preserveinput khi lỗi.

### Bước5 — Ghép màn, chi tiết và hồ sơ

Files: `InvoicesControl.cs`, `docs/ui/README.md`, UX mục9 và hồ sơ proposal/plan/
test-results/report của bổ sung này. Nếu caller/profilecũ dư do thay đổi của mình
thì bỏ đúng phần đó, không refactor module khác.

- Ghép toolbar/dateRange/grid/pager qua helper đã có, bỏ nútApply dạng chữ cũ
  vì đã có nút kínhlúp cùng hành động. Giữ trạng thái/quyền và error handling.
- Chi tiết/In gọi InvoiceService.Detail rồi shared native manager, title/X/
  owner/ShowInTaskbarfalse, paidreadOnly, đơnmởQuayPOS theo contract. Không đổi
  InvoiceDetailDialog/PrintDocument layout hoặc tài chính vì user chỉ đổi trang.
- Nối đầy đủ trạng tháiLoading/Empty/Error/NotFound; không hiện bản trước như
  thể thuộc filter mới. Dữ liệu thật, không identity/date/code demo củaHTML.
- BuildDebug/Release và smoke compile/bounds kỹ thuật tối thiểu; sau đó trình
  đợt test riêng, không tự tickG04/T17PASS hoặc commit.

## 3. Ma trận kiểm sau duyệt test riêng

| ID bổ sung | Dữ liệu/thao tác | Expected |
|---|---|---|
| IH01 |Header/toolbar/Applyicon/refresh/action/footer ở125% |Đúng bố cục kỹ thuật theo mẫu, không summarytrên; hình thứcuser |
| IH02 |DateBasis2loại, đầu/cuối ngày, khoảng đảo/inputngàysai |Đúng typedrange/nhãn/error, giữinput và ngày mặc địnhcontract |
| IH03 |Mã/tên/SDT/status/channel, Enter/kínhlúp/refresh/clear |Đúng tập;0rowEmpty rõ, dataSQLreadonly |
| IH04 |0/1/49/50/51/100/101rows vớiTotalCount thật |Range/page/nút đúng, đủ50trang cuối khôngNext;fallbackstale đúng |
| IH05 |Querycũ chậm, cancel/error, đổifilter nhanh |Không ghiđèfiltermới;không0giả/selectioncũ mởsai đơn |
| IH06 |Mã dài10, ngàygiờ, tiền lớn, badge/status/customerdài |Không cắt mã/ngày, tiền đúngvi-VN/cănphải, badgecóchữ |
| IH07 |No selection/busy/notfound/detail, paid/open/canceled |Chỉ mở dònghợp lệ, nativeowner đúng, khóa tài chính/QuayPOS đúng |
| IH08 |PhímTab/Enter/click selectall/datepopup, resizeclient125% |Input/caret/validation/focus/bounds/scroll dùng được |
| IH09 |ADMIN/CASH/STORE |Quyền đọc/in/menu giữ nguyên, STOREbịBLLchặn |
| IH10 |Modulekhác dùngworkspace/grid/pager/search/date |Opt-in không làm lệch chức năng/layout/binding hiện có |

Tham chiếu T17-K01/K02/K04/K05/K07/K08/K09 theo ảnh hưởng; giữ nguyên mọi ID và
expected gốc. Tests giao dịch/bất biến đọc/in trên SQLServer DBtest riêng có
marker; snapshot tiền/kho/điểm/receipt trước-sau. Dùng fixture riêng cho51+rows,
không giả tổng ởUI hoặc thay expected để né lỗi.100/150%DPIchưa chạy ghiNOT RUN,
không đổiScale; agent không chạytestthịgiác/chụpảnhsoapp.

## 4. Chuyển phiên bản source và rủi ro

Đợt native modal hiện tại có475Khôngin đãđạt,476Có→Hủy chưa userxácnhận. Trước
sửa source, chốt biên bản lượt đó theo sourcehash đang test, giữ phần chưa chạy;
không đổi source giữa test hoặc gán kết quả cũ cho UIhistoryV4. Không tự chốt/
hủy476 hoặc tạoDBthứ4; review tài nguyên theo manifest active.

Rủi ro chính: sharedworkspace làm đổi mànkhác; grid paint ghiđèformat; pager
UpdateActions overridecount; calendar focus/selectall; detail trỏ stale row.
Kiểm soát bằng opt-in mặc địnhgiữcũ,TotalCounttyped,generation và ma trận hồi quy
trên. Nếu cần đổi data contract/nghiệp vụ thì cập nhật/xin duyệt phần phát sinh,
không tự thêmSQL hoặc hành vi ngoài phạm viUI đã chốt.
