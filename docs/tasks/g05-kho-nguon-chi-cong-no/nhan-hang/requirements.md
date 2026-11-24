# Nhận hàng 2 bước cho phiếu nhập — requirements

Ngày soạn: 23/11/2026. Trạng thái: **phần phát sinh của G05, người dùng đã chốt hướng và duyệt
kế hoạch triển khai trong phiên 23/11/2026** (audit tại [audit-nhap-hang.md](../audit-nhap-hang.md)).
Test chỉ chạy sau khi người dùng duyệt đợt test riêng.

## 1. Mục tiêu

Tách nghiệp vụ "lập phiếu định nhập + in" khỏi "xác nhận hàng đã vào kho" cho một quán, một kho:

1. Quản lí (ADMIN) lập phiếu, hệ thống cấp mã ngay, in phiếu; phiếu ở trạng thái **Chờ nhận**.
2. Nhân viên (STORE) mở phiếu, đếm **thực nhận** từng dòng; được lệch, bắt buộc ghi lý do;
   lưu số kiểm. Chưa đụng tồn.
3. ADMIN chốt nhận; tồn cộng theo **thực nhận**; tạo phiếu chi phần trả ngay và công nợ phần còn lại
   theo tổng thực nhận. Phiếu đóng, chỉ đọc.
4. Kiểm kê giữ nguyên vai trò đối soát định kỳ; không thay bước nhận hàng.

## 2. Quyết định đã chốt

- Luồng trạng thái: `ChoNhan` → `DaKiem` → `DaChot`; huỷ được khi chưa chốt (`DaHuy`).
- Số lượng luôn nguyên; chặn ở UI và `sp_PurchaseValidate`.
- Phiếu nội bộ: không lưu số/ngày hoá đơn, VAT, chiết khấu, phí vận chuyển.
- Quyền: ADMIN lập, huỷ, chốt (chốt trả ngay cần quyền tài chính); STORE đếm/lưu số kiểm;
  CASH không truy cập.
- Mỗi phiếu chốt một lần; giao bổ sung lập phiếu mới, không backorder.
- Không sửa phiếu đã chốt; không sửa số đặt sau khi đã kiểm.
- Tồn/tiền chỉ phát sinh ở bước ADMIN chốt phiếu.

## 3. Phạm vi kỹ thuật

- Migration `043_purchase_receiving.sql`: cột trạng thái/kiểm/huỷ/hạn trả dự kiến trên `PHIEUNHAP`,
  `SoLuongNhan/LyDoLech/DaGhiKho` trên `CHITIET_PN`, backfill dữ liệu cũ,
  viết lại `TR_CTPN_Kho`, `TR_PhieuNhap_Tong`, cập nhật view và procedure.
- Procedure mới: `sp_TaoPhieuNhap`, `sp_LuuKiemNhan`, `sp_ChotNhanHang`, `sp_HuyPhieuNhap`;
  cập nhật `sp_PurchasePreview/List/Get/Receipt`.
- Models/DAL/BLL: request/dto mới, repository/service mới, validate số lượng nguyên, phân quyền.
- UI: sửa `PurchaseEditorDialog`, thêm `PurchaseReceiveDialog`, cập nhật `PurchasesControl`/chi tiết,
  thêm in phiếu nhập PDF.
- Cập nhật `docs/UX_QuanLiQuanCafe.md` §11.2 sau triển khai.

## 4. Ngoài phạm vi

VAT/hoá đơn GTGT, PO/backorder, nhiều kho/chi nhánh, quy đổi đơn vị, bảng giá NCC, đính kèm,
paste Excel, cảnh báo lệch giá/công nợ quá hạn (ghi backlog tại audit), báo cáo giá vốn.

## 5. Tiêu chí hoàn tất

1. Tạo phiếu không đổi tồn; lưu số kiểm không đổi tồn; chốt mới cộng tồn theo thực nhận.
2. Lệch thiếu có lý do; thiếu lý do bị chặn. STORE không chốt được; CASH không vào được.
3. Chống gửi lặp và đối chiếu timeout cho cả ba thao tác tạo/lưu/chốt; revision sai phải tải lại.
4. Dữ liệu cũ giữ nguyên tồn/tổng/nợ; báo cáo tồn không lệch sau migration.
5. In được phiếu chờ nhận và phiếu đã chốt.
6. Build Debug/Release sạch; test SQL/unit/WinForms chạy sau khi được duyệt đợt test.
