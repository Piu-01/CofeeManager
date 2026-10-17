# SEC-PKCS-01 — Cảnh báo dependency PKCS

## Kết quả cuối đợt đã duyệt — 17/10/2026

**Đã xử lý và regression PASS trên môi trường/binary được kiểm.**
PinPKCS10.0.13 và càiWindowsDesktopRuntime10.0.13x64, giữ.NET10LTS/
SqlClient6.1.3. Actual-load probe cóUseWindowsForms xác nhận assembly thực tế
từ `Microsoft.WindowsDesktop.App/10.0.13`, informationalversion10.0.13 và
Environment.Version10.0.13; SHA256 lưu ở
`docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/runs/SEC_PKCS_20261017/actual-pkcs-load.json`.
Không lấy freshaudit sạch thay actual-load; FAIL trước runtime patch giữ nguyên.

G02 sau runtime patch222/222PASS và sauB07 484/484PASS; DAL sau runtime patch
20/20PASS. README yêu cầu DesktopRuntime10.0.13 hoặc bảnvá.NET10LTS mới hơn
khi bàn giao; không tuyên bố máy triển khai chưa cập nhật cũng đã được vá.
Chi tiết `test-results.md`. GateWindowsidentity G02 vẫn chưaPASS, tách biệt
với finding dependency này. Không chạyDoSPoC hoặc chứng minh exploit trongapp.

## Kiểm chứng 16–17/10/2026 (chưa đóng finding)

- Đã pin PackageReference PKCS10.0.13, giữ SqlClient6.1.3/.NET10LTS.
  Fresh restore có bật NuGetAudit all/low; audit không liệt kê package vulnerable.
- G02 runner `20261016_223718_97354082`:222/222PASS; T05 regression
  `20261016_223751_05e20ffe`:20/20PASS. Các kết quả trên **chưa** chứng minh
  runtime đã dùng PKCS vá.
- Probe có UseWindowsForms xác nhận assembly thực tế vẫn từ
  `Microsoft.WindowsDesktop.App/10.0.12`, informationalversion10.0.12.
  Giữ FAIL ở `G02_WINDOWS_REAL_R4_20261017/runtime-principal.json`; observation
  chi tiết sẽ lưu ở hồ sơ handoff. Vì vậy không đóng finding chỉ từ audit sạch.
- Đang cài Desktop Runtime10.0.13x64 trong phạm vi runtime patch đã duyệt;
  installer từ Microsoft builds, SHA512 đối chiếu metadata và Authenticode
  Microsoft hợp lệ trước khi chạy. Evidence `runs/SEC_PKCS_20261017/` của G02.
  Phải chạy lại actual-load probe và regression sau runtime patch.

**Trạng thái:** người dùng đã duyệt vá vàregression ngày16/10/2026;
đang triển khai/kiểm chứng, chưa chốtPASS.
Ngày16/10/2026, restore/build bản sao sạch trong regression Designer.

## Bằng chứng

- Dependencygraph trong `src/CafeManager.DataAccess/obj/project.assets.json`:
  `Microsoft.Data.SqlClient/6.1.3` → `System.Security.Cryptography.Pkcs/9.0.4`.
  Package trực tiếp tạiDataAccess.csproj làSqlClient6.1.3; PKCS làtransitive,
  tiếp tục truyền sangBusiness/WinForms. Không nâng/downgrade package trongtest.
- Restore sạch vàbuildclone báoNU1902/NU1903; log tại
  `docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/runs/G02_B03_B06_HANDOFF_20261016/fresh-clone-build.txt`.
- Advisory kiểm tra trực tiếp16/10, côngbố13/10/2026:
  - [GHSA-2fmv-rv2c-59x7 / CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7):
    Moderate5.9, CPUDoS khiWindowsEnvelopedCms.Decode xử lýCMS/PKCS#7 khôngtin cậy.
  - [GHSA-846f-f764-76mx / CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx):
    High7.5, CPUDoS ởSignedCms.CheckSignature vớiX.509data khôngtin cậy.
  Cả2 liệt kê9.0.0–9.0.20 bịảnhhưởng,9.0.21 cóbảnvá; nhánh10.0 cóbảnvá10.0.13.

## Ảnh hưởng và giới hạn kết luận

Đã xác nhận package nằm trongaffectedrange, **chưa** chứng minh đường khai
thác trongCoffeeManager hoặc thực hiệnDoSPoC. Không được biếncảnhbáo thành
kết luậnSQLServerđãbịkhai thác. Build/testfunctionalityPASS không xóa rủi ro.
Buildworkingtree dùngrestorecache báo0warning; freshrestore phát hiệnwarning:
hai bằng chứng khác nhau, không báo toànbộdependency“sạch”.

## Đề xuất cần duyệt

Đã được duyệt qua lựa chọn “Duyệt vá và regression”. Chọn pinPKCS10.0.13
(đã kiểm cótrênNuGet), giữSqlClient6.1.3 và.NET10LTS; không đổiDAO/framework.
Banđầu xét9.0.21, nhưngWindowsDesktop.App10.0.12 trênmáy cũngcungcấpPKCS10
thuộcaffectedrange; chọnpatch10 đểkhôngbịassemblyunification quayvềbảnframework
10.0.12. Phải kiểmactualassemblyload ởprobe cóUseWindowsForms vàappdeployment.
Đóngmanifest sau thayđổi rồi freshrestore/audit, kiểmactualassemblyload và
regressionG02/T05. Khôngtắt audit. Userđồngthờicho phépUAC provision2Windows
identity tạm để hoàn tất kiểm thử thật; tài khoản/SQLlogin thử phải cleanup.

1. Kiểm actualPKCSassembly/runtime đượcload vàcácđườngCMS/certificate qua
   SqlClient; kiểm compatibility với.NET10LTS vàWindowsAuthentication hiện tại.
2. ChọnbảnSqlClient được hỗtrợ đãnhậndependencyvá, hoặcpinPKCSpatchedversion
   phùhợp nếu cần. Xem cảruntime/SDKpatch; khôngtựđổi frameworkhaythưviệnDAL.
3. Freshrestore/build/audit; regressionT05kếtnối/typederrors/disposal,
   T06setup/auth/rehash vàG02integration trênSQLServerDBtest mới, nativeUI
   theoảnhhưởng. KhôngdisableNuGetAudit/NoWarn để giấu cảnhbáo.

Rủi ro nângdependency: nativeSNI/TLS/auth vàassemblyresolution; cần kiểm
đúngbinarypublish/runtime, không chỉ build cached trênmáydev. Chưa cóthayđổi
code/migration/package nào cho finding này.
