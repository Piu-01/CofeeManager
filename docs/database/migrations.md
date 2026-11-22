# Áp dụng migration và tách principal

1. Cài mới: DB_Schema.sql → seed một lần nếu cần demo → migrations tăng version.
   Seed chưa được chạy lại; không chạy CREATE DATABASE vào DB đã có.
2. Đóng app khi bootstrap sequence; runner khóa migration và bảng nguồn, tính
   max suffix hiện có. Không có writers bên ngoài lấy MAX+1 trong thời gian này.
3. Migration principal có quyền DDL/DML setup; app dùng Windows login chuyên
   biệt ánh xạ role CafeManagerApp, không sa/sysadmin/db_owner.
4. `Apply-Migration.ps1 -Database <DB đã chọn> -Version 1` đọc SHA256 bytes SQL,
   transaction cùng connection qua GO và sp_getapplock; ghi version sau DDL.
5. Version/checksum trùng → AlreadyApplied; checksum khác → từ chối; dùng
   version mới thay vì sửa migration đã phát hành. Lỗi rollback DDL/version.
6. Connection runtime tách `CAFEMANAGER_APP_CONNECTION` (app) và
   `CAFEMANAGER_MIGRATION_CONNECTION` (test setup); không lưu trong Git/log.
   Runner PowerShell migration dùng Windows identity quản trị của process.
7. Local trust certificate chỉ localhost development; LAN phải cấu hình chứng
   chỉ hợp lệ. Mẫu `database/security/provision-app-user.sql` nhận tham số
   WindowsLogin/DatabaseUser, login đã được quản trị tạo; không password mặc định.

G02 đã có code setup/login/shell/danh mục; đợt nghiệm thu riêng chưa chạy.
Thiếu cấu hình báo rõ, không fallback administrator/DBtest. Contract:
[auth-contract.md](auth-contract.md), [catalog-contract.md](catalog-contract.md).

## Chuỗi migration G02

```powershell
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon>
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon> -Version 2 -File .\database\migrations\002_auth_and_bootstrap.sql
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon> -Version 3 -File .\database\migrations\003_reference_catalogs.sql
```

002 tạo role/setupstate/loginKey/stamp/procedure;003 tạo catalogrevision,
systemexpense/payment-support/receipt/CRUD/lookup. Clean không cần seeddemo;
demo chỉseed trước001 trên schema rỗng. Preflightconflict rollback, giữ dữ liệu
và báo người cài; không reset cấu hình để bỏ qua xung đột. Rerun qua runner
version/checksum, không chạy SQL003 trực tiếp nhiều lần. Các DB smoke có bản
migration tạm trong triển khai được giữ làm lịch sử; chạy bản cuối trên DB
test mới, không sửa checksum đã ghi để ép rerun.

## Kiểm chứng sau duyệt test

`Test-ContractMigration.ps1` tạo DB T03 mới, seed và empty, áp dụng migration,
đối chiếu rows/FK/version, hai session cấp mã, quyền user WITHOUT LOGIN.
`Test-DataAccess.ps1` tạo DB T05 mới, migration và SQL test-only fixtures,
chạy xUnit; không dùng DB ứng dụng. Test/bug ở task con trong G01, giữ các lượt.
G01 đã nghiệm thu45/45; lịch sử tại reportG01. RunnerG02/collectorUI chỉ chạy
sau duyệt đợt testG02; build và dependency smoke được ghi riêng trong triển khai.

## Development cố định (18/10/2026)

Ngoại lệ development được duyệt: `Initialize-Development.ps1` dùng Windows operator
để provision SQL login runtime `CoffeeManager_Development_Runtime` vào đúng DB
`CoffeeManager_Development`, chỉ role `CafeManagerApp`; không tạo Windows account.
SQL login setup riêng tạm thời gọi BootstrapService/contract hiện có rồi bị thu hồi.
Chỉ công cụ provision development kích hoạt hai placeholder CASH/STORE chính xác;
bootstrap của ứng dụng thông thường không tự bật chúng. Không reset account đã active.
Không đổi migration hoặc contract đăng nhập. Cấu hình và bí mật lưu DPAPI CurrentUser;
profile F5 không chứa credential. Xem README và biên bản development-handoff-results.md
trong hồ sơ G02. Quy trình `--setup` thông thường giữ nguyên.

## Chuỗi G03 (004–008)

Chạy `Apply-Migration.ps1` lần lượt Version4..8 với file tương ứng:
004_inventory_materials.sql →005_products_recipes.sql →006_tables_areas.sql →
007_customers.sql →008_promotions_loyalty.sql. Không sửa001–003/baseline/seed.
Preflight không hợp lệ dừng, không tự sửa dữ liệu hoặc ghi đèchecksum.

Development cố định: `tools/database/Update-DevelopmentG03.ps1` kiểmmarker,
backupCOPY_ONLY CHECKSUM khi đang dưới008, áp dụng migration, đối chiếu tồn/
điểm+hạng/tài chính hóađơn. Khôngseed/resetaccount/ghi fixture; app phảiđóng.
Backup giữ tới khi G03 nghiệm thu và người dùng chấp nhận, đường dẫn trong manifest.
Initialize-Development gọi cập nhật này sau003; app runtime không tựchạy migration.
Contract: material-contract.md, product-contract.md, table-contract.md,
customer-contract.md, loyalty-contract.md. Technicalsmoke không là nghiệm thu.

009_promotion_legacy_codes.sql thuộc T13, chạy sau008: cho phép sửa/xóa mã seed cũ
như KM24BF, giữ mãPK; tạo mới vẫn KM+8số. Development/runnerG03 hiện đến009.

## G04 —010 đến015 (23/10/2026)

Sau009 áp010_sales_orders →011_checkout →012_sales_cancel →013_invoice_queries
→014_delivery →015_sales_read_normalization bằng Apply-Migration với Version tương ứng.
010thêm Revision header/API đơn,011snapshot+checkout bắt buộc actor/version/token,
012hủy có quyền/version,013read-onlyquery,014tiến độ giao hiện tại.015hoàn thiện
optional NULL và đọc ưu đãi đơn chốt từ snapshot; không đổi checksum010–014.

Seed baseline vẫn chỉ trên schema rỗng trước migrations; không gọi chữ ký checkout
baseline sau011. Đơn đã trả cũ được LegacyUnknown cho nguồn/tỷ lệ, không tái tính
lại tiền/kho/điểm. App không tự migrate. Update-DevelopmentG04.ps1 kiểm marker,
backup COPY_ONLY CHECKSUM, checksum001–015 và so sánh số dư/tài khoản; không reseed.
Khởi tạo development mới vẫn đi Initialize-Development (đến009) rồi Update-DevelopmentG04.
Contract: sales-contract.md, checkout-contract.md, invoice-contract.md, delivery-contract.md.

## G04 —016 payment guard point locks (24/10/2026)

016_payment_guard_point_locks.sql chạy sau015 bằng Apply-Migration.ps1 Version16.
Chỉ ALTER TR_CTTT_KiemTra, không backfill hoặc sửa rows. Khóa affected MaHD theoPK
seek, tránh scan/range khóa sang đơn đang chờ kho. Giữ closed/overpayment guards
và xử lý nhiều MaHD trong cùng trigger. Không sửa baseline/checksum001–015.
Actual plan trước/sau: Clustered Index Scan9rows→Seek1row trên fixture đã lưu tại
docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/regression_20261024_concurrency_v3/.
68ca SQL regressionPASS; đây không là nghiệm thu toàn bộ GUI G04.

Development đã có015: dùng tools/database/Update-DevelopmentG04Concurrency.ps1;
chỉ nhận DB CoffeeManager_Development có marker/schema015–016, backup COPY_ONLY
CHECKSUM trước nâng, kiểm checksum001–016 và đối chiếu tồn/điểm/hóa đơn/tài khoản.
Fresh development: Initialize đến009, Update-DevelopmentG04 đến015 rồi updater016.
Không seed lại. App đang chạy thì updater dừng, không tự kill process người dùng.

## G04 —030 draft order flush (11/11/2026)

030_draft_order_flush.sql chạy sau029 bằng Apply-Migration.ps1 Version30. Thêm TVP
dbo.SalesDraftLines (RowNo/Product/Quantity/Note/Price) và procedure
dbo.sp_TaoHoaDonTuNhap: nhận header + toàn bộ dòng nháp, tự mở transaction, dùng
sp_SalesBegin/sp_SalesFinish với Operation SalesCreate để giữ idempotency/reconcile,
khóa bàn UPDLOCK/HOLDLOCK và trả 52104 khi bàn đã có đơn, kiểm giá dòng bằng
SANPHAM.GiaBan (lệch giá trả 52105), chèn HOADON + CHITIET_HD rồi
sp_SalesRefreshDiscount. Không sửa procedure/rows của028–029; không đổi
checksum001–029.

Đã áp migration trên DB test riêng CoffeeManager_Test_G04_20261111_draftflush
(clone COPY_ONLY từ CoffeeManager_Test_DemoSeed2 đang ở schema029) và kiểm
type/proc/grant tồn tại. Chưa chạy ca nghiệp vụ DK01–DK10 trên DB này — chờ duyệt
đợt test.

Cập nhật 15/11/2026 (người dùng duyệt): 030 cũng đã áp lên
CoffeeManager_Development đang ở schema029, vì app gọi proc mới trong khi DB thiếu
đối tượng (bug G04-POS-04). Backup COPY_ONLY + CHECKSUM đã Verify
(CoffeeManager_Development_before030_20261115.bak), Apply-Migration Version30 trả
Applied, SHA-256 của cả 30 file khớp checksum trong dbo.SchemaMigrations nên
001–029 không lệch; type SalesDraftLines, proc sp_TaoHoaDonTuNhap và GRANT EXECUTE
cho CafeManagerApp đã có. Không reseed, không sửa 028–029. Chưa chạy DK01–DK10 —
vẫn chờ duyệt đợt test.

## G04 —032 sales pending payment (16/11/2026, T33)

032_sales_pending_payment.sql chạy sau031 bằng Apply-Migration.ps1 Version32. Thêm
trạng thái HOADON Cho thanh toan: mở rộng CHK_HD_TrangThai và index lọc UX_HD_BanMo,
ALTER vw_TrangThaiBan, tạo sp_SalesCheckPayable (còn thu/hủy) tách khỏi
sp_SalesCheckOpen (còn sửa), tạo sp_YeuCauThanhToan/sp_MoLaiHoaDon dùng receipt
SalesRequestPay/SalesReopen, mở rộng whitelist sp_SalesReconcile, cập nhật các đọc/
đếm (sp_SalesReadCore, sp_SalesOpenOrders, sp_TaoHoaDon, sp_TaoHoaDonTuNhap,
sp_ReportDashboard, sp_DashboardAnalyticsRead/Sources) và ba trigger
TR_CTTT_KiemTra/TR_DiscountSnapshot/TR_TieuHao_Kho. Không sửa TR_CTHD_TongTien,
TR_HD_Chot hay checksum001–031; GRANT EXECUTE hai procedure mới cho CafeManagerApp,
không cấp quyền bảng.

Đã áp thành công trên DB scratch riêng CoffeeManager_Test_G04_20261116_pendingpay
(clone schema+seed) đến Version32; chưa áp lên CoffeeManager_Development và chưa
chạy PP01–PP12 — chờ duyệt đợt test. Acceptance SQL:
database/tests/g04/acceptance-pending-payment.sql; milestone kỹ thuật v16 nằm trong
Invoke-G04Technical.ps1. Lưu ý migration031 thiếu GRANT EXECUTE cho
sp_SalesNoteSuggestions (ghi nhận riêng, ngoài phạm vi 032).

## G04 — 034 sales table sessions (16/11/2026, T34)

034_sales_table_sessions.sql chạy sau033 bằng Apply-Migration.ps1 Version34; tiền đề
`SchemaMigrations Version=32` (034 không phụ thuộc dữ liệu nhãn của 033). Preflight
chặn nếu PHIEN_BAN đã tồn tại hoặc có bàn mở nhiều đơn, rollback nguyên trạng. Thêm
bảng dbo.PHIEN_BAN (GioVao/GioRa, MaNVMo/MaNVTra char(5), Revision rowversion),
unique filtered index UX_PHIEN_BAN_Mo(MaBan) WHERE GioRa IS NULL, DENY
INSERT/UPDATE/DELETE cho CafeManagerApp; ALTER HOADON thêm MaPhien int NULL (FK
FK_HD_Phien, IX_HD_Phien), không backfill. ALTER vw_TrangThaiBan thêm MaPhien,
SessionRevision, Retained; ALTER sp_TableList/sp_TableSelectCurrent thêm 3 cột;
ALTER sp_TaoHoaDon/sp_TaoHoaDonTuNhap tự mở/nối phiên; ALTER sp_SalesReadCore trả
MaPhien + SessionRevision; tạo sp_TraBan (receipt TableClose, 52110/52111/52103) và
GRANT EXECUTE. Không sửa checksum001–033, trigger hay dữ liệu cũ.

Đã áp thành công trên DB scratch CoffeeManager_Test_G04_20261116_pendingpay (đang
schema032, chưa áp 033) bằng Apply-Migration Version34; kiểm Version=34, bảng/deny/
index/view 3 cột mới, sp_TraBan + grant, sp_TableList 10 cột và sp_SalesOrderGet 27
cột. Chưa áp lên CoffeeManager_Development và chưa chạy TS01–TS12 — chờ duyệt đợt
test. Acceptance SQL: database/tests/g04/acceptance-table-sessions.sql; milestone kỹ
thuật v17 trong Invoke-G04Technical.ps1; Test-G04.ps1 đã nâng ThroughVersion=17 và
ánh xạ schema034.

## G04 — 035 invoice detail sessions (17/11/2026, T34-01)

035_sales_invoice_detail_sessions.sql chạy sau034 bằng Apply-Migration.ps1 Version35;
tiền đề `SchemaMigrations Version=34` và sp_InvoiceDetail tồn tại. Lỗi phát hiện trong
đợt test 17/11/2026: DAL T34 đọc 27 cột header (thêm MaPhien, SessionRevision) nhưng
sp_InvoiceDetail (013) còn trả 25 cột, làm chi tiết hóa đơn/in lại ném
IndexOutOfRangeException. 035 ALTER PROCEDURE sp_InvoiceDetail: giữ nguyên 25 cột cũ
theo thứ tự, thêm `h.MaPhien` và `sessionRow.Revision SessionRevision` qua
OUTER APPLY PHIEN_BAN; các resultset dòng món/thanh toán/snapshot không đổi; GRANT
EXECUTE giữ cho CafeManagerApp. Không sửa 013/034, không đổi dữ liệu.

Milestone kỹ thuật v18: database/tests/g04/technical-v18.sql kiểm metadata 27 cột
(ordinal 26/27 = MaPhien/SessionRevision), gọi sp_InvoiceDetail trên đơn có phiên và
đối chiếu MaPhien/SessionRevision, dọn đơn/phiên. Test-G04.ps1 và
Invoke-G04Technical.ps1 đã nâng ThroughVersion=18, ánh xạ schema035. Regression
17/11/2026: technical `technical_20261117_020349_e6dedcdf` typed+native Success;
acceptance `acceptance_20261117_020633_de5483fe` 5/5 SQL subset PASS, typed smoke
Success, unit 23/23.

## G03 — 039 promotion auto-apply (21/11/2026)

039_promotion_dieu_kien_tu_ap.sql chạy sau038 bằng Apply-Migration.ps1 Version39;
tiền đề `SchemaMigrations Version=38` (guard `THROW 53307`). Thêm `KHUYENMAI.TrangThai/
DoiTuongKhach/PhamVi` + CHECK, `NgayKT` NULL, CHECK ngày mới; bảng
`KHUYENMAI_HANG/KHUYENMAI_MON`, `HoaDonUuDaiApDung` và cột breakdown/digest trên
`HoaDonUuDaiSnapshot`; trigger con bất biến. Thay hàm ưu đãi bằng bộ inline TVF dùng
chung (`fn_HoaDonLinesJson`, `fn_UuDaiCap/ApDung/Dong/Tong/Chia`, `fn_HoaDonGiamGia`,
`fn_HoaDonUuDaiApDung`, digest) và **DROP `fn_DiscountPreview`**. `sp_DiscountPreview`
nhận `dbo.PreviewLines`; `sp_PromotionList/Get/Write` thêm điều kiện/bật-tạm dừng;
các procedure bán hàng bỏ `@Promotion`, trả danh sách ưu đãi và token digest. Không
sửa checksum001–038.

Trong lượt test được duyệt, 039 được sửa lỗi B01–B05 (tách batch `GO` cho DDL, bọc
`[Percent]`, `sys.sp_getapplock`, `sp_PromotionGet`, `sp_DiscountPreview`,
`sp_ThanhToanHoaDon`; xem `docs/tasks/promotion-dieu-kien-tu-ap/bug-report.md`). Bộ
test PA01–PA13 PASS (`runs/20261122_135707_532a959f`). Ngày 22/11/2026 đã áp lên
`CoffeeManager_Development` bằng `Apply-Migration.ps1 -Version 39` (app đóng): kết quả
`Applied`, checksum `D2D1E735…86F0D1`; do 040 đã áp trước đó nên journal 1–38 rồi 40,
039 bổ sung sau nhưng độc lập. Backup trước áp:
`CoffeeManager_Development_before039_20261122_135842.bak` (COPY_ONLY, CHECKSUM, INIT,
đã `RESTORE VERIFYONLY`). Không đổi dữ liệu: 3.442 hóa đơn đã trả giữ tổng
336.840.000/19.617.820/331.182.180, tồn NVL01 68,7280, `HoaDonUuDaiApDung` 0 dòng;
smoke `sp_InvoiceDetail` 5 set, `sp_PromotionList` 17 KM, `sp_PromotionGet`/
`sp_DiscountPreview` chạy đúng. PA14 (GUI) chưa chạy, PA15 do người dùng tự kiểm.
Lưu ý: seed demo cũ trong
`database/demo/seed-demo-daily.sql` và `seed-demo-operational.sql` còn gọi
`fn_DiscountPreview` đã bị bỏ — chỉ dùng cho DB mới sau 039 thì cần cập nhật ở task
seed riêng; không chạy lại nguyên trạng trên schema039.
