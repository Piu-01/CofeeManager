# CHƯƠNG 1 TỔNG QUAN VỀ CƠ SỞ KHẢO SÁT

## 1.1 Giới thiệu về cơ sở nơi khảo sát

Cơ sở được lựa chọn cho đề tài là **Ther Thẩn Cafe**, tại **49 Đường 449, phường Tăng Nhơn Phú, Thành phố Hồ Chí Minh**, theo thông tin do người thực hiện cung cấp. Bài đăng ngày 01/07/2026 được trang FindGlocal lưu lại dưới tên Ther Thẩn Cafe cũng ghi địa chỉ này cho địa điểm Thủ Đức. Nguồn tra cứu là trang tổng hợp, dùng để đối chiếu tên và địa chỉ, không thay cho biên bản khảo sát trực tiếp. [13]

Đề tài giới hạn tại địa điểm trên và xây dựng hệ thống CoffeeManager cho một quán với một kho nguyên vật liệu chung. Thông tin công khai của thương hiệu có nhắc các địa điểm khác, nhưng phần mềm trong báo cáo không quản lý chuỗi hoặc đồng bộ nhiều chi nhánh. Không có căn cứ để khẳng định quán đã sử dụng CoffeeManager; đây là hệ thống được xây dựng trong phạm vi bài tập lớn. [1], [13]

Thông tin cần bổ sung gồm người cung cấp dữ liệu khảo sát, thời điểm khảo sát, quy mô phục vụ và cách ghi nhận giao dịch hiện hành. Các nhận định như quán quản lý bằng sổ, thường thất thoát nguyên liệu hoặc chậm tính tiền chỉ được đưa vào sau khi có ghi nhận thực tế. Dữ liệu mẫu trong dự án phục vụ phát triển và thử nghiệm; tên khách, nhân viên, bàn và chứng từ mẫu không dùng để suy ra quy mô hoạt động của quán.

Về đối tượng nghiên cứu, hệ thống theo dõi mối liên hệ giữa đơn bán, các lần phân bổ thanh toán, nguyên liệu tiêu hao, điểm khách hàng và chứng từ kho. Một giao dịch bán hàng vì vậy không dừng ở việc lưu món: hệ thống còn phải giữ đúng giá đã bán và bảo đảm các số liệu liên quan được cập nhật nhất quán. Đây là lý do báo cáo tập trung vào mô hình chức năng, luồng dữ liệu và các ràng buộc khi thiết kế CSDL.

## 1.2 Sơ đồ tổ chức của cơ sở

Chưa có thông tin xác nhận về quan hệ quản lý giữa các bộ phận của quán. Không sử dụng danh mục chức vụ trong dữ liệu mẫu để tạo sơ đồ tổ chức. Sau khi có dữ liệu khảo sát, sơ đồ sẽ thể hiện người quản lý, bộ phận phục vụ/thu ngân, bộ phận pha chế và kho nếu cơ sở thực sự tổ chức như vậy; tên gọi và tuyến báo cáo phải theo thông tin được xác nhận.

:::missing SD01 | Sơ đồ tổ chức cơ sở khảo sát | Đặt sau đoạn giới thiệu cơ cấu; rộng khoảng 14,5 cm, cao dự kiến 7–10 cm. Cần tên bộ phận và quan hệ quản lý thực tế. Chưa vẽ vì chưa có dữ liệu xác nhận.

Trong phạm vi phần mềm, ba vai trò đã được xác định là ADMIN, CASH và STORE. Đây là phân quyền thao tác, không thay thế cơ cấu nhân sự của quán. Một chức vụ như quản lý kho không tự động quyết định tài khoản phải mang vai trò nào. Việc tách hai khái niệm giúp điều chỉnh quyền sử dụng mà không làm thay đổi hồ sơ chức vụ. [1], [2]

# CHƯƠNG 2 CƠ SỞ LÝ THUYẾT

## 2.1 Phân tích chức năng của hệ thống

Phân tích chức năng bắt đầu từ phạm vi công việc cần hỗ trợ rồi chia thành các nhóm nhỏ có ý nghĩa nghiệp vụ. Sơ đồ phân cấp chức năng BFD thể hiện quan hệ toàn thể–bộ phận: chức năng ở mức trên được làm rõ bởi các chức năng con. BFD dùng để xác định hệ thống làm những công việc nào; thứ tự thực hiện và đường đi dữ liệu được trình bày bằng các mô hình khác.

Với CoffeeManager, việc phân nhóm theo bán hàng, kho hoặc công nợ phù hợp hơn việc tạo một chức năng cho mỗi bảng dữ liệu. Chẳng hạn, lập phiếu nhập là một công việc nhưng cần ghi nhiều bảng; ngược lại, hồ sơ nguyên liệu được nhiều công việc cùng sử dụng. Cách phân rã trong báo cáo được xây dựng từ phạm vi của dự án và được đối chiếu với bảng tiến trình ở Chương 3. [1]

## 2.2 Biểu đồ luồng dữ liệu

DFD mô tả dữ liệu đi từ tác nhân, qua các tiến trình xử lý và các kho lưu trữ. Bốn thành phần được sử dụng gồm tác nhân ngoài, tiến trình, kho dữ liệu và luồng có hướng. Trong báo cáo, tiến trình dùng hình bo góc có mã; tác nhân dùng hình chữ nhật; kho dùng hai đường ngang; nhãn mũi tên ghi tên dữ liệu. Mã tiến trình là định danh, không phải số thứ tự chạy. [3]

Mức ngữ cảnh biểu diễn toàn bộ phần mềm bằng một tiến trình. Mức đỉnh phân rã tiến trình đó thành các nhóm xử lý. Mức dưới đỉnh làm rõ một nhóm; dữ liệu qua ranh giới của nhóm phải khớp với mức cha. Báo cáo gọi ba mức theo tên trên để tránh nhầm với cách đánh số mức khác nhau giữa tài liệu. [4]

Tác nhân trong mô hình này là người trực tiếp dùng phần mềm. Khách hàng, nhà cung cấp và đối tác giao hàng cung cấp thông tin thông qua nhân viên; chương trình chưa kết nối trực tiếp các bên này. Vì thế, không vẽ cổng thanh toán hoặc API đối tác như thành phần đã có. DFD cũng không mô tả thao tác nhấn nút, thời gian chờ hay điều kiện rẽ nhánh giao diện; các tình huống đó được giải thích trong luồng nghiệp vụ.

## 2.3 Mô hình thực thể liên kết và mô hình quan hệ

Mô hình ER xác định các đối tượng cần quản lý và liên kết giữa chúng. Thực thể có thuộc tính mô tả; thuộc tính định danh phân biệt các thể hiện. Bội số cho biết số thể hiện có thể tham gia liên kết. Khi chuyển sang mô hình quan hệ, mỗi thực thể thông thường thành một bảng; khóa ngoại thể hiện liên kết, còn liên kết nhiều–nhiều cần một quan hệ trung gian. [5]

Trong đề tài, sản phẩm sử dụng nhiều nguyên liệu và một nguyên liệu có thể tham gia nhiều sản phẩm. CONGTHUC biểu diễn liên kết này, đồng thời lưu định mức. Hóa đơn có các dòng món; mỗi dòng giữ số lượng, giá tại thời điểm ghi nhận và ghi chú. Không lấy cặp hóa đơn–sản phẩm làm định danh dòng vì cùng một sản phẩm có thể xuất hiện nhiều lần với ghi chú hoặc giá khác nhau. [6]

## 2.4 Phụ thuộc hàm và chuẩn hóa

Phụ thuộc hàm X → Y biểu thị việc một giá trị của X xác định một giá trị của Y trong mọi trạng thái hợp lệ của quan hệ. Khóa ứng viên được xem xét cùng các phụ thuộc này; quan sát một bộ dữ liệu mẫu chưa đủ chứng minh quy tắc luôn đúng. [7]

Chuẩn hóa giúp nhận diện dư thừa và bất thường khi thêm, sửa hoặc xóa dữ liệu. 1NF loại nhóm giá trị lặp trong một ô; 2NF yêu cầu thuộc tính không khóa phụ thuộc đầy đủ vào khóa ứng viên; 3NF kiểm soát phụ thuộc bắc cầu theo điều kiện của dạng chuẩn. [8]

Ứng dụng thực tế còn lưu dữ liệu lịch sử có chủ đích. Giá bán trên dòng hóa đơn là giá của lần bán, không phải bản sao luôn phải bằng giá menu hiện tại. Phân tích phụ thuộc phải xét đúng ý nghĩa thời gian này. Chương 4 trình bày các quan hệ đại diện và chỉ rõ giới hạn kết luận thay vì mặc định toàn bộ CSDL đã đạt 3NF.

## 2.5 Công nghệ và tổ chức chương trình

Windows Forms hỗ trợ xây dựng giao diện desktop Windows bằng Form, control, cơ chế sự kiện và liên kết dữ liệu. Trong CoffeeManager, cửa sổ chính chứa các UserControl theo công việc; hộp nhập hoặc xác nhận dùng Form modal có cửa sổ chủ. Đây là cách tổ chức giao diện phù hợp với ứng dụng dùng chuột và bàn phím. [9], [2]

Source hiện tại dùng C# với WinForms target net10.0-windows; các project Business, DataAccess và Models tách trách nhiệm. DataAccess sử dụng Microsoft.Data.SqlClient để gọi SQL Server bằng tham số typed. Mã kết nối, tài khoản thử và cấu hình máy phát triển không phải nội dung cần công bố trong báo cáo. Phiên bản công nghệ này được xác định từ project và hồ sơ môi trường, không suy từ phiên bản mới nhất được nêu trên website. [1], [6]

## 2.6 Giao dịch và tính nhất quán dữ liệu

Một transaction là đơn vị công việc được commit toàn bộ hoặc rollback khi không thể hoàn thành. Cơ chế này cần cho nghiệp vụ tác động nhiều bảng. [10] CoffeeManager đặt giao dịch chính thức trong procedure: thanh toán vừa ghi tiền, tiêu hao, điểm và trạng thái đơn; nếu một phần thất bại thì không được giữ lại các phần còn lại. Giao diện không giữ transaction trong lúc người dùng đếm kho hoặc chọn máy in.

TVP cho phép truyền nhiều dòng dữ liệu có cấu trúc vào SQL Server trong một lời gọi. [11] Chương trình sử dụng cách này cho các dòng thanh toán và chứng từ kho. TVP là cách chuyển dữ liệu; quyền thao tác, số lượng hợp lệ và điều kiện số dư vẫn cần được kiểm tra trong nghiệp vụ.

# CHƯƠNG 3 PHÂN TÍCH HỆ THỐNG

## 3.1 Mô tả bài toán

### 3.1.1 Phạm vi và người sử dụng

CoffeeManager được xây dựng để quản lý một quán cà phê với một kho chung. Đơn có thể phục vụ tại bàn, mang đi hoặc giao hàng. Cùng một nguồn dữ liệu lưu thông tin món, công thức, khách hàng, ưu đãi, giao dịch kho và công nợ nhà cung cấp. Hệ thống đích còn có nhân sự, chấm công, lương, tài sản và báo cáo; báo cáo này phân biệt các phần thiết kế đó với phạm vi chương trình hiện đã xây dựng. [1]

Thu ngân chọn bàn hoặc loại đơn, thêm món, chọn khách và xác nhận thu tiền. Người làm kho quản lý nguyên liệu, lập chứng từ nhập/xuất và kiểm kê. Quản lý có quyền điều chỉnh chính sách, thực hiện nghiệp vụ tài chính và quản trị dữ liệu được phép. Người dùng đăng nhập bằng tài khoản gắn với nhân viên; nhân viên lập chứng từ lấy từ phiên làm việc, không chọn tùy ý trên màn hình.

:::table roles

Hệ thống vận hành trực tiếp với SQL Server, chưa có website, API server hoặc đồng bộ offline. Việc ghi nhận phương thức chuyển khoản chỉ lưu thông tin phân bổ và mã tham chiếu do nhân viên cung cấp; không đồng nghĩa đã xác minh với ngân hàng. Không có đặt bàn, size/topping, gộp/tách hóa đơn, hoàn tiền sau thanh toán hoặc dùng điểm đổi tiền trong phạm vi này. [1], [2]

### 3.1.2 Quy trình bán hàng và ưu đãi

Đơn tại quán phải gắn bàn. Chọn một bàn trống mới xác định ngữ cảnh; hóa đơn được tạo cùng món đầu tiên theo contract tạo đơn, tránh giữ bàn bằng hóa đơn rỗng. Bàn có đơn đang mở được tải lại để tiếp tục phục vụ. Đơn mang đi không gắn bàn hoặc đối tác. Đơn giao hàng cần tên người nhận, số điện thoại, địa chỉ và trạng thái giao; đối tác có thể để trống khi quán tự giao. [2], [6]

Mỗi lần thêm món lấy giá hợp lệ và lưu trên dòng. Sửa số lượng của dòng không lấy lại giá menu mới. MaDong xác định dòng cần sửa hoặc xóa. Vì vậy, cùng một sản phẩm có ghi chú khác nhau vẫn có thể được quản lý riêng mà không làm mất yêu cầu pha chế. Khi rời POS, đơn đã lưu vẫn còn trong CSDL; rời màn hình không tự hủy đơn.

Khách vãng lai được biểu diễn bằng MaKH rỗng, không dùng một hồ sơ khách giả chung. Nếu chọn khuyến mãi hợp lệ, ưu đãi chương trình thay ưu đãi hạng; hệ thống không cộng hai tỷ lệ. Không chọn chương trình vẫn có thể được giảm theo hạng thành viên. Tổng giảm chỉ tính trên tiền món, không tính phí giao hàng. [1], [6]

### 3.1.3 Thanh toán và khóa chứng từ

Tổng tiền món được tính từ số lượng nhân giá snapshot của từng dòng. Phải trả bằng tiền món cộng phí ship rồi trừ giảm giá. Tổng các dòng phân bổ phải đúng số phải trả. Tiền khách đưa và tiền thối phục vụ thao tác tiền mặt; doanh thu ghi nhận theo số thu phân bổ, không theo số tiền khách đưa. Với hóa đơn phải trả bằng không, người dùng vẫn xác nhận chốt, nhưng không tạo dòng thanh toán giá trị không. [1], [6]

SQL tính lại preview ngay khi chốt. Nếu chính sách hoặc phiên bản đơn thay đổi, hệ thống yêu cầu tải lại và xác nhận số mới. Trong cùng giao dịch, procedure ghi phân bổ, snapshot ưu đãi, tiêu hao theo công thức, lịch sử điểm nếu có và trạng thái thanh toán. Trigger cập nhật tồn hoặc điểm được dùng đúng một lần; ứng dụng không cập nhật lần nữa. Các dòng tiêu hao đã lưu không thay đổi khi công thức món được sửa về sau.

Sau khi chốt, tài chính của hóa đơn bị khóa. In diễn ra sau commit; lỗi in chỉ dẫn tới in lại, không thanh toán lại. Nếu timeout sau khi gửi lệnh, giao diện chuyển sang trạng thái chưa biết kết quả và đối chiếu mã yêu cầu/hóa đơn. Không kết luận giao dịch thất bại chỉ vì màn hình ngừng chờ. [2], [6]

Hủy chỉ áp dụng cho đơn còn mở. Do chưa trừ kho theo bán hàng, hủy đơn không hoàn nguyên liệu. Nếu món đã được pha rồi bỏ, nhân viên có quyền lập phiếu xuất hủy lượng thực dùng; đó là nghiệp vụ kho riêng. Trạng thái giao hàng độc lập trạng thái thanh toán: chuyển tiến độ giao không mở lại quyền sửa tiền của đơn đã chốt.

### 3.1.4 Kho và công nợ nhà cung cấp

Mỗi nguyên liệu có một đơn vị tính; định mức và số lượng nhập/xuất phải cùng đơn vị. Một kho chung không phát sinh chuyển kho ra quầy. Tồn mở sổ lưu mốc và nguồn; sau mở sổ không sửa trực tiếp tồn bằng màn danh mục.

Nhập hàng được soạn nháp trước khi ghi. Khi xác nhận, procedure tạo phiếu và các dòng, cộng tồn qua trigger, tính tổng rồi tạo chi trả ngay và nợ còn lại nếu có. Số trả ngay nằm trong khoảng từ không đến tổng phiếu. Một phiếu nhập chỉ có tối đa một khoản nợ gốc; nhà cung cấp của nợ được truy ngược từ phiếu nhập. Không tạo một chi trả ngay rồi đồng thời tính lại toàn bộ tổng phiếu thành nợ. [1], [6]

Xuất kho chỉ nhận loại hủy đồ hỏng hoặc trả hàng. Không đủ một nguyên liệu thì giao dịch bị từ chối toàn bộ. Xuất trả hàng chưa tự tạo hoàn tiền hay cấn trừ nợ. Kiểm kê lấy tồn hệ thống tại một thời điểm, nhân viên nhập số đếm và lý do lệch. Số đếm chưa nhập khác số không. Khi chốt, nếu dữ liệu liên quan đã thay đổi thì phải lấy lại snapshot; không dùng một bản tồn cũ để điều chỉnh sau nhiều giao dịch phát sinh.

Trả nợ là quyền của ADMIN. Số trả phải dương và không vượt số dư được kiểm tra trong giao dịch. Procedure tạo đúng một phiếu chi gắn với một dòng lịch sử trả nợ. Ứng dụng đọc kết quả, không tạo thêm phiếu chi. Số dư hiện tại lấy từ view; số dư tại một ngày cần giới hạn các nguồn phát sinh theo ngày, không dùng số dư hiện tại để thay thế. [6]

### 3.1.5 Các chức năng thiết kế mở rộng và giới hạn hiện tại

Nhân sự, chấm công, chốt/chi lương, tài sản và báo cáo đã có trong kế hoạch. Tuy nhiên, không được xem sự tồn tại của bảng BANGLUONG hay TAISAN_CODINH là bằng chứng đã có giao diện và đầy đủ use case. Các phần này được nhận diện là thiết kế đích khi mô hình hóa, không dựng ảnh chương trình để minh họa phần chưa xây dựng. [1]

Tại mốc đối chiếu ngày 27/10/2026, G05 đã có source và migration 017–021 nhưng đợt nghiệm thu ghi 38 PASS, 5 FAIL, 10 BLOCKED và 2 NOT RUN trên 55 ID gốc. Các lỗi được ghi nhận gồm hiển thị workspace, ánh xạ dữ liệu kiểm kê/công nợ rỗng và kiểm tra nâng cấp dữ liệu. Báo cáo không biến kết quả triển khai hoặc build thành kết luận hệ thống đã hoàn tất. G04 còn giới hạn nghiệm thu theo phiên bản; G06–G08 chưa triển khai theo chỉ mục kế hoạch. [12]

## 3.2 Sơ đồ phân cấp chức năng BFD

Các chức năng được phân thành sáu nhóm để theo dõi từ tổng thể đến chi tiết. Nhóm 2 kết hợp dữ liệu nền với khách hàng vì cùng cung cấp thông tin cho giao dịch; nhóm 6 thể hiện các chức năng đích chưa xây dựng đầy đủ. Tên nhóm được dùng thống nhất trong bảng phân tích và DFD, không suy một nút BFD tương ứng một Form.

:::figure bfd | Sơ đồ phân cấp chức năng CoffeeManager

Nhóm bán hàng được ưu tiên làm rõ do liên quan đồng thời tiền, kho và điểm. Nhóm kho chuyển yêu cầu tạo chi trả ngay sang nhóm tài chính, tránh để hai nhóm cùng ghi một khoản chi. Phân rã thể hiện trách nhiệm nghiệp vụ; thứ tự thao tác của nhân viên được diễn giải ở mục 3.1.

## 3.3 Bảng phân tích xác định tiến trình tác nhân và hồ sơ

Ranh giới mô hình là phần mềm CoffeeManager. Tác nhân A1 là ADMIN, A2 là CASH và A3 là STORE. Khách, nhà cung cấp và đối tác là đối tượng trao đổi nghiệp vụ ngoài quán nhưng chưa trực tiếp dùng phần mềm trong mô hình này. Nhân viên nhập thông tin họ cung cấp. Ba tác nhân phần mềm có thể cùng tham gia một tiến trình với các quyền khác nhau.

:::table processes

:::table stores

Các kho D1–D8 là nhóm dữ liệu logic, có thể bao gồm nhiều bảng vật lý. Ví dụ D5 chứa nguyên liệu, mốc mở sổ và chứng từ kho; tồn liên quan bán hàng đọc/ghi qua các xử lý nghiệp vụ, không sao chép thành một số dư khác của riêng POS. Hồ sơ bảng phụ trợ như receipt hoặc schema version được giải thích ở Chương 4, không gán thành bộ phận của quán.

:::table flows

Bảng luồng quy định các gói dữ liệu qua ranh giới. Một gói có thể tách thành nhiều nội dung tại mức dưới nhưng không tự xuất hiện tác nhân mới. Các kết quả từ chối hoặc xung đột thuộc gói kết quả nghiệp vụ tương ứng. Chúng không được biểu diễn bằng các mũi tên điều khiển “đúng/sai” trên DFD.

## 3.4 Biểu đồ luồng dữ liệu

### 3.4.1 Mức ngữ cảnh

:::figure context | DFD mức ngữ cảnh của CoffeeManager

Hệ thống nhận thông tin đăng nhập và yêu cầu công việc từ ba vai trò; trả kết quả xác thực, dữ liệu tra cứu và kết quả giao dịch theo quyền. Mã F trong hình tham chiếu bảng gói dữ liệu. Không có luồng hệ thống tự thu tiền trực tuyến từ khách hoặc tự nhận đơn của đối tác, vì các tích hợp đó chưa thuộc phạm vi.

### 3.4.2 Mức đỉnh

:::figure top | DFD mức đỉnh của CoffeeManager

:::figure top-interfaces | Trao đổi nguồn chi và công nợ giữa các nhóm ở mức đỉnh

Hình lặp ký hiệu tác nhân và một số kho để giảm giao cắt; các bản lặp cùng mã biểu thị cùng một đối tượng. P1 cung cấp ngữ cảnh phiên được kiểm tra tại các nhóm nghiệp vụ. Dữ liệu xác thực dùng để xác nhận quyền, không thay thế thông tin đầu vào nghiệp vụ. Cơ chế gọi service hoặc khóa SQL là thiết kế triển khai, không bổ sung thành luồng dữ liệu kinh doanh.

Nhóm P6 giữ ký hiệu thiết kế đích. Kho D8 và các bảng liên quan đã có trong baseline nhưng việc nhập/chốt công, lương, tài sản và báo cáo đích chưa được coi là chương trình đã vận hành. Việc đặt P6 trong mô hình nhằm thể hiện đầy đủ phạm vi phân tích; phần 4.2 chỉ minh họa màn có thật.

### 3.4.3 Mức dưới đỉnh

:::figure dfd1 | Phân rã P1 xác thực và kiểm tra phiên

P1.1 tiếp nhận tên đăng nhập, đọc thông tin tài khoản; việc kiểm chứng mật khẩu được thực hiện ở lớp Business bằng thư viện hash của ứng dụng. P1.2 xác lập vai trò và phiên. P1.3 kiểm tra phiên/quyền tại thời điểm thao tác và kết thúc phiên khi đăng xuất. Hệ thống không giữ mật khẩu trong session. [6]

:::figure dfd2 | Phân rã P2 dữ liệu nền và khách hàng

P2.1 quản lý danh mục, menu/công thức, bàn/khu vực và chính sách có quyền. P2.2 quản lý hồ sơ khách; P2.3 ghi điều chỉnh điểm bằng lịch sử theo quyền quản lý. Thu ngân chỉ được các nhánh tìm/thêm khách phục vụ bán. Thủ kho đọc công thức theo quyền, không thay đổi giá hoặc định mức. Gói F02 được chia theo đúng nội dung này; dữ liệu nguyên liệu gốc thuộc P4.

:::figure dfd3 | Phân rã P3 bán hàng và thanh toán

P3.1 tạo/sửa đơn mở, P3.2 xác định ưu đãi và phân bổ, P3.3 chốt đồng thời tiền–kho–điểm, P3.4 tra cứu/in và cập nhật giao hàng. P3.3 sử dụng D5 để ghi tiêu hao và D3 để ghi điểm; các tác động đó xuất hiện cả ở mức cha. Từ P3.2 sang P3.3 là dữ liệu xác nhận số tiền và phiên bản preview, không phải tín hiệu “nhấn thanh toán”.

:::figure dfd4 | Phân rã P4 quản lý kho

P4.1 kiểm soát nguyên liệu và mở sổ. P4.2 lập phiếu nhập và chuyển dữ liệu nguồn chi/nợ cho P5. P4.3 ghi phiếu xuất. P4.4 đối chiếu snapshot với số đếm trước khi chốt kiểm kê. Nháp chưa chốt không làm tăng hoặc giảm tồn. Gói F07 gồm nguồn nhập, tiền trả ngay và phần nợ; kết quả F08 là mã/liên kết chứng từ đã ghi trong cùng nghiệp vụ.

:::figure dfd5 | Phân rã P5 thu chi và công nợ

P5.1 ghi thu/chi thường được phép. P5.2 xử lý chi trả ngay và nợ nguồn phiếu nhập. P5.3 trả nợ và truy xuất lịch sử/số dư. STORE chỉ nhận dữ liệu xem nợ theo F05, không thực hiện yêu cầu ghi trả nợ. Khoản tiền trả nợ được nhận diện qua PHIEUTHUCHI liên kết THANHTOAN_CONGNO, tránh cộng cả hai thành hai khoản chi.

:::figure dfd6 | Phân rã P6 nhân sự tài sản và báo cáo theo thiết kế đích

P6.1 dự kiến quản lý nhân viên/tài khoản, chấm công và lương; P6.2 quản lý hồ sơ tài sản; P6.3 tổng hợp báo cáo từ dữ liệu đã chốt. Nguồn chi lương F09 và kết quả F10 biểu diễn contract đích giữa P6 và P5; chưa được mô tả là đã chạy qua chương trình hiện tại. Sơ đồ không thêm hoàn tiền, giá vốn lịch sử hoặc lợi nhuận chính xác ngoài phạm vi.

# CHƯƠNG 4 THIẾT KẾ HỆ THỐNG

## 4.1 Thiết kế cơ sở dữ liệu

CSDL được đối chiếu từ DB_Schema.sql và các migration 001–021 có trong source tại thời điểm biên soạn. Baseline có 35 bảng nghiệp vụ. Các migration bổ sung cấu trúc mở sổ, phiên bản, receipt và snapshot để thực hiện contract mới. Danh sách sau mô tả thiết kế trong source; việc đọc file không thay thế kiểm chứng toàn bộ schema trên SQL Server. [6]

Tiền dùng decimal(18,0), lượng nguyên liệu dùng decimal(18,4), số ly dùng int. Số điện thoại là chuỗi để giữ số không ở đầu. NULL biểu thị thông tin không có hoặc chưa xác định theo contract; không tùy tiện thay bằng chuỗi rỗng hay số không. Ngày giờ dùng date/datetime2 theo từng cột. Các trường tính ThanhTien và ThucLinh không nhận UPDATE từ giao diện.

### 4.1.1 Các thực thể và thuộc tính

Các bảng được trình bày theo nhóm nghiệp vụ. PK là khóa chính, FK là khóa ngoại; PK/FK có thể cùng xuất hiện trên thuộc tính của bảng liên kết. Kiểu dữ liệu và khả năng NULL trong các bảng dưới phản ánh baseline; các thay đổi sau baseline được trình bày riêng ngay sau danh mục. Định danh không khóa chính nhưng UNIQUE được nêu trong giải thích quan hệ.

:::dictionary

#### Các bổ sung sau baseline

:::table changes

Ngoài các cột trên, migration tạo nhóm bảng hỗ trợ. SchemaMigrations ghi version và checksum; AppSetupState giữ trạng thái thiết lập quản trị đầu tiên. SystemExpenseCategory ánh xạ các nguồn chi hệ thống; PaymentMethodSupport xác định phương thức được hỗ trợ. CatalogCreateReceipt và G03OperationReceipt ghi kết quả yêu cầu để đối chiếu và chống thực hiện lặp. MaterialOpening và CustomerOpening lưu mốc số dư đầu kỳ. LoyaltyPolicyState giữ phiên bản chính sách, HoaDonUuDaiSnapshot giữ nguồn/tỷ lệ/tiền giảm đã chốt. [6]

Các bảng hỗ trợ có vai trò kỹ thuật hoặc bảo toàn lịch sử, không phải các bộ phận kinh doanh mới. Ví dụ G03OperationReceipt ghi RequestId, tài khoản thực hiện, tên thao tác, mã đối tượng và hash nội dung. Khi cùng một mã yêu cầu được gửi lại, hệ thống phải nhận diện kết quả cũ hoặc phát hiện nội dung không khớp. Hash nội dung này không phải hash mật khẩu.

HoaDonUuDaiSnapshot phân biệt Checkout với LegacyUnknown. Hóa đơn cũ chỉ biết tiền giảm được gắn trạng thái không rõ lịch sử chính sách; không suy ra tỷ lệ hoặc nguồn giảm từ số tiền rồi coi đó là dữ liệu thật. TierCode và PromotionCode của snapshot là mã chụp lịch sử; không tự vẽ FK cho chúng nếu DDL không khai báo.

### 4.1.2 Xây dựng mô hình thực thể liên kết

Mô hình khái niệm được chia theo cụm để giữ chữ đọc được khi in A4. Hình dùng tên nghiệp vụ và liên kết; thuộc tính được trình bày ở mục 4.1.1. Các hình sau bổ sung cho nhau, không phải các CSDL độc lập. Cận dưới 0 ở phía nhiều có nghĩa một đối tượng danh mục có thể chưa phát sinh chứng từ; mỗi chứng từ hợp lệ vẫn phải có đối tượng bắt buộc tương ứng. Bội số đặt sát mỗi thực thể biểu thị số thể hiện của thực thể đó có thể tương ứng với một thể hiện phía còn lại. Dấu (*) ở dòng chứng từ là cận dưới theo nghiệp vụ, không được bảo đảm chỉ bằng FK; dấu (*) ở bảng lương/tài sản nhắc phần chương trình còn là thiết kế đích.

:::figure er-sales | Mô hình ER khái niệm về menu và bán hàng

Một hóa đơn có các dòng món; mỗi dòng thuộc đúng một hóa đơn và một sản phẩm. Quan hệ nghiệp vụ sau tạo đơn yêu cầu ít nhất một dòng, nhưng FK riêng lẻ không tự bảo đảm cận dưới đó. Một khách có thể có nhiều hóa đơn; một hóa đơn có thể không gắn khách. Một bàn có nhiều hóa đơn trong lịch sử, song tại một thời điểm chỉ có tối đa một đơn mở, được kiểm soát bằng unique index có điều kiện. Một hóa đơn có thể có nhiều phân bổ; trường hợp phải trả không có thể không có dòng tiền.

:::figure er-stock | Mô hình ER khái niệm về kho và công nợ

Mỗi phiếu nhập thuộc một nhà cung cấp; mỗi nhà cung cấp có thể có nhiều phiếu. Phiếu nhập có tối đa một nợ gốc do MaPN được UNIQUE trong CONGNO_NCC. Mỗi khoản nợ có nhiều lần trả; mỗi lần trả gắn đúng một phiếu chi và MaTC là UNIQUE trong lịch sử trả. Như vậy số tiền trả được lấy từ phiếu chi liên kết, không lưu một giá trị thứ hai để cộng lặp.

:::figure er-people | Mô hình ER khái niệm về khách hàng nhân sự và tài chính

Nhân viên có tối đa một tài khoản theo UNIQUE MaNV; nhân viên có thể chưa có tài khoản. Vai trò và chức vụ liên kết riêng. Bảng lương có định danh riêng và UNIQUE theo nhân viên/tháng/năm. Phiếu chi có thể gắn tài sản; liên kết này không có nghĩa cứ tạo tài sản là tự tạo tiền chi. Các phần công/lương/tài sản thể hiện mô hình dữ liệu đích, chưa khẳng định có chương trình sử dụng hoàn chỉnh.

### 4.1.3 Chuyển sang mô hình quan hệ

Thực thể độc lập được chuyển thành bảng có khóa chính. Liên kết một–nhiều được biểu diễn bằng FK ở phía nhiều; FK tùy chọn được cho phép NULL. Liên kết nhiều–nhiều sản phẩm–nguyên liệu trở thành CONGTHUC với khóa ghép (MaSP, MaNVL) và DinhMuc. Các dòng chứng từ kho dùng khóa ghép mã phiếu–nguyên liệu, nhờ đó một nguyên liệu có một dòng trong từng phiếu. [5], [6]

CHITIET_HD dùng MaDong; CHITIET_THANHTOAN dùng MaTT. Không ép MaHD–MaSP hay MaHD–MaPT thành khóa chính vì nhiều dòng cùng món hoặc nhiều tham chiếu cùng phương thức có thể có ý nghĩa. Các hình quan hệ dưới hiển thị PK/FK trực tiếp, khác với tên nghiệp vụ ở ER khái niệm. Đường liên kết ghi trường FK tới PK; không suy thêm quan hệ chỉ vì tên cột giống nhau.

:::relations

:::figure rel-sales | Lược đồ quan hệ của nhóm bán hàng

:::figure rel-menu | Lược đồ quan hệ của nhóm menu và khách hàng

:::figure rel-stock | Lược đồ quan hệ của nhóm chứng từ kho

:::figure rel-finance | Lược đồ quan hệ của nhóm công nợ và thu chi

:::figure rel-people | Lược đồ quan hệ của nhóm nhân sự và tài khoản

Giữa các cụm, HOADON.MaNV, các phiếu kho và PHIEUTHUCHI.MaNV cùng tham chiếu NHANVIEN. PHIEUTHUCHI.MaPN và MaBL được bổ sung bằng migration 017, kèm unique index khi khác NULL. MaBL tạo đầu mối cho nguồn chi lương về sau; chưa đủ để kết luận T26 đã triển khai. LICHSU_DIEM.MaHD là FK tùy chọn; bản ghi điều chỉnh quản trị có thể không phát sinh từ hóa đơn.

Hai view quan trọng là vw_TrangThaiBan và vw_CongNoNCC. Trạng thái bàn suy từ đơn mở; số dư nợ suy từ nợ gốc và lịch sử trả. Không giữ một bản trạng thái BAN.TrangThai hoặc CONGNO_NCC.TrangThai/MaNCC trong chương trình vì schema không có các cột đó. View về số dư hiện tại không tự trở thành báo cáo công nợ tại một ngày trong quá khứ.

### 4.1.4 Chuẩn hóa và bảo toàn dữ liệu lịch sử

Để minh họa quá trình chuẩn hóa, xét một quan hệ phân tích ban đầu R(MaDong, MaHD, NgayLap, MaNV, TenNV, MaKH, TenKH, MaSP, TenSP, SoLuong, GiaBanDong, GhiChu). R chỉ là biểu diễn phục vụ lập luận, không phải bảng từng tồn tại trong source. Dùng MaDong thay vì cặp MaHD–MaSP bảo đảm không làm mất các dòng cùng sản phẩm.

R đã dùng từng giá trị đơn cho từng cột và mỗi món là một dòng, đáp ứng cách biểu diễn 1NF. Với giả định khóa ứng viên duy nhất đang xét là MaDong, không có phụ thuộc vào một phần khóa ghép; tuy nhiên các phụ thuộc MaHD → NgayLap, MaNV, MaKH; MaNV → TenNV; MaSP → TenSP làm thông tin lặp theo dòng. Đổi tên sản phẩm hoặc nhân viên trên nhiều dòng dễ tạo bất nhất. Phần mô tả tên khách còn phải xét MaKH tùy chọn, không coi NULL là một mã khách xác định.

Tách thông tin hóa đơn, nhân viên, sản phẩm và khách thành các quan hệ tương ứng; dòng hóa đơn giữ MaDong, MaHD, MaSP, SoLuong, GiaBan và GhiChu. Phép tách theo các định danh làm cho dữ liệu mô tả đối tượng được quản lý tại nguồn của nó. Với từng bước tách dựa trên X → Y và X là khóa của quan hệ chứa X,Y, có thể nối theo X để khôi phục phần thông tin tương ứng; không chỉ tách cột tùy ý theo tên gọi. [7], [8]

:::table normalization

Trong CHITIET_HD, MaSP không xác định GiaBan của mọi giao dịch vì giá có thể đổi theo thời gian. Phụ thuộc phù hợp là MaDong → GiaBan của lần bán. Tương tự, TIEUHAO_HOADON lưu lượng đã dùng cho hóa đơn; lấy lại CONGTHUC hiện tại để tính lại lịch sử sẽ làm sai ý nghĩa dữ liệu. Lưu snapshot vì yêu cầu lịch sử khác với lưu một bản sao dư thừa luôn phải cập nhật cùng danh mục.

Các tổng số dư là điểm cần phân tích riêng. ThanhTien là biểu thức tính toán từ TongTien, PhiShip và GiamGia; TongTien và tồn được duy trì bằng nghiệp vụ/trigger thay vì được người dùng sửa tự do. KHACHHANG lưu cả điểm hiện tại và hạng được suy từ chính sách. BANGLUONG lưu ThucLinh tính từ các thành phần. Những thuộc tính dẫn xuất này tạo phụ thuộc cần quản lý; không dùng việc đã tách bảng để tuyên bố toàn bộ schema vật lý ở 3NF. Báo cáo xác nhận việc phân tách các quan hệ đại diện theo các phụ thuộc đã nêu, đồng thời nhận diện dữ liệu dẫn xuất và snapshot cần cơ chế bảo toàn. [6]

## 4.2 Xây dựng chương trình

### 4.2.1 Thiết kế giao diện chính

:::figure architecture | Tổ chức các tầng của chương trình CoffeeManager

WinForms tiếp nhận thao tác, binding và điều hướng. Business kiểm quyền và chuẩn hóa request; DataAccess tạo tham số, gọi procedure và ánh xạ DTO. SQL Server giữ kết quả tiền, kho, điểm và trạng thái chính thức. Models cung cấp kiểu dữ liệu dùng chung và không phụ thuộc UI hoặc SqlClient. Program.cs nối các dependency. Chương trình chưa có backend HTTP riêng. [6]

Đăng nhập chỉ mở cửa sổ chính sau khi xác thực thành công. Màn hình phân biệt sai thông tin đăng nhập với lỗi kết nối dữ liệu. Hình dưới là ảnh lưu từ ngày 17/10/2026 ở tình huống chưa thiết lập kết nối; ảnh được dùng để minh họa bố cục và cách báo trạng thái, không chứng minh lần đăng nhập thành công hoặc hiện trạng mọi thành phần giao diện.

:::figure ui-login | Giao diện đăng nhập trong ảnh lưu ngày 17 tháng 10 năm 2026

Cửa sổ chính tổ chức menu bên trái, vùng danh tính và UserControl công việc. Nội dung có thể thay theo vai trò; việc ẩn menu không thay thế kiểm tra quyền ở Business. Ảnh ngày 22/10/2026 dưới đây cho thấy khung chính và màn khách hàng trước các thay đổi ngày 26–27/10. Dữ liệu hiển thị là dữ liệu mẫu của dự án, không phải hồ sơ khách từ khảo sát.

:::figure ui-customers | Cửa sổ chính và màn khách hàng trong ảnh lưu ngày 22 tháng 10 năm 2026

POS hiện có SalesControl, OrderPanel và các dialog chọn khách/đơn mở/thanh toán trong source. Bố cục chia vùng bàn/loại đơn, tìm món và chi tiết đơn; F2 tìm món, F3 chọn khách, F4 xem đơn mở, F8 mở hộp thanh toán. Phím tắt chỉ mở đúng công việc, không chốt tiền trực tiếp. Ảnh POS hiện tại chưa được tìm thấy ở dạng native phù hợp trong bộ bằng chứng đã chọn, nên để vị trí ảnh bổ sung thay vì lấy HTML mô phỏng.

:::missing UI03 | Giao diện POS đang mở một đơn mẫu | Đặt tại đây; rộng 15,5 cm, cao khoảng 9 cm. Cần ảnh WinForms thật có loại đơn/bàn, ít nhất hai dòng món và tổng từ SQL; không dùng ảnh đề xuất HTML. Phiên bản ảnh phải sau các thay đổi dialog ngày 26/10.

### 4.2.2 Thiết kế giao diện cập nhật dữ liệu

Các màn danh sách cung cấp tìm kiếm, bộ lọc và bảng dữ liệu; người dùng chọn một dòng rồi mở editor có owner. Editor thuộc MainForm hoặc hộp đang hoạt động, có thanh tiêu đề và nút X native, không hiện thêm cửa sổ nghiệp vụ độc lập trên taskbar. Khi hộp con đóng, kết quả chỉ trả một lần và dữ liệu nhập của cha được giữ theo luồng. [2], [6]

Việc cập nhật có hai tầng kiểm tra. Giao diện báo lỗi gần trường nhập và giữ dữ liệu để sửa; Business tiếp tục kiểm quyền, chuẩn hóa độ dài, số lượng và NULL. SQL bảo vệ khóa, tham chiếu, duy nhất và xung đột phiên bản. Không bỏ bước kiểm tra nghiệp vụ chỉ vì ô nhập đã khóa trên màn hình.

Ảnh hộp thêm danh mục ngày 17/10 dưới đây minh họa editor có thật ở phiên bản cũ; không dùng để xác nhận hộp của bản hiện tại đã được kiểm lại. Quy tắc mới chọn hết nội dung khi vừa vào ô nhập được thực hiện bằng helper chung; click tiếp trong cùng lần focus vẫn cho đặt caret bình thường. [2]

:::figure ui-editor | Hộp thêm danh mục trong ảnh lưu ngày 17 tháng 10 năm 2026

:::table ui-inputs

Khi phiên bản dữ liệu đã đổi ở cửa sổ khác, chương trình yêu cầu tải lại. Người dùng không được âm thầm ghi đè dữ liệu mới bằng bản cũ. Với các trường danh mục đã có giao dịch, quy tắc bất biến và tham chiếu quyết định có được sửa/xóa hay không; không cố xóa lịch sử để làm cho nút Xóa hoạt động.

### 4.2.3 Thiết kế giao diện xử lý dữ liệu

Hộp thanh toán hiển thị tiền món, nguồn giảm, giảm giá, ship, số phải trả và bảng phân bổ. Người dùng có thể trả nhiều phương thức; tiền thối chỉ tính trên phần tiền mặt. Khi đang ghi, hộp khóa gửi lặp và không coi đóng tác vụ chờ là hủy giao dịch SQL. Trạng thái chưa biết yêu cầu đối chiếu; trạng thái đã chốt mở đường xem/in, không cho thu lần hai. [2], [6]

:::missing UI05 | Hộp thanh toán và xác nhận có owner native | Đặt sau mô tả thanh toán; rộng 13–15,5 cm, cao khoảng 10 cm. Cần ảnh thật thể hiện tổng SQL, các dòng phân bổ và nút xác nhận; bổ sung theo bản hiện tại, không tạo ảnh giả từ mẫu đề xuất.

Lịch sử hóa đơn cho lọc, phân trang, mở chi tiết và in lại dựa dữ liệu đã lưu. Giá trên chi tiết lấy từ snapshot dòng. Mẫu in sử dụng PrintDocument/preview của chương trình. Ngày thanh toán phục vụ doanh thu; không dùng ngày lập đơn để tự gọi là doanh thu của ngày. Không ghép trực tiếp các dòng món với các dòng thanh toán rồi cộng, vì hóa đơn nhiều món và nhiều phương thức có thể bị nhân số tiền.

:::missing UI06 | Lịch sử và chi tiết hóa đơn của bản hiện tại | Đặt tại đây; một ảnh lịch sử rộng 15,5 cm và một ảnh chi tiết/preview ở trang kế nếu cần. Cần thấy bộ lọc ngày đang dùng và mã hóa đơn mẫu; không dùng ảnh thiết kế V4 làm chứng cứ màn thật.

Quản lý kho có các workspace tồn, nhập, xuất và kiểm kê. Người dùng soạn nhiều dòng nguyên liệu trước khi gửi. Chứng từ chốt không cho sửa tài chính. Kiểm kê cần phân biệt chưa nhập số đếm, số đếm bằng không và kết quả khác snapshot. Công nợ cung cấp số dư cùng lịch sử trả; STORE chỉ xem, ADMIN mới có lệnh trả. Các màn này có source hiện hữu nhưng còn các lỗi nghiệm thu G05 đã nêu ở mục 3.1.5; báo cáo không gắn nhãn hoàn chỉnh cho chúng.

:::missing UI07 | Phiếu nhập kiểm kê và công nợ | Dự trù ba ảnh ở phần kho/công nợ, mỗi ảnh rộng 15,5 cm, một đến hai ảnh mỗi trang tùy độ cao thực tế. Chỉ chèn sau khi có ảnh thật xác định phiên bản; hình ảnh không được che các lỗi chức năng đang ghi trong hồ sơ G05.

:::table ui-states

Các lỗi in xảy ra sau commit không làm mất kết quả chốt. Với timeout, mã yêu cầu và kết quả đối chiếu quyết định hành động tiếp theo. Nhờ tách trạng thái giao diện khỏi trạng thái giao dịch, ứng dụng tránh việc người dùng nhìn một thông báo lỗi mạng rồi thực hiện lại một khoản tiền đã ghi thành công.

### 4.2.4 Phạm vi đã xây dựng và hướng hoàn thiện

Source đã có đăng nhập/phiên, danh mục nền, menu/công thức, bàn, khách/ưu đãi, bán hàng/thanh toán/tra cứu/in/giao hàng và các nghiệp vụ kho–thu chi–công nợ. Tình trạng nghiệm thu khác nhau theo nhóm và phiên bản; không có một số PASS tổng hợp có thể đại diện cho mọi thay đổi của ứng dụng. Đặc biệt, G05 chưa hoàn tất; G06–G08 vẫn là phần tiếp theo của kế hoạch. [12]

Nội dung xây dựng chương trình vì vậy tập trung giải thích trách nhiệm các tầng và luồng xử lý có source, kèm hình thật có nguồn hoặc chỗ ảnh còn thiếu. Không có ảnh giả cho bảng lương, khấu hao, dashboard doanh thu hoặc bản publish cuối. Đó là giới hạn của bản báo cáo tại mốc khảo sát source, cần được giữ rõ khi trình bày và bảo vệ.

# KẾT LUẬN VÀ KIẾN NGHỊ

Báo cáo đã hệ thống hóa bài toán quản lý một quán cà phê với một kho chung, xác định nhóm chức năng, tác nhân và hồ sơ, đồng thời xây dựng BFD và DFD theo một ranh giới phần mềm thống nhất. Phần thiết kế dữ liệu làm rõ thực thể, khóa, liên kết, chuyển sang quan hệ và các phụ thuộc hàm đại diện. Những quyết định như định danh dòng món, giữ giá bán lịch sử và tách nguồn chi giúp giải thích vì sao hệ thống cần nhiều ràng buộc hơn một tập màn hình thêm/sửa/xóa.

Phần chương trình được trình bày theo source và hồ sơ dự án hiện có. Bán hàng gắn với tiền, tiêu hao và điểm trong giao dịch; nhập hàng gắn với tồn, chi trả ngay và nợ; timeout phải được đối chiếu trước khi gửi lại. Tuy nhiên, triển khai source chưa đồng nghĩa mọi tiêu chí đã được nghiệm thu. G05 còn kết quả chưa đạt; các nhóm nhân sự/lương, tài sản/báo cáo và vận hành cuối chưa hoàn tất theo kế hoạch được cung cấp.

Trước khi nộp, cần bổ sung ghi nhận khảo sát thực tế, sơ đồ tổ chức được xác nhận và ảnh chương trình đúng phiên bản tại các vị trí đã ghi. Về phát triển phần mềm, ưu tiên xử lý các lỗi đã được ghi nhận và được duyệt, hoàn thiện kiểm thử theo ảnh hưởng rồi tiếp tục các nhóm phụ thuộc. Kiến nghị này giữ phạm vi đề tài; không xem các tính năng như nhiều chi nhánh, thanh toán trực tuyến hoặc lợi nhuận có giá vốn lịch sử là kết quả đã đạt.

:::bibliography
