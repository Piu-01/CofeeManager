# Contract ưu đãi/hạng/điểm — migration008 / T13

ADMIN sửa KM/chính sách/điểm và xem history; CASH lookupKM/preview ưu đãi; STORE
không truy cập. Mọi procedure Actor/Stamp và BLL kiểm phiên/quyền. Preview không
phải checkout hoặc kết quả đã chốt; T14/T15 dùng lại fn_DiscountPreview bằng dữ liệu
nguồn chính thức, không tin tổng do UI gửi vào màn dùng thử G03.

fn_DiscountPreview: %hạng + %KM còn hiệu lực tại ngày lập, cộng thẳng và chặn 100%
(migration 038); khôngKH/KM giảm0. Source None/Tier/Promotion/TierAndPromotion; trả
TierPercent/PromotionPercent và PercentDiscount là tổng. ROUND(tiềnmón×%/100,0), không
giảmship; payable=tiềnmón+ship−giảm. Wrapper
sp_DiscountPreview kiểm FK/hiệu lực/nguyênVND/biên, trả Source/Tier/Promotion/%/
TierPercent/PromotionPercent/Discount/Payable/ReadAt/PolicyRevision. Giữ khóa policy
Shared và hàng nguồn đến hết preview transaction; UI chỉ hiển thị DTO, đổi input vô
hiệu kết quả cũ.

KM mã KM+8số, tên≤100, DateOnly hai đầu inclusive/cùngngày, tỷlệint0–100; xóa có
hóađơn bịFKchặn, sửaKM không UPDATE hóađơn. sp_PromotionList/Get/Create/Update/Delete.

PolicyGet trả toàn hạng+revision. LoyaltyTierInput Code1–5ASCIIuppercase, ngưỡng
intkhôngâm/duynhất/có0, %0–100. PolicyPreview trả Revision/PayloadHash/AffectedCustomers
ước tính tại thờiđiểmđọc. Replace yêu cầu khớp revision/hash, Exclusive policy lock;
ngưỡng cũ được chuyển sang các giá trị tạm không nằm trong policy cũ/mới rồi ghi
policy mới, táiphânhạng và xóa hạng bỏ trong cùng transaction. UNIQUE/FK luôn giữ,
không DDL trong usecase, runtime không cần quyền migration. Kết quả trả số khách
đổi hạng thực tế, lưu ResultCount trong receipt để retry trả đúng kết quả gốc.

AdjustPreview trả sốdư/hạng dựkiến và CustomerRevision/PolicyRevision. Adjust nhận
Cong/Tru, điểmintdương, lýdo≤100required, khớp cả2revision; Shared policy→kháchUPDLOCK.
Ghi LICHSU_DIEM MaHDNULL, trigger cập nhật điểm/hạng đúng1lần. Khóa lịch sử update/
delete; mua hàng chỉCong, đúngKH/hóađơnđãtrả, unique mỗiMaHD. Checkout baseline thêm
Shared Policy trước Shared Recipes/header, chưa chuyển công thức checkout đích.

G03OperationReceipt có ResultCount bổ sung008; PolicyReplace ghi count cùng commit.
Các thao tác khác dùng receipt chung theo material-contract. Mọi bảng nguồn DENY
DML cho CafeManagerApp, grant execute/type cụ thể; helper nội bộ không public grant.

Technical SQL clean/demo+preview/promocreate/policyreplace/adjust thành công;
manifest cuối G03/runs/technical_20261018_194616_96506431. Lượt compile trước đã
rollback/dọn: lỗi khai báo biến cùng table và fixture CHAR5 cópadding; không gán
thành kết quả nghiệm thu. Chưa chạy các ca policyrollback/concurrency/timeout/GUI.

Bổ sung T13/009: tạo mới KM+8số, còn mã seed đã tồn tại như KM24BF được sửa/xóa
với expected revision, giữ nguyên khóa. Không remap lịch sử. 009 giữ chữ ký
procedure, chỉ thu hẹp kiểm định dạng tạo mới. 008 giữ nguyên checksum đã áp dụng.

## Ưu đãi tự áp theo điều kiện — migration039 (21/11/2026)

`KHUYENMAI` thêm `TrangThai` (`Nhap/DangChay/TamDung`, mặc định `DangChay`),
`DoiTuongKhach` (`ALL/FIRST/MEMBER/TIER`) và `PhamVi` (`ALL/ITEM`); `NgayKT` cho phép
NULL = vô hạn; CHECK ngày chỉ còn `NgayKT IS NULL OR NgayKT >= NgayBD`. Hai bảng con
`KHUYENMAI_HANG(MaKM,MaHang)` và `KHUYENMAI_MON(MaKM,MaSP)` bị DENY DML trực tiếp;
sửa bảng con/trạng thái bump `KHUYENMAI.Revision` để token preview cũ mất hiệu lực.
`HetHan` chỉ là trạng thái suy ra khi `NgayKT < NgayLap`, không ghi.

`fn_DiscountPreview` bị bỏ. Điều kiện duy nhất nằm trong `fn_UuDaiDong` (trạng thái,
ngày, đối tượng, phạm vi món, legacy): `ALL` mọi khách, `FIRST` khách có hồ sơ chưa
từng có hóa đơn `Da thanh toan`, `MEMBER` khách có hồ sơ, `TIER` thuộc `KHUYENMAI_HANG`;
`ITEM` chỉ giảm dòng thuộc `KHUYENMAI_MON`. Nhiều KM thỏa trên cùng dòng được cộng %
và chặn 100%/dòng; giảm dòng làm tròn một lần `ROUND(amount × pct / 100, 0)`;
`GiamGia = min(Σ giảm dòng, TongTien)`; `TierDiscount = min(GiamGia, ROUND(TongTien ×
tier% / 100, 0))`, `PromotionDiscount = GiamGia − TierDiscount`; ship không giảm;
điểm `FLOOR((TongTien − GiamGia) / 1000)`. Đơn mở cũ có `HOADON.MaKM` được đọc như KM
legacy phạm vi `ALL`, vẫn xét hiệu lực ngày; không backfill.

`sp_DiscountPreview @Lines dbo.PreviewLines READONLY` (không còn `@Promotion`), trả
Source/Tier/TierPercent/PromotionPercent/PercentDiscount/TierDiscount/PromotionDiscount/
PromotionCount/Discount/Payable/ReadAt/PolicyRevision và result set danh sách
`AppliedPromotion(Code,Name,Percent,BaseAmount,Discount)`. TVP rỗng dùng `@Money` như
một dòng giả cho KM phạm vi `ALL` (chỉ preview). `sp_PromotionList` trả thêm trạng
thái/đối tượng/phạm vi/số món; CASH chỉ thấy `DangChay` còn hiệu lực. `sp_PromotionGet`
trả 3 set: header, hạng, món. `sp_PromotionWrite` nhận `@Audience,@Status,@TiersJson,
@ItemsJson`; action Create/Update/Delete/Pause/Resume, giữ RequestId + ExpectedRevision
và receipt như các ghi G03. Ghi KM vẫn ADMIN; CASH chỉ đọc.

Snapshot checkout thêm `TierDiscount/PromotionDiscount/PromotionCount/PromotionDigest`
và `HoaDonUuDaiApDung(MaHD,MaKM,...)`; trigger con cấm UPDATE/DELETE, chỉ INSERT khi
đơn còn mở và buộc tổng `Discount` bằng `snapshot.PromotionDiscount`. Hóa đơn cũ không
có breakdown vẫn đọc theo `PercentDiscount`; UI/in giữ nhánh legacy.

## Một món một khuyến mãi, hạng sau KM — migration044 (23/11/2026)

Thay luật cộng dồn additive của 038/039. Ứng viên là KM `Giam` đang chạy, đúng
ngày/đối tượng, `%>0`; KM legacy `HOADON.MaKM` của đơn mở được thêm như ứng viên
phạm vi `ALL` (chỉ xét hiệu lực ngày), không còn ép cộng thêm. Hàm mới
`fn_UuDaiChon` phủ dòng (`ALL` mọi dòng, `ITEM` theo `KHUYENMAI_MON`; nhiều dòng cùng
`MaSP` cùng thuộc phạm vi) và chọn tập KM có tập dòng phủ rời nhau với tổng
`ROUND(amount×%/100)` lớn nhất; hòa chọn ít KM hơn rồi theo chuỗi mã tăng dần. Tách
nhóm xung đột; duyệt chính xác khi nhóm ≤ 15 KM, hơn ngưỡng dùng tham lam tất định
(giới hạn cấu hình có tài liệu), không bao giờ cộng hai KM trên cùng dòng.

`fn_UuDaiCap` chỉ trả cặp của tổ hợp được chọn. `fn_UuDaiDong` trả phần KM của dòng,
không cộng hạng. `fn_UuDaiTong`: `PromotionDiscount = min(Σ giảm dòng, TongTien)`;
`TierDiscount = ROUND((TongTien − PromotionDiscount) × hạng% / 100, 0)`;
`Discount = PromotionDiscount + TierDiscount`; ship không giảm; điểm
`FLOOR((TongTien − Discount)/1000)`. `fn_UuDaiChia/fn_UuDaiApDung` giữ cách phân bổ
039 (RawShare, floor từng KM, dồn phần lẻ KM cuối). `PercentDiscount` của preview/
snapshot là mức giảm thực tế `ROUND(100 × Discount / TongTien)`; snapshot mới chỉ bị
ràng buộc `Discount = TierDiscount + PromotionDiscount`, dòng lịch sử không backfill.
`sp_DiscountPreview`/`sp_SalesCheckoutPreview`/`sp_ThanhToanHoaDon`/`sp_InvoiceDetail`/
`sp_SalesReadCore` giữ chữ ký, đọc chung các hàm trên. KM `Tang` độc lập, không tham
gia tổ hợp. UI chỉ đổi nhãn hiển thị "(KM x%, hạng y% sau KM)", không tự tính lại.
