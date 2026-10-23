# Bàn giao màn mẫu — chờ review

**Cập nhật R10:** theo yêu cầu mới xem Program.cs vì mờ: không có ép125%; đã khai báo PerMonitorV2 trongcsproj, Programghi chú chuẩn96DPI/100%. Debug/Releasebuildsạch; generated startupgọi đúngmode. AppR10đãmởlại, chờđăngnhập đểkiểmđộnét; chưa kếtluậnhếtmờ. KhôngđổiScaleWindows hoặcDpiUnawaređểép100%. FilelistbổsungProgram.cs/csproj, tổngbảysửa/támthêm. CácmụcR9dướiđây làlịchsử đãkiểm, không thaythếacceptanceR10.

**Cập nhật R9:** người dùng báo avatar cũ còn lơ lửng sau resize ở100%. Đã sửa repaint sidebar (ResizeRedraw + Invalidate sau layout), buildDebug/Release sạch và probe vùng avatar cũ được invalidatePASS ởDeviceDpi96. Chưa có ảnh native maximize/restore R9; app đã mở lại, đang chờ đăng nhập. Chi tiết B09 ở [bug-report.md](bug-report.md). Không dùng kết quả R8 để kết luận B09PASS.

**Xác nhận native R9:** đã đăng nhập và kiểm chuỗi restore→maximize→restore ở100%. Avatar thừa đã hết trên cả ảnh phóng to và thu nhỏ; B09 PASS. Ảnh phóng to: `runs/native-avatar-r9-maximized-100-settled-0.png`; ảnh cuối client1366×768: `runs/native-avatar-r9-restored-after-100-0.png` (toàn cửa sổ1368×800). Source Drift0. App để lại màn inventory, chưa triển khai tám màn tiếp theo. Các mục chưa kiểm của pilot vẫn giữ, không chốt toàn bộ task.

Đã triển khai control WinForms dùng chung và áp dụng cho Nguyên liệu / Tồn, sidebar. Chưa triển khai tám màn tiếp theo. Các handler nghiệp vụ, quyền và dữ liệu giữ nguyên; chỉ thêm cách tải snapshot/lọc/phân trang hiển thị theo quyết định đã duyệt.

Chi tiết file và quyết định: [audit.md](audit.md). Kết quả có phân biệt native GUI, probe trong process và BLL: [test-results.md](test-results.md).

Build R4 Debug/Release sạch; rules 20/20 PASS; probe paging 9/9, search 3/3, CRUD fixture 5/5, hover leave R4 PASS. Không đồng nghĩa toàn bộ GUI/DPI đã PASS.

Đã đăng nhập thủ công và chụp R4 ở desktop 100%: `runs/native-inventory-r4-100-final-0.png` (toàn cửa sổ gồm title bar; client mặc định 1366×768, sidebar 200; probe DeviceDpi 96). Search NVL02, chọn lọc Lit và chuyển hai trang đã kiểm trực tiếp trên R4. Build/source R4 cố định, audit 163 file không drift.

Đã kiểm R4 native150% và xác nhận DPI144; bảng cuộn riêng, header/tools/pager cố định. Phát hiện profile sidebar tràn và border dọc mảnh; đã kết thúc lượt R4 rồi tự sửa R5–R8 trong đúng scope. R8 profile/border/hover probe đềuPASS, Release build sạch. App R8 đã mở lại, đang chờ người dùng đăng nhập ở150% để chụp native sau sửa; ảnh R4 không đại diện bản cuối.

**Cập nhật sau đăng nhập:** đã kiểm nativeR8 tại150%, ảnh cuối `runs/native-inventory-r8-150-final-0.png`. Profile sidebar hiện trọn, đường dọc không còn; mouse chọnNVL02, Down sangNVL03, Ctrl+End chọnNVL07/cuộn trong bảng, Ctrl+Home trở vềNVL01; header/tools/pager giữ vị trí. Công cụ trả ảnh1274×720, giữ nguyên khôngupscale; probe độc lập xác nhậnDeviceDpi144, không khẳng định đây là ảnh client1366×768 ở150%. Metadata ở `runs/native-r8-150-capture-metadata.json`.

Native resize bằng công cụ chưa thành công, không tính PASS; probe resize có kết quả riêng. Motion/chuỗi keyboard đầy đủ và GUI CRUD hợp lệ chưa đủ bằng chứng. ComboBox vẫn có phần vẽ native; không tuyên bố pixel-identical. Source đóng băng cuối `runs/source-r8-final.csv`; file production vẫn năm sửa/tám thêm như audit.

Ảnh100%/125% đã có ở bản trước sửa, chưa chụp lạiR8 tại hai mức đó. Các giới hạn và lịch sửFAIL giữ trong test-results.md. Source R8 Drift0 sau kiểm tra. Máy hiện150%; chưa tự trả125% doSettings automationtimeout. Dừng để người dùng review màn mẫu; chưa nghiệm thu toàn bộ task hoặc triển khai tám màn còn lại.

Chưa triển khai tám màn tiếp theo. Giữ app/DB test để người dùng review màn mẫu. Bản 100% là ảnh app thật, không render HTML thay thế. Ảnh có halo con trỏ của công cụ, không phải bụi sao của app.
