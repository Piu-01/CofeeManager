from pathlib import Path
from datetime import datetime
from shutil import copy2
from zipfile import ZipFile, ZIP_DEFLATED
from collections import Counter
import re,json,hashlib,sys
from lxml import etree
from docx import Document
from docx.shared import Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH

sys.stdout.reconfigure(encoding='utf-8')
task=Path(__file__).resolve().parent
root=task.parents[2]
target=root/'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx'
run=task/'revisions/20261110';run.mkdir(parents=True,exist_ok=True)
backup=run/'BaoCao_CoffeeManager-before.docx'
if backup.exists():raise RuntimeError('Backup already exists; do not repeat mutation blindly.')
copy2(target,backup)
doc=Document(target)
before_tables=[[[cell.text for cell in row.cells] for row in table.rows] for table in doc.tables]

def find(text):
    matches=[p for p in doc.paragraphs if p.text==text]
    if len(matches)!=1:raise RuntimeError('Anchor missing/ambiguous: '+text)
    return matches[0]

old=find('4.2.4 Phạm vi đã xây dựng và hướng hoàn thiện')
old.text='4.2.6 Phạm vi đã xây dựng và hướng hoàn thiện'
new_sections=[
('Heading 3','4.2.4 Trang tổng quan quản lý'),
('Normal','Trang Tổng quan dành cho ADMIN được bổ sung để người quản lý theo dõi kết quả kinh doanh và các công việc cần xử lý từ cùng một màn hình. Bản source ngày 10/11/2026 tổ chức thông tin theo hai chế độ Hôm nay và Tháng này. Khi thay đổi kỳ hoặc làm mới, hệ thống đọc lại dữ liệu theo khoảng thời gian tương ứng; thời điểm cập nhật và kỳ so sánh được hiển thị để người xem biết phạm vi của các chỉ tiêu.'),
('Normal','Bốn chỉ tiêu chính gồm doanh thu đã thanh toán, số hóa đơn đã thanh toán, bình quân mỗi hóa đơn và thực chi. Doanh thu lấy theo ngày thanh toán, sau giảm giá và gồm phí giao hàng; thực chi lấy từ các phiếu chi theo ngày lập. Bình quân mỗi hóa đơn được xác định từ doanh thu và số hóa đơn đã thanh toán trong kỳ. Các mức chênh lệch được đối chiếu với kỳ trước tới mốc thời gian tương ứng. Số thu và số chi không được gọi là lợi nhuận vì hệ thống chưa có đầy đủ giá vốn lịch sử và đối soát đối tác.'),
('Normal','Khu vực phân tích có biểu đồ xu hướng doanh thu, biểu đồ cơ cấu doanh thu theo kênh bán và danh sách năm món bán chạy nhất theo số ly của hóa đơn đã thanh toán. Người dùng có thể chọn điểm trên biểu đồ xu hướng, kênh bán hoặc chỉ tiêu có hỗ trợ để mở các chứng từ nguồn tương ứng. Việc tra nguồn giữ khoảng ngày và điều kiện đã chọn, giúp đối chiếu số tổng với dữ liệu chi tiết.'),
('Normal','Nhóm Cần chú ý thể hiện số đơn đang mở, đơn giao hàng chưa xong, nguyên liệu hết tồn và công nợ nhà cung cấp tại thời điểm đọc. Các số này phản ánh tình trạng hiện tại, không phải số phát sinh riêng của kỳ Hôm nay hoặc Tháng này. Khi tải lỗi hoặc kết quả trả về thuộc một lần chọn kỳ cũ, giao diện không dùng số không hoặc số tổng cũ để thay thế dữ liệu của kỳ mới.'),
('Normal','Phần mở rộng Tổng quan được triển khai bằng các control biểu đồ WinForms và các truy vấn tổng hợp có tham số. Theo hồ sơ đối chiếu ngày 10/11/2026, migration 028 bổ sung hai procedure đọc phục vụ phân tích; chưa áp dụng migration này vào cơ sở dữ liệu development tại mốc ghi nhận. Các kiểm tra đã chạy được lưu riêng trong hồ sơ T28, còn giới hạn kiểm chứng theo phiên bản. Vì vậy, mô tả chức năng đã có trong source không được hiểu thành xác nhận mọi ca nghiệm thu đã hoàn tất.'),
('Normal','[UI08 — CHỜ BỔ SUNG ẢNH] Trang Tổng quan quản lý sau cập nhật. Đặt ảnh tại đây, rộng khoảng 15,5 cm; cần ảnh WinForms thật thể hiện chế độ ngày/tháng, bốn chỉ tiêu, xu hướng, cơ cấu kênh và nhóm Cần chú ý. Nếu không đủ độ rõ, chia thành hai hình liên tiếp thay vì thu nhỏ toàn màn. Chưa dùng ảnh thiết kế hoặc số liệu tự dựng để thay ảnh chương trình.'),
('Heading 3','4.2.5 Lựa chọn ngôn ngữ tiếng Việt và tiếng Anh'),
('Normal','Ứng dụng bổ sung hai lựa chọn tiếng Việt và tiếng Anh để thay đổi các nội dung hiển thị. Tại màn đăng nhập, các nút VI và EN cho phép đổi ngôn ngữ ngay trên form, giữ nội dung tên đăng nhập đang nhập và không tự gửi yêu cầu đăng nhập. Nếu không lưu được lựa chọn, chương trình thông báo lỗi để người dùng xử lý.'),
('Normal','Trong phiên làm việc, người dùng mở Thông tin hệ thống, chọn Tiếng Việt hoặc English rồi bấm Áp dụng. Lựa chọn được lưu theo tài khoản Windows đang sử dụng. Chương trình thông báo ngôn ngữ sẽ áp dụng khi đăng xuất hoặc mở lại ứng dụng, đồng thời cho phép chọn Đăng xuất ngay hoặc Để sau. Các màn nghiệp vụ đang mở không bị dựng lại giữa phiên chỉ vì đổi lựa chọn ngôn ngữ.'),
('Normal','Cơ chế dịch sử dụng dịch vụ ngôn ngữ và từ điển nhúng trong ứng dụng. Tiếng Việt là mặc định; khi chưa có bản dịch phù hợp, nội dung gốc tiếng Việt được giữ lại. Dữ liệu người dùng nhập như tên khách, địa chỉ và ghi chú không được tự dịch. Mã chứng từ, mã quyền và mã trạng thái lưu trong CSDL giữ nguyên, chỉ nhãn hiển thị thay đổi theo ngôn ngữ.'),
('Normal','Tiền vẫn được trình bày theo quy ước VND, chẳng hạn 81.000 ₫, ngày theo dd/MM/yyyy và giờ theo HH:mm ở cả hai ngôn ngữ. Các chuỗi hiển thị trên hóa đơn, preview và thông báo lỗi được xử lý qua cùng cơ chế dịch; việc đổi ngôn ngữ không làm thay đổi số tiền, quyền thao tác hoặc kết quả giao dịch. Chức năng không cần dịch trực tuyến và không bổ sung cột ngôn ngữ vào các bảng nghiệp vụ.'),
('Normal','Hồ sơ triển khai ngày 10/11/2026 đã ghi các kiểm tra kỹ thuật về tra từ điển, tham số định dạng, lựa chọn ngôn ngữ và giữ nguyên dữ liệu nghiệp vụ. Đợt nghiệm thu WinForms/SQL của chức năng VI/EN chưa được ghi là đã hoàn tất tại mốc đối chiếu. Phạm vi diễn giải trong báo cáo vì vậy giữ riêng chức năng đã triển khai với kết quả kiểm chứng đã có.'),
('Normal','[UI09 — CHỜ BỔ SUNG ẢNH] Lựa chọn ngôn ngữ VI/EN. Đặt hai ảnh thật sau phần mô tả: màn đăng nhập với nút VI/EN và mục Ngôn ngữ trong Thông tin hệ thống. Mỗi ảnh rộng khoảng 12–15,5 cm theo bố cục; giữ cùng một trạng thái dữ liệu để thấy thay đổi nhãn tiếng Việt và tiếng Anh.')]
for style,text in new_sections:old.insert_paragraph_before(text,style)

# Cập nhật các câu liên quan trực tiếp để tránh coi nhận xét ngày 27/10 là hiện trạng ngày 10/11.
for p in doc.paragraphs:
    if p.text.startswith('Nhân sự, chấm công, chốt/chi lương, tài sản và báo cáo đã có trong kế hoạch.'):
        p.text='Ở bản đối chiếu ngày 27/10/2026, các nhóm nhân sự, chấm công, chốt/chi lương, tài sản và báo cáo được trình bày theo phạm vi thiết kế tại thời điểm đó. Các mô hình và ghi nhận lịch sử dưới đây không tự chứng nhận toàn bộ chương trình ở phiên bản mới. Bản cập nhật ngày 10/11/2026 bổ sung phần Tổng quan quản lý và lựa chọn ngôn ngữ VI/EN ở mục 4.2.4–4.2.5, dựa trên source và hồ sơ triển khai hiện có.'
    elif p.text.startswith('Source đã có đăng nhập/phiên, danh mục nền, menu/công thức, bàn, khách/ưu đãi, bán hàng/'):
        p.text='Phần nội dung ban đầu được đối chiếu với source và hồ sơ nghiệm thu ngày 27/10/2026. Bản cập nhật này bổ sung Tổng quan quản lý và lựa chọn ngôn ngữ Việt–Anh theo các thay đổi ngày 09–10/11/2026. Trạng thái triển khai và kết quả kiểm chứng được xem theo từng phiên bản; các số liệu PASS, FAIL hoặc phần chưa chạy của đợt cũ được giữ là lịch sử, không dùng để chứng nhận toàn bộ bản hiện tại.'
    elif p.text.startswith('Nội dung xây dựng chương trình vì vậy tập trung giải thích trách nhiệm các tầng'):
        p.text='Nội dung xây dựng chương trình giải thích trách nhiệm các tầng và luồng xử lý có source, kèm hình thật có nguồn hoặc vị trí ảnh còn thiếu. Phần Tổng quan và VI/EN được mô tả bằng các thao tác, dữ liệu và giới hạn đã đối chiếu. Khi chưa có ảnh chương trình đúng phiên bản, báo cáo ghi chỗ bổ sung thay vì dựng ảnh hoặc số liệu minh họa thành kết quả thực tế.'
    elif p.text.startswith('Phần chương trình được trình bày theo source và hồ sơ dự án hiện có. Bán hàng'):
        p.text='Phần chương trình được trình bày theo source và hồ sơ dự án ở từng mốc đối chiếu. Bán hàng gắn với tiền, tiêu hao và điểm trong giao dịch; nhập hàng gắn với tồn, chi trả ngay và nợ; timeout phải được đối chiếu trước khi gửi lại. Bản cập nhật bổ sung Tổng quan quản lý để theo dõi chỉ tiêu, biểu đồ và công việc cần chú ý, đồng thời mô tả lựa chọn ngôn ngữ tiếng Việt/tiếng Anh. Kết quả kỹ thuật và các phần nghiệm thu còn thiếu được giữ rõ theo hồ sơ, không suy hoàn tất từ việc đã có source.'

ref_heading=find('TÀI LIỆU THAM KHẢO')
ref=doc.add_paragraph('Hồ sơ dự án CoffeeManager. Tổng quan phân tích và đa ngôn ngữ VI/EN. Hồ sơ cập nhật ngày 09–10/11/2026: docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/t28-bao-cao-doi-chieu/ và docs/tasks/localization-vi-en/. Nội dung đối chiếu cùng AdminDashboardControl, LoginForm, SystemInfoControl và các thành phần Localization trong source.')

W='http://schemas.openxmlformats.org/wordprocessingml/2006/main'
ns={'w':W}
def tag(s):return '{'+W+'}'+s

# Xóa đúng phạm vi ký tự, không gộp run hoặc làm mất định dạng/phần ảnh/trường Word.
removed=0
for paragraph in doc.element.iter(tag('p')):
    nodes=list(paragraph.iter(tag('t')))
    whole=''.join(x.text or '' for x in nodes)
    spans=[]
    start=re.match(r'^\s*\[\d+\]\.\s*',whole)
    if start:spans.append(start.span())
    for m in re.finditer(r'\s*\[\d+\](?:\s*[,;]\s*\[\d+\])*',whole):
        if not any(m.start()<b and m.end()>a for a,b in spans):spans.append(m.span())
    if not spans:continue
    removed+=len(re.findall(r'\[\d+\]',whole))
    offset=0
    for node in nodes:
        value=node.text or '';chars=[]
        for i,c in enumerate(value):
            if not any(a<=offset+i<b for a,b in spans):chars.append(c)
        offset+=len(value);node.text=''.join(chars)

indented=0
for paragraph in doc.paragraphs:
    text=paragraph.text.strip();style=paragraph.style.name.lower()
    if not text or style.startswith(('heading','toc','caption','figurecaption','tablecaption','unnumbered')):continue
    if list(paragraph._p.iter(tag('drawing'))):continue
    if paragraph.alignment in (WD_ALIGN_PARAGRAPH.CENTER,WD_ALIGN_PARAGRAPH.RIGHT):continue
    if re.fullmatch(r'[.\s]+',text):continue
    # Các tiêu đề/cụm nhãn in đậm nguyên đoạn không phải đoạn văn diễn giải.
    visible=[r for r in paragraph.runs if r.text.strip()]
    if visible and all(r.bold for r in visible) and len(text)<100:continue
    paragraph.paragraph_format.first_line_indent=Cm(1.27);indented+=1

after_tables=[[[cell.text for cell in row.cells] for row in table.rows] for table in doc.tables]
assert before_tables==after_tables,'Table contents changed'
tmp=run/'revised.docx';doc.save(tmp)

# Áp dụng màu tường minh cả style lẫn run để màu theme không ghi đè khi cập nhật TOC.
with ZipFile(tmp) as zin,ZipFile(target,'w',ZIP_DEFLATED) as zout:
    for info in zin.infolist():
        data=zin.read(info.filename)
        if info.filename.startswith('word/') and info.filename.endswith('.xml') and not info.filename.startswith('word/theme/'):
            tree=etree.fromstring(data)
            for shd in tree.iter(tag('shd')):
                shd.attrib.clear();shd.set(tag('val'),'clear');shd.set(tag('color'),'auto');shd.set(tag('fill'),'FFFFFF')
            for color in tree.iter(tag('color')):
                color.attrib.clear();color.set(tag('val'),'000000')
            for high in list(tree.iter(tag('highlight'))):high.getparent().remove(high)
            for borders in tree.iter(tag('tblBorders')):
                for edge in borders:
                    edge.set(tag('color'),'000000')
                    for a in list(edge.attrib):
                        if 'theme' in a.lower():del edge.attrib[a]
            data=etree.tostring(tree,encoding='UTF-8',xml_declaration=True,standalone=True)
        zout.writestr(info,data)

check=Document(target)
assert before_tables==[[[c.text for c in r.cells] for r in t.rows] for t in check.tables]
assert not any(re.search(r'\[\d+\]',p.text) for p in check.paragraphs)
assert len(check.tables)==44
sources=[root/'src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs',root/'src/CafeManager.Models/Localization/L.cs',root/'src/CafeManager.Models/Localization/LanguageService.cs',root/'src/CafeManager.WinForms/Configuration/LanguagePreference.cs',root/'src/CafeManager.WinForms/Forms/LoginForm.cs',root/'src/CafeManager.WinForms/Modules/System/SystemInfoControl.cs',root/'docs/tasks/localization-vi-en/test-results.md',root/'docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/t28-bao-cao-doi-chieu/dashboard-analytics-test-results.md']
manifest=[{'path':str(x.relative_to(root)),'sha256':hashlib.sha256(x.read_bytes()).hexdigest()} for x in sources]
(run/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
result={'backup':str(backup),'tables_unchanged':True,'tables':len(check.tables),'removed_reference_markers':removed,'indented_paragraphs':indented,'added_sections':['4.2.4 Trang tổng quan quản lý','4.2.5 Lựa chọn ngôn ngữ tiếng Việt và tiếng Anh'],'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
(run/'edit-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
