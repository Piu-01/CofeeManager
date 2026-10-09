# Ma trận nơi ghi — nền G01

Role CafeManagerApp chỉ SELECT hai view, EXECUTE bốn procedure baseline và cấp
mã, EXECUTE/REFERENCES TVP. Không DML toàn schema/đọc password hash. Các quyền
CRUD/query mới cấp tường minh tại task sử dụng; SQL principal không thay role
ADMIN/CASH/STORE nghiệp vụ ở BLL T06.

| Bảng | Nơi ghi / bất biến | Task mở contract app |
|---|---|---|
| CHUCVU | Quản trị danh mục | T08/T24 |
| CALAMVIEC | Quản trị ca | T08/T25 |
| NHANVIEN | Quản trị hồ sơ, cấp NV từ sequence | T24 |
| CHAMCONG | Procedure chống overlap/kỳ chốt | T25 |
| DANHMUC_SP | Danh mục, cấp DM từ sequence | T08 |
| NGUYENVATLIEU | Hồ sơ; SoLuongTon chỉ trigger nhập/xuất/tiêu hao hoặc sp_ChotKiemKe | T09/T20–T22 |
| NHACUNGCAP | Hồ sơ | T08/T20 |
| SANPHAM | Menu/giá hiện hành | T10 |
| CONGTHUC | Công thức, không sửa tiêu hao lịch sử | T10 |
| PHIEUNHAP | Header qua nghiệp vụ; TongTien qua TR_CTPN_Kho | T20 |
| CHITIET_PN | Trigger tính tổng/cộng kho theo delta | T20 |
| PHIEUXUAT | Chứng từ xuất | T21 |
| CHITIET_PX | Trigger trừ kho theo delta | T21 |
| PHIEUKIEMKE | sp_ChotKiemKe chốt; app DML bị DENY | T22 |
| CHITIET_KK | Snapshot/đếm; TR_KK_LichSu khóa chi tiết chốt | T22 |
| HANGTHANHVIEN | Chính sách, tái phân hạng | T13 |
| KHACHHANG | Hồ sơ; điểm/hạng chỉ TR_LichSuDiem; app UPDATE cột bị DENY | T12/T13 |
| LICHSU_DIEM | Checkout ghi/trigger điểm; app DML DENY, không sửa/xóa | T13/T15 |
| KHUYENMAI | Chính sách ưu đãi | T13 |
| KHUVUC | Danh mục | T08/T11 |
| BAN | Danh mục; trạng thái từ view | T11 |
| DOITACGIAOHANG | Danh mục | T08/T18 |
| PHUONGTHUCTT | Danh mục, PT010 ngoài scope | T08 |
| HOADON | Tổng trigger, chốt/hủy qua procedure; app DML DENY; ThanhTien computed | T14–T18 |
| CHITIET_HD | Món/giá snapshot, tổng qua trigger; MaDong identity | T14 |
| CHITIET_THANHTOAN | Checkout allocation; app DML DENY; MaTT identity | T15 |
| DANHMUC_THUCHI | Danh mục loại, trigger khóa loại đã có trả nợ | T08/T19 |
| TAISAN_CODINH | Hồ sơ tài sản | T27 |
| PHIEUTHUCHI | sp_TraCongNo ghi chi hệ thống; app DML DENY | T19/T20/T23/T26 |
| CONGNO_NCC | Nợ theo nhập; trigger bảo vệ số gốc/đã trả | T20/T23 |
| VAITRO | Cài đặt ADMIN/CASH/STORE | T06 |
| TAIKHOAN | BLL hash; không expose hash/grid/log | T06/T24 |
| BANGLUONG | Chốt snapshot, ThucLinh computed; chốt khác chi | T26 |
| THANHTOAN_CONGNO | sp_TraCongNo/trigger; app DML DENY, không sửa/xóa | T23 |
| TIEUHAO_HOADON | Checkout snapshot/trigger kho; app DML DENY, không sửa/xóa | T15 |
| SchemaMigrations (001) | Chỉ migration principal, app DML DENY | T03 |

App không được trực tiếp sửa tài chính để bypass procedure. Cùng owner dbo
giữ ownership chain cho procedure/trigger tĩnh; DENY không ngăn các câu lệnh
trong ownership chain. Không cấp trực tiếp UPDATE sequence: execute sp_CapMa.
DENY toàn bảng HOADON/PHIEUTHUCHI là quyền nền hiện tại; mở contract tại task
nghiệp vụ bằng procedure, không GRANT đại trà. Bootstrap số dư ban đầu chỉ cài
mới/seed; không sửa tồn/điểm sau mở sổ. Migration 001 không thay rows baseline.
