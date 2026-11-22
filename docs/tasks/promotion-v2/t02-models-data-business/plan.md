# Plan t02 — Models, DAL, BLL

Phụ thuộc t01 (contract SQL). Không đổi tên trường cũ ngoài phạm vi dưới đây.

## Bước 1 — Models

`src/CafeManager.Models/Requests/Loyalty/LoyaltyRequests.cs`
- `PromotionRequest` thêm `string Kind = "Giam"`, `IReadOnlyList<PromotionGift>? Gifts = null`.
- Thêm `public sealed record PromotionGift(string Product,int Quantity);`.
- Giữ nguyên `StartDate/EndDate` kiểu `DateOnly`; BLL chịu trách nhiệm gán năm 2000/2001.

`src/CafeManager.Models/Dtos/Loyalty/LoyaltyRows.cs`
- `PromotionRow` thêm `string Kind`, `int GiftItemCount`, `int GiftQuantity` (đặt cuối
  record để call site cũ ít vỡ; cập nhật mọi constructor).
- Thêm `PromotionGiftRow(string Code,string Name,decimal Price,string Status,int Quantity)`.
- `PromotionDetail` thêm `IReadOnlyList<PromotionGiftRow> Gifts`.

`src/CafeManager.Models/Dtos/Loyalty/LoyaltyRows.cs` (preview)
- `DiscountPreview` thêm `IReadOnlyList<PreviewGift> Gifts`; thêm record
  `PreviewGift(string Code,string Name,string Product,string ProductName,int Quantity)`.

`src/CafeManager.Models/Dtos/Sales/`
- `CheckoutPreview` thêm `IReadOnlyList<PreviewGift> Gifts`, `int GiftCount`.
- `SalesLine` thêm `string? GiftPromotion = null` (mã KM quà; null = món bán).
- `InvoiceDetailDto` thêm `IReadOnlyList<PreviewGift> GiftProposals`; giữ constructor cũ
  (truyền `[]`).

## Bước 2 — DAL

`src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs`
- `Promotion(SqlDataReader)` đọc thêm cột Kind/GiftItemCount/GiftQuantity theo thứ tự
  t01 trả (cập nhật đọc 10 → 13 cột).
- `SavePromotionAsync`: thêm tham số `@Kind`, `@GiftJson` (serialize `r.Gifts`).
- `SetPromotionStateAsync`: giữ nguyên (Pause/Resume) nhưng không còn call từ UI.
- `PromotionDetailAsync`: đọc set 4 quà; trả `PromotionDetail(...,gifts)`.
- `DiscountAsync`: đọc thêm set 3 quà nếu có (`NextResultAsync` kiểm tra), trả `Gifts`.
- Helper serialize gift JSON dùng tên trường `Product`,`Quantity` khớp SQL.

`src/CafeManager.DataAccess/Repositories/SalesRepository.cs`
- `ReadOrder` set 2: `SalesLine` nhận thêm cột `MaKMQuaTang` (null → null; ngược lại
  TrimEnd). Cột thêm ở cuối nên các index cũ không đổi.
- `CheckoutAsync`/preview đọc set quà: map `PreviewGift`; header `GiftCount`.
- Không đổi `ReadOrder` 28 cột header (tránh rủi ro như B06).

`src/CafeManager.DataAccess/Repositories/InvoiceRepository.cs`
- `ReadDetail`: đọc thêm set 6 quà (nếu reader có result); set 2 dùng map `SalesLine`
  mới (đã bao gồm cột 8).
- `InvoiceDetailDto` nhận `GiftProposals`.

## Bước 3 — BLL

`src/CafeManager.Business/Services/LoyaltyService.cs`
- `Normalize`:
  - `Kind` chỉ `Giam/Tang`; mặc định `Giam`.
  - `Status` chỉ `DangChay/TamDung` (giá trị Nhap cũ tự chuyển DangChay).
  - `Audience` `ALL/TIER/WALKIN`.
  - Ngày: `StartDate` bắt buộc, gán năm 2000; `EndDate` null hoặc gán 2001 nếu
    `(End.Month,End.Day) < (Start.Month,Start.Day)`, còn lại 2000. Không dùng so
    `EndDate<StartDate` kiểu DateOnly nữa.
  - `TIER`: cho phép 0 hạng; non-TIER: cấm hạng.
  - `Giam`: `Discount` 0–100; items ≤ 500; `Gifts` phải rỗng.
  - `Tang`: `Discount=0`; `Gifts` 1–50 dòng, mã món 5 ký tự, SL 1–999, không trùng
    món (gộp SL nếu trùng), `Items` rỗng.
  - Thông điệp lỗi tiếng Việt mới thêm key vào `Translations/en.json`/`vi.json`
    nếu dự án dùng cặp key; giữ các key cũ còn dùng.
- `DeletePromotionAsync`: dựng lại `PromotionRequest` với `Kind` của row (thay vì
  mặc định) để payload khớp.
- `DiscountAsync` không đổi chữ ký.

Kiểm tra build: `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug`.
Lỗi call site constructor `PromotionRow`/`SalesLine` phải sửa tối thiểu (thêm tham số
cuối) — rà bằng `grep "new PromotionRow\|new SalesLine\|new DiscountPreview"`.

## Đầu ra

- C# compile Debug/Release.
- Chưa có UI mới; lỗi biên dịch do record đổi chữ ký phải xử lý trong task này.
- Không chạy test DB ở task này (thuộc t05).
