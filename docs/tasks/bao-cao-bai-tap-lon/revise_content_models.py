from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
from lxml import etree as E
from copy import deepcopy as cp
import re,json,shutil
from PIL import Image

ROOT=Path(__file__).resolve().parents[3]
TASK=Path(__file__).parent
REV=TASK/'revisions/20261110-content-models'
FIG=REV/'diagrams'
SOURCE=ROOT/'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager_KhungWord.docx'
DEST=SOURCE.with_name('BaoCao_CoffeeManager_HoanThien.docx')
if not (REV/'before.docx').exists():shutil.copy2(SOURCE,REV/'before.docx')
W='http://schemas.openxmlformats.org/wordprocessingml/2006/main';R='http://schemas.openxmlformats.org/officeDocument/2006/relationships'
NS={'w':W,'r':R,'a':'http://schemas.openxmlformats.org/drawingml/2006/main','wp':'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing','pic':'http://schemas.openxmlformats.org/drawingml/2006/picture'}
def q(s):return '{'+W+'}'+s
def sub(e,name,attrs={}):return E.SubElement(e,q(name),{q(k):str(v) for k,v in attrs.items()})
def dump(e):return E.tostring(e,xml_declaration=True,encoding='UTF-8',standalone=True)
with ZipFile(SOURCE) as z:parts={n:z.read(n) for n in z.namelist()}
d=E.fromstring(parts['word/document.xml']);body=d.find(q('body'));original=list(body)
def text(x):return ''.join(x.xpath('.//w:t/text()',namespaces=NS))
def prop(p):
 x=p.find(q('pPr'))
 if x is None:x=E.Element(q('pPr'));p.insert(0,x)
 return x
def setprop(p,name,attrs):
 pp=prop(p);old=pp.find(q(name))
 if old is not None:pp.remove(old)
 return sub(pp,name,attrs)
def settext(p,t):
 for c in list(p):
  if c.tag!=q('pPr'):p.remove(c)
 r=sub(p,'r');rp=sub(r,'rPr');sub(rp,'rFonts',{'ascii':'Times New Roman','hAnsi':'Times New Roman','eastAsia':'Times New Roman'});sub(rp,'sz',{'val':26});sub(rp,'color',{'val':'000000'})
 tt=sub(r,'t');tt.set('{http://www.w3.org/XML/1998/namespace}space','preserve');tt.text=t
def paragraph(t,template=None):
 p=E.Element(q('p'))
 if template is not None and template.find(q('pPr')) is not None:p.append(cp(template.find(q('pPr'))))
 settext(p,t);return p

# Rewritten paragraphs retain source facts, with citations at paragraph ends.
REWRITE={
134:'Ther Thẩn Cafe tại 49 Đường 449, phường Tăng Nhơn Phú, Thành phố Hồ Chí Minh là cơ sở được lựa chọn cho đề tài. Tên và địa chỉ do người thực hiện cung cấp, đồng thời được đối chiếu với bài đăng ngày 01/07/2026 lưu trên FindGlocal. Trang này là nguồn tổng hợp thông tin công khai, vì vậy chỉ được dùng để đối chiếu địa điểm, không thay thế kết quả khảo sát trực tiếp. [13]',
135:'CoffeeManager được xây dựng cho một quán cà phê có một kho nguyên liệu chung. Đề tài không mở rộng sang quản lý chuỗi hoặc đồng bộ nhiều chi nhánh. Phần mềm là sản phẩm của bài tập lớn, chưa có căn cứ để khẳng định cơ sở khảo sát đã đưa hệ thống vào sử dụng. [1]',
136:'Hồ sơ hiện có chưa xác nhận thời điểm khảo sát, người cung cấp thông tin, quy mô phục vụ và cách ghi nhận giao dịch tại quán. Vì vậy, báo cáo không kết luận quán đang quản lý bằng sổ sách hay gặp thất thoát nguyên liệu. Dữ liệu trong chương trình là dữ liệu mẫu phục vụ phát triển và thử nghiệm, không được dùng để suy ra quy mô hoạt động thực tế của cơ sở. [6]',
137:'Đề tài tập trung vào mối liên hệ giữa đơn bán, tiền thu, nguyên liệu tiêu hao và điểm khách hàng. Khi một hóa đơn được thanh toán, những thông tin này phải được cập nhật nhất quán. Việc phân tích hệ thống vì thế cần làm rõ cả công việc của người sử dụng lẫn cách dữ liệu được lưu và xử lý. [1]',
139:'Cơ cấu tổ chức của quán chưa được xác nhận trong hồ sơ khảo sát. Báo cáo giữ vị trí để bổ sung sơ đồ sau khi có tên các bộ phận và quan hệ quản lý thực tế. Danh mục chức vụ trong dữ liệu mẫu chỉ phục vụ phần mềm, không đủ để xác định cơ cấu nhân sự của quán. [6]',
140:'Vị trí bổ sung sơ đồ tổ chức của cơ sở khảo sát sau khi xác nhận các bộ phận và quan hệ quản lý.',
141:'Phần mềm phân quyền theo ba vai trò là người quản lý, nhân viên thu ngân và nhân viên kho. Mã quyền tương ứng là ADMIN, CASH và STORE. Vai trò sử dụng phần mềm được quản lý độc lập với chức vụ, nhờ đó có thể điều chỉnh quyền thao tác mà không phải thay đổi hồ sơ công việc của nhân viên. [1]',
144:'Phân tích chức năng xác định những công việc hệ thống cần hỗ trợ rồi chia từng công việc thành các chức năng nhỏ hơn. Sơ đồ BFD thể hiện quan hệ giữa chức năng tổng thể và các chức năng thành phần. Mũi tên trong sơ đồ của đề tài chỉ chiều phân rã từ chức năng cha đến chức năng con, không diễn tả thứ tự xử lý dữ liệu. [1]',
145:'CoffeeManager được phân nhóm theo công việc như bán hàng, quản lý kho và quản lý công nợ. Cách phân chia này giúp gắn chức năng với nhu cầu sử dụng. Chẳng hạn, nhập hàng là một công việc thống nhất dù khi thực hiện phải ghi thông tin vào nhiều bảng dữ liệu. [1]',
147:'Biểu đồ luồng dữ liệu mô tả thông tin được trao đổi giữa tác nhân ngoài, tiến trình xử lý và kho dữ liệu. Mỗi mũi tên có hướng và được đặt tên theo nội dung dữ liệu truyền đi. Tên tiến trình diễn tả công việc xử lý, còn tên kho cho biết loại hồ sơ được lưu trữ. [3]',
148:'Ở mức ngữ cảnh, toàn bộ phần mềm được biểu diễn bằng một tiến trình duy nhất. Mức đỉnh tách tiến trình đó thành các chức năng chính. Khi tiếp tục phân rã xuống mức dưới đỉnh, các dữ liệu đi qua ranh giới của tiến trình cha phải được giữ đầy đủ về nguồn gửi, nơi nhận và nội dung. Việc chia nhỏ không được làm xuất hiện tác nhân ngoài mới hoặc làm mất luồng thông tin đã có. [4]',
149:'Trong đề tài, tác nhân trực tiếp sử dụng phần mềm là người quản lý, nhân viên thu ngân và nhân viên kho. Khách hàng, nhà cung cấp và đối tác giao hàng trao đổi thông tin thông qua nhân viên. Hệ thống chưa kết nối trực tiếp với ngân hàng hoặc đối tác, vì vậy các bên này không được vẽ thành tác nhân truy cập phần mềm. [1]',
151:'Mô hình thực thể liên kết mô tả các đối tượng cần quản lý, thuộc tính của từng đối tượng và quan hệ giữa chúng. Báo cáo sử dụng ký pháp Chen với hình chữ nhật cho thực thể, hình elip cho thuộc tính và hình thoi cho liên kết. Bội số đặt ở hai đầu liên kết cho biết số đối tượng có thể tham gia. Mô hình này được xây dựng trước khi chuyển thành các bảng và khóa ngoại. [5]',
152:'Một sản phẩm có thể sử dụng nhiều nguyên liệu, đồng thời một nguyên liệu có thể được dùng để pha nhiều sản phẩm. Định mức thuộc về sự kết hợp giữa sản phẩm và nguyên liệu. Khi chuyển sang mô hình quan hệ, liên kết này được lưu trong bảng CONGTHUC. Giá của một dòng hóa đơn được giữ theo lần bán để việc thay đổi giá menu không làm đổi dữ liệu giao dịch cũ. [6]',
154:'Phụ thuộc hàm biểu thị khả năng xác định giá trị của một nhóm thuộc tính từ một nhóm thuộc tính khác. Nếu X xác định duy nhất Y trong mọi trạng thái hợp lệ của quan hệ thì Y phụ thuộc hàm vào X. Việc xác định phụ thuộc phải dựa trên ý nghĩa nghiệp vụ, không chỉ dựa vào sự trùng hợp của dữ liệu mẫu. [7]',
155:'Chuẩn hóa giúp giảm dữ liệu dư thừa và hạn chế bất thường khi thêm, sửa hoặc xóa. Dạng chuẩn thứ nhất yêu cầu mỗi ô chứa một giá trị đơn. Dạng chuẩn thứ hai loại phụ thuộc bộ phận vào khóa ghép, còn dạng chuẩn thứ ba tiếp tục kiểm soát phụ thuộc bắc cầu của thuộc tính không khóa. [8]',
156:'Trong ứng dụng, một số dữ liệu được giữ lại để phản ánh lịch sử. Giá bán đã ghi trên dòng hóa đơn phải được hiểu là giá của giao dịch đó, không phải giá menu hiện tại. Vì thế, phân tích chuẩn hóa cần xem xét ý nghĩa thời gian của dữ liệu và phân biệt thông tin lịch sử với bản sao dư thừa. [6]',
158:'Windows Forms cung cấp các thành phần để xây dựng giao diện ứng dụng Windows, tiếp nhận thao tác và liên kết dữ liệu. Giao diện được tổ chức bằng cửa sổ, điều khiển và các hàm xử lý sự kiện. Những thành phần này phù hợp với chương trình quản lý được thao tác bằng chuột và bàn phím. [9]',
159:'CoffeeManager sử dụng C# WinForms trên .NET 10. Các phần giao diện, xử lý nghiệp vụ, truy cập dữ liệu và mô hình dữ liệu được tách thành các dự án riêng. Chương trình truy cập SQL Server bằng Microsoft.Data.SqlClient và truyền tham số có kiểu dữ liệu xác định. Cấu hình kết nối và tài khoản phát triển không được đưa vào nội dung công bố của báo cáo. [6]',
161:'Giao dịch cơ sở dữ liệu là một đơn vị công việc được ghi nhận toàn bộ khi thành công hoặc hoàn tác khi không thể hoàn thành. Cơ chế này cần thiết khi một nghiệp vụ thay đổi nhiều bảng. Trong CoffeeManager, việc chốt thanh toán phải bảo đảm tiền thu, tiêu hao nguyên liệu, điểm khách hàng và trạng thái hóa đơn được ghi nhất quán. [10]',
162:'Tham số dạng bảng cho phép gửi nhiều dòng dữ liệu có cùng cấu trúc tới SQL Server trong một lần gọi. Ứng dụng sử dụng cơ chế này để chuyển các dòng phân bổ thanh toán và các dòng chứng từ kho. Dữ liệu được truyền thuận tiện hơn nhưng vẫn phải được kiểm tra về quyền thao tác, số lượng và điều kiện nghiệp vụ. [11]',
166:'CoffeeManager hỗ trợ quản lý một quán cà phê với ba loại đơn là tại bàn, mang đi và giao hàng. Hệ thống dùng chung thông tin món, công thức, khách hàng, ưu đãi, nguyên liệu và công nợ nhà cung cấp. Phạm vi phân tích còn bao gồm nhân sự, chấm công, tiền lương, tài sản và báo cáo phục vụ người quản lý. [1]',
167:'Thu ngân tiếp nhận yêu cầu của khách, chọn bàn hoặc loại đơn, thêm món và xác nhận thanh toán. Nhân viên kho theo dõi nguyên liệu, lập phiếu nhập, phiếu xuất và kiểm kê. Người quản lý được thực hiện các nghiệp vụ tài chính và điều chỉnh chính sách theo quyền. Nhân viên lập chứng từ được lấy từ tài khoản đang đăng nhập để xác định trách nhiệm của từng giao dịch. [2]',
171:'Chương trình làm việc trực tiếp với SQL Server và chưa có website hoặc cơ chế đồng bộ khi mất kết nối. Khi ghi nhận chuyển khoản, hệ thống lưu số tiền và mã tham chiếu do nhân viên cung cấp, chưa tự xác minh giao dịch với ngân hàng. Đặt bàn, gộp hoặc tách hóa đơn, hoàn tiền sau thanh toán và đổi điểm thành tiền nằm ngoài phạm vi đề tài. [1]',
173:'Đơn tại quán phải gắn với một bàn. Khi người dùng chọn bàn trống, hệ thống chỉ tạo hóa đơn cùng với món đầu tiên để tránh giữ bàn bằng một đơn rỗng. Bàn đang có đơn sẽ mở lại hóa đơn đó. Đơn mang đi không gắn bàn, còn đơn giao hàng cần tên người nhận, số điện thoại và địa chỉ. Quán có thể tự giao hoặc chọn đối tác giao hàng. [2]',
174:'Mỗi dòng món lưu giá được áp dụng tại thời điểm thêm vào đơn. Khi sửa số lượng, hệ thống giữ giá đó thay vì lấy lại giá menu mới. Mã dòng giúp phân biệt các lần gọi cùng một món có ghi chú khác nhau. Đơn đã lưu vẫn được giữ khi người dùng rời màn bán hàng, việc chuyển màn không đồng nghĩa hủy đơn. [6]',
175:'Khách vãng lai được ghi nhận bằng việc để trống mã khách hàng. Khi áp dụng một chương trình khuyến mãi hợp lệ, mức giảm của chương trình thay thế ưu đãi theo hạng thành viên. Hai nguồn ưu đãi không cộng dồn và chỉ giảm trên tiền món, không giảm phí giao hàng. [1]',
177:'Tổng tiền món được tính từ số lượng và giá đã lưu trên từng dòng. Số phải trả bằng tổng tiền món cộng phí giao hàng rồi trừ giảm giá. Tổng các khoản phân bổ thanh toán phải bằng số phải trả. Tiền khách đưa và tiền thối chỉ hỗ trợ thu tiền mặt, không làm thay đổi doanh thu. Hóa đơn có số phải trả bằng không vẫn cần được xác nhận chốt nhưng không tạo dòng thu tiền bằng không. [6]',
178:'Trước khi chốt, SQL Server tính lại số tiền và kiểm tra phiên bản hóa đơn. Nếu thông tin đã thay đổi, người dùng phải tải lại và xác nhận kết quả mới. Tiền thu, ưu đãi, tiêu hao nguyên liệu, điểm và trạng thái thanh toán được ghi trong cùng giao dịch. Dữ liệu tiêu hao đã ghi được giữ nguyên kể cả khi công thức sản phẩm thay đổi về sau. [6]',
179:'Hóa đơn đã thanh toán bị khóa phần tài chính. Việc in chỉ diễn ra sau khi giao dịch đã được ghi thành công, do đó lỗi in chỉ yêu cầu in lại. Nếu mất kết nối sau khi gửi lệnh, chương trình đối chiếu mã yêu cầu và trạng thái hóa đơn trước khi cho phép thử lại để tránh thu tiền hai lần. [2]',
180:'Chỉ đơn đang mở mới được hủy. Do hệ thống chưa trừ nguyên liệu theo bán hàng ở giai đoạn này, hủy đơn không làm hoàn kho. Nếu món đã pha phải bỏ, nhân viên có quyền lập phiếu xuất hủy theo lượng thực dùng. Việc cập nhật tiến độ giao hàng không mở lại quyền sửa tiền của hóa đơn đã thanh toán. [1]',
182:'Mỗi nguyên liệu sử dụng một đơn vị tính thống nhất cho định mức, nhập, xuất và kiểm kê. Quán có một kho chung nên không phát sinh chuyển nguyên liệu sang kho quầy. Sau khi ghi số dư đầu kỳ, tồn kho được điều chỉnh bằng giao dịch, không sửa trực tiếp trong danh mục nguyên liệu. [1]',
183:'Nhân viên soạn phiếu nhập và các dòng nguyên liệu trước khi xác nhận. Khi ghi nhận, hệ thống tăng tồn kho, tính tổng tiền, đồng thời ghi khoản trả ngay và công nợ còn lại nếu có. Khoản trả ngay không được vượt tổng phiếu. Mỗi phiếu nhập có tối đa một khoản nợ gốc để tránh ghi trùng nghĩa vụ thanh toán. [6]',
184:'Phiếu xuất dùng để ghi lượng hủy đồ hỏng hoặc trả hàng. Nếu một nguyên liệu không đủ tồn, toàn bộ giao dịch bị từ chối. Khi kiểm kê, nhân viên nhập số đếm và lý do chênh lệch dựa trên tồn hệ thống tại thời điểm lấy dữ liệu. Nếu tồn đã thay đổi trước khi chốt, chương trình yêu cầu đối chiếu lại thay vì dùng số liệu cũ để điều chỉnh. [6]',
185:'Người quản lý được trả nợ trong phạm vi số dư còn lại. Mỗi lần trả tạo một phiếu chi gắn với một dòng lịch sử trả nợ. Ứng dụng sử dụng kết quả này và không lập thêm phiếu chi thứ hai. Số dư hiện tại được tính từ nợ gốc và các lần đã trả, còn báo cáo tại một ngày phải giới hạn dữ liệu theo thời điểm đó. [6]',
187:'Ngoài các nghiệp vụ bán hàng và kho, đề tài phân tích thêm quản lý nhân viên, chấm công, tính lương, tài sản và báo cáo. Trang Tổng quan hỗ trợ theo dõi kết quả kinh doanh, còn lựa chọn tiếng Việt hoặc tiếng Anh thay đổi ngôn ngữ hiển thị. Các phần này được trình bày theo mã nguồn và hồ sơ triển khai tương ứng. [14]',
188:'Hồ sơ dự án phân biệt phần đã xây dựng với phần đã được kiểm chứng. Một chức năng có mã nguồn hoặc chạy được khi thử riêng chưa đồng nghĩa toàn bộ tình huống sử dụng đã được nghiệm thu. Báo cáo vì vậy giữ giới hạn này và không sử dụng kết quả kiểm tra của phiên bản cũ để xác nhận toàn bộ phiên bản hiện tại. [12]',
190:'Sơ đồ phân cấp xác định sáu chức năng chính, gồm xác thực, quản lý dữ liệu nền và khách hàng, bán hàng, quản lý kho, quản lý thu chi và công nợ, cùng quản lý nhân sự, tài sản và báo cáo. Mỗi chức năng được tách thành các công việc cụ thể. Các ô được nối riêng để người đọc theo dõi được quan hệ giữa chức năng cha và chức năng con. [1]',
193:'Chức năng bán hàng được làm rõ đến bước tính tiền và chốt thanh toán vì liên quan đồng thời đến tiền thu, kho và điểm. Khi nhập hàng hoặc chi lương, dữ liệu nguồn được chuyển sang chức năng tài chính để ghi nhận khoản chi đúng một lần. [1]',
195:'Ranh giới phân tích là phần mềm CoffeeManager. Ba tác nhân trực tiếp là người quản lý, nhân viên thu ngân và nhân viên kho. Khách hàng, nhà cung cấp và đối tác giao hàng được quản lý như các đối tượng nghiệp vụ, thông tin của họ do nhân viên nhập vào chương trình. Tên tác nhân được giữ thống nhất trong bảng phân tích và toàn bộ các mức DFD. [1]',
202:'Các kho trong mô hình là kho dữ liệu logic. Một kho có thể được triển khai bằng nhiều bảng nhưng chỉ có một ý nghĩa nghiệp vụ thống nhất. Chẳng hạn, hồ sơ bán hàng lưu hóa đơn, dòng món và các khoản phân bổ thanh toán. Việc phân rã tiến trình chỉ làm rõ cách xử lý, không tự tạo một bản dữ liệu độc lập khác của cùng hồ sơ. [3]',
206:'Tên các luồng được viết đầy đủ theo nội dung trao đổi. Bảng dưới mô tả thông tin gửi vào và kết quả nhận lại của từng công việc. Khi phân rã, các luồng có thể đi vào những tiến trình con khác nhau nhưng vẫn phải giữ đúng tác nhân, kho dữ liệu và nội dung đã xác định ở mức cha. [3]',
211:'Mức ngữ cảnh chỉ có một tiến trình đại diện cho CoffeeManager và ba tác nhân sử dụng trực tiếp. Mỗi tác nhân gửi thông tin đăng nhập cùng yêu cầu nghiệp vụ trong phạm vi quyền của mình, sau đó nhận kết quả xác thực và kết quả xử lý. Không bổ sung tác nhân ngân hàng hoặc cổng giao hàng vì hệ thống chưa có các kết nối đó. [1]',
217:'Mức đỉnh gồm sáu tiến trình chính. Để giữ chữ và nhãn luồng đọc rõ trên khổ A4, các hình trình bày từng phần của cùng một mức. Một kho hoặc tiến trình cùng tên xuất hiện ở nhiều hình vẫn là cùng một đối tượng. Các mũi tên ghi rõ nguồn gửi, nơi nhận và nội dung trao đổi, không dùng mã gói dữ liệu để thay cho tên luồng. [1]',
218:'Hai trao đổi nội bộ cần được giữ thống nhất là dữ liệu nhập hàng chuyển sang ghi chi và công nợ, cùng dữ liệu lương đã chốt chuyển sang ghi chi lương. Kết quả trả về là liên kết chứng từ đã tạo. Đây là sự phân chia trách nhiệm xử lý trong mô hình, không phải hai giao dịch độc lập cùng ghi một khoản tiền. [1]',
222:'Quá trình xác thực được tách thành kiểm tra thông tin đăng nhập, xác lập phiên và kiểm tra hoặc kết thúc phiên. Hồ sơ tài khoản cung cấp dữ liệu để đối chiếu mật khẩu và quyền. Mật khẩu được kiểm chứng bằng thư viện bảo mật ở tầng nghiệp vụ và không được giữ trong phiên làm việc. [6]',
225:'Quản lý dữ liệu nền gồm cập nhật danh mục và chính sách, quản lý hồ sơ khách hàng và ghi điều chỉnh điểm. Người quản lý có quyền thay đổi chính sách. Thu ngân chỉ tìm hoặc thêm khách phục vụ bán hàng, còn nhân viên kho chỉ xem phần danh mục được cấp quyền. Điều chỉnh điểm phải có lịch sử, không sửa số điểm mà không lưu lý do. [1]',
228:'Bán hàng được phân thành lập đơn, xác định ưu đãi và số phải trả, chốt thanh toán, sau đó tra cứu hoặc in hóa đơn và theo dõi giao hàng. Tiến trình chốt nhận đơn và phân bổ đã xác nhận, ghi hồ sơ bán hàng, tiêu hao nguyên liệu và lịch sử điểm trong cùng nghiệp vụ. Các hồ sơ đọc và ghi vẫn được giữ như ở mức đỉnh. [6]',
231:'Quản lý kho gồm nguyên liệu và tồn đầu kỳ, nhập hàng, xuất hàng và kiểm kê. Nhập hàng chuyển nguồn phiếu cùng số tiền sang chức năng tài chính để ghi chi và công nợ. Kiểm kê sử dụng số đếm cùng tồn tại thời điểm lấy dữ liệu. Mỗi loại chứng từ chỉ làm thay đổi tồn sau khi được chốt hợp lệ. [6]',
234:'Chức năng tài chính ghi thu chi thông thường, chi trả ngay từ nhập hàng, trả nợ và chi lương theo bảng đã chốt. Nhân viên kho chỉ được nhận số dư và lịch sử công nợ. Khi trả nợ, số tiền nằm trên phiếu chi liên kết với lần trả, nhờ đó không bị cộng thành hai khoản chi khác nhau. [6]',
237:'Chức năng nhân sự, tài sản và báo cáo tiếp nhận yêu cầu của người quản lý. Hồ sơ nhân sự cung cấp dữ liệu chấm công và lương, hồ sơ tài sản phục vụ theo dõi tài sản, còn báo cáo tổng hợp dữ liệu bán hàng, kho, công nợ và thu chi. Nguồn chi lương được chuyển sang chức năng tài chính và nhận lại liên kết phiếu chi. [1]',
240:'Thiết kế dữ liệu bắt đầu từ 35 bảng nghiệp vụ trong DB_Schema.sql. Các bản cập nhật cơ sở dữ liệu bổ sung những cấu trúc phục vụ mở sổ, kiểm tra phiên bản dữ liệu và lưu kết quả giao dịch. Phần từ điển dưới đây trình bày cấu trúc nền, sau đó giải thích các bổ sung liên quan. Việc mô tả thiết kế không thay thế kết quả kiểm tra trên SQL Server. [6]',
241:'Tiền được lưu bằng decimal(18,0), lượng nguyên liệu bằng decimal(18,4) và số ly bằng int. Số điện thoại là chuỗi để giữ số không ở đầu. Giá trị NULL biểu thị thông tin không có hoặc chưa xác định theo quy tắc của từng trường. Các cột tính toán như ThanhTien và ThucLinh được cơ sở dữ liệu xác định, không nhận giá trị nhập trực tiếp từ giao diện. [6]',
243:'Mỗi thực thể được mô tả bằng các thuộc tính và kiểu dữ liệu tương ứng. PK là khóa chính, FK là khóa ngoại. Một thuộc tính trong bảng liên kết có thể đồng thời tham gia cả hai loại khóa. Các bảng trình bày cấu trúc nền, còn thay đổi của những phiên bản sau được nêu riêng để tránh trộn lẫn các mốc thiết kế. [6]',
359:'Các bảng hỗ trợ phục vụ quản lý phiên bản, mở sổ và bảo toàn kết quả giao dịch. SchemaMigrations ghi các bản cập nhật đã áp dụng. MaterialOpening và CustomerOpening lưu số dư đầu kỳ. Các bảng lưu kết quả yêu cầu giúp chương trình đối chiếu một thao tác đã gửi, còn HoaDonUuDaiSnapshot giữ nguồn và số tiền giảm đã chốt. [6]',
360:'Khi cùng một mã yêu cầu được gửi lại, hệ thống kiểm tra kết quả đã ghi hoặc từ chối nếu nội dung mới không khớp. Cách xử lý này giúp tránh tạo lại giao dịch do mất kết nối. Thông tin kiểm tra nội dung yêu cầu chỉ phục vụ đối chiếu giao dịch và có ý nghĩa khác với dữ liệu dùng để kiểm chứng mật khẩu. [6]',
361:'Đối với hóa đơn cũ chỉ còn biết số tiền giảm, hệ thống giữ trạng thái chưa xác định được lịch sử chính sách. Không suy ngược nguồn ưu đãi từ số tiền rồi coi đó là thông tin đã được ghi nhận. Mã hạng và mã khuyến mãi trong bản lưu lịch sử không được tự xem là khóa ngoại nếu cơ sở dữ liệu không khai báo ràng buộc. [6]',
363:'Các hình dưới sử dụng ký pháp Chen và tên đối tượng theo nghiệp vụ. Thực thể, thuộc tính và liên kết được vẽ thành những thành phần riêng. Thuộc tính thể hiện trong hình là các thuộc tính tiêu biểu, danh sách đầy đủ nằm trong từ điển dữ liệu. Số 1 chỉ một đối tượng, N và M chỉ nhiều đối tượng, còn 0..1 chỉ quan hệ có thể không phát sinh. Các đường ER mô tả liên kết dữ liệu, vì vậy được đọc theo tên liên kết và bội số, không được hiểu là luồng xử lý như mũi tên DFD. [5]',
366:'Danh mục phân loại sản phẩm. Mỗi sản phẩm có thể dùng nhiều nguyên liệu theo công thức, còn một nguyên liệu có thể xuất hiện trong nhiều sản phẩm. Định mức gắn với liên kết giữa sản phẩm và nguyên liệu. Khu vực và bàn được quản lý thành hai thực thể riêng để xác định vị trí phục vụ. [6]',
369:'Hóa đơn gồm các dòng món và có thể được thanh toán bằng nhiều lần phân bổ. Mỗi dòng món thuộc một hóa đơn và chọn một sản phẩm. Khách hàng, bàn, khuyến mãi và đối tác giao hàng là các liên kết tùy chọn tùy loại đơn. Một hóa đơn hợp lệ phải có món, còn hóa đơn có số phải trả bằng không có thể không phát sinh dòng thu tiền. [6]',
372:'Các mô hình kho, tài chính, nhân sự và khách hàng tiếp tục dùng cùng cách biểu diễn thực thể và liên kết. Một nguyên liệu có thể xuất hiện trong nhiều chứng từ kho, nhưng số lượng và đơn giá thuộc về lần nhập hoặc xuất cụ thể. Khoản nợ phát sinh từ một phiếu nhập và các lần trả nợ gắn với phiếu chi. Vai trò tài khoản được tách khỏi chức vụ của nhân viên. [6]',
374:'Khi chuyển sang mô hình quan hệ, mỗi thực thể độc lập được lưu bằng một bảng có khóa chính. Liên kết một với nhiều được thể hiện bằng khóa ngoại ở phía nhiều. Liên kết nhiều với nhiều cần một quan hệ trung gian chứa khóa của hai phía cùng các thuộc tính của liên kết. Những quan hệ tùy chọn được thể hiện bằng khóa ngoại cho phép NULL. [5]',
375:'Trong thiết kế của đề tài, CONGTHUC lưu cặp sản phẩm và nguyên liệu cùng định mức. Các dòng chứng từ kho dùng mã phiếu kết hợp mã nguyên liệu. Riêng dòng hóa đơn và lần phân bổ thanh toán có định danh riêng để không làm mất các dòng cùng món hoặc cùng phương thức. Danh sách quan hệ dưới đây được trình bày bằng ký hiệu thuộc tính, không lặp lại các hình nối khóa ngoại theo từng nhóm. [6]',
417:'Quy ước trình bày: Thuộc tính gạch chân tham gia khóa chính, thuộc tính in nghiêng là khóa ngoại. Một thuộc tính có thể đồng thời có cả hai đặc điểm. Danh sách thể hiện cấu trúc nền và cần được đọc cùng các bổ sung của những bản cập nhật cơ sở dữ liệu. [6]',
428:'Các hóa đơn, chứng từ kho và phiếu thu chi đều lưu nhân viên lập để xác định người thực hiện. Phiếu thu chi có thể gắn nguồn phiếu nhập hoặc bảng lương, trong đó mỗi nguồn được kiểm soát để tránh ghi chi trùng. Một giao dịch điều chỉnh điểm có thể không gắn hóa đơn vì phát sinh từ thao tác quản lý có lưu lý do. [6]',
429:'Hai khung nhìn quan trọng là vw_TrangThaiBan và vw_CongNoNCC. Trạng thái bàn được suy từ đơn đang mở, còn công nợ được tính từ nợ gốc và lịch sử trả. Chương trình không lưu thêm một trạng thái hoặc số dư độc lập để thay thế các kết quả này. Khi xem số dư tại một ngày trong quá khứ, việc tổng hợp phải giới hạn giao dịch theo ngày được chọn. [6]',
431:'Để phân tích chuẩn hóa, xét quan hệ giả định R gồm mã dòng, mã hóa đơn, ngày lập, mã và tên nhân viên, mã và tên khách hàng, mã và tên sản phẩm, số lượng, giá bán dòng cùng ghi chú. Đây là quan hệ dùng để lập luận, không phải bảng đã từng tồn tại trong chương trình. Mã dòng được dùng làm định danh để giữ được nhiều dòng của cùng một sản phẩm. [6]',
432:'Mỗi ô của R chứa một giá trị đơn nên đáp ứng cách biểu diễn của dạng chuẩn thứ nhất. Nếu chỉ xét mã dòng là khóa ứng viên, không có phụ thuộc vào một phần khóa ghép. Tuy nhiên, mã hóa đơn xác định ngày lập và người lập, mã nhân viên xác định tên nhân viên, còn mã sản phẩm xác định tên sản phẩm. Các thông tin này lặp trên nhiều dòng và có thể gây bất nhất khi thay đổi. [6]',
433:'Việc tách hóa đơn, nhân viên, sản phẩm và khách hàng thành các quan hệ riêng giúp quản lý thông tin mô tả tại đúng nơi phát sinh. Dòng hóa đơn chỉ giữ những thuộc tính của lần gọi món và khóa tham chiếu cần thiết. Mỗi phép tách dựa trên phụ thuộc đã xác định và dùng định danh chung để nối lại thông tin, thay vì chia cột tùy ý theo tên gọi. [7]',
437:'Giá bán của một sản phẩm có thể thay đổi theo thời gian, vì vậy mã sản phẩm không xác định duy nhất giá của mọi lần bán. Mã dòng mới xác định được giá đã áp dụng cho dòng đó. Tương tự, lượng tiêu hao đã lưu theo hóa đơn không được tính lại từ công thức mới. Những dữ liệu này được giữ vì yêu cầu lịch sử giao dịch. [6]',
438:'Một số thuộc tính dẫn xuất vẫn được lưu hoặc tính trong cơ sở dữ liệu để phục vụ nghiệp vụ. Thành tiền phụ thuộc tiền món, phí giao hàng và giảm giá. Điểm hiện tại, tồn kho và các thành phần lương cũng cần cơ chế duy trì nhất quán. Vì vậy, báo cáo phân tích các quan hệ đại diện và chỉ ra những dữ liệu cần bảo toàn, không kết luận toàn bộ cấu trúc vật lý đã đạt dạng chuẩn thứ ba chỉ vì có khóa chính. [6]',
443:'Tầng giao diện tiếp nhận thao tác và hiển thị kết quả. Tầng nghiệp vụ kiểm tra quyền, chuẩn hóa yêu cầu và điều phối công việc. Tầng truy cập dữ liệu tạo tham số, gọi thủ tục và ánh xạ kết quả. SQL Server lưu kết quả chính thức về tiền, kho, điểm và trạng thái, còn mô hình dữ liệu dùng chung cung cấp các kiểu trao đổi giữa các tầng. [6]',
444:'Sau khi xác thực thành công, chương trình mở cửa sổ chính theo vai trò. Màn đăng nhập phân biệt sai thông tin tài khoản với lỗi kết nối. Ảnh lưu ngày 17/10/2026 dưới đây được dùng để minh họa bố cục và cách báo trạng thái của phiên bản đó, không dùng để chứng minh toàn bộ chức năng hiện tại đã được nghiệm thu. [12]',
447:'Cửa sổ chính có menu bên trái, vùng thông tin người dùng và vùng làm việc theo chức năng. Menu thay đổi theo vai trò nhưng quyền thao tác vẫn được kiểm tra ở tầng nghiệp vụ. Ảnh lưu ngày 22/10/2026 minh họa màn khách hàng với dữ liệu mẫu của dự án. [6]',
450:'Màn bán hàng chia thành vùng chọn bàn hoặc loại đơn, vùng tìm món và vùng chi tiết đơn. Các phím F2, F3, F4 và F8 lần lượt hỗ trợ tìm món, chọn khách, xem đơn mở và mở hộp thanh toán. Phím tắt chỉ đưa người dùng đến công việc tương ứng, không trực tiếp chốt tiền. Ảnh đúng phiên bản của màn này được giữ ở vị trí chờ bổ sung. [2]',
451:'Vị trí bổ sung ảnh màn bán hàng từ chương trình thật, thể hiện loại đơn, các dòng món và tổng tiền.',
453:'Màn danh sách cung cấp tìm kiếm, bộ lọc và bảng dữ liệu. Người dùng chọn một dòng để mở hộp thêm, sửa hoặc xem chi tiết. Hộp thoại có cửa sổ chủ, thanh tiêu đề và nút đóng của Windows. Khi đóng hộp con, dữ liệu và vị trí làm việc của cửa sổ cha được giữ theo luồng sử dụng. [2]',
454:'Dữ liệu được kiểm tra ở cả giao diện, tầng nghiệp vụ và cơ sở dữ liệu. Giao diện báo lỗi gần ô nhập và giữ nội dung để sửa. Tầng nghiệp vụ tiếp tục kiểm tra quyền cùng quy tắc dữ liệu. SQL Server bảo vệ khóa, tham chiếu và phiên bản, nhờ đó thao tác từ một màn khác vẫn không bỏ qua các ràng buộc. [6]',
455:'Ảnh hộp thêm danh mục lưu ngày 17/10/2026 minh họa một hộp nhập đã có trong chương trình. Với quy tắc nhập hiện tại, khi người dùng vừa vào ô bằng chuột hoặc phím Tab, nội dung được chọn để có thể gõ thay. Trong cùng lần nhập, các lần bấm tiếp vẫn cho phép đặt con trỏ và chọn đoạn bình thường. [2]',
461:'Nếu dữ liệu đã được thay đổi ở một cửa sổ khác, chương trình yêu cầu tải lại để người dùng đối chiếu. Các danh mục đã phát sinh giao dịch được bảo vệ theo ràng buộc tham chiếu và quy tắc nghiệp vụ. Hệ thống không xóa lịch sử chỉ để cho phép xóa một dòng danh mục. [6]',
463:'Hộp thanh toán hiển thị tiền món, nguồn ưu đãi, giảm giá, phí giao hàng và số phải trả. Người dùng phân bổ số tiền cho các phương thức được hỗ trợ. Trong lúc ghi, chương trình chặn gửi lặp. Nếu chưa biết kết quả do mất kết nối, người dùng phải đối chiếu giao dịch, còn hóa đơn đã chốt chỉ được xem hoặc in lại. [2]',
464:'Vị trí bổ sung ảnh hộp thanh toán từ chương trình thật, có số phải trả, các dòng phân bổ và nút xác nhận.',
465:'Lịch sử hóa đơn cho phép lọc theo ngày, mở chi tiết và in lại từ dữ liệu đã lưu. Giá trên hóa đơn lấy theo lần bán, còn doanh thu được xác định theo ngày thanh toán. Khi tổng hợp, các dòng món và các dòng thanh toán được tính riêng trước khi ghép để tránh nhân lặp số tiền của hóa đơn có nhiều món và nhiều phương thức. [6]',
466:'Vị trí bổ sung ảnh lịch sử hóa đơn và cửa sổ chi tiết hoặc xem trước bản in, sử dụng cùng một hóa đơn mẫu.',
467:'Phần kho có các màn theo dõi tồn, nhập, xuất và kiểm kê. Nhân viên soạn nhiều dòng nguyên liệu trước khi xác nhận, chứng từ đã chốt không được sửa tài chính. Màn công nợ cung cấp số dư và lịch sử trả. Nhân viên kho được xem theo quyền, còn người quản lý mới thực hiện trả nợ. [2]',
468:'Vị trí bổ sung ảnh phiếu nhập, kiểm kê và công nợ từ đúng phiên bản chương trình đang được mô tả.',
472:'Kết quả giao dịch được tách khỏi kết quả hiển thị hoặc in. Một hóa đơn đã ghi thành công vẫn giữ trạng thái đã chốt dù máy in gặp lỗi. Khi mất kết nối sau khi gửi yêu cầu, chương trình kiểm tra lại giao dịch trước khi cho phép thực hiện tiếp, nhờ đó tránh ghi tiền hoặc kho lần thứ hai. [2]',
474:'Trang Tổng quan giúp người quản lý theo dõi kết quả kinh doanh và các công việc cần xử lý từ cùng một màn hình. Người dùng chọn Hôm nay hoặc Tháng này để xem chỉ tiêu trong khoảng thời gian tương ứng. Khi làm mới, chương trình đọc lại dữ liệu và hiển thị thời điểm cập nhật cùng kỳ so sánh. [14]',
475:'Bốn chỉ tiêu chính là doanh thu đã thanh toán, số hóa đơn đã thanh toán, bình quân mỗi hóa đơn và thực chi. Doanh thu lấy theo ngày thanh toán, sau giảm giá và gồm phí giao hàng. Thực chi lấy từ các phiếu chi trong kỳ. Chênh lệch được so với kỳ trước đến mốc thời gian tương ứng. Hệ thống không gọi phần chênh thu chi là lợi nhuận vì chưa có đầy đủ giá vốn lịch sử. [14]',
476:'Khu vực phân tích gồm xu hướng doanh thu, cơ cấu theo kênh bán và năm món bán chạy nhất theo số ly. Từ các chỉ tiêu có hỗ trợ, người dùng mở chứng từ nguồn để đối chiếu. Khoảng ngày và điều kiện đã chọn được giữ khi chuyển sang dữ liệu chi tiết. [14]',
477:'Nhóm thông tin cần chú ý cho biết số đơn đang mở, đơn giao chưa hoàn tất, nguyên liệu hết tồn và công nợ hiện tại. Đây là tình trạng tại thời điểm đọc, không phải số phát sinh riêng của kỳ đang xem. Khi truy vấn gặp lỗi, giao diện không dùng số không để thay cho kết quả chưa lấy được. [14]',
478:'Các biểu đồ và chỉ tiêu được xây dựng bằng điều khiển WinForms cùng truy vấn tổng hợp có tham số. Hồ sơ triển khai ghi nhận phạm vi đã xây dựng và các kiểm tra kỹ thuật đã thực hiện. Việc mô tả chức năng trong báo cáo không đồng nghĩa mọi tình huống nghiệm thu của phiên bản hiện tại đã hoàn tất. [14]',
479:'Vị trí bổ sung ảnh trang Tổng quan từ chương trình thật, thể hiện kỳ xem, các chỉ tiêu, biểu đồ và thông tin cần chú ý.',
481:'Ứng dụng có hai lựa chọn ngôn ngữ là tiếng Việt và tiếng Anh. Tại màn đăng nhập, người dùng đổi bằng nút VI hoặc EN mà vẫn giữ tên đăng nhập đang nhập. Việc đổi ngôn ngữ không tự gửi yêu cầu xác thực. Nếu lựa chọn không lưu được, chương trình hiển thị thông báo để người dùng xử lý. [14]',
482:'Trong phiên làm việc, người dùng mở Thông tin hệ thống, chọn ngôn ngữ rồi bấm Áp dụng. Lựa chọn được lưu theo tài khoản Windows. Chương trình cho biết thay đổi sẽ có hiệu lực khi đăng xuất hoặc mở lại, đồng thời cho phép đăng xuất ngay hoặc thực hiện sau. Các màn nghiệp vụ đang mở không bị dựng lại giữa phiên. [14]',
483:'Bản dịch được lưu cùng ứng dụng và tiếng Việt là ngôn ngữ mặc định. Nếu chưa có bản dịch phù hợp, chương trình giữ nội dung tiếng Việt. Tên khách hàng, địa chỉ và ghi chú do người dùng nhập không bị tự dịch. Mã chứng từ, mã quyền và trạng thái lưu trong cơ sở dữ liệu vẫn giữ nguyên. [14]',
484:'Tiền được trình bày bằng số kèm chữ đ thông thường, chẳng hạn 81.000 đ. Ngày dùng dạng dd/MM/yyyy và giờ dùng dạng HH:mm ở cả hai ngôn ngữ. Việc dịch các nhãn trên giao diện, bản xem trước và thông báo không làm thay đổi số tiền, quyền thao tác hoặc kết quả giao dịch. Chức năng hoạt động bằng từ điển trong ứng dụng, không cần dịch trực tuyến. [14]',
485:'Hồ sơ triển khai ghi nhận các kiểm tra về từ điển, tham số định dạng, lưu lựa chọn và giữ nguyên dữ liệu nghiệp vụ. Những kiểm tra này có phạm vi riêng và không được dùng thay cho toàn bộ nghiệm thu chức năng trên WinForms và SQL Server. [14]',
486:'Vị trí bổ sung ảnh lựa chọn ngôn ngữ tại màn đăng nhập và trong Thông tin hệ thống. Ảnh dùng dữ liệu mẫu giống nhau để thể hiện rõ sự thay đổi của nhãn hiển thị.',
488:'Báo cáo sử dụng mã nguồn, thiết kế dữ liệu và hồ sơ triển khai làm căn cứ mô tả chương trình. Các ảnh được ghi rõ mốc lưu để người đọc phân biệt bố cục của phiên bản cũ với chức năng đang được trình bày. Kết quả kiểm tra chỉ có giá trị trong phạm vi phiên bản và tình huống đã ghi nhận. [12]',
489:'Những vị trí chưa có ảnh phù hợp được để chờ bổ sung từ chương trình thật. Các mô hình phân tích và thiết kế được xây dựng theo nghiệp vụ của dự án, còn thông tin khảo sát thực tế được giữ đúng giới hạn đã xác nhận. Báo cáo không dùng hình giao diện đề xuất hoặc số liệu tự dựng để thay cho kết quả triển khai. [6]',
491:'Bài tập lớn đã xác định phạm vi quản lý một quán cà phê với một kho chung, phân chia chức năng và mô tả các luồng dữ liệu giữa tác nhân, tiến trình và hồ sơ. Phần thiết kế dữ liệu làm rõ thực thể, thuộc tính, liên kết và cách chuyển sang mô hình quan hệ. Các quyết định giữ giá bán lịch sử, định danh riêng dòng món và kiểm soát nguồn chi giúp hệ thống duy trì dữ liệu nhất quán. [1]',
492:'Phần xây dựng chương trình trình bày giao diện và luồng xử lý dựa trên mã nguồn của dự án. Thanh toán gắn với tiền, kho và điểm trong cùng giao dịch. Nhập hàng gắn với tồn, tiền trả ngay và công nợ. Trang Tổng quan hỗ trợ đối chiếu tình hình kinh doanh, còn lựa chọn tiếng Việt và tiếng Anh thay đổi cách hiển thị mà vẫn giữ nguyên dữ liệu nghiệp vụ. [6], [14]',
493:'Để hoàn thiện báo cáo, cần bổ sung kết quả khảo sát trực tiếp, cơ cấu tổ chức được xác nhận và ảnh chương trình đúng phiên bản. Đối với phần mềm, cần tiếp tục kiểm tra các tình huống còn thiếu và xử lý lỗi theo hồ sơ dự án trước khi kết luận đủ điều kiện vận hành. Hướng phát triển vẫn nằm trong phạm vi một quán, không mở rộng sang thanh toán trực tuyến hoặc kế toán giá vốn khi chưa có dữ liệu và thiết kế tương ứng. [12]'
}
REWRITE[202]=REWRITE[202].replace('[3]','[6]')
REWRITE[206]=REWRITE[206].replace('[3]','[1]')
REWRITE[363]=REWRITE[363].replace('[5]','[6]')
REWRITE[145]='CoffeeManager được phân nhóm theo công việc như bán hàng, quản lý kho và công nợ. Cách chia này gắn hệ thống với nhu cầu sử dụng thay vì tạo một chức năng riêng cho mỗi bảng dữ liệu. [1]'
REWRITE[159]='CoffeeManager dùng C# WinForms trên .NET 10 và truy cập SQL Server bằng Microsoft.Data.SqlClient. Giao diện, nghiệp vụ, truy cập dữ liệu và mô hình dữ liệu được tách trách nhiệm. Cấu hình kết nối và tài khoản phát triển không được công bố trong báo cáo. [6]'
REWRITE[148]='Mức ngữ cảnh biểu diễn toàn bộ phần mềm bằng một tiến trình. Mức đỉnh tách thành các chức năng chính, còn mức dưới đỉnh làm rõ từng chức năng. Khi phân rã, phải giữ đủ nguồn gửi, nơi nhận và nội dung các luồng qua ranh giới tiến trình cha, không thêm hoặc làm mất tác nhân ngoài. [4]'
REWRITE[237]='Phân hệ này quản lý hồ sơ nhân sự và tài sản, đồng thời tổng hợp dữ liệu bán hàng, kho, công nợ và thu chi để lập báo cáo. Nguồn chi lương được gửi sang chức năng tài chính và nhận lại liên kết phiếu chi. [1]'
for i,t in REWRITE.items():settext(original[i],t)

# Replace compact code-heavy tables with prose labels.
def replace_table(old,headers,rows):
 tbl=cp(old)
 for tr in list(tbl.findall(q('tr'))):tbl.remove(tr)
 template=old.findall(q('tr'))[-1]
 for ri,row in enumerate([headers]+rows):
  tr=cp(template)
  for extra in tr.xpath('./w:trPr/w:tblHeader',namespaces=NS):extra.getparent().remove(extra)
  if ri==0:
   trpr=tr.find(q('trPr'))
   if trpr is None:trpr=E.Element(q('trPr'));tr.insert(0,trpr)
   sub(trpr,'tblHeader')
  cells=tr.findall(q('tc'))
  for c,t in zip(cells,row):
   ps=c.findall(q('p'));p=ps[0]
   for x in list(c):
    if x.tag!=q('tcPr'):c.remove(x)
   c.append(p);settext(p,t)
   if ri==0:
    for rp in p.findall('.//'+q('rPr')):sub(rp,'b')
  tbl.append(tr)
 old.getparent().replace(old,tbl)
 return tbl

ABBR=[('BFD','Business Function Diagram – Sơ đồ phân cấp chức năng'),('DFD','Data Flow Diagram – Biểu đồ luồng dữ liệu'),('ER','Entity Relationship – Thực thể liên kết'),('CSDL','Cơ sở dữ liệu'),('PK','Primary Key – Khóa chính'),('FK','Foreign Key – Khóa ngoại'),('BLL','Business Logic Layer – Tầng xử lý nghiệp vụ'),('DAL','Data Access Layer – Tầng truy cập dữ liệu'),('DTO','Data Transfer Object – Đối tượng truyền dữ liệu'),('TVP','Table-Valued Parameter – Tham số dạng bảng'),('POS','Point of Sale – Màn hình bán hàng'),('NCC','Nhà cung cấp'),('NVL','Nguyên vật liệu'),('ADMIN','Vai trò người quản lý'),('CASH','Vai trò nhân viên thu ngân'),('STORE','Vai trò nhân viên kho')]
replace_table(original[129],['Từ viết tắt','Diễn giải'],ABBR)
replace_table(original[169],['Vai trò','Công việc được hỗ trợ','Giới hạn'],[
 ('Người quản lý','Quản lý dữ liệu, bán hàng, kho và tài chính theo quyền.','Không sửa tài chính đã chốt hoặc tự nhập lại số dư.'),('Nhân viên thu ngân','Lập đơn, chọn khách, thanh toán, tra cứu, in và theo dõi giao hàng.','Không thay đổi chính sách, ghi kho, trả nợ hoặc chi lương.'),('Nhân viên kho','Quản lý nguyên liệu, nhập, xuất, kiểm kê và xem công nợ.','Không trả nợ hoặc ghi khoản chi của người quản lý.')])
replace_table(original[197],['Tiến trình','Tác nhân','Thông tin nhận và trả','Hồ sơ sử dụng'],[
 ('Xác thực và quản lý phiên','Cả ba vai trò','Nhận thông tin đăng nhập và yêu cầu phiên. Trả kết quả xác thực và trạng thái phiên.','Tài khoản và quyền.'),
 ('Quản lý dữ liệu nền và khách hàng','Người quản lý, thu ngân và nhân viên kho theo quyền','Nhận yêu cầu cập nhật hoặc tra cứu. Trả dữ liệu và kết quả xử lý theo quyền.','Danh mục nền, hồ sơ khách hàng và điểm.'),
 ('Bán hàng và thanh toán','Người quản lý, nhân viên thu ngân','Nhận thông tin đơn bán. Trả hóa đơn, kết quả thu tiền, bản in và trạng thái giao.','Hồ sơ bán hàng, danh mục, khách hàng và kho.'),
 ('Quản lý kho','Người quản lý, nhân viên kho','Nhận yêu cầu nhập, xuất hoặc kiểm kê. Trả chứng từ và tồn sau xử lý.','Hồ sơ kho và nhà cung cấp.'),
 ('Quản lý thu chi và công nợ','Người quản lý, nhân viên kho chỉ tra cứu nợ','Nhận yêu cầu tài chính. Trả phiếu thu chi, liên kết nguồn và số dư.','Chứng từ thu chi và hồ sơ công nợ.'),
 ('Quản lý nhân sự, tài sản và báo cáo','Người quản lý','Nhận hồ sơ hoặc điều kiện xem báo cáo. Trả kết quả quản lý và báo cáo.','Hồ sơ nhân sự, tài sản và các nguồn tổng hợp.')])
replace_table(original[200],['Kho dữ liệu','Ý nghĩa','Bảng lưu trữ tiêu biểu'],[
 ('Tài khoản và quyền','Lưu danh tính và vai trò dùng để xác thực.','TAIKHOAN, VAITRO và NHANVIEN.'),('Danh mục nền','Cung cấp các danh mục và chính sách dùng chung.','SANPHAM, CONGTHUC, BAN, KHUYENMAI và các danh mục liên quan.'),('Hồ sơ khách hàng và điểm','Lưu thông tin khách, hạng và lịch sử điểm.','KHACHHANG, HANGTHANHVIEN, LICHSU_DIEM.'),('Hồ sơ bán hàng','Lưu đơn bán, các dòng món và tiền thu.','HOADON, CHITIET_HD, CHITIET_THANHTOAN.'),('Hồ sơ kho','Lưu nguyên liệu, tồn đầu kỳ và các chứng từ thay đổi kho.','NGUYENVATLIEU, MaterialOpening và các phiếu kho.'),('Hồ sơ công nợ','Lưu nợ phát sinh và các lần trả.','CONGNO_NCC, THANHTOAN_CONGNO.'),('Chứng từ thu chi','Lưu tiền thu, tiền chi và liên kết nguồn.','PHIEUTHUCHI.'),('Hồ sơ nhân sự và tài sản','Lưu nhân viên, chấm công, lương và tài sản.','NHANVIEN, CHAMCONG, BANGLUONG, TAISAN_CODINH.')])
replace_table(original[204],['Công việc','Thông tin gửi vào','Kết quả nhận lại'],[
 ('Đăng nhập và quản lý phiên','Tên đăng nhập, mật khẩu và yêu cầu kiểm tra hoặc kết thúc phiên.','Kết quả xác thực, quyền và trạng thái phiên.'),('Dữ liệu nền và khách hàng','Thông tin danh mục, chính sách, hồ sơ khách hoặc yêu cầu tra cứu.','Danh mục, hồ sơ và kết quả cập nhật được phép.'),('Bán hàng','Thông tin đơn, món, ưu đãi, phân bổ tiền, yêu cầu in hoặc giao hàng.','Hóa đơn, số phải trả, kết quả thanh toán, bản in và trạng thái giao.'),('Kho','Nguyên liệu, dòng nhập, dòng xuất, số đếm và thông tin nhà cung cấp.','Chứng từ kho, tồn sau xử lý và nguồn chi hoặc nợ phát sinh.'),('Thu chi và công nợ','Khoản thu chi, nguồn nhập hàng, nguồn chi lương hoặc yêu cầu trả nợ.','Phiếu thu chi, liên kết nguồn, số dư và lịch sử trả.'),('Nhân sự, tài sản và báo cáo','Hồ sơ nhân viên, giờ công, kỳ lương, tài sản hoặc điều kiện tổng hợp.','Hồ sơ đã cập nhật và báo cáo theo yêu cầu.')])
settext(original[203],'Bảng 3.4. Thông tin trao đổi trong các công việc')
# This caption has a SEQ field in the source; it is recreated below with all captions.

replace_table(original[459],['Dữ liệu cập nhật','Nội dung kiểm tra','Kết quả'],[
 ('Sản phẩm và công thức','Kiểm tra tên, danh mục, giá bán, định mức và nguyên liệu tham chiếu.','Lưu thông tin hợp lệ, giữ nguyên giá của các lần bán cũ.'),('Nguyên liệu','Kiểm tra tên, đơn vị tính và điều kiện mở sổ.','Lưu hồ sơ, tồn được thay đổi theo giao dịch.'),('Khách hàng','Kiểm tra tên và số điện thoại theo ràng buộc.','Lưu hồ sơ và áp dụng hạng, điểm theo chính sách.'),('Danh mục nền','Kiểm tra độ dài, trùng tên và dữ liệu đang tham chiếu.','Lưu thay đổi hoặc giải thích lý do từ chối.')])
replace_table(original[470],['Trạng thái','Biểu hiện','Cách xử lý'],[
 ('Đang tải','Truy vấn chưa hoàn tất.','Chờ kết quả, không dùng số không để thay lỗi.'),('Không có dữ liệu','Truy vấn thành công nhưng không có dòng phù hợp.','Đổi điều kiện tìm kiếm khi cần.'),('Lỗi nhập','Dữ liệu không thỏa điều kiện, nội dung nhập được giữ lại.','Sửa trường được báo lỗi rồi xác nhận.'),('Xung đột dữ liệu','Thông tin đã thay đổi ở lần làm việc khác.','Tải lại, đối chiếu và xác nhận dữ liệu mới.'),('Đang ghi','Yêu cầu đã được gửi, thao tác gửi lặp bị khóa.','Chờ kết quả, không tạo yêu cầu mới.'),('Chưa biết kết quả','Mất kết nối hoặc hết thời gian chờ sau khi gửi.','Đối chiếu mã yêu cầu và chứng từ trước khi thử lại.'),('Đã chốt','Giao dịch đã được ghi thành công.','Xem hoặc in lại, không ghi thêm tiền hay kho.'),('Lỗi in sau chốt','Giao dịch thành công nhưng chưa in được.','Kiểm tra máy in và in lại chứng từ đã lưu.')])
replace_table(original[435],['Quan hệ và khóa','Phụ thuộc cần xét','Nhận xét'],[
 ('DANHMUC_SP, khóa MaDM','MaDM xác định TenDM. TenDM cũng là giá trị duy nhất.','Không có thuộc tính mô tả phụ thuộc bắc cầu trong quan hệ này.'),
 ('CONGTHUC, khóa MaSP và MaNVL','Cặp MaSP và MaNVL xác định DinhMuc.','Định mức thuộc cả sản phẩm lẫn nguyên liệu.'),
 ('CHITIET_PN, khóa MaPN và MaNVL','Cặp MaPN và MaNVL xác định SoLuong và DonGiaNhap.','Đơn giá thuộc lần nhập, không chỉ thuộc nguyên liệu.'),
 ('CHITIET_HD, khóa MaDong','MaDong xác định các thuộc tính của dòng món.','Giữ được nhiều dòng cùng món và giá theo từng lần bán.'),
 ('CONGNO_NCC, khóa MaCN','MaCN xác định khoản nợ. MaPN là một khóa ứng viên khác.','Nhà cung cấp được xác định qua phiếu nhập.'),
 ('THANHTOAN_CONGNO, khóa MaTT','MaTT xác định MaCN, MaTC và MaPT.','Số tiền được lấy từ phiếu chi liên kết.'),
 ('HOADON, khóa MaHD','MaHD xác định hóa đơn. Thành tiền phụ thuộc các thành phần tiền.','Cần xét riêng thuộc tính dẫn xuất khi đánh giá dạng chuẩn.')])

# Insert regenerated figures as inline objects with captions kept on the same page.
rels=E.fromstring(parts['word/_rels/document.xml.rels']);PR='{http://schemas.openxmlformats.org/package/2006/relationships}'
nextid=1000
def imagepara(name):
 global nextid
 nextid+=1;rid='rIdRevision'+str(nextid);media='media/revision-'+name+'.png'
 E.SubElement(rels,PR+'Relationship',Id=rid,Type=R+'/image',Target=media)
 parts['word/'+media]=(FIG/(name+'.png')).read_bytes()
 p=cp(original[191]);
 for x in p.xpath('.//a:blip',namespaces=NS):x.set('{'+R+'}embed',rid)
 w,h=Image.open(FIG/(name+'.png')).size
 width=13.8 if name=='er-customers' else 15.5
 height=width*h/w
 if height>21:width*=21/height;height=21
 cx=int(width*360000);cy=int(height*360000)
 for x in p.xpath('.//wp:extent|.//a:xfrm/a:ext',namespaces=NS):x.set('cx',str(cx));x.set('cy',str(cy))
 for x in p.xpath('.//wp:docPr|.//pic:cNvPr',namespaces=NS):x.set('id',str(nextid));x.set('name',name);x.set('descr',name)
 setprop(p,'ind',{'firstLine':0});setprop(p,'jc',{'val':'center'});setprop(p,'keepNext',{'val':1});setprop(p,'keepLines',{'val':1});setprop(p,'spacing',{'before':60,'after':60,'line':240,'lineRule':'auto'})
 return p
caption_template=original[192]
def caption(title):
 p=paragraph('Hình 0.0. '+title,caption_template);setprop(p,'keepNext',{'val':0});setprop(p,'keepLines',{'val':1});setprop(p,'ind',{'firstLine':0});return p
def replace_figure(index,names_titles):
 old=original[index];cap=original[index+1];at=body.index(old)
 body.remove(old);body.remove(cap)
 for name,title in names_titles:
  body.insert(at,imagepara(name));body.insert(at+1,caption(title));at+=2
  note={
   'er-stock':'Các thuộc tính số lượng và đơn giá gắn với liên kết giữa chứng từ và nguyên liệu vì chúng thay đổi theo từng lần nhập hoặc xuất. Số đếm và tồn hệ thống cũng thuộc lần kiểm kê cụ thể. Khi chuyển sang bảng, những liên kết này trở thành các bảng chi tiết chứng từ. [6]',
   'er-finance':'Mỗi khoản nợ gắn với một phiếu nhập và có thể được trả nhiều lần. Mỗi lần trả sử dụng đúng một phiếu chi, còn một phiếu chi có thể không phải khoản trả nợ. Liên kết với tài sản, phiếu nhập hoặc bảng lương chỉ xác định nguồn của khoản chi, không tự làm phát sinh thêm một khoản tiền. [6]'
  }.get(name)
  if note:body.insert(at,paragraph(note,original[366]));at+=1
replace_figure(191,[('bfd','Sơ đồ phân cấp chức năng')])
replace_figure(209,[('context','DFD mức ngữ cảnh')])
replace_figure(213,[(f'top-{k}','DFD mức đỉnh về '+t) for k,t in [('auth','xác thực'),('catalogue','dữ liệu nền và khách hàng'),('sales','bán hàng'),('inventory','quản lý kho'),('money','thu chi và công nợ'),('people','nhân sự, tài sản và báo cáo')]])
for idx in [215,216]:body.remove(original[idx])
for idx,k,title in [(220,'auth','xác thực và quản lý phiên'),(223,'catalogue','dữ liệu nền và khách hàng'),(226,'sales','bán hàng và thanh toán'),(229,'inventory','quản lý kho'),(232,'money','thu chi và công nợ'),(235,'people','nhân sự, tài sản và báo cáo')]:replace_figure(idx,[(f'detail-{k}','DFD phân rã '+title)])
replace_figure(364,[('er-menu','ER về sản phẩm, nguyên liệu và bàn')])
replace_figure(367,[('er-sales','ER về bán hàng và thanh toán')])
replace_figure(370,[('er-stock','ER về nghiệp vụ kho'),('er-finance','ER về thu chi và công nợ'),('er-people','ER về nhân sự và tài khoản'),('er-customers','ER về khách hàng và điểm')])
for idx in range(418,428):body.remove(original[idx])
replace_figure(441,[('architecture','Trao đổi dữ liệu giữa các tầng chương trình')])
parts['word/_rels/document.xml.rels']=dump(rels)

# Numbered, short captions are rebuilt as SEQ fields for automatic lists.
chapter=0;counts={};real_body=False
for p in body.findall(q('p')):
 if p is original[132]:real_body=True
 if not real_body:continue
 t=text(p)
 m=re.match(r'CHƯƠNG (\d)',t)
 if m:chapter=int(m[1])
 m=re.match(r'(Hình|Bảng)\s+\d+\.\d+\.\s*(.*)',t)
 if m and chapter:
  label,title=m.groups();key=(label,chapter);counts[key]=counts.get(key,0)+1
  # Captions must fit one line in the generated lists.
  short={'Giao diện đăng nhập trong ảnh lưu ngày 17 tháng 10 năm 2026':'Màn đăng nhập, ảnh lưu ngày 17/10/2026','Cửa sổ chính và màn khách hàng trong ảnh lưu ngày 22 tháng 10 năm 2026':'Màn khách hàng, ảnh lưu ngày 22/10/2026','Hộp thêm danh mục trong ảnh lưu ngày 17 tháng 10 năm 2026':'Hộp thêm danh mục, ảnh lưu ngày 17/10/2026','Các thay đổi cấu trúc đáng chú ý sau baseline':'Các bổ sung cho cấu trúc dữ liệu nền','Phụ thuộc hàm và nhận xét các quan hệ đại diện':'Phụ thuộc hàm của các quan hệ đại diện'}
  title=short.get(title,title)
  settext(p,f'{label} {chapter}.')
  def fieldpart(kind,value=None):
   r=sub(p,'r')
   if kind=='instruction':x=sub(r,'instrText');x.set('{http://www.w3.org/XML/1998/namespace}space','preserve');x.text=value
   elif kind=='text':sub(r,'t').text=value
   else:sub(r,'fldChar',{'fldCharType':kind})
  fieldpart('begin');fieldpart('instruction',f' SEQ {"Hinh" if label=="Hình" else "Bang"}{chapter} \\* ARABIC ');fieldpart('separate');fieldpart('text',str(counts[key]));fieldpart('end');fieldpart('text','. '+title)
  setprop(p,'ind',{'firstLine':0});setprop(p,'keepNext',{'val':1 if label=='Bảng' else 0});setprop(p,'keepLines',{'val':1})

# Global language cleanup is applied to visible text, never to URLs or source identifiers.
def clean(t):
 t=t.replace('₫','đ').replace('(*)','').replace('→',' xác định ').replace(';',',')
 t=re.sub(r'\s+', ' ',t).strip()
 t=re.sub(r'(?<!https)(?<!http):\s*([a-zà-ỹ])',lambda m:': '+m[1].upper(),t)
 return t
start=False
for p in body.findall(q('p')):
 t=text(p)
 if t.startswith('CHƯƠNG 1 '):start=True
 if not start:continue
 if p.xpath('.//w:drawing|.//w:instrText',namespaces=NS):continue
 if t.startswith('TÀI LIỆU THAM KHẢO'):break
 new=clean(t)
 # Remove shorthand section pointers from narrative paragraphs.
 new=re.sub(r'\s+(?:ở|tại|theo|trong) mục \d+(?:\.\d+)+(?:[–-]\d+(?:\.\d+)+)?','',new)
 if new!=t:settext(p,new)

# Turn the technical dictionary's constraints into readable statements.
for tbl in body.findall(q('tbl')):
 for tc in tbl.xpath('./w:tr/w:tc',namespaces=NS):
  for p in tc.findall(q('p')):
   t=text(p)
   if 'PK;' in t or 'FK →' in t or t in ['NULL','NOT NULL'] or 'Biểu thức tính;' in t:
    t=t.replace('PK;','Khóa chính,').replace('FK →','Tham chiếu').replace('NOT NULL','Bắt buộc').replace('NULL','Có thể để trống').replace(';',',')
   else:
    t=t.replace('→',' xác định ').replace(';','. ')
   t=t.replace('₫','đ').replace('(*)','')
   t=re.sub(r'\.\s*([a-zà-ỹ])',lambda m:'. '+m[1].upper(),t)
   t=re.sub(r':\s*([a-zà-ỹ])',lambda m:': '+m[1].upper(),t)
   if t!=text(p):settext(p,t)

# Source numbering follows the first citation in the revised body.
source_order=[]
active=False
for x in body:
 if x is original[132]:active=True
 if x is original[494]:break
 if not active:continue
 for number in re.findall(r'\[(\d+)\]',text(x)):
  if int(number) not in source_order:source_order.append(int(number))
mapping={old:i+1 for i,old in enumerate(source_order)}
for x in body:
 if x is original[494]:break
 for t in x.xpath('.//w:t',namespaces=NS):
  if t.text:t.text=re.sub(r'\[(\d+)\]',lambda m:f'[{mapping[int(m[1])]}]' if int(m[1]) in mapping else m[0],t.text)

# IEEE bibliography, with hanging numbers and italicized publication titles.
SOURCES={
1:('N. M. Phú, ', 'Kế hoạch dự án WinForms – Quản lí quán cafe', ', phiên bản 3.3. Tài liệu nội bộ CoffeeManager, 2026.'),
2:('N. M. Phú, ', 'Thiết kế UX và hướng dẫn giao diện Orbit Café', '. Tài liệu nội bộ dự án CoffeeManager, 2026.'),
3:('University of Cape Town, Computer Science Department, “Elements of data-flow diagrams,” ', 'MSc-IT Study Material', ', Jan. 2011. Accessed: Nov. 10, 2026. [Online]. Available: https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s05.html'),
4:('University of Cape Town, Computer Science Department, “Context diagrams,” ', 'MSc-IT Study Material', ', Jan. 2011. Accessed: Nov. 10, 2026. [Online]. Available: https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s06.html'),
5:('A. Watt, “The entity relationship data model,” in ', 'Database Design', ', 2nd ed. BCcampus, 2014, ch. 8. Accessed: Nov. 10, 2026. [Online]. Available: https://opentextbc.ca/dbdesign01/chapter/chapter-8-entity-relationship-model/'),
6:('N. M. Phú, ', 'Mã nguồn và thiết kế cơ sở dữ liệu CoffeeManager', '. Tài liệu nội bộ, 2026.'),
7:('A. Watt, “Functional dependencies,” in ', 'Database Design', ', 2nd ed. BCcampus, 2014, ch. 11. Accessed: Nov. 10, 2026. [Online]. Available: https://opentextbc.ca/dbdesign01/chapter/chapter-11-functional-dependencies/'),
8:('A. Watt, “Normalization,” in ', 'Database Design', ', 2nd ed. BCcampus, 2014, ch. 12. Accessed: Nov. 10, 2026. [Online]. Available: https://opentextbc.ca/dbdesign01/chapter/chapter-12-normalization/'),
9:('Microsoft, “Windows Forms Overview,” ', 'Microsoft Learn', '. Accessed: Nov. 10, 2026. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/'),
10:('Microsoft, “Transactions (Transact-SQL),” ', 'Microsoft Learn', '. Accessed: Nov. 10, 2026. [Online]. Available: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql'),
11:('Microsoft, “Table-Valued Parameters,” ', 'Microsoft Learn', '. Accessed: Nov. 10, 2026. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters'),
12:('N. M. Phú, ', 'Hồ sơ triển khai và kiểm thử CoffeeManager', '. Tài liệu nội bộ, cập nhật ngày 10/11/2026.'),
13:('Ther Thẩn Cafe, “Ther Thẩn Cafe,” ', 'FindGlocal', ', bài đăng ngày 01/07/2026. Accessed: Oct. 27, 2026. [Online]. Available: https://www.findglocal.com/VN/Ho-Chi-Minh-City/696962493504088/Ther-Th%E1%BA%A9n-Cafe'),
14:('N. M. Phú, ', 'Tổng quan phân tích và đa ngôn ngữ VI/EN', '. Tài liệu nội bộ CoffeeManager, ngày 09–10/11/2026.')}
for idx in range(495,510):
 if original[idx].getparent() is body and original[idx].tag==q('p'):body.remove(original[idx])
for old in source_order:
 number=mapping[old];a,title,b=SOURCES[old];p=paragraph(f'[{number}]\t'+a,original[495]);
 r=sub(p,'r');rp=sub(r,'rPr');sub(rp,'i');sub(r,'t').text=title
 r=sub(p,'r');sub(r,'t').text=b
 setprop(p,'ind',{'left':567,'hanging':567});setprop(p,'jc',{'val':'left'});setprop(p,'keepNext',{'val':0});setprop(p,'keepLines',{'val':1})
 setprop(p,'spacing',{'before':0,'after':60,'line':300,'lineRule':'exact'})
 tabs=setprop(p,'tabs',{});sub(tabs,'tab',{'val':'left','pos':567})
 body.insert(len(body)-1,p)
for p in body.findall(q('p')):
 if re.match(r'^\[\d+\]',text(p)):
  for t in p.xpath('.//w:t',namespaces=NS):
   if t.text:t.text=t.text.replace('N. M. Phú','Nguyễn Minh Phú')
for index in [490,494]:setprop(original[index],'pageBreakBefore',{'val':1})

# Layout: isolate figures/captions, let tables flow between rows, and prevent
# accidental chains of keep-with-next paragraphs from pushing whole sections.
for i,p in enumerate(body.findall(q('p'))):
 if p in [original[0],original[2]]:continue
 t=text(p);pp=prop(p)
 if p.xpath('.//w:sectPr',namespaces=NS):continue
 if not t and not p.xpath('.//w:drawing|.//w:br|.//w:fldChar|.//w:instrText',namespaces=NS):
  p.getparent().remove(p);continue
 style=p.xpath('./w:pPr/w:pStyle/@w:val',namespaces=NS)
 isheading=bool(style and style[0] in ['2','3','4','5','167']) or bool(re.match(r'^\d+(\.\d+)+\s',t))
 iscap=t.startswith(('Hình ','Bảng ')) and bool(re.match(r'^(Hình|Bảng) \d+\.\d+\.',t))
 if p.xpath('.//w:drawing',namespaces=NS):continue
 if not iscap:
  setprop(p,'keepNext',{'val':1 if isheading else 0})
  setprop(p,'keepLines',{'val':1 if isheading else 0})
  setprop(p,'widowControl',{'val':1})
 if isheading:setprop(p,'ind',{'firstLine':0})
 elif not iscap and not re.match(r'^\[\d+\]',t) and p not in original[:132]:setprop(p,'ind',{'firstLine':720})
 # Body prose has one consistent line and paragraph rhythm.
 if p in original[132:494] and not iscap:setprop(p,'spacing',{'before':120,'after':120,'line':276,'lineRule':'auto'})
for tbl in body.findall(q('tbl')):
 headers=[text(tc) for tc in tbl.findall(q('tr'))[0].findall(q('tc'))]
 widths=None
 if headers[0]=='Kho dữ liệu':widths=[4.0,5.3,6.2]
 elif headers[0]=='Tiến trình':widths=[4.0,3.0,4.5,4.0]
 elif headers[0]=='Công việc':widths=[3.4,6.0,6.1]
 elif headers[0]=='Quan hệ và khóa':widths=[4.0,5.5,6.0]
 if widths:
  grid=tbl.find(q('tblGrid'))
  for col,width in zip(grid,widths):col.set(q('w'),str(round(width*567)))
  for tr in tbl.findall(q('tr')):
   for tc,width in zip(tr.findall(q('tc')),widths):
    tcw=tc.find(q('tcPr')).find(q('tcW'))
    if tcw is not None:tcw.set(q('w'),str(round(width*567)));tcw.set(q('type'),'dxa')
 for ri,tr in enumerate(tbl.findall(q('tr'))):
  trpr=tr.find(q('trPr'))
  if trpr is None:trpr=E.Element(q('trPr'));tr.insert(0,trpr)
  if trpr.find(q('cantSplit')) is None:sub(trpr,'cantSplit')
  for p in tr.xpath('.//w:p',namespaces=NS):
   setprop(p,'keepNext',{'val':1 if ri==0 else 0});setprop(p,'keepLines',{'val':1});setprop(p,'ind',{'firstLine':0});setprop(p,'spacing',{'before':60,'after':60,'line':252,'lineRule':'auto'})
   # Keep the required 13 pt table text and permit breaks between complete rows.
   for rp in p.xpath('.//w:rPr',namespaces=NS):
    for sz in rp.findall(q('sz')):sz.set(q('val'),'26')

# Remove superscript currency and all colored heading/theme overrides.
for p in d.xpath('//w:p',namespaces=NS):
 if 'đ' in text(p):
  for e in p.xpath('.//w:vertAlign|.//w:position',namespaces=NS):e.getparent().remove(e)
for node in d.xpath('//w:color',namespaces=NS):
 node.attrib.clear();node.set(q('val'),'000000')
styles=E.fromstring(parts['word/styles.xml'])
for style in styles:
 sid=style.get(q('styleId'),'');name=style.find(q('name'));name=name.get(q('val'),'') if name is not None else ''
 if sid.startswith('Cover_'):continue
 if re.match(r'(?i)toc [1-9]',name):
  level=int(name[-1]);pp=style.find(q('pPr'))
  if pp is None:pp=sub(style,'pPr')
  for old in pp.findall(q('ind')):pp.remove(old)
  sub(pp,'ind',{'left':(level-1)*360,'firstLine':0,'right':280})
 if 'heading' in name.lower() or sid in ['2','3','4','5','167','168','169']:
  for e in style.xpath('.//w:color',namespaces=NS):e.attrib.clear();e.set(q('val'),'000000')
  for e in style.xpath('.//*[local-name()="textFill"]//*[local-name()="srgbClr"]'):e.set('val','000000')
parts['word/styles.xml']=dump(styles)
parts['word/document.xml']=dump(d)
with ZipFile(DEST,'w',ZIP_DEFLATED) as z:
 for name,b in parts.items():z.writestr(name,b)
(REV/'citation-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'output':str(DEST),'rewritten_paragraphs':len(REWRITE),'references':len(source_order),'caption_counts':{str(k):v for k,v in counts.items()}},ensure_ascii=False))
