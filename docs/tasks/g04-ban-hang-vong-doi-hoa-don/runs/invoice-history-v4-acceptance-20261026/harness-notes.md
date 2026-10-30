# Ghi chú harness và phạm vi bằng chứng

Source sản phẩm cố định142 file, DLL WinForms trùng bản triển khai. Chỉ harness
ngoài src được điều chỉnh để có input/precondition/synchronization đúng; giữ
mọi log, expected hành vi ứng dụng không thay đổi.

1. attempt-1:25 PASS,1 lỗi runner InvalidCastException vì SDT KH011 là DBNull.
   Đây là lỗi ép kiểu trong harness, chưa thực hiện ca search SDT. Đã tự dọn
  101 đơn/3actor, snapshot trước/sau khớp. Bổ sung khách fixture IV426 có SDT
  0900012626, thay vì sửa dữ liệu khách cũ. Lỗi compile do SDK tự include bản
  Program.cs lưu trong attempt-1 cũng được giữ; csproj chuyển explicit compile.
2. attempt-2:45 PASS,3 assertion native-owner FAIL và1 runner ArgumentException.
   Assertion sai vì Form.Enabled managed vẫn true khi Windows ShowDialog khóa
   HWND owner; bản tiếp dùng user32 IsWindowEnabled để kiểm khóa Windows đúng
   contract, không bỏ điều kiện owner/modal/taskbar. Mã missing IH26MISSING dài11
   bị service từ chối đúng; dùng IH26MISS00 dài10 để đi đến nhánh notfound.
   Không ghi nhận đây là bug source sản phẩm. Evidence cũ giữ nguyên.
3. results.json:53/53 PASS sau sửa harness; source sản phẩm không đổi.
4. dialog-results-initial.json:87/87 native/dialog regression. Bổ sung7list dùng
   chung thì dialog-results-shared-first.json có93PASS/1FAIL tại ProductsControl:
   state Đang tải, rows0. Products.Load tải CategoriesAsync rồi SelectedValueChanged
   khởi động query mới, thay thế ResetAndReload đang được harness await. Assertion
   chạy trước generation mới hoàn tất. Bản sau đợi busy=false (deadline10s) trước
   cùng assertion Đã cập nhật, profile cũ, không stacked/searchactions. Không sửa
   ProductsControl hoặc workspace để né lỗi; lưu Regression-first.cs.txt.

Các thao tác WinForms do harness gọi PerformClick/focus/OnKeyDown và shared
manager trên UI thread thật. Đây là component functional tests, không phải
physical-input/visual acceptance. Query seam chỉ thay delegate trong instance
test cho race/error/cancel/fallback, không thay procedure hoặc permission DB.
Fixture SQL là101 header đơn mở không thanh toán, ngày2001-01-01 đến2001-04-11,
mã IH26000001–IH26000101, actor IHC01 và khách IV426; chỉ phục vụ query/pager,
không chứng minh invariant checkout/đơn có dòng hoặc paid boundary từ fixture này.
Runtime login có kiểm marker, AuthService thật với IHA01/IHC01/IHS01; cleanup
allowlist tự chạy trong finally. Không tạo DB mới, không in, không chốt476–478.
