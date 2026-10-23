# Kiểm tra kỹ thuật T11 — 18/10/2026

SQL clean/demo/createarea→createtable→selectcurrent compile/smoke thành công:
`../runs/technical_20261018_193303_14bb0710/manifest.json` (tạo2/dọn2/giữ0DB).
Build cập nhật callsite StartupForm theo service session mới; T05 fixture kiểmDAL
trực tiếp vì chỉ có migration001, không tạo session giả để né auth.

Đọc development ADMIN/CASH10bàn với view, ảnh đúng màn tại
`../runs/development-ui-technical-r3/`; STORE không tạo menu bàn trong collector.
Đây chưa là kiểm quyền mutate/directSQL, chọn bàn stale hay GUI CRUD.
Build cuối Debug/Release0warning/error tại `../runs/build_20261018_200326/`.
T11-K01–K08/UX-K06 tích hợpPOS/GUI/DPI/Designer: NOT RUN.
