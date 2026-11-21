# Test results — Chính sách hạng 4 hạng, trần giảm 8%

- Ngày: 21/11/2026
- Môi trường: Windows, .NET SDK 10.0.401, SQL Server `localhost`, DPI hệ thống 125%
- Source: working tree `main` (chưa commit) gồm `database/Insert_Data.sql` sửa seed và `database/migrations/037_loyalty_tier_policy_conservative.sql`
- Duyệt đợt test: người dùng đồng ý ngày 21/11/2026, kèm yêu cầu xác nhận **ưu đãi hạng khách hàng không ảnh hưởng đến màn Ưu đãi (KHUYENMAI)**
- Kịch bản và log: [`runs/tier-20261121/verify-tier-policy.ps1`](runs/tier-20261121/verify-tier-policy.ps1), [`runs/tier-20261121/output.log`](runs/tier-20261121/output.log)
- Không sửa source trong lúc test.

## Kịch bản

Hai DB test tạo mới hoàn toàn, tự xóa sau khi chạy:

- **S1 cài mới**: `CoffeeManager_Test_TierPolicy_<run>_fresh` = `DB_Schema.sql` + `Insert_Data.sql` (seed mới) + migration 001→037.
- **S2 nâng cấp**: `CoffeeManager_Test_TierPolicy_<run>_upgrade` = `DB_Schema.sql` + seed tại `git HEAD` (chính sách 10 hạng cũ) + migration 001→036, chụp trạng thái, rồi áp 037.

Mỗi DB bật tạm tài khoản `admin` (ADMIN) để gọi procedure đúng quyền; dữ liệu test chỉ nằm trong DB test, đã DROP sau khi chạy.

## Kết quả (31/31 PASS)

### S1 — Cài mới

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| TP-01 | `HANGTHANHVIEN` đúng 4 hạng DONG 0/0, BAC 2.000/3, VANG 10.000/5, KIM 30.000/8 | PASS | `tiers=DONG\|0\|0;BAC\|2000\|3;VANG\|10000\|5;KIM\|30000\|8` |
| TP-02 | Không khách nào trỏ hạng sai ngưỡng | PASS | `tier mismatch=0` |
| TP-03 | 10 khách seed được tính hạng theo ngưỡng mới (KH001 3.433 BAC, KH003 6.910 BAC, KH005 1.800 DONG, KH009 14.500 VANG…) | PASS | `customers=KH001\|3433\|BAC;…` |
| TP-04 | Hóa đơn lịch sử `HD20250401` giữ `GiamGia=12.500`, trạng thái đã trả | PASS | `HD20250401=12500/Da thanh toan` |
| TP-05 | Preview khách BAC, món 250.000, ship 0 → hạng 3%, giảm 7.500, phải trả 242.500 | PASS | `Tier/BAC/3%/7500/242500` |
| TP-06 | KM 10% thay hạng: KH001 (BAC 3%) + KM 10% → 10%, giảm 25.000, phải trả 225.000 | PASS | `Promotion/10%/25000/225000` |
| TP-07 | KM 4% vẫn thay hạng VANG 5% (không cộng dồn, không lấy max) | PASS | `Promotion/4%/10000/240000` |
| TP-08 | Preview không làm đổi dữ liệu/revision các KM seed | PASS | `seed promotion rows stable` |
| TP-09 | Danh sách KM chỉ tăng đúng 2 dòng fixture test | PASS | `promotion list gained exactly fixture rows` |
| TP-10 | Checkout A (KH001, 250.000): preview hạng 3%, 7.500/242.500, 242 điểm | PASS | `checkout A preview=Tier/7500/242500/pts242` |
| TP-11 | Thanh toán A: hóa đơn đã trả, giảm 7.500; KH001 3.675 điểm, giữ BAC | PASS | `A paid giam=7500 KH001=3675/BAC` |
| TP-12 | Snapshot A = `Checkout/Tier/BAC/3%/7500` | PASS | `snapshot A=Checkout\|Tier\|BAC\|\|3\|7500` |
| TP-13 | Checkout B (KH005 1.800 điểm, DONG): preview 0%, 250.000, 250 điểm | PASS | `checkout B preview=Tier/0/250000/pts250` |
| TP-14 | Sau B: KH005 2.050 điểm tự lên BAC 3% | PASS | `KH005 after B=2050/BAC` |
| TP-15 | Checkout C (KH001 + KM 10%): preview 10%, giảm 25.000, 225 điểm | PASS | `checkout C preview=Promotion/10/pts225` |
| TP-16 | Sau C: KH001 3.900 điểm, giữ BAC | PASS | `C paid KH001=3900/BAC` |
| TP-17 | Snapshot C = `Checkout/Promotion/BAC/KM20261121/10%/25000` | PASS | `snapshot C=…` |
| TP-18 | Danh sách KM không đổi sau các checkout | PASS | `promotion list unchanged by checkouts` |
| TP-19 | `HD20250401` vẫn 12.500; 3 dòng lịch sử điểm mới đúng 3 hóa đơn test | PASS | `legacy invoice/points history ok` |

### S2 — Nâng cấp DB 10 hạng cũ

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| TP-20 | Trước 037: KH001 (3.150 điểm) hạng BKIM 10%, preview 250.000 → giảm 25.000 | PASS | `pre-037 preview=BKIM/10%/25000` |
| TP-21 | Trước 037 đúng 10 hạng cũ, trần LEGE 35% | PASS | `pre-037 tiers=…LEGE\|50000\|35` |
| TP-22 | Sau 037 còn đúng 4 hạng mới | PASS | `post-037 tiers=DONG\|0\|0;…KIM\|30000\|8` |
| TP-23 | Không khách nào trỏ hạng đã xóa | PASS | `tier mismatch=0` |
| TP-24 | Điểm của mọi khách không đổi; 8/10 khách đổi hạng theo chính sách | PASS | `points changed=0 tiers changed=8` |
| TP-25 | Dữ liệu bảng `KHUYENMAI` (ngày, %, revision) không đổi sau 037 | PASS | `promotion rows unchanged by 037` |
| TP-26 | Kết quả `sp_PromotionList` (nguồn của tab Ưu đãi) giống hệt trước/sau 037 | PASS | `promotion list output unchanged by 037` |
| TP-27 | Hóa đơn và snapshot lịch sử không đổi | PASS | `legacy invoice/snapshot unchanged` |
| TP-28 | Sau 037: preview KH001 → BAC 3%, 7.500/242.500 | PASS | `post-037 preview=BAC/3%/7500/242500` |
| TP-29 | Revision chính sách tăng (preview cũ phải xác nhận lại) | PASS | `policy revision 2184 -> 2276` |
| TP-30 | Chạy lại 037 (runner) không đổi dữ liệu | PASS | `re-run 037 keeps tiers` |
| TP-31 | `SchemaMigrations` version 37 | PASS | `schema version 37` |

Yêu cầu bổ sung của người dùng được chứng minh bởi **TP-25/TP-26**: chính sách hạng (ưu đãi % của khách) và bảng/danh sách khuyến mãi hoàn toàn độc lập; 037 không UPDATE/INSERT/DELETE `KHUYENMAI`. Khi bán hàng, TP-06/TP-07/TP-15 xác nhận chỉ có một nguồn giảm giá được áp: chọn KM thì KM thay hạng, không cộng dồn.

## Build và unit test

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| TP-B01 | `dotnet build CafeManager.slnx` (BaseOutputPath tạm do app/VS đang giữ `bin`) | PASS — 0 error, 2 warning có sẵn (WebView2/WindowsBase) | log phiên làm việc |
| TP-B02 | `dotnet test CafeManager.Tests` | BLOCKED — 171 pass, 461 fail do fixture tích hợp G02 đòi DB test được đánh dấu riêng (`Use Test-G02.ps1 with an isolated marked DB.`); không liên quan thay đổi SQL/không có C# nào bị sửa | output test |

## Phần chưa chạy

| Mã | Nội dung | Trạng thái | Lý do |
|---|---|---|---|
| TP-U1 | Kiểm chức năng UI ở 125%: màn Hạng thành viên 4 dòng, Khách hàng hiển thị hạng/%, F3/F8 áp đúng | NOT RUN | Không sửa UI/C#; dữ liệu đã kiểm qua procedure. Cần người dùng mở app (đóng bản đang chạy để rebuild) kiểm cùng phần hình thức |
| TP-U2 | Hình thức giao diện, DPI 100%/150% | SKIPPED theo phân công | Người dùng tự kiểm |

## Ghi chú

- Số điểm seed thực tế sau khi chạy seed là KH001 3.433 (không phải 3.150 như bảng trong plan): các hóa đơn mẫu trong seed cũng cộng điểm qua trigger khi seed chạy. Hạng sau 037 vẫn đúng như thiết kế.
- Trên DB phát triển `CoffeeManager_Development` (đã áp 037 trước đợt test): version 37, 4 hạng, 0 khách sai hạng, KH001 từ PLAT 18% xuống BAC 3% (8.281 điểm thực tế), `HD20250401` vẫn 12.500.
- Không có bug phát sinh trong đợt test.
