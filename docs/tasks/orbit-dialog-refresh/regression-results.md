# Regression sau sửa B01/B02

22/10/2026 — người dùng duyệt sửa cả hai lỗi và kiểm lại. **B01/B02 PASS; 44 ca chức năng có ID riêng đều PASS ở kết quả cuối.** Không kiểm hình thức theo yêu cầu. Không thay checklist hoặc nghiệm thu toàn G03/T01–T32.

## Sửa đúng phạm vi

- `src/CafeManager.WinForms/UI/OrbitDialogField.cs`: chỉ cập nhật MaximumSize khi caption đã khởi tạo; vẫn gọi base.OnLayout. Không thay control input, event hoặc binding.
- `src/CafeManager.WinForms/Forms/OrbitConfirmationDialog.cs`: chuẩn hóa None/Cancel về kết quả từ chối của tập nút; giữ Yes/OK. X của YesNo trả No, OKCancel trả Cancel.
- So với `runs/source-implementation.csv`, chỉ hai source trên thay đổi. Đóng băng tại `runs/source-b01-b02-fixed.csv`; `runs/regression-integrity.json` kiểm107file, **0 thay đổi trong regression**. Các chỉnh sửa harness dưới runs không phải source ứng dụng.
- Build Debug/Release: 0 warning, 0 error. Output riêng `runs/build-fixed/Debug` và `runs/build-fixed/Release`; log Release: `runs/build-fixed-release.log`. Debug được xác nhận qua output lệnh build của phiên này. Không cập nhật exe đang mở trước đó hoặc yêu cầu đăng nhập lại.

## Môi trường và cách chạy

Windows, .NET10.0.401/runtime10.0.13; harness Release STA + message loop WinForms. Dùng form/control và handler thật qua PerformClick/reflection trong process fixture do probe sở hữu, không tự đăng nhập hoặc điều khiển cửa sổ app của người dùng. Identity ADMIN/STORE lấy từ tài khoản DB test và BLL kiểm quyền thực; không dùng việc tạo SessionContext để kết luận xác thực PASS.

SQL Server localhost, DB riêng `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`, kiểm marker `20261019_215457_29e552a9` trước mỗi lượt. Không seed/migration, không ghi DB sử dụng thật. Không đổi WindowsScale. 42/42 rules PASS của đợt trước được kế thừa vì Business/Models không đổi; không báo đã chạy lại rules trong regression này.

Lệnh dùng `dotnet run --project docs/tasks/orbit-dialog-refresh/runs/FunctionalProbe/FunctionalProbe.csproj -c Release --no-restore -- <output.json> <mode>`:

| Mode / bằng chứng | Kết quả |
|---|---|
| `--regression`, `runs/regression-b01-b02-r2.json` | 47 record PASS, 1 record FAIL harness policy; gồm6record marker/fixture, không tính thành ca chức năng |
| `--policy-only`, `runs/regression-policy-snapshot.json` | Policy PASS sau đối chiếu nội dung và bytes revision; có đầy đủ snapshot trước/sau |
| `--remaining`, `runs/regression-opening-points-store-r2.json` | Customer edit/open/adjust Cancel và STORE recipe readonly PASS; fixture mới KH015 dọn thành công |
| Tổng hợp `runs/regression-summary.json` | 44 ID ca chức năng PASS, loại record marker/fixture/snapshot; ca kiểm lại dùng kết quả cuối, vẫn giữ JSON gốc |

## Các chức năng đã kiểm

| Nhóm | Thao tác và expected đã kiểm | Kết quả |
|---|---|---|
| B01 text/wrapper | Mở AddText, giữ nháp tiếng Việt khi validation lỗi, ErrorProvider/focus tới input | PASS |
| B01 dirty | Đổi nháp, Hủy đóng giữ input; xác nhận bỏ nháp đóng được | PASS |
| B02 xác nhận | YesNo/OKCancel × đồng ý/từ chối/Escape/X/Enter | 10/10 PASS; Enter không đi nhánh đồng ý |
| Busy/unknown | Nhấn Lưu lặp, Close khi đang ghi; timeout; receipt; retry | PASS: chỉ1write khi busy, khóa đúng, requestId/payload giữ nguyên khi retry |
| Catalog | 7 loại editor giữ typed binding/readonly/ẩn trường; validation ratio; PT010 khóa support | PASS |
| Picker | SDT0911111111→KH001, không action quản lý; vãng lai đúng context; Đóng sau tải | PASS |
| Hóa đơn con | KH001/KH999 đối chiếu service;3cột, readonly, batch≤50 | PASS trong dữ liệu hiện có; chưa kiểm nhiều trang>50 |
| Công thức ADMIN | Mở từ handler ProductsControl.Recipe, thêm nháp/chọn nguyên liệu, cập nhật đơn vị ngay; xóa dòng; Hủy | PASS; công thức DB trước/sau không đổi |
| Công thức STORE | Identity tài khoản kho có role STORE; BLL đọc thật; mở recipe từ handler | PASS: grid readonly, cấm thêm/xóa, không Lưu hoặc nút xóa dòng |
| Policy | Lưu khi chưa preview bị chặn; preview SQL; Hủy xác nhận; sửa nháp làm preview cũ hết hiệu lực | PASS;10hạng, ngưỡng, giảm và revision bytes trước/sau giống nhau |
| Preview ưu đãi | Tiền100000, ship10000, không dùngKM; preview SQL; đổi tiền200000 | PASS; kết quả cũ chuyển “Cần tải preview mới.” |
| Ngày | Typed22/10/2026;31/02/2026 bị từ chối và giữ chuỗi sai | PASS |
| Material handler thật | NVL99, tên theo run, unitKg; Save, đọc lại, mở sổ1,2345 rồi Hủy | PASS; tồn0, chưa mở sổ, revision không đổi sau Hủy; DeleteAsync dọn đúng fixture |
| Product handler thật | SP999, giá35000, status mặc định Ngung ban; Save/đọc/Delete | PASS |
| Area/Table handler thật | KV999+B9999, chọn đúng khu vực; Save/đọc; xóa bàn rồi khu vực | PASS; trạng thái Trong, không còn fixture sau dọn |
| Promotion handler thật | KM10222054,22/10/2026–23/10/2026,17%; Save/đọc/Delete | PASS |
| Customer editor/service | Thêm nháp fixture và Save/đọc/Delete; thêm bằng handler CustomersControl, sửa tên; mở sổ300 rồi Hủy | PASS ở lượt cô lập modal; điểm0, revision giữ nguyên khi Hủy |
| Point adjustment handler thật | Fixture KH015 điểm0; nhập10điểm/lý do, chưa preview bị chặn; preview SQL; Hủy; đổi11điểm | PASS; preview cũ không dùng được, điểm/revision/lịch sử không đổi; fixture dọn qua service |

## Lỗi harness được điều tra, không che log

1. Lượt `regression-b01-b02.json` dừng sau5ca: timer trả lời được tạo khi đóng form không dirty còn sống và trả lời vào ca kế tiếp. Hai assertion FAIL không đủ kết luận bug app; process FunctionalProbe của lượt đó được dừng riêng. Sửa vòng đời timer trong harness theo từng case, không sửa app/expected; lượt r2 các ca này đều PASS.
2. Policy `SequenceEqual(TierRow)` so byte[] theo tham chiếu, nên hai lần đọc SQL khác instance tạo FAIL dù nội dung có thể giống. Sửa phép đo thành Code/MinimumPoints/Discount và nội dung revision hex. Lượt policy riêng ghi snapshot trước/sau đầy đủ:10hạng và revision `000000000000088C` không đổi, PASS. Không thay expected “Hủy không ghi”.
3. Lượt `regression-opening-points-store.json` phát hiện timer còn sót giữa hai modal trong cùng case. Đối chiếu SQL thật ở `runs/customer-cancel-audit.json`: fixture **KH014, tênDlgRP1022205634 edit đã được mở sổ300điểm**, có receipt CustomerOpen. Nguyên nhân harness: sau modal lưu thành công, vòng timer chưa kết thúc thấy form vẫn Visible trong lúc message loop đang thoát, gọi CloseDirty và để lại câu trả lời “Đồng ý”; timer đó chọn hộp kế tiếp. Không coi FAIL này là PASS và không xóa dấu vết dữ liệu. Đã đợi callback kết thúc, bỏ CloseDirty khi DialogResult.OK, hủy timer ở ranh giới từng modal. Chạy lại trên fixture mới KH015: Hủy mở sổ/điều chỉnh điểm đúng, điểm và lịch sử không đổi, PASS. Source app không thay đổi giữa hai lượt.

**Dữ liệu giữ lại:** KH014 trong DB test có300điểm mở sổ do harness trên; giữ nguyên CustomerOpening/receipt, không reset điểm hoặc xóa lịch sử. Các fixture không lịch sử (DM012/DM013, khách fixture tạo ở lượt đầu, NVL99/SP999/B9999/KV999/KM10222054/KH015) đã được dọn qua service theo từng assertion. Không tác động dữ liệu người dùng hoặc dữ liệu demo có lịch sử.

## Giới hạn còn lại

- Hình thức/ảnh/hover/resize/DPI/Designer **SKIPPED theo yêu cầu người dùng**.
- Chưa thao tác toàn bộ trong app đăng nhập đầy đủ bằng chuột/phím vật lý; ProcessDialogKey và binding input/grid trong process fixture không chứng minh toàn bộ keyboard/IME/native automation.
- Chưa tạo>50record để kiểm nhiều trang trong bảng con; chưa kiểm quyền CASH/ma trận toàn hệ thống, concurrency hoặc giao dịch commit kho/điểm/chính sách. Các ca Cancel đã kiểm qua handler và đối chiếu DB; không gán PASS cho commit tiền/kho/điểm.
- Không có bug app mới đã xác nhận; B01/B02 đã đóng trong phạm vi regression chức năng. Không thay source trong lượt test hoặc commit/push.
