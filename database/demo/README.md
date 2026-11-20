# Dữ liệu mẫu để đăng nhập và dùng thử

DB mẫu: **CoffeeManager_Development** trên SQL Server **localhost**.
Mở `CafeManager.slnx`, Startup Project `CafeManager.WinForms`, profile
`CoffeeManager.Development`, bấm F5. Tại Login, tự nhập một tài khoản dưới đây.
Không chọn vai trò trên màn hình và không cần chạy terminal.

| Người dùng mẫu | Tên đăng nhập | Mật khẩu demo | Vai trò | Nhân viên |
|---|---|---|---|---|
| Người quản lý | `admin` | `Admin@123` | ADMIN | NV001 |
| Thu ngân | `thungan` | `Cash@123` | CASH | NV004 |
| Thủ kho | `kho` | `Store@123` | STORE | NV010 |

Đây là thông tin mock công khai phục vụ demo theo yêu cầu, không phải tài khoản
SQL Server/Windows hoặc mật khẩu dùng thật. DB chỉ lưu hash của các mật khẩu này.
Dữ liệu máy đọc nằm ở [accounts.json](accounts.json); đã đồng bộ ba account trong
DB mẫu ngày 18/10/2026. Nút Hiện mật khẩu chỉ bật/tắt che nội dung bạn đang nhập.

## Dữ liệu nghiệp vụ mẫu

`database/Insert_Data.sql` là nguồn seed nghiệp vụ. Có menu/công thức/nguyên liệu,
khách/hạng/khuyến mãi, bàn/khu vực, NCC/phiếu kho/nợ, nhân viên/công/lương và tài sản.
10 hóa đơn, 18 dòng món: 9 đơn đã thanh toán, 1 đơn mở ở bàn B0003.
Ví dụ lịch sử: HD20250401 phải trả 112.500 ₫; HD20250409 phải trả 81.000 ₫,
phân bổ 30.000 + 51.000 ₫. Dữ liệu lịch sử chủ yếu tháng 04/2025: chọn đúng ngày
khi tra cứu, không coi khuyến mãi cũ là còn hiệu lực hôm nay.

Source hiện có các module đến G07/schema027, nhưng DB development không tự nâng
theo source và các nhóm còn ca chưa đủ bằng chứng nghiệm thu. Kiểm schema thực tế
và báo cáo nhóm trước dùng thử; không suy toàn bộ chức năng PASS từ đăng nhập được.
Hướng dẫn cài clean/demo và package mới tại [operations](../../docs/operations/install.md).

## Dữ liệu vận hành 60 ngày

[demo/seed-demo-operational.sql](seed-demo-operational.sql) bổ sung dữ liệu vận hành
neo theo `SYSDATETIME()`: 60 ngày bán hàng gần nhất (khoảng 3.300 hóa đơn đã thu,
đủ 3 kênh tại quán/mang đi/giao hàng), đơn đang mở, nhập kho và công nợ NCC có trả
một phần, chấm công 3 tháng, bảng lương 2 tháng đã chốt gần nhất (còn 2 phiếu lương
part-time chờ chi), tài sản, chi phí vận hành, xuất kho, kiểm kê, khách hàng mới và
khuyến mãi còn hiệu lực. Script một transaction, guard `KM26HRO` chống chạy lặp, và
tự kiểm tra tổng tiền = thanh toán, tiêu hao, tồn không âm, điểm, công nợ, lương
trước khi COMMIT.

Chạy sau khi đã có schema 029, `Insert_Data.sql` và bootstrap development:

```bash
sqlcmd -S localhost -d CoffeeManager_Development -C -b -i database/demo/seed-demo-operational.sql
```

Script chỉ thêm dữ liệu, không sửa/xóa dữ liệu cũ; đơn mở đang có của bạn được giữ nguyên.
Chạy lại sẽ dừng ngay do guard.

## Top-up dữ liệu hằng ngày

Dữ liệu 60 ngày neo theo ngày chạy nên vài ngày sau sẽ thiếu hóa đơn "hôm nay"
(màn Lịch sử hóa đơn mặc định lọc ngày thanh toán = hôm nay). Trước khi demo,
chạy thêm script top-up; script chỉ thêm phần còn thiếu và chạy lại trong ngày
không nhân đôi dữ liệu:

```bash
sqlcmd -S localhost -d CoffeeManager_Development -C -b -i database/demo/seed-demo-daily.sql
```

Script bổ sung cho 7 ngày gần nhất: hóa đơn đủ 3 kênh với khung giờ thực tế
(sáng, trưa, chiều, tối), nhiều phương thức thanh toán và chia nhiều hình thức;
chấm công hôm nay theo ca; bảng lương tháng hiện tại cho nhân viên part-time
(nếu ca đã đủ giờ); tài sản mới và chi phí bảo trì gắn tài sản; một khoản chi
vận hành trong ngày. Script tự kiểm tra tổng thanh toán khớp hóa đơn, tiêu hao,
tồn không âm trước khi COMMIT.

Lưu ý màn hình Tài sản chỉ mở cho vai trò ADMIN; tài khoản `thungan`/`kho`
không thấy mục này theo phân quyền, không phải dữ liệu trống.

## Dành cho người cài dữ liệu mẫu

Baseline seed gốc giữ nguyên ba placeholder inactive để không tự tạo credential
công khai trong cài đặt thật. Bộ development mới chạy schema → seed → migrations →
bootstrap → áp dụng fixture accounts.json. Không chạy lại Insert_Data.sql trên DB có dữ liệu.

Khởi tạo development mới qua `tools/database/Initialize-Development.ps1` tự đồng bộ
fixture ở lần đầu. Khởi tạo lại không reset account đang hoạt động. Khi chủ động cần
đưa ba mật khẩu development về đúng file mock này, operator chạy:

```powershell
dotnet artifacts/development-setup/CafeManager.DevelopmentSetup.dll --apply-mock-accounts database/demo/accounts.json
```

Lệnh chỉ chấp nhận DB development cố định có marker và đúng ba identity/role/nhân viên;
chỉ đổi hash mật khẩu trong một transaction khi chưa khớp, không xóa/tạo lại dữ liệu.
Đây là thao tác chuẩn bị của người cài, không phải bước người dùng cần làm để đăng nhập.
