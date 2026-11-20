# T32 — Kiểm kỹ thuật build/publish/inventory

02/11/2026. Không phải nghiệm thu9ID hoặc gate native cuối.

- Publish-G08.ps1 build solution Debug/Release bằng MSBuild1luồng, publish riêng WinForms Release/win-x64/framework-dependent. Không đổi csproj/SDK/NuGet hoặc source sản phẩm.
- Ứng viên artifacts/g08/20261102_154615_7a524f5b tạo được nhưng kiểm sâu phát hiện CodeView trong DLL chứa PDB path máy tác giả. Không dùng làm ứng viên cuối. Sửa bước publish PathMap và kiểm PE debug directory; không chỉ xóa .pdb rồi coi đủ.
- artifacts/g08/20261102_155027_19c8013d:CandidateBuilt,94file,inventorySucceeded/0issues; metadata9assembly Debug/Release/publish không helper bypass và có positive LoginForm/AuthService/AuthRepository. PDB CodeView publish dùng /_/CoffeeManager, symbol ngoài không đóng gói. ArchiveSHA256:92EFA18CD2EDF44E2CE5FAC56489BE471D29CAADA73061475162CCC43B008DEE.
- Có cảnh báo NU1900 trong build/publish do NuGet audit feed không truy cập được.0error; không gọi0warning/audit vulnerability sạch. Desktop Runtime10.0.13 x64 có trên máy; chưa chạy native package hoặc máy khác, không suy PASS từ runtime inventory.
- Package cuối sẽ có release-manifest/package-manifest với filehash/migrationversion/runtimeconfig/SDK/sourcehash và candidate.zip; inventory chỉ kiểm thành phần tự động, chưa kiểm cờ cũ/login/logout/phiên/quyền trên app thật.

Xem release-candidate.json cạnh file này để biết ứng viên cuối sau đóng gói cùng hồ sơ hoàn chỉnh. Package chưa kèm source repo/backup/localconfig/DPAPI/credential thật/rawprivate evidence. Source bàn giao vẫn ở repo; chưa commit/push. Phần nguồn lịch sử325ID được giữ, không ghiPASS thay người dùng.

Ứng viên cuối: artifacts/g08/20261102_155435_c6b76b42/candidate.zip; CandidateBuilt, inventory0issues, giải nén và đối chiếu đủ94/94filehash. ArchiveSHA256:D1F7A782FB0C083DE02D30D0597DE4EC1AF68F138D17C36BF3143EBF88D8C54A. Không chạy exe/native trong kiểm integrity. Xem release-candidate.json; bản unzip integrity được giữ cùng ứng viên trong artifacts ignore, không là DB/filebackup còn sót.
