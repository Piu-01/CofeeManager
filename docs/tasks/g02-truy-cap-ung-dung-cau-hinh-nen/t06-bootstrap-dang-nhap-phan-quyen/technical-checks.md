# T06 — Kiểm tra kỹ thuật trong triển khai

Không là nghiệm thu T06-Kxx; chưa chạy ma trận nghiệm thu G02.

09/10/2026: `dotnet build CafeManager.slnx -c Debug`: thành công, 0 warning,
0 error. Migration001→002 chạy qua Apply-Migration.ps1 trên SQL Server localhost,
DB riêng `CoffeeManager_Test_G02_20261009_200203_a6502e88` có G02TestMarker.
Migration002 checksum tại lượt này:
`2AC389786AD1BCF70004687241D4F43242F6BEB49C5CADBCA9D1AAB5534C119B`.

`dotnet run --project tools/G02TechnicalSmoke -- <DB trên>`: tạo admin mới bằng
password sinh trong RAM, login case khác và Demand CatalogAdmin thành công.
Đây là smoke dependency bằng Windows identity của người triển khai, không chứng
minh runtime/setup principal hoặc bootstrap concurrency/rollback/rehash/GUI.
DB giữ để smoke lớp sau, không dùng DB ứng dụng; không password trong output.

T06 có migration/auth/session/setup/login đầu ra để T07 dùng. Procedure/setup
UI và unit/integration fixture còn phải nghiệm thu theo plan trong đợt riêng.

Lượt kỹ thuật cuối09/10/2026: DB
`CoffeeManager_Test_G02_20261009_212048_828af430`, migrations001→002→003 bản cuối;
checksum/manifest/log build/smoke trong
`../runs/technical_20261009_212048_828af430/technical.json`. Các DB/lượt checksum
trước giữ lịch sử triển khai, không ép rerun bảnSQL đã đổi vào DB cũ. Không chạy
nghiệm thu T06-Kxx hoặc xem smoke owner như kiểm chứng role hạn chế.
