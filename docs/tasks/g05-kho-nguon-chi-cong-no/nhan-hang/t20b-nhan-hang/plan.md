# T20b — Nhận hàng 2 bước: plan triển khai

Đọc kèm [requirements](../requirements.md), [audit](../../audit-nhap-hang.md), kế hoạch G05.
Không đổi phạm vi ngoài các mục dưới; phát sinh mới phải duyệt bổ sung.

## 1. Migration `database/migrations/043_purchase_receiving.sql`

Tiền tố kiểm tra: `@@TRANCOUNT=0` hoặc thiếu `SchemaMigrations Version=42` thì THROW.

`PHIEUNHAP`:

- `TraNgayDuKien decimal(18,0) NOT NULL DEFAULT 0` — dự kiến trả ngay, chưa tạo phiếu chi.
- `HanTraDuKien date NULL` — hạn trả dự kiến người lập nhập (bổ sung khi triển khai; chỉ ghi nhận,
  chưa có nhắc nợ theo hạn).
- `NgayKiem datetime2(0) NULL`, `MaNVKiem char(5) NULL` FK `NHANVIEN`.
- `DaHuy bit NOT NULL DEFAULT 0`, `LyDoHuy nvarchar(200) NULL`, `NgayHuy datetime2(0) NULL`.
- Computed `TrangThai`: `DaHuy → 'DaHuy'`, `DaChot=1 → 'DaChot'`, `NgayKiem NULL → 'ChoNhan'`, còn lại `'DaKiem'`.

`CHITIET_PN`:

- `SoLuongNhan decimal(18,4) NULL` CHECK `0 ≤ SoLuongNhan ≤ SoLuong`.
- `LyDoLech nvarchar(200) NULL`, `DaGhiKho bit NOT NULL DEFAULT 0`.
- Backfill dòng cũ: `SoLuongNhan=SoLuong, DaGhiKho=1`.

Trigger:

- `TR_CTPN_Kho` dùng `CASE WHEN DaGhiKho=1 THEN SoLuongNhan ELSE 0 END`; `TongTien` theo dòng đã ghi kho.
- `TR_PhieuNhap_Tong` chỉ đối chiếu tổng khi `DaChot=1`; nháp bắt buộc `TongTien=0`.
- `TR_PurchaseLinesClosed` giữ khóa khi `DaChot=1`; chặn đổi `SoLuong` sau khi đã kiểm.

View: `vw_PurchaseRead` thêm `TrangThai, TraNgayDuKien, HanTraDuKien, NgayKiem, MaNVKiem, LyDoHuy`;
`vw_ReportStockEvents` nguồn Purchase dùng `SoLuongNhan` với `DaGhiKho=1`.

Procedure:

- `sp_TaoPhieuNhap` (ADMIN, replay theo RequestId, operation `PurchaseCreate`): validate lượng nguyên,
  giá nguyên, không trùng; insert header `DaChot=0`, `TongTien=0`, `TraNgayDuKien`, `HanTraDuKien`; insert dòng;
  không tạo chi/nợ.
- `sp_LuuKiemNhan` (ADMIN/STORE): chỉ `ChoNhan/DaKiem`, kiểm `Revision`, đủ `SoLuongNhan`,
  lệch phải có lý do; ghi `NgayKiem/MaNVKiem`; không đụng tồn.
- `sp_ChotNhanHang` (ADMIN + Finance nếu trả ngay > 0): chỉ `DaKiem`; khóa NVL;
  `DaGhiKho=1` → trigger cộng tồn; chốt tổng thực nhận; tạo chi `PurchaseImmediate` và nợ
  `CONGNO_NCC` theo thực nhận; `DaChot=1`; replay operation `PurchaseReceive`.
- `sp_HuyPhieuNhap` (ADMIN): chỉ `DaChot=0`, bắt buộc lý do; `DaHuy=1`; không đụng tồn.
- `sp_PurchaseList/Get/Receipt`: trả trạng thái/người kiểm/đặt-thực nhận-lệch-lý do; lọc trạng thái.
- `GRANT EXECUTE` đầy đủ cho `CafeManagerApp`.

## 2. Models/DAL/BLL

- `PurchaseRequests.cs`: `PurchaseReceiveLine/Request`, `PurchaseCancelRequest`.
- `PurchaseRow.cs`: `Status, TraNgayDuKien, DueDate, CountedAt, CountedBy, CancelReason`;
  `PurchaseDetailLine` thêm `Received, Diff, Reason`.
- `PurchaseTableBuilder`: builder dòng nhận.
- `PurchaseRepository`: `CreateDraftAsync, SaveCountAsync, PostReceiveAsync, CancelAsync`, map mới.
- `PurchaseService`: `CreateDraftAsync, SaveCountAsync, PostReceiveAsync, CancelAsync`;
  validate lượng nguyên; quyền tạo/chốt/huỷ ADMIN, lưu kiểm Inventory, chốt trả ngay Finance.
- Nhãn localization mới.

## 3. UI

- `PurchaseEditorDialog`: cột `MÃ NVL/NGUYÊN LIỆU/ĐVT/SỐ LƯỢNG/ĐƠN GIÁ (₫)/THÀNH TIỀN`;
  tổng realtime; chặn lẻ; định dạng tiền VN; hạn nợ theo nợ; hiện tồn/giá gần nhất;
  sửa nút cắt chữ; nút **Lưu phiếu chờ nhận**; in sau khi lưu; chặn ngày tương lai.
- `PurchaseReceiveDialog`: bảng đặt/thực nhận/lệch/lý do; STORE lưu kiểm; ADMIN chốt + chỉnh trả ngay;
  in phiếu; trạng thái/replay theo khuôn kiểm kê.
- `PurchasesControl`: cột trạng thái, lọc trạng thái, hành động theo dòng; chi tiết có đặt/thực nhận/lệch;
  ADMIN huỷ phiếu chờ nhận.
- `PurchaseSlipPdfReport`: in A4, preview `PdfPreviewForm`; không VAT/số hoá đơn.

## 4. Kiểm chứng

Build Debug/Release; chạy migration trên DB test; script SQL cũ + mới; unit test;
thao tác thật bằng ADMIN/STORE tại 125%; báo cáo tồn trước/sau khớp.
Test chỉ chạy sau khi duyệt đợt test.

## 5. Rủi ro

- Backfill sai có thể lệch tồn: kiểm tra Count/SUM trước commit migration.
- Trigger mới ảnh hưởng checkout/xuất/kiểm kê: regression bắt buộc.
- Legacy `CutoverKind` và báo cáo: giữ nguyên công thức cho dòng cũ.
- In PDF là chức năng mới: kiểm preview và lỗi in, không tự đổi mẫu hoá đơn bán.
