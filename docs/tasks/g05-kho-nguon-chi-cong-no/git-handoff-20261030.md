# Bàn giao Git30/10/2026

Người dùng yêu cầu “push đi”, sau đó “continue”. Repo xác thực tại CoffeeManager, nhánh main, remote origin `https://github.com/Piu-01/CofeeManager.git`.

Commit lưu source G04/G05, migrations010–021, tests/tools, UI dùng chung và hồ sơ/bằng chứng kiểm thử phù hợp. HEAD trước bàn giao là bce84b4 (G03), nên cần kèm đầu ra G04 để G05 có đủ phụ thuộc. Giữ G04/G05 Đang làm; G05 đối chiếu hiện tại44/55 đủ bằng chứng PASS và11ca chưa đủ, xem coverage-current.md. Không coi push là nghiệm thu hoàn tất.

G06 đang được triển khai đồng thời: không đưa hồ sơ G06, migration022, module nhân sự hoặc quyền EmployeeManage vào commit. Năm file chung được stage bằng snapshot bỏ riêng phần G06, không sửa/khôi phục file working tree đang làm. PermissionService G06 giữ ngoài stage. Source UI bán hàng tham chiếu OrbitSurface/OrbitBadge nên hai control này được đưa kèm để snapshot đủ phụ thuộc.

Đã xuất chính index ra thư mục riêng và build Release toàn solution:0warning/0error. Chạy22/22unit G05 PASS trên snapshot đó. Bằng chứng regression chức năng trước push ở runs/b07-20261030; snapshot build không thay nghiệm thu native toàn UI. Lượt kiểm snapshot đầu phát hiện thiếu hai file UI dependency; bổ sung file có sẵn, không sửa logic nghiệp vụ.

Rà danh sách stage và dữ liệu text: không có credential/token/private key/config local, SQL backup, DB files, bin/obj, exe/dll. Không đưa hàng loạt build artifacts cũ hoặc bộ guide cũ đang bị xóa local vào commit. Working tree còn tài liệu/artifact chưa chọn và G06 làm dở; không reset/clean hay xóa thay đổi đó. Log build/unit và manifest lựa chọn nằm local ở runs/push-20261030.
