# Test results — Gộp tồn đầu vào "Thêm nguyên liệu", bỏ "Mở sổ" và tab Tồn kho trong Kho

- Ngày: 22/11/2026
- Môi trường: Windows 10.0.26200, .NET SDK 10.0.401, SQL Server 2022 (localhost, Windows auth)
- DB test riêng: `CoffeeManager_Test_041_20261122_manual` (restore từ bản clone schema 40
  `CoffeeManager_Test_040_20261121_214133`, sau đó apply migration 041 — checksum
  `5E0AF4906D6C40393955C32F3E7C4639BCAE97795C69670F0C16EA4D4AB8A156`).
  DB `CoffeeManager_Development` **không** bị đụng, vẫn ở schema 40.
- Source: working tree sau triển khai (chưa commit). Trong đợt test **không sửa source/migration**.
- Bằng chứng: `runs/verify-20261122/` (build.log, verify041.sql, verify041.out.txt, unit-tests.log, g03-tests.log).

## Compile

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| MO-B01 | `dotnet build CafeManager.slnx` (OutputPath tạm vì bin repo đang bị app đang chạy khóa) | PASS — 0 error, 2 warning có sẵn (WebView2/WindowsBase) | `runs/verify-20261122/build.log` |

## SQL / integration trên DB test thật

Chạy `sqlcmd -f 65001 -i runs/verify-20261122/verify041.sql` trên `CoffeeManager_Test_041_20261122_manual`.
Cột `Pass` = 1 là PASS.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| MO-S01 | Tạo nguyên liệu không nhập tồn đầu → NGUYENVATLIEU tồn 0; MaterialOpening Quantity 0, Kind Manual, nguồn mặc định "Khởi tạo nguyên liệu", UnitSnapshot/actor đúng | PASS (K01=1) | `verify041.out.txt` |
| MO-S02 | Tạo nguyên liệu có tồn đầu 1.25 + nguồn → tồn 1.25, mốc 1.25, UnitSnapshot Kg, nguồn đúng | PASS (K02=1) | `verify041.out.txt` |
| MO-S03 | Gửi lặp cùng RequestId/nội dung → không nhân bản (1 mốc, tồn không đổi) | PASS (K03=1) | `verify041.out.txt` |
| MO-S04 | Có tồn đầu nhưng thiếu nguồn → bị chặn và không tạo nguyên liệu | PASS (K04 Rejected=1, NotCreated=1) | `verify041.out.txt` |
| MO-S05 | STORE tạo nguyên liệu → được phép, mốc ghi đúng actor STORE | PASS (K05=1) | `verify041.out.txt` |
| MO-S06 | Sau nâng cấp không còn nguyên liệu thiếu mốc MaterialOpening | PASS (K06=1) | `verify041.out.txt` |
| MO-S07 | Báo cáo tồn kỳ bắt đầu **sau** mốc → Coverage `Complete`, Opening 1.25 = Closing = CurrentStock | PASS (K07: Complete=1, dòng NVL94 `Complete`) | `verify041.out.txt` |
| MO-S08 | Báo cáo tồn kỳ gồm ngày tạo (mốc nằm trong kỳ) → `Incomplete: kỳ bắt đầu trước mốc mở sổ` | PASS về mặt quy ước hiện có (K07b: Complete=0, đúng semantics báo cáo 026) | `verify041.out.txt` |
| MO-S09 | Nguồn kho của nguyên liệu mới có dòng `Opening` (1.25, ngày = RecordedAt) | PASS (K08, TotalCount=1, SourceType=Opening) | `verify041.out.txt` |
| MO-S10 | Đổi đơn vị khi tồn đầu 0 → cho phép, cập nhật cả UnitSnapshot của mốc | PASS (K09=1) | `verify041.out.txt` |
| MO-S11 | Đổi đơn vị khi tồn đầu > 0 → bị chặn, đơn vị giữ nguyên | PASS (K10 Rejected=1, UnitAfter=Kg) | `verify041.out.txt` |
| MO-S12 | Xóa nguyên liệu chưa dùng → xóa kèm mốc, không lỗi FK | PASS (K11=1, K12=1) | `verify041.out.txt` |
| MO-S13 | User runtime của app mất quyền `sp_MaterialOpen` nhưng vẫn tạo nguyên liệu qua `sp_MaterialCreate`, mốc được tạo | PASS (K13 OpenDenied=1, AppCreateAllowed=1, AppOpeningRows=1) | `verify041.out.txt` |
| MO-S14 | Schema version sau migrate = 41 | PASS (K14=1) | `verify041.out.txt` |

Lưu ý: lần chạy đầu dùng sqlcmd không có `-f 65001` nên literal tiếng Việt trong script bị lệch encoding
(K01 so nguồn mặc định báo sai) và principal impersonate sai tên; đã reset lại DB test từ bản clone rồi chạy
lại bằng UTF-8 và user runtime thật. Kết quả trên là lượt chạy sạch.

## Unit test (xUnit)

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| MO-U01 | Unit test không phụ thuộc DB: `--filter "FullyQualifiedName!~Integration&FullyQualifiedName!~G02"` | PASS — 173/173 | `unit-tests.log` |
| MO-U02 | Nhóm G03 (gồm 3 test mới: `OpeningQuantityIsOptionalOnCreate`, `OpeningQuantityNeedsSource`, `OpeningIsIgnoredWhenUpdating`, permission MaterialRead/Manage 2 tham số) | PASS — 18/18 | `g03-tests.log` |
| MO-U03 | Test integration cần fixture DB riêng (G02/Integration/ParameterAndError) | BLOCKED — 367 test fail vì thiếu `Test-DataAccess.ps1`/`Test-G02.ps1` và DB test cách ly; điều kiện có sẵn từ trước, không liên quan thay đổi này | lần chạy toàn bộ (không lưu log fail) |

## Kiểm chức năng UI

| Mã | Nội dung | Kết quả | Ghi chú |
|---|---|---|---|
| MO-F01 | Mở "Thêm nguyên liệu": hiện 2 ô tùy chọn tồn đầu/nguồn; bỏ trống tạo bình thường; nhập > 0 hiện xác nhận | NOT RUN | App đang chạy là build cũ trước thay đổi (bin bị khóa); chưa có thông tin đăng nhập để chạy bản build tạm trên DB test |
| MO-F02 | Màn Nguyên liệu không còn nút "Mở sổ"; Kho chỉ còn Nhập hàng/Xuất kho/Kiểm kê | NOT RUN | cùng lý do trên |
| MO-F03 | Hình thức/màu/spacing | SKIPPED theo phân công — người dùng tự kiểm | — |

## Kết luận đợt test

- Không có ca FAIL. SQL nghiệp vụ chính (tạo có/không tồn đầu, chống trùng, thiếu nguồn, đổi đơn vị,
  xóa kèm mốc, báo cáo, quyền app) đều PASS trên SQL Server thật ở DB test riêng.
- Phần chưa kiểm được: UI chức năng (MO-F01/F02) do app cũ đang chạy và thiếu tài khoản đăng nhập;
  367 test fixture DB bị BLOCKED bởi điều kiện môi trường có sẵn.
- Chờ người dùng quyết định: chạy đợt kiểm UI trên build tạm (cần tài khoản/DB test).
- Quyết định 22/11/2026: giữ nguyên quyền nhập tồn đầu cho ADMIN+STORE như hiện tại; không đổi source.
