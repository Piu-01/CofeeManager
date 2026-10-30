# Fixture G05

`fixture.sql` và t19–t23 chỉ dành DB SQL Server test có G05TestMarker. Ghi rõ phần SQL được chạy trong G05AcceptanceLog; các dòng có hậu tố .SQL không thay PASS toàn ID có GUI hoặc nhánh khác.

Chỉ thực thi sau duyệt đợt test. `tools/database/Test-G05.ps1` chạy xUnit và các subcase SQL này trên DB demo riêng, lưu kết quả thô, không tự nâng 55ID. Hai session/fault/legacy-invalid/runtime principal/WinForms theo test-plan.md phải có bằng chứng bổ sung, không suy từ runner này.

Actor fixture kích hoạt chỉ trong DB test, không reset mật khẩu hoặc thay account development. Native login cần chuẩn bị mật khẩu fixture riêng khi duyệt test; không ghi bí mật vào log/repo.
