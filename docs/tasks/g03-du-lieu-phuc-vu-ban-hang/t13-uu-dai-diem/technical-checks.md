# Kiểm tra kỹ thuật T13 — 18/10/2026

008/009 compile trên clean/demo. Smoke preview, tạoKM, replace policy cùng dữ liệu,
preview/ghiđiểm và sửa mãKM seed cũ thành công. Evidence cuối:
`../runs/technical_20261018_195707_26208544/manifest.json`, tạo2/dọn2/giữ0DB.
Các lượt thất bại trước giữ nguyên: khai báo scalar chung table gây lỗi cú pháp;
fixture đưa CHAR5 padded vào TVP cầnRTRIM. Đã xử lý trong pha triển khai, chưa test.

009 giữ checksum008 đã áp dụng, cho sửa/xóa mã seed như KM24BF, không remap;
tạo mới vẫn KM+8số. Runtime procedure, BLL và editor thống nhất quy tắc này.
Đọc development: ADMIN thấy11KM cũ; CASH không có KM hiện tại nên trảrỗng thành công;
STORE không có menu. `../runs/development-ui-technical-r3/result.json` chỉ đọc màn,
không chứng nhận preview dialog/policy/adjust/role mutate.

Build cuối Debug/Release0warning/error tại `../runs/build_20261018_200326/`.
T13-K01–K11/UX-K10/33, rollback/concurrency/timeout/GUI/DPI/Designer: NOT RUN.
