# Nhận hàng 2 bước — bộ plan

Trạng thái: kế hoạch triển khai đã được duyệt 23/11/2026. Một task tại một thời điểm.
Requirements: [requirements.md](requirements.md). Audit nguồn: [audit-nhap-hang.md](../audit-nhap-hang.md).

| Thứ tự | Task | Đầu ra | Phụ thuộc |
|---|---|---|---|
| 1 | T20b — Nền dữ liệu nhận hàng | Migration 043, trigger/view/procedure, backfill | T20 (đã có) |
| 2 | T20b — Lớp dữ liệu và nghiệp vụ | Models, DAL, BLL, quyền, validate số nguyên | 1 |
| 3 | T20b — UI lập phiếu theo audit | Editor sửa theo A1–A14, lưu chờ nhận | 2 |
| 4 | T20b — UI nhận hàng và danh sách | `PurchaseReceiveDialog`, danh sách/chi tiết, huỷ | 3 |
| 5 | T20b — In phiếu nhập | PDF FastReport + preview, nút in | 4 |

Chi tiết từng bước: [t20b-nhan-hang/plan.md](t20b-nhan-hang/plan.md).
Test để sau khi người dùng duyệt đợt test; không sửa source trong lượt test.
