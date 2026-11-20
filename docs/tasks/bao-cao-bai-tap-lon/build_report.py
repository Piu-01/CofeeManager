from pathlib import Path
import re, json, hashlib, math, html, sys
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION_START
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path(__file__).resolve().parents[3]
TASK=Path(__file__).resolve().parent
OUT=ROOT/'artifacts/bao-cao-bai-tap-lon'
FIG=OUT/'so-do'; QA=TASK/'qa'
for p in [OUT,FIG,QA]: p.mkdir(parents=True,exist_ok=True)
sys.stdout.reconfigure(encoding='utf-8')

# Đọc cấu trúc khai báo, không kết nối hay thay đổi SQL Server.
sql=(ROOT/'database/DB_Schema.sql').read_text(encoding='utf-8-sig')
tables={}
type_pattern=r'(CHAR|VARCHAR|NVARCHAR|DECIMAL|DATETIME2|DATE|TIME|INT|BIT|TINYINT|SMALLINT)\s*(\([^)]*\))?'
for m in re.finditer(r'CREATE TABLE (\w+)\s*\((.*?)\n\);',sql,re.S|re.I):
    name,body=m[1],m[2]
    fields=[]
    for line in body.splitlines():
        a=re.match(r'\s*(\w+)\s+'+type_pattern+r'(.*)',line,re.I)
        if a:
            f,t,prec,tail=a.groups()
            fields.append({'name':f,'type':t.upper()+(prec or ''),'nullable':not bool(re.search(r'\bNOT NULL\b',tail,re.I)), 'tail':tail})
        a=re.match(r'\s*(\w+)\s+AS\s*\(',line,re.I)
        if a: fields.append({'name':a[1],'type':'Computed','nullable':False,'tail':'Computed'})
    pk=[]
    a=re.search(r'PRIMARY KEY\s*\(([^)]+)\)',body,re.I)
    if a:pk=[x.strip() for x in a[1].split(',')]
    else:
        pk=[f['name'] for f in fields if 'PRIMARY KEY' in f['tail'].upper()]
    fks={}
    for a in re.finditer(r'FOREIGN KEY\s*\((\w+)\)\s*REFERENCES\s+(\w+)\s*\((\w+)\)',body,re.I):
        fks[a[1]]=(a[2],a[3])
    tables[name]={'fields':fields,'pk':pk,'fk':fks,'body':body}
assert len(tables)==35,len(tables)
tables['NHANVIEN']['fields'].append({'name':'DangLamViec','type':'BIT','nullable':False,'tail':'DEFAULT 1; ALTER TABLE trong baseline'})
tables['LICHSU_DIEM']['fk']['MaHD']=('HOADON','MaHD')

VN={
'CHUCVU':'Chức vụ','CALAMVIEC':'Ca làm việc','NHANVIEN':'Nhân viên','CHAMCONG':'Chấm công',
'DANHMUC_SP':'Danh mục sản phẩm','NGUYENVATLIEU':'Nguyên vật liệu','NHACUNGCAP':'Nhà cung cấp',
'SANPHAM':'Sản phẩm','CONGTHUC':'Công thức','PHIEUNHAP':'Phiếu nhập','CHITIET_PN':'Chi tiết nhập',
'PHIEUXUAT':'Phiếu xuất','CHITIET_PX':'Chi tiết xuất','PHIEUKIEMKE':'Phiếu kiểm kê','CHITIET_KK':'Chi tiết kiểm kê',
'HANGTHANHVIEN':'Hạng thành viên','KHACHHANG':'Khách hàng','LICHSU_DIEM':'Lịch sử điểm','KHUYENMAI':'Khuyến mãi',
'KHUVUC':'Khu vực','BAN':'Bàn','DOITACGIAOHANG':'Đối tác giao hàng','PHUONGTHUCTT':'Phương thức thanh toán',
'HOADON':'Hóa đơn','CHITIET_HD':'Dòng hóa đơn','CHITIET_THANHTOAN':'Phân bổ thanh toán',
'DANHMUC_THUCHI':'Danh mục thu chi','TAISAN_CODINH':'Tài sản cố định','PHIEUTHUCHI':'Phiếu thu chi',
'CONGNO_NCC':'Công nợ nhà cung cấp','VAITRO':'Vai trò','TAIKHOAN':'Tài khoản','BANGLUONG':'Bảng lương',
'THANHTOAN_CONGNO':'Lịch sử trả nợ','TIEUHAO_HOADON':'Tiêu hao hóa đơn'}
groups=[('Danh mục và menu',['DANHMUC_SP','SANPHAM','NGUYENVATLIEU','CONGTHUC','KHUVUC','BAN']),
('Khách hàng và ưu đãi',['HANGTHANHVIEN','KHACHHANG','LICHSU_DIEM','KHUYENMAI']),
('Bán hàng và giao hàng',['DOITACGIAOHANG','PHUONGTHUCTT','HOADON','CHITIET_HD','CHITIET_THANHTOAN','TIEUHAO_HOADON']),
('Kho và nhà cung cấp',['NHACUNGCAP','PHIEUNHAP','CHITIET_PN','PHIEUXUAT','CHITIET_PX','PHIEUKIEMKE','CHITIET_KK']),
('Thu chi công nợ và tài sản',['DANHMUC_THUCHI','TAISAN_CODINH','PHIEUTHUCHI','CONGNO_NCC','THANHTOAN_CONGNO']),
('Nhân sự và tài khoản',['CHUCVU','CALAMVIEC','NHANVIEN','CHAMCONG','VAITRO','TAIKHOAN','BANGLUONG'])]
assert len({x for _,g in groups for x in g})==35

# Cùng một đặc tả sinh hình PNG và SVG, lưu nguồn JSON để chỉnh sửa.
FONT='C:/Windows/Fonts/segoeui.ttf'; BOLD='C:/Windows/Fonts/segoeuib.ttf'
class Canvas:
    def __init__(self,w,h):
        self.w=w;self.h=h;self.im=Image.new('RGB',(w,h),'white');self.d=ImageDraw.Draw(self.im)
        self.svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">','<rect width="100%" height="100%" fill="white"/>']
    def line(self,pts,arrow=False,color='#25394b',width=3):
        self.d.line(pts,fill=color,width=width)
        self.svg.append(f'<polyline points="{" ".join(f"{x},{y}" for x,y in pts)}" fill="none" stroke="{color}" stroke-width="{width}"/>')
        if arrow:
            (x0,y0),(x,y)=pts[-2:];a=math.atan2(y-y0,x-x0)
            p=[(x,y),(x-14*math.cos(a-.45),y-14*math.sin(a-.45)),(x-14*math.cos(a+.45),y-14*math.sin(a+.45))]
            self.d.polygon(p,fill=color);self.svg.append(f'<polygon points="{" ".join(f"{i},{j}" for i,j in p)}" fill="{color}"/>')
    def text(self,box,text,size=28,bold=False,align='center'):
        x,y,w,h=box
        while True:
            f=ImageFont.truetype(BOLD if bold else FONT,size);lines=[]
            for line in text.split('\n'):
                cur=''
                for word in line.split():
                    test=(cur+' '+word).strip()
                    if self.d.textlength(test,font=f)>w-18 and cur:lines.append(cur);cur=word
                    else:cur=test
                lines.append(cur)
            if len(lines)*int(size*1.3)<=h-4 and all(self.d.textlength(s,font=f)<=w-12 for s in lines):break
            size-=1
            if size<18:raise ValueError('Diagram text cannot fit: '+text)
        lh=int(size*1.3);yy=y+(h-len(lines)*lh)/2
        for s in lines:
            xx=x+(w-self.d.textlength(s,font=f))/2 if align=='center' else x+10
            self.d.text((xx,yy),s,font=f,fill='#172532')
            self.svg.append(f'<text x="{xx}" y="{yy+size}" font-family="Segoe UI, sans-serif" font-size="{size}" font-weight="{"bold" if bold else "normal"}" fill="#172532">{html.escape(s)}</text>')
            yy+=lh
    def node(self,box,text,kind='process',size=28):
        x,y,w,h=box
        fill='#eef3f6' if kind=='process' else 'white'
        if kind=='store':
            self.line([(x,y),(x+w,y)]);self.line([(x,y+h),(x+w,y+h)])
        elif kind=='diamond':
            p=[(x+w/2,y),(x+w,y+h/2),(x+w/2,y+h),(x,y+h/2)]
            self.d.polygon(p,fill='white',outline='#25394b',width=3)
            self.svg.append(f'<polygon points="{" ".join(f"{i},{j}" for i,j in p)}" fill="white" stroke="#25394b" stroke-width="3"/>')
        else:
            radius=15 if kind=='process' else 0
            self.d.rounded_rectangle((x,y,x+w,y+h),radius=radius,fill=fill,outline='#25394b',width=3)
            self.svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="#25394b" stroke-width="3"/>')
        text_box=(x+w*.16,y+h*.18,w*.68,h*.64) if kind=='diamond' else box
        self.text(text_box,text,size,bold=kind=='process')
    def label(self,x,y,w,text,size=24):
        # Nhãn nằm trên nền trắng để không đè lên đường nối.
        self.text((x,y,w,100),text,size)
    def save(self,name):
        self.im.save(FIG/(name+'.png'));(FIG/(name+'.svg')).write_text('\n'.join(self.svg+['</svg>']),encoding='utf-8')

spec={}
def dfd(name,title,rows):
    h=155+len(rows)*335
    c=Canvas(1800,h);c.text((20,5,1760,100),title,34,True)
    for i,(actor,proc,store,inp,outp,read,write) in enumerate(rows):
        y=140+i*335
        c.node((20,y+65,300,150),actor,'process' if actor.startswith('P') else 'entity',36)
        c.node((675,y+45,420,190),proc,'process',42)
        c.node((1460,y+65,315,150),store,'store',36)
        c.line([(320,y+100),(675,y+100)],True);c.label(320,y-5,355,inp,35)
        c.line([(675,y+185),(320,y+185)],True);c.label(320,y+192,355,outp,35)
        if read:
            c.line([(1460,y+100),(1095,y+100)],True);c.label(1095,y-5,365,read,35)
        if write:
            c.line([(1095,y+185),(1460,y+185)],True);c.label(1095,y+192,365,write,35)
    c.save(name);spec[name]={'title':title,'rows':rows}

dfd('top','Mức đỉnh • các tác nhân/kho lặp cùng mã là cùng đối tượng',[
('A1 / A2 / A3','P1 Xác thực\nvà kiểm tra phiên','D1 Tài khoản / vai trò','F01a Đăng nhập / phiên','F01b Kết quả xác thực','Thông tin xác thực','Thông tin cần cập nhật'),
('A1 / A2 / A3\nTheo quyền','P2 Dữ liệu nền\nvà khách hàng','D2 Danh mục\nD3 Khách / điểm','F02a Hồ sơ / chính sách','F02b Kết quả / tra cứu','Danh mục / hồ sơ','Danh mục / lịch sử điểm'),
('A1 / A2','P3 Bán hàng\nvà thanh toán','D2 / D3 / D4 / D5','F03a Đơn / phân bổ / giao','F03b Đơn / tiền / bản in','Menu / ưu đãi / tồn / đơn','Đơn / tiền / điểm / tiêu hao'),
('A1 / A3','P4 Quản lý kho','D2 NCC / D5 Kho','F04a Nguyên liệu / phiếu / đếm','F04b Chứng từ / tồn','NCC / tồn / snapshot','Mở sổ / nhập / xuất / kiểm kê'),
('A1 / A3\nA3 chỉ xem nợ','P5 Thu chi\nvà công nợ','D2 / D5 / D6 / D7','F05a Thu chi / trả nợ / tra cứu','F05b Phiếu / số dư / lịch sử','Danh mục / PN / nợ / chi','Phiếu chi / nợ / trả nợ'),
('A1','P6 Nhân sự tài sản\nvà báo cáo (thiết kế đích)','D1–D8 theo báo cáo','F06a Hồ sơ / công / kỳ / lọc','F06b Hồ sơ / lương / báo cáo','Dữ liệu nguồn đã chốt','Hồ sơ / công / lương / tài sản')])

# Sơ đồ ngữ cảnh: chỉ một tiến trình hệ thống.
c=Canvas(1800,1270);c.text((10,0,1780,85),'Ranh giới phần mềm CoffeeManager',40,True)
c.node((1100,100,650,980),'0\nHỆ THỐNG\nCOFFEEMANAGER',size=44)
for y,actor,ins,outs in [(160,'A1 Quản lý\nADMIN','F01a–F06a theo quyền','F01b–F06b theo quyền'),(470,'A2 Thu ngân\nCASH','F01a / F02a / F03a','F01b / F02b / F03b'),(780,'A3 Thủ kho\nSTORE','F01a / F02a / F04a / F05a','F01b / F02b / F04b / F05b')]:
    c.node((25,y,350,170),actor,'entity',40)
    c.line([(375,y+45),(1100,y+45)],True);c.label(380,y-60,710,ins,36)
    c.line([(1100,y+135),(375,y+135)],True);c.label(380,y+140,710,outs,36)
c.text((40,1110,1710,130),'Khách hàng, NCC và đối tác cung cấp thông tin qua nhân viên.\nKhông có tích hợp trực tiếp với các bên này trong phạm vi.',34)
c.save('context')

dfd('dfd1','P1 • F01a → F01b • xác thực và kiểm tra phiên',[
('A1 / A2 / A3','1.1 Tiếp nhận\nvà kiểm chứng đăng nhập','D1 Tài khoản','Tên đăng nhập / mật khẩu','Kết quả đăng nhập','Hash / hoạt động / nhân viên','Hash cập nhật nếu rehash'),
('P1.1 Kiểm chứng\nđăng nhập','1.2 Xác lập\nngữ cảnh phiên','D1 Vai trò / tài khoản','Thông tin đăng nhập hợp lệ','Ngữ cảnh phiên / kết quả','Tài khoản / SecurityStamp',''),
('A1 / A2 / A3','1.3 Kiểm quyền\nvà kết thúc phiên','D1 Tài khoản / vai trò','Yêu cầu kiểm tra / đăng xuất','Kết quả quyền / kết thúc phiên','Hoạt động / quyền / stamp','')])
dfd('dfd2','P2 • dữ liệu nền và khách hàng • quyền giới hạn từng nhánh',[
('A1 / A3 đọc công thức','2.1 Quản lý danh mục\nmenu bàn và chính sách','D2 Dữ liệu nền','F02a Danh mục / công thức','F02b Danh mục / kết quả','Phiên bản / tham chiếu','Danh mục / giá / công thức'),
('A1 / A2','2.2 Tìm và cập nhật\nhồ sơ khách','D3 Khách hàng','F02a Hồ sơ / từ khóa','F02b Hồ sơ / kết quả','Khách / hạng / điểm','Hồ sơ / mốc điểm mở sổ'),
('A1','2.3 Điều chỉnh\nđiểm và chính sách','D2 / D3 Chính sách / điểm','F02a Chính sách / điều chỉnh','F02b Điểm / hạng / kết quả','Ngưỡng / lịch sử / số dư','Lịch sử / chính sách / hạng')])
dfd('dfd3','P3 • F03a → F03b • các kho D3 và D5 cùng xuất hiện ở mức cha',[
('A1 / A2','3.1 Tạo và sửa\nđơn mở','D2 / D4','Đơn / dòng món / phiên bản','Đơn mở / tổng tạm','Menu / bàn / đơn','Header / dòng giá snapshot'),
('A1 / A2','3.2 Xác định ưu đãi\nvà phân bổ','D2 / D3 / D4','Khách / KM / phân bổ','Preview / số phải trả','Chính sách / khách / đơn','Lựa chọn khách / KM'),
('A1 / A2','3.3 Chốt thanh toán\nnguyên tử','D3 / D4 / D5','Xác nhận / preview / tiền','Kết quả chốt / đối chiếu','Phiên bản / định mức / tồn','Tiền / tiêu hao / điểm / đơn'),
('A1 / A2','3.4 Tra cứu in\nvà chuyển giao','D4 Hóa đơn','Bộ lọc / mã đơn / bước giao','Chi tiết / bản in / trạng thái','Snapshot / tiền / trạng thái','Trạng thái giao hợp lệ')])
dfd('dfd4','P4 • F04a → F04b • trao đổi F07/F08 với P5',[
('A1 / A3','4.1 Quản lý nguyên liệu\nvà mở sổ','D5 Kho','Thông tin NVL / mở sổ','Hồ sơ / tồn / kết quả','Đơn vị / mốc / phát sinh','Hồ sơ / mở sổ một lần'),
('A1 / A3\nP5 qua F07 / F08','4.2 Lập phiếu nhập','D2 NCC / D5 Kho','Dòng nhập / NCC / trả ngay','Mã PN / tồn / link chi nợ','NCC / danh mục / tồn','Phiếu nhập / chi tiết / tồn'),
('A1 / A3','4.3 Lập phiếu xuất','D5 Kho','Dòng xuất / loại / lý do','Mã PX / tồn / kết quả','Tồn / nguyên liệu','Phiếu xuất / chi tiết / tồn'),
('A1 / A3','4.4 Chốt kiểm kê','D5 Kho','Snapshot / số đếm / lý do','Mã KK / kết quả đối chiếu','Tồn / phiên bản / mốc','Chi tiết KK / điều chỉnh')])
dfd('dfd5','P5 • F05a → F05b • F07/F08 từ kho • F09/F10 từ lương đích',[
('A1','5.1 Ghi thu chi thường','D2 / D7','Nội dung / số tiền / ngày','Phiếu thu chi / kết quả','Danh mục / tài sản','Phiếu nguồn Manual'),
('P4 / P6 thiết kế đích','5.2 Ghi nguồn chi\nvà nợ phát sinh','D5 / D6 / D7','F07 Nguồn nhập / F09 lương','F08 Link chi nợ / F10 link chi','PN / BL / nguồn / mapping','Chi trả ngay / nợ / chi lương'),
('A1 trả nợ\nA1 / A3 tra cứu','5.3 Trả nợ\nvà đối chiếu số dư','D2 / D6 / D7','Khoản trả / nợ / thời điểm','Phiếu chi / lịch sử / số dư','Nợ gốc / lịch sử / nguồn','Trả nợ gắn đúng một chi')])
dfd('dfd6','P6 • thiết kế đích, chưa xác nhận chương trình hoàn chỉnh',[
('A1','6.1 Quản lý nhân sự\ncông và lương','D1 / D8','F06a Hồ sơ / công / kỳ','F06b Hồ sơ / lương','Nhân viên / giờ / kỳ','Hồ sơ / công / lương'),
('A1','6.2 Quản lý tài sản','D8 Tài sản','F06a Thông tin tài sản','F06b Hồ sơ / khấu hao tham khảo','Tài sản / nguyên giá','Hồ sơ tài sản'),
('A1','6.3 Tổng hợp báo cáo','D3 / D4 / D5 / D6 / D7 / D8','F06a Khoảng ngày / bộ lọc','F06b Báo cáo / đối chiếu','Nguồn tiền kho nợ công lương','')])
dfd('top-interfaces','Mức đỉnh • phần trao đổi nguồn giữa các nhóm xử lý',[
('P4 Quản lý kho','P5 Thu chi\nvà công nợ','D6 / D7','F07 Nguồn nhập / trả ngay / nợ','F08 Mã chi / nợ và liên kết','Nguồn / mapping','Chi trả ngay / nợ'),
('P6 Nhân sự\nthiết kế đích','P5 Thu chi\nvà công nợ','D7 Phiếu thu chi','F09 Nguồn chi lương','F10 Mã / liên kết chi lương','Nguồn / mapping','Chi lương (đích)')])

# BFD theo sáu nhóm, dạng 2 cột x 3 hàng để đọc được ở A4.
branches=[('1 Xác thực và phiên','1.1 Đăng nhập\n1.2 Xác lập phiên\n1.3 Kiểm quyền / đăng xuất'),
('2 Dữ liệu nền và khách','2.1 Danh mục / menu / bàn\n2.2 Hồ sơ khách\n2.3 Chính sách / điểm'),
('3 Bán hàng','3.1 Đơn mở\n3.2 Ưu đãi / phân bổ\n3.3 Thanh toán\n3.4 Tra cứu / in / giao'),
('4 Kho','4.1 Nguyên liệu / mở sổ\n4.2 Nhập hàng\n4.3 Xuất hàng\n4.4 Kiểm kê'),
('5 Thu chi và công nợ','5.1 Thu chi thường\n5.2 Nguồn chi / nợ\n5.3 Trả nợ / đối chiếu'),
('6 Nhân sự tài sản và báo cáo','6.1 Nhân sự / công / lương\n6.2 Tài sản\n6.3 Báo cáo\n(Thiết kế đích)')]
c=Canvas(1800,1640);c.node((480,20,840,130),'QUẢN LÝ QUÁN CÀ PHÊ',size=38)
for i,(a,b) in enumerate(branches):
    x=90+(i%2)*850;y=245+(i//2)*440
    c.line([(900,150),(900,y+65),(x+340,y+65)],False)
    c.node((x,y,680,110),a,size=33)
    c.line([(x+340,y+110),(x+340,y+145)])
    c.node((x,y+145,680,240),b,'entity',30)
c.save('bfd')

def er(name,title,rows):
    c=Canvas(1800,120+len(rows)*230);c.text((20,0,1760,90),title,34,True)
    for i,(a,r,b,ca,cb) in enumerate(rows):
        y=120+i*230
        c.line([(470,y+65),(700,y+65)]);c.line([(1030,y+65),(1290,y+65)])
        c.node((15,y,455,130),a,'entity',42);c.node((700,y-15,330,160),r,'diamond',36);c.node((1290,y,490,130),b,'entity',42)
        c.text((470,y+5,230,60),ca,36);c.text((1030,y+5,260,60),cb,36)
    c.save(name);spec[name]={'title':title,'relations':rows}
er('er-sales','ER khái niệm • cụm menu và bán hàng',[
('Danh mục','phân loại','Sản phẩm','1','0..N'),('Sản phẩm','cấu thành qua\nCông thức','Nguyên liệu','0..N','0..N'),
('Khách hàng','mua','Hóa đơn','0..1','0..N'),('Bàn','phục vụ','Hóa đơn','0..1','0..N'),
('Hóa đơn','có','Dòng món','1','1..N (*)'),('Hóa đơn','được thu qua','Phân bổ thanh toán','1','0..N')])
er('er-stock','ER khái niệm • cụm kho và công nợ',[
('Nhà cung cấp','cung cấp theo','Phiếu nhập','1','0..N'),('Phiếu nhập','có','Dòng nhập nguyên liệu','1','1..N (*)'),
('Phiếu nhập','phát sinh','Nợ gốc','1','0..1'),('Nợ gốc','được trả qua','Lần trả nợ','1','0..N'),
('Phiếu chi','ghi tiền của','Lần trả nợ','1','0..1'),('Phiếu xuất / kiểm kê','có','Dòng nguyên liệu','1','1..N (*)')])
er('er-people','ER khái niệm • các cụm liên quan',[
('Hạng thành viên','phân hạng','Khách hàng','1','0..N'),('Khách hàng','có','Giao dịch điểm','1','0..N'),
('Chức vụ','được gán cho','Nhân viên','1','0..N'),('Nhân viên','có','Tài khoản','1','0..1'),
('Vai trò','phân quyền','Tài khoản','1','0..N'),('Nhân viên','có kết quả kỳ','Bảng lương (*)','1','0..N'),
('Tài sản (*)','liên quan','Phiếu thu chi','0..1','0..N')])

# Lược đồ quan hệ theo cặp FK→PK, mỗi quan hệ xuất hiện trên một hàng.
def relational(name,title,pairs):
    c=Canvas(1800,125+len(pairs)*170);c.text((15,0,1770,90),title,33,True)
    for i,(child,col,parent,key) in enumerate(pairs):
        y=125+i*170
        cp=', '.join(tables[child]['pk']) if child in tables else 'MaHD'
        pp=', '.join(tables[parent]['pk']) if parent in tables else key
        c.node((20,y,650,115),f'{child}\nPK {cp} • FK {col}','entity',40)
        c.node((1120,y,650,115),f'{parent}\nPK {pp}','entity',40)
        c.line([(670,y+60),(1120,y+60)],True)
        c.text((680,y-5,430,70),f'{col} → {key}',34)
    c.save(name);spec[name]={'title':title,'foreign_keys':pairs}
relational('rel-sales','Quan hệ bán hàng • mũi tên FK → PK',[
('CHITIET_HD','MaHD','HOADON','MaHD'),('CHITIET_HD','MaSP','SANPHAM','MaSP'),('CHITIET_THANHTOAN','MaHD','HOADON','MaHD'),
('CHITIET_THANHTOAN','MaPT','PHUONGTHUCTT','MaPT'),('TIEUHAO_HOADON','MaHD','HOADON','MaHD'),('TIEUHAO_HOADON','MaNVL','NGUYENVATLIEU','MaNVL'),('HoaDonUuDaiSnapshot','MaHD','HOADON','MaHD')])
relational('rel-menu','Quan hệ menu khách hàng và không gian quán',[
('SANPHAM','MaDM','DANHMUC_SP','MaDM'),('CONGTHUC','MaSP','SANPHAM','MaSP'),('CONGTHUC','MaNVL','NGUYENVATLIEU','MaNVL'),
('KHACHHANG','MaHang','HANGTHANHVIEN','MaHang'),('LICHSU_DIEM','MaKH','KHACHHANG','MaKH'),('LICHSU_DIEM','MaHD','HOADON','MaHD'),('BAN','MaKV','KHUVUC','MaKV')])
relational('rel-stock','Quan hệ chứng từ kho',[
('PHIEUNHAP','MaNCC','NHACUNGCAP','MaNCC'),('CHITIET_PN','MaPN','PHIEUNHAP','MaPN'),('CHITIET_PN','MaNVL','NGUYENVATLIEU','MaNVL'),
('CHITIET_PX','MaPX','PHIEUXUAT','MaPX'),('CHITIET_PX','MaNVL','NGUYENVATLIEU','MaNVL'),('CHITIET_KK','MaKK','PHIEUKIEMKE','MaKK'),('CHITIET_KK','MaNVL','NGUYENVATLIEU','MaNVL')])
relational('rel-finance','Quan hệ thu chi và công nợ • gồm bổ sung 017',[
('CONGNO_NCC','MaPN','PHIEUNHAP','MaPN'),('THANHTOAN_CONGNO','MaCN','CONGNO_NCC','MaCN'),('THANHTOAN_CONGNO','MaTC','PHIEUTHUCHI','MaTC'),
('THANHTOAN_CONGNO','MaPT','PHUONGTHUCTT','MaPT'),('PHIEUTHUCHI','MaDMTC','DANHMUC_THUCHI','MaDMTC'),('PHIEUTHUCHI','MaPN','PHIEUNHAP','MaPN'),('PHIEUTHUCHI','MaBL','BANGLUONG','MaBL'),('PHIEUTHUCHI','MaTS','TAISAN_CODINH','MaTS')])
relational('rel-people','Quan hệ nhân sự và tài khoản • nghiệp vụ HR còn là đích',[
('NHANVIEN','MaCV','CHUCVU','MaCV'),('TAIKHOAN','MaNV','NHANVIEN','MaNV'),('TAIKHOAN','MaVT','VAITRO','MaVT'),
('CHAMCONG','MaNV','NHANVIEN','MaNV'),('CHAMCONG','MaCa','CALAMVIEC','MaCa'),('BANGLUONG','MaNV','NHANVIEN','MaNV'),('HOADON','MaNV','NHANVIEN','MaNV')])
c=Canvas(1800,1100)
for i,(a,b) in enumerate([('WinForms','Form / UserControl / binding / in'),('Business','Quyền / chuẩn hóa / điều phối'),('DataAccess','SqlClient / tham số / DTO'),('SQL Server','Procedure / trigger / view / transaction')]):
    y=40+i*245;c.node((40,y,1100,160),a+'\n'+b,size=32)
    if i<3:c.line([(590,y+160),(590,y+245)],True)
c.node((1290,275,475,300),'Models\nEntity / Request\nDTO / State','entity',32)
for y in [120,365,610]:c.line([(1140,y),(1210,y),(1210,425),(1290,425)],True)
c.text((1250,670,530,220),'Các project C# trong cùng ứng dụng.\nKhông có API server.',30)
c.save('architecture')
(FIG/'diagram-specifications.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
(TASK/'schema-baseline.json').write_text(json.dumps(tables,ensure_ascii=False,indent=2),encoding='utf-8')

doc=Document()
sec=doc.sections[0]
def setup_section(s):
    s.page_width=Cm(21);s.page_height=Cm(29.7);s.left_margin=Cm(3.5);s.right_margin=Cm(2);s.top_margin=Cm(2);s.bottom_margin=Cm(2)
    s.header_distance=Cm(.8);s.footer_distance=Cm(1)
setup_section(sec)
normal=doc.styles['Normal'];normal.font.name='Times New Roman';normal.font.size=Pt(13)
normal._element.rPr.rFonts.set(qn('w:eastAsia'),'Times New Roman')
normal.paragraph_format.line_spacing=1.15;normal.paragraph_format.space_before=Pt(6);normal.paragraph_format.space_after=Pt(6)
normal.paragraph_format.widow_control=True
for name in ['Title','Subtitle','Heading 1','Heading 2','Heading 3','Heading 4','Caption']:
    s=doc.styles[name];s.font.name='Times New Roman';s.font.color.rgb=RGBColor(0,0,0);s.font.size=Pt(13)
    s._element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'),'Times New Roman')
    s.paragraph_format.line_spacing=1.15;s.paragraph_format.space_before=Pt(6);s.paragraph_format.space_after=Pt(6)
for n in [1,2,3,4]:doc.styles[f'Heading {n}'].font.bold=True
for name in ['Normal','Title','Subtitle','Heading 1','Heading 2','Heading 3','Heading 4','Caption']:
    rf=doc.styles[name].element.get_or_add_rPr().rFonts
    for key in list(rf.attrib):
        if 'theme' in key.lower():del rf.attrib[key]
    for kind in ['ascii','hAnsi','eastAsia','cs']:rf.set(qn('w:'+kind),'Times New Roman')
doc.styles['Heading 1'].font.size=Pt(13);doc.styles['Heading 1'].paragraph_format.page_break_before=True
doc.styles['Heading 1'].paragraph_format.alignment=WD_ALIGN_PARAGRAPH.CENTER
doc.styles['Caption'].font.italic=True
doc.styles['Caption'].font.bold=False
doc.styles['Caption'].paragraph_format.alignment=WD_ALIGN_PARAGRAPH.CENTER
settings=doc.settings.element
u=OxmlElement('w:updateFields');u.set(qn('w:val'),'true');settings.append(u)
doc.core_properties.title='Quản lí quán cafe'
doc.core_properties.author='Nguyễn Minh Phú'
doc.core_properties.subject='Bản nháp học thuật dựa trên source dự án CoffeeManager ngày 27/10/2026'
un=doc.styles.add_style('UnnumberedHeading',1);un.base_style=doc.styles['Title'];un.font.bold=True;un.font.size=Pt(13)
lev=OxmlElement('w:outlineLvl');lev.set(qn('w:val'),'0');un.element.get_or_add_pPr().append(lev)
for style in doc.styles:
    for border in list(style.element.iter(qn('w:pBdr'))):border.getparent().remove(border)

def p(text='',style=None):
    a=doc.add_paragraph(text,style);a.paragraph_format.alignment=WD_ALIGN_PARAGRAPH.JUSTIFY if not style else a.paragraph_format.alignment
    return a
def heading(text,level=1):return doc.add_heading(text,level)
def field(par,code,result='Cập nhật trường trong Word'):
    r=par.add_run();b=OxmlElement('w:fldChar');b.set(qn('w:fldCharType'),'begin');r._r.append(b)
    r=par.add_run();ins=OxmlElement('w:instrText');ins.set(qn('xml:space'),'preserve');ins.text=' '+code+' ';r._r.append(ins)
    r=par.add_run();s=OxmlElement('w:fldChar');s.set(qn('w:fldCharType'),'separate');r._r.append(s)
    par.add_run(str(result));r=par.add_run();e=OxmlElement('w:fldChar');e.set(qn('w:fldCharType'),'end');r._r.append(e)

chapter=0;figcount={};tabcount={};register=[]
def caption(label,title):
    counts=figcount if label=='Hình' else tabcount
    counts[chapter]=counts.get(chapter,0)+1;n=counts[chapter]
    par=p(style='Caption');par.add_run(f'{label} {chapter}.')
    # Chuỗi SEQ riêng theo chương, số tự cập nhật trong Word.
    field(par,f'SEQ {"Hinh" if label=="Hình" else "Bang"}{chapter} \\* ARABIC',n)
    par.add_run('. '+title)
    return par,f'{chapter}.{n}'

def table(title,headers,rows,widths=None):
    cap,num=caption('Bảng',title);cap.paragraph_format.keep_with_next=True
    t=doc.add_table(rows=1,cols=len(headers));t.autofit=False
    ind=OxmlElement('w:tblInd');ind.set(qn('w:w'),'80');ind.set(qn('w:type'),'dxa');t._tbl.tblPr.append(ind)
    widths=widths or [15.5/len(headers)]*len(headers)
    for col,w in zip(t.columns,widths):col.width=Cm(w)
    for i,h in enumerate(headers):t.rows[0].cells[i].text=str(h)
    repeat=OxmlElement('w:tblHeader');t.rows[0]._tr.get_or_add_trPr().append(repeat)
    for row in rows:
        cells=t.add_row().cells
        for i,val in enumerate(row):cells[i].text=str(val)
    borders=OxmlElement('w:tblBorders')
    for edge in ['top','left','bottom','right','insideH','insideV']:
        el=OxmlElement('w:'+edge);el.set(qn('w:val'),'single');el.set(qn('w:sz'),'4');el.set(qn('w:color'),'D9D9D9');borders.append(el)
    t._tbl.tblPr.append(borders)
    for ir,row in enumerate(t.rows):
        # Không để một hàng bị xé qua trang; toàn bảng vẫn được phép chạy nhiều trang.
        no=OxmlElement('w:cantSplit');row._tr.get_or_add_trPr().append(no)
        for i,cell in enumerate(row.cells):
            cell.width=Cm(widths[i]);cell.vertical_alignment=1
            tcPr=cell._tc.get_or_add_tcPr();mar=OxmlElement('w:tcMar')
            for side in ['top','left','bottom','right']:
                e=OxmlElement('w:'+side);e.set(qn('w:w'),'80');e.set(qn('w:type'),'dxa');mar.append(e)
            tcPr.append(mar)
            if ir==0:
                sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'E9EDF0');tcPr.append(sh)
            for par in cell.paragraphs:
                par.paragraph_format.space_before=Pt(3);par.paragraph_format.space_after=Pt(3);par.paragraph_format.line_spacing=1.15
                par.paragraph_format.keep_with_next=ir==0 or ir==len(t.rows)-2
                for r in par.runs:r.bold=ir==0;r.font.size=Pt(13);r.font.name='Times New Roman'
    blank=p();blank.paragraph_format.space_before=Pt(0);blank.paragraph_format.space_after=Pt(0);blank.paragraph_format.line_spacing=Pt(1);blank.add_run().font.size=Pt(1)
    return t

ui={
'ui-login':ROOT/'docs/tasks/orbit-ui-refresh/runs/20261017-acceptance/login-native.png',
'ui-customers':ROOT/'docs/tasks/orbit-9-remaining-screens/runs/native-picker-management-page-20261022-0.jpg',
'ui-editor':ROOT/'docs/tasks/orbit-ui-refresh/runs/20261017-acceptance/editor-product-125.png'}
def figure(name,title,with_narrative=False):
    path=ui.get(name,FIG/(name+'.png'))
    with Image.open(path) as im:w,h=im.size
    width=min(15.5,19*w/h)
    if name=='ui-login':width=11
    if name=='ui-editor':width=11
    par=p();par.alignment=WD_ALIGN_PARAGRAPH.CENTER;par.paragraph_format.keep_with_next=True
    par.add_run().add_picture(str(path),width=Cm(width))
    cap,num=caption('Hình',title)
    cap.paragraph_format.keep_with_next=with_narrative
    register.append((name,num,title,str(path.relative_to(ROOT)),f'{width:.1f} cm','Đã chèn'))

# Giữ nguyên các đối tượng đồ họa từ PDF nguồn, thay phần chữ bằng thông tin đã xác nhận.
from io import BytesIO
from pypdf import PdfReader, PdfWriter, PageObject, Transformation
from pypdf.generic import ContentStream
import pypdfium2 as pdfium
from reportlab.pdfgen import canvas as rlcanvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
pdfmetrics.registerFont(TTFont('TNR','C:/Windows/Fonts/times.ttf'))
pdfmetrics.registerFont(TTFont('TNRB','C:/Windows/Fonts/timesbd.ttf'))
source=PdfReader(r'C:/Users/Lenovo/AppData/Local/Temp/Zalo Temp/TempDownloads/PTTKHDT_6451071038_NgoThanhKhoi.pdf')
page=source.pages[0];stream=ContentStream(page.get_contents(),source);ops=[];intext=False
for operands,operator in stream.operations:
    if operator==b'BT':intext=True
    elif operator==b'ET':intext=False
    elif not intext:ops.append((operands,operator))
stream.operations=ops;page.replace_contents(stream)
buf=BytesIO();rc=rlcanvas.Canvas(buf,pagesize=(612,792))
def center_cover(text,y,size=14.04,bold=True):
    rc.setFont('TNRB' if bold else 'TNR',size);rc.drawCentredString(320,y,text)
center_cover('TRƯỜNG ĐẠI HỌC GIAO THÔNG VẬN TẢI',686.74)
center_cover('PHÂN HIỆU TẠI TP. HỒ CHÍ MINH',662.5)
center_cover('BỘ MÔN CÔNG NGHỆ THÔNG TIN',638.38)
center_cover('BÁO CÁO BÀI TẬP LỚN',502.39,15)
center_cover('HỌC PHẦN: PHÂN TÍCH THIẾT KẾ',470.47,13)
center_cover('HỆ THỐNG THÔNG TIN',450.5,13)
center_cover('ĐỀ TÀI: QUẢN LÍ QUÁN CAFE',414,15)
rc.setFont('TNR',14.04)
for y,label,value in [(338.69,'Giảng viên hướng dẫn:','ThS. PHẠM THỊ MIÊN'),(308.57,'Sinh viên thực hiện:','NGUYỄN MINH PHÚ'),(278.45,'Mã số sinh viên:','6551071065'),(248.33,'Lớp:','CQ.65.CNTT'),(218.21,'Khoá:','64')]:
    rc.drawString(113.42,y,label);rc.drawString(265.13,y,value)
center_cover('Tp. Hồ Chí Minh, năm 2026',97.58,14.04,False)
rc.save();buf.seek(0);page.merge_page(PdfReader(buf).pages[0])
# PDF mẫu là Letter; chuẩn hóa về A4 bằng biến đổi đồng dạng, không méo khung/logo.
a4=PageObject.create_blank_page(width=595.2756,height=841.8898);scale=595.2756/612
a4.merge_transformed_page(page,Transformation().scale(scale).translate(0,(841.8898-792*scale)/2))
writer=PdfWriter();writer.add_page(a4);writer.add_page(a4)
coverpdf=TASK/'qa/bia-coffeManager.pdf'
with coverpdf.open('wb') as f:writer.write(f)
coverrender=pdfium.PdfDocument(str(coverpdf));coverimage=QA/'bia-coffeManager.png'
coverrender[0].render(scale=3).to_pil().save(coverimage)
for cover in range(2):
    if cover:doc.add_page_break()
    a=p();a.paragraph_format.space_before=Pt(0);a.paragraph_format.space_after=Pt(0)
    run=a.add_run();run.add_picture(str(coverimage),width=Cm(21),height=Cm(29.7))
    inline=run._r.find('.//'+qn('wp:inline'))
    anchor=OxmlElement('wp:anchor')
    for k,v in {'distT':'0','distB':'0','distL':'0','distR':'0','simplePos':'0','relativeHeight':'0','behindDoc':'1','locked':'0','layoutInCell':'1','allowOverlap':'1'}.items():anchor.set(k,v)
    simple=OxmlElement('wp:simplePos');simple.set('x','0');simple.set('y','0');anchor.append(simple)
    for axis in ['H','V']:
        pos=OxmlElement('wp:position'+axis);pos.set('relativeFrom','page');offset=OxmlElement('wp:posOffset');offset.text='0';pos.append(offset);anchor.append(pos)
    for tag in ['extent','effectExtent']:
        e=inline.find(qn('wp:'+tag))
        if e is not None:anchor.append(e)
    anchor.append(OxmlElement('wp:wrapNone'))
    for tag in ['docPr','cNvGraphicFramePr']:
        e=inline.find(qn('wp:'+tag))
        if e is not None:anchor.append(e)
    anchor.append(inline.find(qn('a:graphic')))
    inline.getparent().replace(inline,anchor)
front=doc.add_section(WD_SECTION_START.NEW_PAGE);setup_section(front)
old=front._sectPr.find(qn('w:pgBorders'))
if old is not None:front._sectPr.remove(old)
front.footer.is_linked_to_previous=False

def front_title(text):
    a=p();a.alignment=WD_ALIGN_PARAGRAPH.CENTER;r=a.add_run(text);r.bold=True;r.font.size=Pt(13)
front_title('LỜI MỞ ĐẦU')
p('Một đơn bán trong quán cà phê liên quan đến nhiều thông tin: món được gọi, số tiền phải thu, lượng nguyên liệu đã dùng và quyền lợi của khách. Nếu các thông tin này được xử lý tách rời, việc đối chiếu sau giao dịch trở nên khó khăn. Đề tài CoffeeManager được lựa chọn để nghiên cứu cách tổ chức các chức năng và dữ liệu trong cùng một hệ thống quản lý, với phạm vi một quán và một kho chung.')
p('Báo cáo vận dụng phân tích chức năng, mô hình luồng dữ liệu và thiết kế CSDL quan hệ để giải thích giải pháp của dự án. Nội dung chương trình được đối chiếu với source hiện có; các phần mới ở mức kế hoạch được nhận diện riêng. Cách tiếp cận này giúp làm rõ quan hệ giữa yêu cầu nghiệp vụ và quyết định thiết kế, thay vì chỉ giới thiệu các màn hình.')
p('Cơ sở được lựa chọn là Ther Thẩn Cafe tại 49 Đường 449, phường Tăng Nhơn Phú, Thành phố Hồ Chí Minh. Bản thảo còn cần thông tin cơ cấu tổ chức, ghi nhận khảo sát thực tế và một số ảnh chương trình đúng phiên bản. Các vị trí này được ghi rõ để bổ sung; dữ liệu mẫu không được dùng để thay thông tin khảo sát. Phần kết luận giữ các giới hạn triển khai và nghiệm thu của dự án tại ngày đối chiếu 27/10/2026.')
doc.add_page_break();front_title('NHẬN XÉT CỦA GIẢNG VIÊN')
for _ in range(15):p('...................................................................................................................')
a=p('Ngày ........ tháng ........ năm ........');a.alignment=WD_ALIGN_PARAGRAPH.RIGHT
a=p('Giảng viên hướng dẫn');a.alignment=WD_ALIGN_PARAGRAPH.RIGHT
doc.add_page_break();front_title('MỤC LỤC');field(p(),'TOC \\o "1-3" \\h \\z \\u')
doc.add_page_break();front_title('DANH MỤC BẢNG BIỂU');field(p(),'TOC \\h \\z \\t "Caption,1"')
# Dùng style riêng để danh mục hình và bảng không lẫn nhau; cập nhật ở dưới.
doc.add_page_break();front_title('DANH MỤC HÌNH VÀ SƠ ĐỒ');field(p(),'TOC \\h \\z \\t "FigureCaption,1"')
doc.add_page_break();front_title('DANH MỤC TỪ VIẾT TẮT')
chapter=0
table('Các từ viết tắt sử dụng trong báo cáo',['Ký hiệu','Diễn giải'],[
('BFD','Business Function Diagram — sơ đồ phân cấp chức năng'),('DFD','Data Flow Diagram — biểu đồ luồng dữ liệu'),('ER','Entity Relationship — thực thể liên kết'),
('CSDL','Cơ sở dữ liệu'),('PK / FK','Primary Key / Foreign Key — khóa chính / khóa ngoại'),('BLL / DAL','Business Logic Layer / Data Access Layer'),
('DTO / TVP','Data Transfer Object / Table-Valued Parameter'),('POS','Point of Sale — màn bán hàng'),('NCC / NVL','Nhà cung cấp / nguyên vật liệu'),('ADMIN / CASH / STORE','Quản trị / thu ngân / thủ kho')],[3,12.5])
# Không đưa bảng viết tắt vào danh mục bảng chương.
for par in doc.paragraphs:
    if par.text.startswith('Bảng 0.'):
        par._element.getparent().remove(par._element)

body=doc.add_section(WD_SECTION_START.NEW_PAGE);setup_section(body);body.footer.is_linked_to_previous=False
pg=OxmlElement('w:pgNumType');pg.set(qn('w:start'),'1');body._sectPr.append(pg)
f=body.footer.paragraphs[0];f.alignment=WD_ALIGN_PARAGRAPH.CENTER;field(f,'PAGE','1')

data={
'roles':('Vai trò và giới hạn thao tác',['Tác nhân','Công việc được hỗ trợ','Giới hạn'],[
('A1 ADMIN','Danh mục, bán hàng, kho, khách, thu chi và trả nợ; các chức năng đích theo kế hoạch.','Không sửa tài chính đã chốt; không tùy ý sửa số dư.'),
('A2 CASH','Bán hàng, chọn/thêm khách, thanh toán, tra cứu/in và giao.','Không sửa giá/công thức/chính sách; không ghi kho, nợ hoặc lương.'),
('A3 STORE','Nguyên liệu, nhập, xuất, kiểm kê; xem nợ theo quyền.','Không trả nợ, không ghi chi trả ngay hoặc tài chính quản trị.')],[2.5,7,6]),
'processes':('Xác định tiến trình tác nhân và hồ sơ',['Mã / tiến trình','Tác nhân','Đầu vào → đầu ra','Hồ sơ'],[
('P1 Xác thực và phiên','A1, A2, A3','Đăng nhập/kiểm tra → kết quả, ngữ cảnh phiên','Tài khoản, vai trò, nhân viên'),
('P2 Dữ liệu nền và khách','A1; A2/A3 theo quyền','Danh mục/hồ sơ/chính sách → dữ liệu và kết quả cập nhật','Menu, công thức, bàn, khách, hạng, điểm'),
('P3 Bán hàng','A1, A2','Đơn/phân bổ/giao → đơn, kết quả tiền, bản in','Hóa đơn, dòng, thanh toán, tiêu hao, điểm'),
('P4 Kho','A1, A3','Nguyên liệu/nhập/xuất/số đếm → tồn, chứng từ, nguồn chi/nợ','Mở sổ, phiếu/chi tiết kho, NCC'),
('P5 Thu chi và công nợ','A1; A3 xem nợ','Thu chi/nguồn nhập/trả → phiếu chi, số dư, lịch sử','Phiếu thu chi, nợ, lịch sử trả'),
('P6 Nhân sự tài sản và báo cáo','A1','Hồ sơ/công/kỳ/lọc → kết quả và báo cáo đích','Nhân viên, công, lương, tài sản; nguồn tổng hợp')],[3.1,2.2,5.5,4.7]),
'stores':('Các kho dữ liệu logic',['Mã','Nội dung','Bảng đại diện'],[
('D1','Tài khoản và quyền','TAIKHOAN, VAITRO; NHANVIEN phục vụ xác thực'),('D2','Dữ liệu nền','DANHMUC_SP, SANPHAM, CONGTHUC, BAN, KHUVUC, KHUYENMAI, PHUONGTHUCTT, DOITACGIAOHANG, NHACUNGCAP, DANHMUC_THUCHI'),
('D3','Khách và điểm','KHACHHANG, HANGTHANHVIEN, LICHSU_DIEM, CustomerOpening'),('D4','Đơn bán và tiền thu','HOADON, CHITIET_HD, CHITIET_THANHTOAN, HoaDonUuDaiSnapshot'),
('D5','Kho','NGUYENVATLIEU, MaterialOpening, phiếu/chi tiết nhập–xuất–kiểm kê, TIEUHAO_HOADON'),('D6','Công nợ','CONGNO_NCC, THANHTOAN_CONGNO'),
('D7','Thu chi','PHIEUTHUCHI và liên kết nguồn'),('D8','Nhân sự và tài sản đích','NHANVIEN, CHUCVU, CALAMVIEC, CHAMCONG, BANGLUONG, TAISAN_CODINH')],[1.2,4,10.3]),
'flows':('Từ điển gói dữ liệu qua các mức',['Mã','Dữ liệu vào','Dữ liệu ra / đối ứng'],[
('F01a / F01b','Tên đăng nhập, mật khẩu, yêu cầu kiểm tra/đăng xuất.','Kết quả xác thực, vai trò, phiên hoặc kết thúc phiên.'),
('F02a / F02b','Hồ sơ, danh mục, công thức, chính sách, điều chỉnh điểm, từ khóa.','Danh mục/hồ sơ, điểm/hạng, kết quả cập nhật hoặc từ chối.'),
('F03a / F03b','Loại đơn/bàn, món, khách/KM, phân bổ, preview, mã tra cứu/in, bước giao.','Đơn, preview/phải trả, kết quả chốt/đối chiếu, chi tiết/bản in và trạng thái giao.'),
('F04a / F04b','Nguyên liệu, mốc mở sổ, dòng nhập/xuất, NCC, số trả ngay, snapshot/số đếm.','Hồ sơ/tồn, mã chứng từ, kết quả chốt/đối chiếu, liên kết chi/nợ.'),
('F05a / F05b','Thu chi thường, yêu cầu trả nợ, mã nợ và khoảng ngày tra cứu.','Phiếu thu chi, lịch sử/số dư; STORE chỉ nhận phần xem nợ.'),
('F06a / F06b','Hồ sơ nhân sự/tài sản, giờ công, kỳ lương, bộ lọc theo thiết kế đích.','Hồ sơ, kết quả công/lương/tài sản, báo cáo đích.'),
('F07 / F08','P4 → P5: nguồn phiếu nhập, trả ngay và nợ.','P5 → P4: mã/liên kết chi, nợ đã tạo cùng nghiệp vụ.'),
('F09 / F10','P6 → P5: nguồn chi lương đã chốt (thiết kế đích).','P5 → P6: mã/liên kết phiếu chi lương (thiết kế đích).')],[2.6,6.3,6.6]),
'changes':('Các thay đổi cấu trúc đáng chú ý sau baseline',['Đối tượng','Bổ sung / thay đổi','Nguồn'],[
('TAIKHOAN','LoginKey computed và SecurityStamp rowversion.','002'),('Các danh mục dùng chung','CatalogRevision rowversion theo danh sách migration.','003'),
('NVL, SP, BAN, KHUVUC, KH, KM, HOADON','Revision rowversion theo từng đối tượng.','004–008, 010'),('PHIEUTHUCHI','MaPN, MaBL tùy chọn; OriginKind; FK và unique index nguồn.','017'),
('PHIEUNHAP','DaChot, Revision, CutoverKind, NgayChot.','018'),('PHIEUXUAT','DaChot, Revision, CutoverKind, NgayChot, LyDo.','019'),
('PHIEUKIEMKE','SnapshotAt, Revision, CutoverKind.','020'),('CHITIET_KK','MaterialRevision; TonThucTe chuyển thành cho phép NULL khi chưa đếm.','020')],[4,9.1,2.4]),
'normalization':('Phụ thuộc hàm và nhận xét các quan hệ đại diện',['Quan hệ / khóa','Phụ thuộc có ý nghĩa','Nhận xét'],[
('DANHMUC_SP; MaDM','MaDM → TenDM; TenDM UNIQUE cũng là khóa ứng viên.','Theo tập thuộc tính đã khai báo, không có thuộc tính không khóa phụ thuộc bắc cầu.'),
('CONGTHUC; (MaSP, MaNVL)','(MaSP, MaNVL) → DinhMuc.','Định mức phụ thuộc cả món và nguyên liệu; không lưu TenSP/TenNVL trong công thức.'),
('CHITIET_PN; (MaPN, MaNVL)','(MaPN, MaNVL) → SoLuong, DonGiaNhap.','Giá nhập thuộc lần nhập, không chỉ phụ thuộc MaNVL.'),
('CHITIET_HD; MaDong','MaDong → MaHD, MaSP, SoLuong, GiaBan, GhiChu.','Không có MaSP → GiaBanDong cho mọi thời điểm; không tự ép gộp các dòng cùng món.'),
('CONGNO_NCC; MaCN; MaPN UNIQUE','MaCN → MaPN, TienNo, HanTra; MaPN → MaCN.','MaNCC lấy qua PHIEUNHAP, không lặp trên bảng nợ.'),
('THANHTOAN_CONGNO; MaTT; MaTC UNIQUE','MaTT → MaCN, MaTC, MaPT.','Tiền thực trả nằm ở phiếu chi liên kết, không cộng thêm một số tiền trùng.'),
('HOADON; MaHD','MaHD → thuộc tính đơn; các thành phần tiền → ThanhTien.','Có thuộc tính dẫn xuất; không kết luận toàn bảng vật lý ở 3NF chỉ từ PK.')],[4,5.6,5.9]),
'ui-inputs':('Đầu vào và kiểm soát của các màn cập nhật',['Màn / dữ liệu','Kiểm soát chính','Kết quả'],[
('Sản phẩm/công thức','Tên, danh mục, giá dương, định mức dương, tham chiếu NVL.','Danh mục/giá và định mức hợp lệ; không sửa giá dòng bán cũ.'),
('Nguyên liệu','Tên/đơn vị; mở sổ theo nguồn; tồn không sửa trực tiếp sau mở.','Hồ sơ NVL; tồn theo giao dịch.'),('Khách hàng','Tên, SDT tùy chọn chuẩn hóa NULL; trùng SDT theo ràng buộc.','Hồ sơ và hạng/điểm theo chính sách; không nhập số dư tùy ý.'),
('Danh mục nền','Độ dài, tên trùng, giá trị hệ thống và dữ liệu tham chiếu.','Kết quả tạo/sửa/xóa hoặc từ chối có lý do.')],[3.5,7,5]),
'ui-states':('Các trạng thái cần phân biệt khi xử lý giao dịch',['Trạng thái','Biểu hiện','Hành động tiếp'],[
('Đang tải / rỗng','Đang truy vấn khác với truy vấn thành công không có dòng.','Chờ hoặc đổi bộ lọc; không hiển thị số tổng 0 thay lỗi DB.'),('Lỗi nhập','Thông báo cạnh trường; giữ dữ liệu.','Sửa input rồi xác nhận lại khi hợp lệ.'),
('Xung đột','Phiên bản đơn hoặc snapshot đã đổi.','Tải lại, đối chiếu và xác nhận dữ liệu mới.'),('Đang ghi','Khóa gửi lặp và thao tác liên quan.','Chờ kết quả; không tự tạo request khác.'),
('Chưa biết kết quả','Timeout/mất kết nối sau gửi.','Đối chiếu mã yêu cầu/chứng từ trước khi thử lại.'),('Đã chốt / lỗi in','Commit đã xác nhận; có thể in thất bại.','Xem hoặc in lại; không ghi tiền/kho lần nữa.')],[3.2,6.1,6.2])}

bib=[
('Hồ sơ dự án CoffeeManager','Kế hoạch dự án WinForms Quản lí quán cafe và các cập nhật trạng thái','Tài liệu nội bộ dự án, phiên bản 3.3 cùng cập nhật đến 27/10/2026; docs/KeHoach_QuanLiQuanCafe.md.'),
('Hồ sơ dự án CoffeeManager','Thiết kế UX và hướng dẫn UI Orbit Café','Tài liệu nội bộ dự án, cập nhật đến 26/10/2026; docs/UX_QuanLiQuanCafe.md và docs/ui/.'),
('University of Cape Town, Computer Science Department','Elements of data-flow diagrams','MSc-IT Study Material, January 2011 Edition. https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s05.html. Truy cập 27/10/2026.'),
('University of Cape Town, Computer Science Department','Context diagrams','MSc-IT Study Material, January 2011 Edition. https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s06.html. Truy cập 27/10/2026.'),
('Adrienne Watt, Nelson Eng','Database Design 2nd Edition, Chapter 8 The Entity Relationship Data Model','BCcampus, 2014. https://opentextbc.ca/dbdesign01/chapter/chapter-8-entity-relationship-model/. Truy cập 27/10/2026.'),
('Hồ sơ dự án CoffeeManager','Mã nguồn và thiết kế CSDL','Tài liệu nội bộ, bản đối chiếu 27/10/2026; database/DB_Schema.sql, Insert_Data.sql, migrations/001–021 và src/CafeManager.*. Hash nguồn lưu trong hồ sơ biên soạn.'),
('Adrienne Watt','Database Design 2nd Edition, Chapter 11 Functional Dependencies','BCcampus, 2014. https://opentextbc.ca/dbdesign01/chapter/chapter-11-functional-dependencies/. Truy cập 27/10/2026.'),
('Adrienne Watt','Database Design 2nd Edition, Chapter 12 Normalization','BCcampus, 2014. https://opentextbc.ca/dbdesign01/chapter/chapter-12-normalization/. Truy cập 27/10/2026.'),
('Microsoft','Windows Forms Overview','Microsoft Learn, không ghi năm xuất bản trong bản tra cứu. https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/. Truy cập 27/10/2026.'),
('Microsoft','Transactions Transact SQL','Microsoft Learn, không ghi năm xuất bản trong bản tra cứu. https://learn.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql. Truy cập 27/10/2026.'),
('Microsoft','Table Valued Parameters','Microsoft Learn, không ghi năm xuất bản trong bản tra cứu. https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters. Truy cập 27/10/2026.'),
('Hồ sơ dự án CoffeeManager','Báo cáo triển khai và kiểm thử theo nhóm','Tài liệu nội bộ, các hồ sơ docs/tasks/g01…g05 và embedded-dialogs; đối chiếu đến ngày 27/10/2026, không thay thế nghiệm thu bản hiện tại.'),
('FindGlocal','Ther Thẩn Cafe, bài đăng ngày 01/07/2026 được lưu trên trang tổng hợp','Nguồn đối chiếu tên và địa chỉ, không dùng xác nhận nội bộ quán. https://www.findglocal.com/VN/Ho-Chi-Minh-City/696962493504088/Ther-Th%E1%BA%A9n-Cafe. Truy cập 27/10/2026.')]

def dictionary():
    for title,names in groups:
        a=p(title);a.runs[0].bold=True;a.paragraph_format.keep_with_next=True
        for name in names:
            t=tables[name];rows=[]
            for f in t['fields']:
                marks=[]
                if f['name'] in t['pk']:marks.append('PK')
                if f['name'] in t['fk']:
                    tar,key=t['fk'][f['name']];marks.append(f'FK → {tar}({key})')
                marks.append('NULL' if f['nullable'] else 'NOT NULL')
                if f['type']=='Computed':marks=['Biểu thức tính; không nhập trực tiếp']
                rows.append((f['name'],f['type'].lower() if f['type']!='Computed' else 'Tính toán','; '.join(marks)))
            table(f'{VN[name]} — {name}',['Thuộc tính','Kiểu dữ liệu','Khóa / khả năng NULL'],rows,[4.5,3.5,7.5])

def relations():
    for title,names in groups:
        a=p(title);a.runs[0].bold=True;a.paragraph_format.keep_with_next=True
        for name in names:
            t=tables[name];a=p();a.paragraph_format.space_before=Pt(3);a.paragraph_format.space_after=Pt(3)
            a.add_run(name+' (').bold=True
            for i,f in enumerate(t['fields']):
                if i:a.add_run(', ')
                r=a.add_run(f['name']);r.underline=f['name'] in t['pk']
                if f['name'] in t['fk']:r.italic=True
            a.add_run(').')
    p('Quy ước: thuộc tính gạch chân thuộc PK; in nghiêng là FK. Một thuộc tính có thể đồng thời có cả hai. Danh sách trên là lược đồ baseline; cột bổ sung và thay đổi NULL ở migration phải đọc cùng bảng các thay đổi cấu trúc trước đó.')

content=(TASK/'noi-dung.md').read_text(encoding='utf-8')
blocks=content.split('\n\n')
for block_index,block in enumerate(blocks):
    block=block.strip()
    if not block:continue
    if block.startswith('# '):
        text=block[2:]
        if text.startswith('CHƯƠNG '):chapter=int(re.match(r'CHƯƠNG (\d)',text)[1])
        a=heading(text,1)
        if text.startswith('CHƯƠNG 1'):a.paragraph_format.page_break_before=False
        if text=='KẾT LUẬN VÀ KIẾN NGHỊ':a.style=doc.styles['UnnumberedHeading'];a.paragraph_format.page_break_before=True;a.alignment=WD_ALIGN_PARAGRAPH.CENTER;a.runs[0].bold=True
    elif block.startswith('#### '):heading(block[5:],4)
    elif block.startswith('### '):heading(block[4:],3)
    elif block.startswith('## '):heading(block[3:],2)
    elif block.startswith(':::figure '):
        name,title=[v.strip() for v in block[len(':::figure '):].split('|',1)]
        next_block=blocks[block_index+1].strip() if block_index+1<len(blocks) else ''
        figure(name,title,bool(next_block) and not next_block.startswith(('#',':::')))
    elif block.startswith(':::missing '):
        name,title,note=[v.strip() for v in block[len(':::missing '):].split('|',2)]
        a=p('['+name+' — CHỜ BỔ SUNG ẢNH] '+title+'. '+note);a.runs[0].italic=True
        register.append((name,'Chưa cấp',title,'Chưa có ảnh phù hợp',note,'Cần bổ sung'))
    elif block==':::dictionary':dictionary()
    elif block==':::relations':relations()
    elif block.startswith(':::table '):table(*data[block.split()[1]])
    elif block==':::bibliography':
        a=p('TÀI LIỆU THAM KHẢO',style='UnnumberedHeading');a.paragraph_format.page_break_before=True;a.alignment=WD_ALIGN_PARAGRAPH.CENTER;a.runs[0].bold=True
        for i,(author,title,rest) in enumerate(bib,1):
            a=p();a.add_run(f'[{i}]. ');a.add_run(author+'. ').bold=True;a.add_run(title+'. ').italic=True;a.add_run(rest)
    else:p(block.replace('\n',' ').replace('**',''))

# Style riêng cho hai danh mục tự động, giữ caption trường SEQ theo chương.
for n in ['FigureCaption','TableCaption']:
    if n not in doc.styles:
        s=doc.styles.add_style(n,1);s.base_style=doc.styles['Caption'];s.font.name='Times New Roman';s.font.size=Pt(13);s.font.color.rgb=RGBColor(0,0,0)
for a in doc.paragraphs:
    if a.style.name=='Caption':
        a.style=doc.styles['FigureCaption' if a.text.startswith('Hình') else 'TableCaption']
for el in doc.element.iter(qn('w:instrText')):
    if 'Caption,1' in (el.text or '') and 'FigureCaption' not in el.text:el.text=' TOC \\h \\z \\t "TableCaption,1" '

dst=OUT/'BaoCao_CoffeeManager.docx';doc.save(dst)
(TASK/'figure-register.md').write_text('# Danh sách hình và vị trí ảnh\n\nẢnh lưu được dùng có nhãn ngày, không chứng nhận bản hiện tại. SD01/UI03/UI05/UI06/UI07 chưa có dữ liệu hoặc ảnh phù hợp.\n\n| Mã | Số | Caption | Nguồn | Kích thước / ghi chú | Trạng thái |\n|---|---|---|---|---|---|\n'+'\n'.join('| '+' | '.join(str(v).replace('|','/') for v in row)+' |' for row in register),encoding='utf-8')
paths=[ROOT/'docs/KeHoach_QuanLiQuanCafe.md',ROOT/'docs/UX_QuanLiQuanCafe.md',ROOT/'database/DB_Schema.sql',ROOT/'database/Insert_Data.sql']
paths+=sorted((ROOT/'database/migrations').glob('*.sql'))
paths+=sorted((ROOT/'src').rglob('*.csproj'))
paths+=[ROOT/'src/CafeManager.WinForms/Program.cs',ROOT/'src/CafeManager.Business/Services/SalesService.cs',ROOT/'src/CafeManager.Business/Services/SalesService.Checkout.cs',ROOT/'src/CafeManager.DataAccess/Repositories/SalesRepository.cs',ROOT/'docs/tasks/g05-kho-nguon-chi-cong-no/report.md',ROOT/'docs/tasks/g04-ban-hang-vong-doi-hoa-don/report.md',ROOT/'docs/tasks/embedded-dialogs/native-test-results.md']
paths=[x for x in paths if 'obj' not in x.parts and 'bin' not in x.parts]
manifest=[{'path':str(x.relative_to(ROOT)),'sha256':hashlib.sha256(x.read_bytes()).hexdigest()} for x in paths]
(TASK/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'docx':str(dst),'baseline_tables':len(tables),'baseline_fields':sum(len(t['fields']) for t in tables.values()),'figures':len(register),'paragraphs':len(doc.paragraphs),'tables':len(doc.tables)},ensure_ascii=False))
