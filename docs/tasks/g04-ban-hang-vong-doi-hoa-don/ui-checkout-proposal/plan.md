# Bổ sung G04/T16 — Orbit Xác nhận thanh toán

Ngày25/10/2026. Người dùng yêu cầu chỉnh CheckoutDialog theo ZIP đính kèm,
ưu tiên theme/control hiện có và thành phần dùng chung. Đã khảo sát source và mẫu;
đây là bổ sung bộ plan G04, người dùng đã duyệt25/10/2026. Không mở nhóm mới.
Đã triển khai;đang hoàn tấtbuild/hồ sơ và chờ duyệt đợt test giao diện mới riêng.

## Nguồn và phạm vi

- ZIP người dùng: `C:/Users/Lenovo/Downloads/Orbit_XacNhanThanhToan.zip` gồm HTML,
  PNG1366×768 và README. [PNG tham chiếu](reference-laptop.png) dùng để thiết kế,
  không phải chứng cứ nghiệm thu WinForms. HTML chứa cả nềnPOS chỉ để minh họa;
  phạm vi triển khai là hộp Xác nhận thanh toán, không sửa lại POS/sidebar.
- Giữ contract, SQL/DAL/BLL/role, checkout/receipt/preview/reconcile và in hiện có.
  Phương thức lấy từ service thật; không cố địnhTiền mặt hoặc hardcode81.000.
- Mẫu có “Kiểm tra lại” để tính lại ở JavaScript; bản thật vẫn dùng nút này cho
  đối chiếu request unknown theo contract. Không biến thành thao tác gửi lại.
- Không nhúngHTML/WebView/JS mẫu hoặc toàn bộ thư viện icon vào app. Không thêm
  blur desktop/overlay toàn màn hình, dashboard, tài nguyên từInternet hoặc theme
  độc lập trùng với Orbit hiện có.

## Bố cục sau sửa

1. Header: tiêu đềXác nhận thanh toán, mã hóa đơn, kênh bán. Font/màu từ themechung.
2. Body hai cột khi đủ rộng: trái là bảng phân bổ phương thức/số thu/tham chiếu,
   toolbarThêm dòng/Xóa dòng; thẻTiền mặt khách đưa; thông báo validation ngay gần.
   Phải là tổng kếtTiền món/Ưu đãi/Ship,Phải thanh toán nổi bật,Đã phân bổ,
   Khách đưa,Thiếu hoặcThối tiền mặt. Giá trị từpreview/allocations typed.
3. Footer cố định: Quay lại bên trái,Kiểm tra lại vàXác nhận bên phải. Giữ nút đóng
   native và kiểmOnFormClosing; không tắt chặn đóng khi đangghi/unknown.
4. Khi hẹp chuyển summary thành vùng trên/dưới theo không gian, body cuộn,
   footer tiếp cận được. Không épDPI hoặc thu font để nhét nội dung.

## Tái sử dụng và file dự kiến

| Thành phần | Cách dùng / phạm vi thay đổi |
|---|---|
| Theme/OrbitManagementTheme.cs,AppTheme.cs | Dùng palette/font/kích thước hiện có; chỉ thêm token cần thiết cho trạng thái/thẻ chung, không đổi mặc định mọi màn |
| UI/OrbitDialogLayout.cs | Tái sử dụng khungdialog; mở rộng tùy chọn header/phân nhómfooter khi cần, giữ hành vi mặc định cho dialog đang dùng |
| UI/OrbitField.cs,OrbitDialogField.cs | Bao inputtiền/nhãn bằng field hiện có; money vẫnstringnhập, parsing/validation theo rulehiện tại |
| UI/OrbitButton.cs,StarDustOverlay.cs | Nút chung và bụi sao hiện có chỉ ởXác nhận; không nhân đôi bộ animation |
| UI/OrbitDataGridView.cs | Thêm profilePayment cho bảng editable:combobox,editor tiền/tham chiếu,selection/focus/tab và lỗi; các profilecũ giữ metrics/hành vi |
| UI/OrbitSummaryPanel.cs (nếu cần) | Controlnhỏ hiển thị các cặpnhãn/giá trị và tổngnhấn; không tính tiền hoặc phụ thuộcSalesService |
| UI/OrbitNotice.cs (nếu cần) | Bannerthông báo có trạng thái/chữ/icon, dùng chung; không sở hữu validation/nghiệp vụ |
| Modules/Sales/CheckoutDialog.cs | Ghép controls chung;bind dữ liệu và trạng thái; giữ handlerscheckout/reconcile/cancel hiện có |

Controlmới chỉ tạo nếu helper hiện có không đáp ứng; không dựng một frameworkform
hoặc trừu tượnghóa tất cảmodule. Designer không chứa logic nghiệp vụ.

## Hành vi nhập và trạng thái

- Tách rõSố thu (phân bổ) vàTiền mặt khách đưa. Ví dụ81.000phải trả,100.000khách đưa:
  phân bổ81.000,thối19.000; không gửi100.000làm số thu.
- Phản hồi ngay khi sai phương thức,tiền không nguyên/âm/quá lớn,phân bổthiếu/dư,
  hoặc tiền mặt khách đưa thiếu. KhóaXác nhận khi dữ liệu chưa hợp lệ theo mẫu;
  handlerConfirm vẫn revalidate trước bất kỳ ghiSQL, không tin trạng thái nút.
- Không có phần tiền mặt thì inputkhách đưa không tham gia tính đổi/validation;
  mixedmethods tính thối trên phần tiền mặt theoCashChange hiện tại.
- Busy/unknown khóa nhập/thêm/xóa/gửi lặp;unknown cóKiểm tra lại theo mã/request cũ.
  Lỗi hoặcpreview đổi giữ input và yêu cầu xem/xác nhận lại. Không tựretrycheckout.
- Enter không tự chốt;Esc/Quay lại hỏi bỏ phân bổ như hiện có;zero-payable giữ luồng
  Chốt hóa đơn0₫. Nútchốt vẫn có bước xác nhận theo contract đã duyệt.
- Chốtthànhcông/thu hồi phiên/dispose dọn animation/handler như hiện tại.

## Trình tự triển khai và kiểm chứng

1. Lưu mẫu/nguồn/hashes và thêm chỉ dẫn trongdocs/ui để tránh lẫn mẫu chung/mẫu
   riêngcheckout. Không coi READMEZIP hoặc báo cáoJS là kết quả test app.
2. Bổ sung sharedpresentation tối thiểu, giữ default cáccontrols hiện tại.
3. GhépCheckoutDialog và wiring summaries/errors/valid-state. Không sửaSQL/BLL.
4. BuildDebug/Release vào output riêng, giữ cửa sổ đang mở. Kết thúc source thay
   đổi rồi đóng băng đợt kiểm; không vừatestvừasửa để néFAIL.
5. Đề nghị duyệt đợt test UI cập nhật một lần theo workflowG04; sauduyệt kiểm
   chức năng/layout kỹ thuật125%,hình thức do người dùng tự kiểm.100/150%NOT RUN.
6. Regression trọng tâmT16-K03/K04/K05/K06/K07, UXkeyboard/modal:81k/100k,
   thiếu80k,abc,overflow,thiếu method,thêm/xóa/sửa nhiều dòng,mixedmethods,
   0₫,preview đổi,busy/unknown/reconcile,cancel/dispose; đối chiếuSQLtest trước/sau.
   Kiểm smoke cácdialog/profilegrid hiện dùng khi sharedcontrol bị thay đổi.

## Bảo toàn lượt test đang dở

HD20250411 đã tạo/thêm/sửa/xóa đạt ở lượtmanual cũ. User chưa xác nhận xong các
ca validation/đóngdialogthanh toán;T16-K03/K04 chưaPASS. Không tự đóng app/chốt đơn
hoặc sửa dữ liệu để đổiUI. Kết quảG04 giữ33PASS/0FAIL/23BLOCKED đến có evidence mới.
Sau triển khai bản mới, các phần bị tác động phải kiểm lại trên đúngbinary mới.

## Tiêu chí hoàn tất

- CheckoutWinForms dùng bố cụcPNG,thống nhất Orbit và dữ liệu thật.
- Sharedcontrols nhỏ chỉ hiển thị; không chép màu/fontvào từnghandler/dialog.
- Các trạng thái validation/busy/error/unknown vàkeyboard không mất; không thay
  số thu/tiền đưa/SQLtransaction/role/request contract.
- Build và kiểm chức năng theođợt được duyệt; lưutest-results gồmexpected/actual,
  dữ liệu/lệnh/binaryhash,giới hạn vàbug nếu có. Không suyPASS hình thức thay user.
