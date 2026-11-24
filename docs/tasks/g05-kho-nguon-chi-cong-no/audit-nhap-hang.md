# Audit màn lập phiếu nhập hàng — 23/11/2026

Trạng thái: **đã ghi nhận theo yêu cầu người dùng**; phần sửa thuộc nhóm phát sinh
[nhận hàng 2 bước](nhan-hang/requirements.md), chưa chạy test.

Phạm vi rà: `PurchaseEditorDialog`, `PurchasesControl`, `PurchaseService`,
`PurchaseRepository`, `sp_LapPhieuNhap` (migration 018), trigger `TR_CTPN_Kho`,
`TR_PhieuNhap_Tong`, schema `PHIEUNHAP/CHITIET_PN/NGUYENVATLIEU/NHACUNGCAP`.

Bối cảnh nghiệp vụ người dùng chốt: quản lí lập phiếu "định nhập gì" và in; NCC giao hàng;
nhân viên kiểm hàng; quản lí chốt kho. Đây là **phiếu nội bộ**, không lưu hoá đơn GTGT.
Kiểm kê giữ vai trò đối soát định kỳ, không thay bước nhận hàng.

## A. Sai/khó hiểu trên màn lập phiếu

| # | Hiện tượng | Bằng chứng | Đề xuất |
|---|---|---|---|
| A1 | "Hạn nợ" hiện ngày dù checkbox chưa tick, gây hiểu nhầm đã đặt hạn | `PurchaseEditorDialog.cs:23` | Chỉ bật khi nợ > 0; hiển thị rõ trạng thái |
| A2 | "Trả ngay" là textbox căn trái, không định dạng; gõ `1.000.000` báo lỗi chung | `PurchaseEditorDialog.cs:24,85` | Mask tiền VN, căn phải, lỗi chỉ rõ |
| A3 | Không thấy tổng/thành tiền/còn nợ realtime, phải bấm "Tính tổng" | `PurchaseEditorDialog.cs:72-78` | Footer tổng realtime khi sửa dòng |
| A4 | Grid thiếu cột "Thành tiền" | `PurchaseEditorDialog.cs:39` | Thêm cột, tính theo dòng |
| A5 | Lỗi nhập grid chỉ ghi status đáy, không tô ô/focus | `PurchaseEditorDialog.cs:40` | ErrorProvider tại ô |
| A6 | Combo nguyên liệu không tìm kiếm/quét; thêm nhiều dòng chậm | `PurchaseEditorDialog.cs:22,43` | Gõ tên/quét mã, Enter thêm dòng |
| A7 | Không paste từ Excel | không có handler | Ghi backlog ngoài đợt này |
| A8 | Không hiện tồn hiện tại và giá nhập gần nhất khi chọn NVL | `InventoryLookup` chỉ Code/Name/Unit | Lookup mở rộng, hiện cạnh dòng |
| A9 | Nút "Thêm dòng"/"Bỏ dòng" bị cắt chữ | `PurchaseEditorDialog.cs:42` | Bố trí lại thanh công cụ |
| A10 | Tên cột "MÃ", "LƯỢNG", "GIÁ / ĐƠN VỊ" tối nghĩa | `PurchaseEditorDialog.cs:39` | MÃ NVL, SỐ LƯỢNG, ĐƠN GIÁ (₫), THÀNH TIỀN, ĐVT |
| A11 | "Xem lại" trong UX thu về MessageBox | `PurchaseEditorDialog.cs:92` | Màn xem lại + xác nhận có bảng tổng |
| A12 | Không có số phiếu để đọc cho NCC lúc giao hàng | mã PN cấp sau commit | Cấp mã ngay khi lưu phiếu chờ nhận |
| A13 | Không chặn ngày nhập tương lai/quá khứ xa | không validate | Chặn tương lai; cảnh báo quá khứ xa |
| A14 | Số lượng đang cho 4 chữ số lẻ nhưng nghiệp vụ luôn nguyên | `DB_Schema.sql:150`, UI không chặn | Chặn số lẻ ở UI + procedure |
| A15 | Không in được phiếu nhập | không có màn in | In phiếu nội bộ từ bản chờ nhận/đã chốt |

## B. Thiếu quy trình nhận hàng

| # | Hiện tượng | Bằng chứng | Hệ quả |
|---|---|---|---|
| B1 | Chốt phiếu là cộng tồn ngay, không có bước kiểm hàng | `TR_CTPN_Kho`, `sp_LapPhieuNhap:71-75` | Nhân viên không đối chiếu được số giao với số đặt |
| B2 | Không lưu phiếu chờ nhận; nháp chỉ trong RAM | `PurchaseEditorDialog.cs:57` pending store | Đóng form mất phiếu, không in, không chuyển ca |
| B3 | Không lưu thực nhận/lệch/lý do | `CHITIET_PN` chỉ `SoLuong` | Giao thiếu phải nhập đúng như đặt hoặc bỏ phiếu |
| B4 | Không tách người lập / người đếm / người chốt | một quyền `Inventory` | STORE lập và chốt được, thiếu kiểm soát |
| B5 | Không huỷ được phiếu lập sai | phiếu chốt bất biến | Phiếu rác tích tụ, phải lập phiếu bù |
| B6 | Danh sách không có trạng thái, không lọc | `PurchasesControl.cs:19` | Không biết phiếu nào chờ nhận |
| B7 | Chi tiết chỉ là bảng đọc, không nêu đặt/thực nhận/lệch | `PurchasesControl.cs:56-83` | Không đối chiếu khiếu nại NCC |

## C. Kiểm soát và tiện ích nên bổ sung sau

| # | Nội dung | Ghi chú |
|---|---|---|
| C1 | Cảnh báo lệch giá so với lần nhập trước | Dữ liệu có trong `CHITIET_PN`; backlog |
| C2 | Cảnh báo NCC đang nợ quá hạn trước khi chốt | `vw_CongNoNCC` có sẵn; backlog |
| C3 | Paste Excel, quét barcode | Backlog năng suất |
| C4 | PO/backorder, nhiều kho, quy đổi đơn vị, bảng giá NCC | Ngoài phạm vi |
| C5 | VAT, số/ngày hoá đơn, chiết khấu, phí vận chuyển | **Không làm**: phiếu nội bộ |

## Không coi là lỗi

- Kiểm kê là đối soát định kỳ, không phải bước xác nhận giao hàng.
- Đơn giá nguyên VND là ràng buộc đã chốt; nếu cần giá lẻ phải mở yêu cầu riêng.
- Phiếu đã chốt bất biến; muốn sửa phải huỷ trước khi chốt hoặc lập phiếu mới.
