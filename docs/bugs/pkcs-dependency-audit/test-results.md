# SEC-PKCS-01 — Kết quả kiểm chứng 16–17/10/2026

Đã duyệt vá và regression; source/package cố định trong từng lượt test.
Giữ.NET10LTS,SqlClient6.1.3; PKCS10.0.13, DesktopRuntime10.0.13x64.
Evidence path dưới đây tính từ `docs/tasks/`.

| ID | Thao tác / expected | Actual / evidence | Kết quả |
|---|---|---|---|
| SEC-01 |Freshrestore NuGetAudit=true/all/low, vulnerable include-transitive |`g02-truy-cap-ung-dung-cau-hinh-nen/runs/SEC_PKCS_20261016/restore.txt`,`audit.json`:không liệt kê package vulnerable |PASS |
| SEC-02 |Debug/Release build không suppressaudit |Buildthànhcông;Release cuối ở`runs/G02_B07_HANDOFF_20261017/release-build.txt` |PASS |
| SEC-03.before |WinForms load patched assembly |Trước runtimepatch vẫn loadDesktop10.0.12,không bỏ quaFAIL |FAIL lịch sử |
| SEC-03.after |WinForms actualassembly version>=10.0.13 |`runs/SEC_PKCS_20261017/actual-pkcs-load.json`:pathDesktop10.0.13, informational10.0.13, runtime10.0.13, fileSHA256 |PASS |
| SEC-04 |G02 SQL/unit regression trênDBtest thật |Runner`20261017_155555_beee42d8`:222/222PASS;B07runner`20261017_155948_e814a21e`:484/484PASS |PASS |
| SEC-05 |T05 typed connection/error/dispose/transaction regressions |`g01-nen-tang-ky-thuat-du-lieu/t05-dal-nen-kiem-thu/runs/20261017_155740_13e97169`:20/20PASS,exit0 |PASS |
| SEC-06 |Officialinstaller integrity trước UACinstall |SHA512khớpmetadata,AuthenticodeMicrosoftValid,exitcode/log tại`runs/SEC_PKCS_20261017/` |PASS |

Không nâng SDK/framework, không đổi DAL, không tắtNuGetAudit/NoWarn.
Windowsidentity thật/setup-popup của G02 chưa có kết quả không suyPASS từđây.
DoSPoC không chạy, ngoài yêu cầu; không cam kết mọi máy chưa cập nhật được vá.
