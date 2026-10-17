# T08 — Kiểm tra kỹ thuật trong triển khai

09/10/2026: migration003 compile/apply trên SQL Server localhost, DB test riêng
có G02TestMarker. Lượt cuối dùng DB
`CoffeeManager_Test_G02_20261009_212048_828af430`, schema rỗng, không seed,
chuỗi001→002→003. Evidence tại `../runs/technical_20261009_212048_828af430/`.

Dependency smoke: bootstrap→login→CatalogAdmin; tạoDM qua requestGUID, đọc và
sửa bằngrevision; selector checkout trả hai phương thức hỗ trợ của DB sạch.
Password sinh trong RAM, không stdout/file; smoke không là nghiệm thu.

Debug/Release thành công, 0 warning/0 error. Test code compile; chưa chạy xUnit,
ma trận7bảng/3role/biên/history/migration-rerun/races hoặc GUI. T08-K01–K10 và
mọi subcase hiện NOT RUN/chờ duyệt đợt test riêng.

Build source UI cuối tại `../runs/build_20261009_212611_777cf7c4/`: Debug và
Release đều0warning/0error; manifest riêng, không chạy lại ma trận nghiệm thu.
