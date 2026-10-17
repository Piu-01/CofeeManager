# Contract danh mục G02 / T08

Migration003 triển khai danh mục sau001/002. Chưa nghiệm thu các ca bắt buộc.

| Kind | Bảng | Fields quản trị | Tạo mã |
|---|---|---|---|
| ProductCategory | DANHMUC_SP | Name≤50 | sp_CapMa, DM001–999 |
| Supplier | NHACUNGCAP | Name≤100, Phone1–15 ASCII digits required/unique | NCC01–99 |
| PaymentMethod | PHUONGTHUCTT | Name≤50 unique, Supported | PT001–999, trừ010 |
| DeliveryPartner | DOITACGIAOHANG | Name≤50, Ratio decimal5,2 trong0–100 | DT001–999 |
| ExpenseCategory | DANHMUC_THUCHI | Name≤50, Type Thu/Chi | TC001–999, trừ005/011/012 |
| Position | CHUCVU | Name≤50, Allowance decimal18,0≥0, Factor decimal5,2≥1 | CV001–999 |
| Shift | CALAMVIEC | Name≤30, Start/End typed TimeSpan theo phút, khác nhau | CA001–999 |

ADMIN CRUD7tab; CASH/STORE không quản trị. Mã nhập uppercaseASCII; mã không
đổi khi sửa. CVADM có sẵn từ setup được đọc/sửa fields hợp lệ, không remap.
Tiền/tỷ lệ quá scale bị BLL từ chối trướcSqlClient, không tự round. End<Start
là ca qua đêm; không tính công/lương. Tìm tên paramLIKE escaped literal và
collation hiện tại, page50. DTO quản trị cụ thể7kind, lookup DTO hẹp độc lập.

## Transaction / revision / đối chiếu

SQL sinh procedure `sp_Catalog{Kind}List/Get/Create/Update/Delete` từ danh sách
metadata đóng trong migration; không nhận table/identifier/SQL do client nhập.
Actor/stamp lấy từ session BLL; DB kiểm ADMIN active/employed trong write
transaction. Runtime EXECUTE, không DML7bảng/mapping/support/receipt.

Boundary Create/Update nhận `@Name nvarchar(max)` và Supplier
`@Phone varchar(max)` để kiểm input gốc trước khi ghi cột có giới hạn.
Tên vượt giới hạn theo kind hoặc SDT vượt15 ký tự phải báo51501, không cắt
âm thầm. Kiểu/kích thước cột, DTO và BLL validation không đổi. Bản sửa nằm
trong migration003 chưa phát hành; DB test đã áp dụng checksum cũ giữ nguyên
lịch sử, regression dùng DB mới, không chỉnh checksum để chạy lại bản khác.

CatalogRevision rowversion cho7bảng; update/delete requireExpectedRevision,
stale không overwrite. FKhistory chặn delete, không cascade. TạoDM dùng
RequestId GUID và CatalogCreateReceipt cùngtransaction; PayloadHash char64 do
SQL tính SHA2_256 trên UnicodeName normalized. Sameactor/request/payload trả
code đãcommit; mismatch51508. sp_CatalogProductCategoryReceipt đọc receipt để
đối chiếu, không cấp mãmới. Receipt vẫn tồn tại nếu danh mục sau này bịxóa.

Timeout/connectionloss sau ghi: khóa gửi lại và giữ input/request; editor có
Kiểm tra/tải lại. Đối chiếu typedfields/key hoặc receipt, không coi timeout là
rollback. Không giữ transaction trong lúc nhân viên nhập. Rowkhácnháp cần tải
lại tường minh, không tự ghi đè. Kết quả muộn của sessionrevoked bị loại.

## Dữ liệu bảo vệ / phương thức

SystemExpenseCategory: PayrollTC005, SupplierDebtTC011, PurchaseImmediateTC012,
đềuChi. Chỉ sửa tên; trigger/procedure/FK chặn đổi mã/Loai/xóa ngay cả chưa
cóhistory. TC005/011 hiệncó giữ tên hợp lệ; TC012 preexisting dừngpreflight để
người cài quyết định. Mapping không được đổi ở runtime.

PaymentMethodSupport: PT001/PT002 hỗ trợ ghi nhận thủ công đã xác nhận bên
ngoài; PT010 luônunsupported. Phương thức mới support0, lưu rồi ADMIN xác
nhận rõcheckbox hoặc SetSupport service. Đổi tên không tự bật/tắt. Revision
phương thức được cập nhật cùngsupport; phương thức cóhistory không xóa.

Checkout selector ADMIN/CASH; Inventory selector ADMIN/STORE. ValidateMethod
từ chối mãlạ/unsupported/PT010 theo purpose và permission. Đây là selector
củaG02; procedurecheckout baseline chưa enforce contract tương laiT15.

Lookup BLL: categoriesADMIN/CASH/STORE; suppliersADMIN/STORE (+Phone);
deliveryADMIN/CASH (Code/Name, khôngRatio); expense/position/shiftADMIN.
Expense lookup ghi tay loại3mãChi hệ thống; positionkhôngAllowance/Factor.
Lookup kho không cấp STORE quyền ghi chi/nợ. Không tạo usecase giả để xemlookup.

Lỗi51501 input,51502 protection,51503 revision,51504 unsupported,51505 actor,
51506 code,51507 migrationconflict,51508 receiptmismatch; lỗiSqlClient khác giữ
mappingG01. Các schema/seed/001 đã bàn giao không bị sửa bởiG02.
