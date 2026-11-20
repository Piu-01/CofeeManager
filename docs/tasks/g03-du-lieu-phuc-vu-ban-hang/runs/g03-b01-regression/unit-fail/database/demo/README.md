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

Tài khoản không đồng nghĩa mọi module đã triển khai. ADMIN hiện dùng các danh mục
G02; POS và nghiệp vụ kho hoàn chỉnh thuộc các task tiếp theo. Nếu role vào màn
thông báo chức năng chưa triển khai, đó là giới hạn module hiện tại, không phải lỗi mật khẩu.

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
