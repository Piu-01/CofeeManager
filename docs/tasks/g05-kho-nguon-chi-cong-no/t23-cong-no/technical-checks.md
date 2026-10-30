# T23 — kiểm kỹ thuật trong triển khai

26/10/2026. Migration001–021 compile/apply SQL Server thật clean/demo tại `../runs/technical_20261026_230814_ae339d91/manifest.json`, tồn trước/sau không đổi; tạo2/dọn2/còn0DB.

Smoke typed sau đó tại `../runs/technical_20261026_231552_1d5d908b/typed-smoke.json`: ghiFinance1234, nhập10000/trả4000/nợ6000, xuất0.1, tạo/lưu/chốtKK, trả nợ1000 còn5000, queryAsOf/List. Chạy connection owner trên DBtest, không chứng minh quyềnCafeManagerApp hoặc GUI/55ID. Tạo2/dọn2/còn0DB. Lượt231506 lỗi runner chưa buildprobe được giữ nguyên; chưa chạy product probe ở lượt đó, runner đã sửa.

Debug/Release sản phẩm và compile project test cuối đều0warning/0error. Không chạyxUnit hoặc đợt nghiệmthu. Sau smoke typed chỉ hoàn thiện UI/pending/quyềnexception, build lại; không gán smokeSQL thành kiểm chức năng UI mới.

Nângdevelopment bị auto-review từchối trước thực thi: targetlocalhost/CoffeeManager_Development cần xác nhận riêng. Script Update-DevelopmentG05.ps1 đã soạn/kiểm cú pháp, chưa backup hoặc nângDB. Không commit/push.
