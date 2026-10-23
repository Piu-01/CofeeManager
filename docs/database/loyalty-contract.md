# Contract ưu đãi/hạng/điểm — migration008 / T13

ADMIN sửa KM/chính sách/điểm và xem history; CASH lookupKM/preview ưu đãi; STORE
không truy cập. Mọi procedure Actor/Stamp và BLL kiểm phiên/quyền. Preview không
phải checkout hoặc kết quả đã chốt; T14/T15 dùng lại fn_DiscountPreview bằng dữ liệu
nguồn chính thức, không tin tổng do UI gửi vào màn dùng thử G03.

fn_DiscountPreview: KM còn hiệu lực tại ngày lập thay hạng, không cộngdồn; khôngKH/
KM giảm0. ROUND(tiềnmón×%/100,0), không giảmship; payable=tiềnmón+ship−giảm. Wrapper
sp_DiscountPreview kiểm FK/hiệu lực/nguyênVND/biên, trả Source/Tier/Promotion/%/
Discount/Payable/ReadAt/PolicyRevision. Giữ khóa policy Shared và hàng nguồn đến
hết preview transaction; UI chỉ hiển thị DTO, đổi input vô hiệu kết quả cũ.

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
