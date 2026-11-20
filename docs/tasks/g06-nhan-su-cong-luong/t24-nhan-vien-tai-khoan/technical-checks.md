# t24-nhan-vien-tai-khoan — Kiểm tra kỹ thuật triển khai

30/10/2026. SDK10.0.401; source chưa commit; không nghiệm thu Txx-Kxx.

- Lượt cuối: [manifest](../runs/technical_20261030_154603_b2bd4fb6/manifest.json), [typed SQL/BLL/DAL và UI actual](../runs/technical_20261030_154603_b2bd4fb6/typed-smoke.json), [Debug/probe build](../runs/technical_20261030_154603_b2bd4fb6/probe-build.log).
- SQL Server thật, clean/demo riêng, baseline→migrations001–024. Runtime G06Runtime/CafeManagerApp cho lời gọi sản phẩm qua EXECUTE AS USER; owner chỉ provisioning/đối chiếu. Tạo2/dọn2/còn0; source unchanged trong smoke.
- Hồ sơ NV011/accountCASH/receipt; công8 giờ; preview/chốt MaBL3=280.000đ và một phiếu chi TC20250505. Không ghi development.
- WinForms control thật trong MainForm:4tab, binding11/11/1/1 dòng; DeviceDpi120 (125%); client1280×720 và1366×768. Payroll modal có owner đúng, HWND owner disabled, no-taskbar, native border, AcceptButton=null. Chưa kiểm đầy đủ editor handler/native keyboard hoặc link nguồn lồng; không nghiệm thu hình thức.
- [Release](../release-build.log) và [compile test](../tests-build.log):0warning/0error; Debug/probe cũng0warning/0error. Chưa chạy dotnet test.
- Chi tiết các lượt kỹ thuật trước/lỗi compile/contract đã xử lý ở [báo cáo](../report.md); giữ log lịch sử. Đã sửa trước lượt cuối, không phải debug trong đợt nghiệm thu.
- Nghiệm thu toàn task NOT RUN, chờ duyệt đợt test G06. Hình thức SKIPPED theo phân công; DPI100/150%NOT RUN. Development chưa nâng, chưa commit/push.
