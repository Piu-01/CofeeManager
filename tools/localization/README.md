# Audit localization VI/EN

Chạy từ thư mục CoffeeManager:

```powershell
./tools/localization/audit.ps1
```

Cần SDK .NET 10 đã dùng cho solution. Tool tham chiếu Roslyn đi kèm SDK, không thêm
NuGet. Phân tích cú pháp C# nên không đếm comment, hiểu literal/nội suy và vị trí dòng.
Chỉ đọc source và từ điển; ghi `inventory.csv` vào hồ sơ task, không kết nối DB.

Kiểm tra chuỗi hiển thị chưa bọc, key thiếu/thừa/trùng, bản dịch rỗng và placeholder.
Các lệnh/profile và mẫu so sánh giữ nguyên giá trị; lý do xuất trong inventory.
Lệnh `ascii` giúp rà thêm literal ASCII (ví dụ Thu, Chi, Kho, Trang sau).

Audit tĩnh không chứng minh được mọi giá trị sinh động ở runtime hoặc layout native.
Khi thêm một trường chứa mã/trạng thái cần dịch nhãn ở nơi hiển thị; không gọi `L.T`
trên tên món, tên khách, ghi chú hay dữ liệu do người dùng nhập. Khi thêm chuỗi mới,
dùng `L.T`/`L.F`, thêm bản EN vào resource rồi chạy audit và unit test.
