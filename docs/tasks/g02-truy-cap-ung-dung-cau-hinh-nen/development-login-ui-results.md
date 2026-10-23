# Login demo trên giao diện — 18/10/2026

Phạm vi người dùng duyệt: nhận tài khoản hiện có ngay trên GUI, không terminal.
Không thay đổi DB, mật khẩu, migration hoặc source BLL/DAL/Models.

| Kiểm chứng | Actual | Trạng thái |
|---|---|---|
| Compile WinForms Debug | 0 warning, 0 error | PASS kỹ thuật |
| Compile WinForms Release | 0 warning, 0 error | PASS kỹ thuật |
| git diff --check các file thay đổi của fix | Không lỗi whitespace | PASS kỹ thuật |
| F5 → chọn từng role → Login → Logout | Chưa thao tác GUI sau fix | NOT RUN |
| Password che mặc định, Hiện mật khẩu, Tab/resize/DPI | Chưa thao tác GUI sau fix | NOT RUN |
| Thiếu/hỏng DPAPI; profile thường/explicit connection không có nút demo | Đã review nhánh source, chưa kiểm runtime | NOT RUN |

Các ca GUI liên quan T06-K03/K06/K07/K08, T07-K01/K02/K04 và UX-K01/K39/K40
cần nghiệm thu trên app thật; không kế thừa PASS service để đánh dấu GUI PASS.
Code chỉ đọc mật khẩu khi chọn role và điền input; không gọi SQL hay tự mở MainForm.
Khả năng nhận credential này dành riêng demo development của Windows user đang sở hữu
DPAPI; không là chức năng reset tài khoản hoặc thiết kế production.

Lệnh compile: dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj
--no-restore -c Debug (và Release) -m:1 -p:UseSharedCompilation=false -v minimal.

## Phương án bàn giao cuối — mock credentials, Login thường (18/10/2026)

Thay thế phương án chọn role ở trên theo yêu cầu người dùng. Đã gỡ
DevelopmentAccounts.cs, các nút chọn role và cờ truyền vào Login/session context.
Program chỉ giữ chọn connection development từ profile, không đọc mật khẩu account.
Dữ liệu nguồn: database/demo/accounts.json; hướng dẫn: database/demo/README.md.
Mật khẩu mock công khai do người dùng yêu cầu; không xuất bí mật SQL/Windows.

| Kiểm chứng | Kết quả |
|---|---|
| Build tool và WinForms Debug/Release cuối | PASS, 0 warning / 0 error |
| Áp dụng fixture đúng DB marker, ba identity đã active | PASS |
| Login bằng PasswordHasher/AuthService hiện có với ba mật khẩu trong fixture | PASS, admin ADMIN / thungan CASH / kho STORE |
| CatalogAdmin chỉ ADMIN, session revoke chặn sử dụng | PASS cả ba account |
| Config DPAPI, runtime không sysadmin/db_owner/setup, raw SQL bị chặn | PASS qua Verify |
| So sánh số dòng/CHECKSUM_AGG BINARY_CHECKSUM mọi bảng dbo ngoài TAIKHOAN trước/sau | PASS không thay đổi; checksum là đối chiếu bổ sung, không chứng minh không va chạm |
| Áp dụng lại fixture không ghi hash/rowversion lần nữa | PASS, snapshot account stamp giống nhau |
| F5 nhập bằng tay, che mật khẩu, logout và ma trận DPI | NOT RUN sau thay đổi cuối; không lấy service PASS thay GUI |

Bằng chứng tại runs/MOCK_LOGIN_20261018. Lần compile đầu khi gỡ helper còn sót
tham chiếu gây lỗi compile; đã gỡ hết và build lại PASS trước lượt kiểm chứng cuối.
Lần SELECT snapshot đầu dùng alias RowCount gây SQL156, dừng trước mọi ghi DB;
đã sửa alias RowsTotal và chạy lại. Không tạo DB test, Windows user hay SQL login mới.
Lần khởi tạo fresh qua tool tự áp dụng fixture sau bootstrap; nhánh fresh chưa được
chạy lại trên DB mới trong đợt này để tránh tạo thêm tài nguyên không cần thiết.
