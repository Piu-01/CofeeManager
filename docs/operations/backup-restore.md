# Sao lưu và phục hồi

Operator xác minh SQL instance, DB nguồn và phiên bản; đóng các phiên app/writer trước lấy snapshot đối chiếu. Backup là SQL BACKUP DATABASE, không copy MDF/LDF đang mở. Đường .bak thuộc filesystem **SQL Server**, có thể không phải máy đang chạy PowerShell. Tài khoản dịch vụ SQL phải ghi được thư mục; script không tự cấp ACL/đổi service.

## Tạo backup mới

```powershell
./tools/database/Backup-Database.ps1 -Server localhost -Database CafeManager_New -BackupFile 'D:\SQLBackups\CafeManager_New_YYYYMMDD_unique.bak' -ManifestPath ./install-evidence/backup-unique.json
```

Thay đường dẫn bằng thư mục server thực đã được operator chuẩn bị. File phải chưa tồn tại; không append/overwrite backup cũ. COPY_ONLY/CHECKSUM không đổi chuỗi backup thường. Script VERIFYONLY, kiểm header/GUID/nguồn và hash các bảng theo PK trước/sau backup; nếu writer thay đổi nguồn thì báo Failed, không tuyên bố snapshot parity. VERIFYONLY không thay thử restore. Manifest có đường dẫn local server và hash; giữ riêng, không đưa lên Git/package công khai. Không log connection string/password/dòng TAIKHOAN.

## Thử restore vào DB test mới

```powershell
./tools/database/Restore-TestDatabase.ps1 -Server localhost -BackupManifest ./install-evidence/backup-unique.json -RunId 20261102_120000_0123abcd -Database CoffeeManager_Test_G08_20261102_120000_0123abcd_restore -ManifestPath ./install-evidence/restore-unique.json
```

RunId phải đúng lượt mới, đích test chưa tồn tại. Công cụ kiểm backup GUID/header, VERIFYONLY, FILELISTONLY và MOVE từng file data/log tới tên mới ở thư mục SQL mặc định. Không WITH REPLACE, không chạm file nguồn, không restore đè DB sử dụng. Chỉ hỗ trợ data/log thường; FILESTREAM trả lỗi cần operator xem xét. File mapping/ownership/manifest và marker đích đều được xác minh; marker sao chép từ nguồn không đủ làm quyền cleanup.

Sau restore kiểm DB online, snapshot từng bảng khớp, SchemaMigrations/version/checksum. Với backup schema cũ, operator áp các version còn thiếu tăng dần qua Apply-Migration.ps1; chạy lại version đã áp dụng chỉ hợp lệ khi checksum trùng. So tiền/kho/điểm/nợ/lương/liên kết trước/sau; không suy từ rowcount. Identity SQL/runtime mapping phải được operator kiểm và map đúng login, không cấp db_owner để vượt lỗi orphan user. Kiểm user Windows đã tồn tại và các grant/deny của CafeManagerApp. Dùng package đúng phiên bản để login và smoke trên DB restore riêng.

## Sử dụng và giữ bản sao

Công cụ chỉ restore test mới, không tự promote sang production hoặc thay config người dùng. Khi cần khôi phục sử dụng thật, operator phải chốt kế hoạch chuyển DB/cấu hình, thời điểm dừng phiên và rollback riêng; không nhầm công cụ test là lệnh ghi đè production. Nếu thiếu quyền/thư mục/chứng chỉ thì dừng ở lỗi cụ thể, không fallback DB khác.

Backup có thể chứa dữ liệu cá nhân/credential hash: giữ ngoài repo và package, phân quyền đọc hạn chế, mã hóa/lưu theo chính sách operator. Không xóa backup có trước. Tài nguyên thử do lượt tạo cần manifest tên/file chính xác và marker/ownership trước dọn; không DROP wildcard hoặc xóa mọi .bak. Lỗi cleanup ghi tài nguyên còn giữ và cách xử lý, không báo sạch từ rollback fixture.
