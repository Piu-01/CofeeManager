# Bàn giao Git G03 — 23/10/2026

Người dùng yêu cầu “push đi”. Phạm vi commit: G03 T09–T13, migrations004–009,
kiểm thử nguồn, UI Orbit và các sửa đã duyệt, tiện ích đăng nhập TEST tạm cùng hồ sơ gỡ.
Giữ nguyên thứ tự/checklistT01–T32 và lịch sử kết quả; yêu cầu push không tự cấpPASS
những ca chưa chạy. Người dùng đã chấp nhận đợtG03 trước đó; xem report và biên bản
từng đợt để phân biệt kiểm tự động, manual, ngoại lệ và phần chưa kiểm.

- Cửa sổ con Orbit: B01/B02 đã sửa,44ca regression chức năngPASS; hình thức người dùng tự kiểm.
- Nút TEST: lỗi quyền marker đã sửa trên DB test riêng, regressionB01PASS ở125%;
  toàn bộQ01–Q10 chưa được nghiệm thu. Tiện ích còn tạm thời; phải gỡ cuốiG08/T32 trướcPASS.
- Có source công cụ provision và probe tối thiểu được launcher/provision tham chiếu;
  manifest test chỉ là metadata, không chứa connection/password. Máy khác phải chuẩn bị
  DBtest/profileDPAPI của riêng mình; commit không cung cấp DB hoặc credential local.
- Fixture mock công khai trong database/demo theo yêu cầu demo trước đó là dữ liệu mẫu;
  không dùng cho cài đặt thật. Không đưa credential SQL/Windows hoặc DPAPI vào Git.
- Không đưa bin/obj, log, ảnh/output probe, DBbackup, cấu hìnhuser/.vs hoặc token vào commit.
  Các thay đổi xóa bộguide cũ ngoài lựa chọn commit được giữ ở working tree, không tự hoàn tác.

Không chạy lại toàn bộ kiểm thử khi đóng commit; kết quả build/test được kế thừa đúng
source và phạm vi đã ghi trong hồ sơ. Các artifact máy-local không được coi là kèm theo
repo chỉ vì tài liệu lịch sử có nhắc đường dẫn. Không tạo PR, không merge hoặc forcepush.
