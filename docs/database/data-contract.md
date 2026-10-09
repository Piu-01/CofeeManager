# Contract dữ liệu nền — migration 001 / G01

## Kiểu và chuẩn hóa

| Dữ liệu | SQL | C# / quy tắc |
|---|---|---|
| Tiền VND | decimal(18,0) | decimal, số nguyên, trị tuyệt đối ≤ 999999999999999999; CHECK của trường quyết định có cho 0/âm |
| Lượng/định mức | decimal(18,4) | decimal, tối đa 4 chữ số lẻ, trị tuyệt đối ≤ 99999999999999.9999; không silently round |
| Ly/identity/điểm | int | int; số lượng bán > 0, điểm theo nguồn SQL |
| Mã nguồn | char(5)/char(10) | string, trim CHAR padding khi đọc, validate length trước gửi |
| SDT | varchar(15) | string, không số nguyên; optional trim/rỗng → NULL |
| Tên/ghi chú | nvarchar(n) | string Unicode; validate n, optional trim/rỗng → NULL |
| Ngày/giờ | date/datetime2(0)/time | DateOnly/DateTime/TimeSpan, typed parameter; datetime2(0) không nhận phần dưới giây |

Không biến field bắt buộc thiếu thành 0. DBNull cho NULL; không gửi `81.000 ₫`
hoặc `dd/MM/yyyy` vào SQL. UI định dạng riêng vi-VN. `ThanhTien` và `ThucLinh`
computed chỉ đọc, không có parameter/setter ghi DB. Giá dòng/tiêu hao/hợp đồng
lương là snapshot, không tính lại lịch sử bằng danh mục mới.

## Trạng thái đúng baseline (không dấu trong DB)

| Trường | Giá trị |
|---|---|
| SANPHAM.TrangThai | Dang ban / Ngung ban |
| NHANVIEN.LoaiHopDong | Full-time / Part-time |
| CHAMCONG.TrangThai | Di lam / Nghi phep / Vang |
| PHIEUXUAT.LoaiXuat | Huy do hong / Tra hang |
| LICHSU_DIEM.Loai | Cong / Tru |
| DANHMUC_THUCHI.Loai | Thu / Chi |
| HOADON.LoaiDon | Tai quan / Mang di / Giao hang |
| HOADON.TrangThai | Chua thanh toan / Da thanh toan / Huy |
| HOADON.TrangThaiGiao | NULL / Cho giao / Dang giao / Da giao / Huy giao |
| vw_TrangThaiBan.TrangThai | Trong / Dang phuc vu (dẫn xuất, không CHECK/field BAN) |
| vw_CongNoNCC.TrangThai | Da tra / Qua han / Chua tra / Tra mot phan (dẫn xuất) |
| PHIEUKIEMKE.DaDieuChinh | bit 0/1 |

## Procedure hiện có

Các procedure baseline trả completion/error, chưa trả result DTO tài chính;
sau thành công đọc lại nguồn. Gọi trực tiếp, không C# outer transaction.

| Procedure | Request typed | Kết quả / nơi đối chiếu |
|---|---|---|
| sp_ThanhToanHoaDon | MaHD char(10), ThanhToan dbo.DanhSachThanhToan READONLY, NgayTT datetime2(0) optional | HOADON, CHITIET_THANHTOAN, TIEUHAO_HOADON, LICHSU_DIEM; kho/điểm cập nhật qua trigger |
| sp_HuyHoaDon | MaHD char(10) | Header Huy, bàn qua view; không hoàn kho đơn mở |
| sp_TraCongNo | MaCN/MaTC char(10), MaNV/MaPT char(5), SoTien decimal(18,0), NgayTra datetime2(0) optional | PHIEUTHUCHI + THANHTOAN_CONGNO, vw_CongNoNCC; không tạo phiếu chi thứ hai |
| sp_ChotKiemKe | MaKK char(10) | DaDieuChinh=1 và tồn bằng TonThucTe; hôm nay/snapshot khớp/chốt một lần |
| sp_CapMa (001) | Loai varchar(20): HOADON / DANHMUC_SP / NHANVIEN | Một row Loai, Ma char(10) hoặc char(5) tùy nguồn |

TVP thứ tự cột: **MaPT char(5), SoTien decimal(18,0)>0,
MaThamChieu nvarchar(100) NULL**. Không có số tiền khách đưa trong allocation.
Checkout baseline dùng GiamGia đã lưu (chính sách mẫu KM thay hạng); SQL tính
ưu đãi chính thức/preview sẽ hoàn thiện T13/T15, chưa triển khai tại G01.

View bàn trả MaBan/TenBan/MaKV/TrangThai/MaHD optional. View nợ trả MaCN/MaNCC/
MaPN/TienNo/HanTra optional/DaTra/ConNo/TrangThai. Aggregate nguồn độc lập;
không JOIN chi tiết món với payment rồi SUM nhân dòng.

## Cấp mã và lỗi

HD + 8 số (1..99999999), DM/NV + 3 số (1..999), sequence riêng NO CYCLE.
Bootstrap max suffix số + 1 một lần, khóa bảng/migration; không MAX+1 runtime.
Seed không đổi; HD không cam kết ngày trong mã. Lấy mã rồi rollback có gap,
không trả lại số. 51301 loại chưa hỗ trợ, 51302 hết giới hạn; identity vẫn SQL cấp.

51001–51022 bảo vệ trigger, 51101–51115 điều kiện procedure; 547 ràng buộc,
2601/2627 trùng, 229 quyền. DAL giữ mã/kind, BLL dịch thông báo an toàn. Unknown
không báo success. Timeout sau ghi là **kết quả chưa biết**, đối chiếu mã nguồn
trước retry; cancellation chờ không chứng minh rollback. In sau commit, lỗi
in chỉ in lại. Không log password/hash/connection string hoặc raw SQL error.
