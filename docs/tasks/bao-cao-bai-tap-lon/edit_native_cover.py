from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from copy import deepcopy as cp
from lxml import etree as E
from PIL import Image
from io import BytesIO
import numpy as np
import shutil, json

ROOT=Path(__file__).resolve().parents[3]
REV=Path(__file__).parent/'revisions/20261110-native-cover'
DOC=ROOT/'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx'
REV.mkdir(exist_ok=True,parents=True)
if not (REV/'before.docx').exists():shutil.copy2(DOC,REV/'before.docx')
W='http://schemas.openxmlformats.org/wordprocessingml/2006/main'
R='http://schemas.openxmlformats.org/officeDocument/2006/relationships'
V='urn:schemas-microsoft-com:vml'
NS={'w':W,'r':R,'v':V}
def q(s): return '{'+W+'}'+s
def xml(b):return E.fromstring(b)
def dump(x):return E.tostring(x,xml_declaration=True,encoding='UTF-8',standalone=True)
with ZipFile(DOC) as z: parts={n:z.read(n) for n in z.namelist()}
with ZipFile(r'C:\Users\Lenovo\Downloads\BÁOCAO.docx') as z: src={n:z.read(n) for n in z.namelist()}
d=xml(parts['word/document.xml']); body=d.find(q('body'))
sd=xml(src['word/document.xml']); sb=sd.find(q('body'))

# Trace the supplied ornament into editable, filled freeform paths.
mask=np.array(Image.open(BytesIO(src['word/media/image2.jpg'])).convert('L'))<150
edges={}
for y,x in np.argwhere(mask):
 x=int(x);y=int(y)
 for a,b,exposed in [((x,y),(x+1,y),y==0 or not mask[y-1,x]),((x+1,y),(x+1,y+1),x==mask.shape[1]-1 or not mask[y,x+1]),((x+1,y+1),(x,y+1),y==mask.shape[0]-1 or not mask[y+1,x]),((x,y+1),(x,y),x==0 or not mask[y,x-1])]:
  if exposed:edges.setdefault(a,[]).append(b)
paths=[]
while edges:
 start=next(iter(edges));a=start;points=[a]
 while True:
  targets=edges[a];b=targets.pop()
  if not targets:del edges[a]
  points.append(b);a=b
  if a==start:break
 # Remove collinear pixel steps, preserving the original ornament contours.
 simp=[]
 for i in range(len(points)-1):
  prev=points[i-1 if i else -2];p=points[i];n=points[i+1]
  if (p[0]-prev[0])*(n[1]-p[1]) != (p[1]-prev[1])*(n[0]-p[0]):simp.append(p)
 if len(simp)>=3:paths.append('m '+','.join(map(str,simp[0]))+' l '+','.join(str(c) for p in simp[1:] for c in p)+' x')
vpath=' '.join(paths)+' e'

# Import cover paragraph styles, keeping the report's existing styles intact.
styles=xml(parts['word/styles.xml']); sstyles=xml(src['word/styles.xml'])
sm={s.get(q('styleId')):s for s in sstyles}
needed=set()
def add_style(sid):
 if sid in needed or sid not in sm:return
 needed.add(sid)
 for ref in sm[sid].xpath('./w:basedOn|./w:next|./w:link',namespaces=NS):add_style(ref.get(q('val')))
for p in list(sb)[:2]:
 for ref in p.xpath('.//w:pStyle|.//w:rStyle',namespaces=NS):add_style(ref.get(q('val')))
for sid in needed:
 s=cp(sm[sid]);s.set(q('styleId'),'Cover_'+sid)
 s.attrib.pop(q('default'),None)
 name=s.find(q('name'))
 if name is not None:name.set(q('val'),'Cover '+sid)
 for ref in s.xpath('./w:basedOn|./w:next|./w:link',namespaces=NS):
  if ref.get(q('val')) in needed:ref.set(q('val'),'Cover_'+ref.get(q('val')))
 styles.append(s)
parts['word/styles.xml']=dump(styles)
rels=xml(parts['word/_rels/document.xml.rels']);pr='{http://schemas.openxmlformats.org/package/2006/relationships}'
logo_id='rIdCoverLogo'
E.SubElement(rels,pr+'Relationship',Id=logo_id,Type=R+'/image',Target='media/cover-logo.jpg')
parts['word/media/cover-logo.jpg']=src['word/media/image1.jpg']
parts['word/_rels/document.xml.rels']=dump(rels)
ct=xml(parts['[Content_Types].xml']);ctns='{http://schemas.openxmlformats.org/package/2006/content-types}'
if not any(x.get('Extension')=='jpg' for x in ct):E.SubElement(ct,ctns+'Default',Extension='jpg',ContentType='image/jpeg')
parts['[Content_Types].xml']=dump(ct)

covers=[]
for idx,original in enumerate(list(sb)[:2]):
 p=cp(original)
 for s in p.xpath('.//w:sectPr',namespaces=NS):s.getparent().remove(s)
 for ref in p.xpath('.//w:pStyle|.//w:rStyle',namespaces=NS):ref.set(q('val'),'Cover_'+ref.get(q('val')))
 # Replace strings in both DrawingML and VML text-box representations.
 for para in p.xpath('.//w:txbxContent//w:p',namespaces=NS):
  ts=para.xpath('.//w:t',namespaces=NS);text=''.join(t.text or '' for t in ts)
  new=None
  if text.startswith('HỌC PHẦN:'):new='HỌC PHẦN: PHÂN TÍCH THIẾT KẾ HỆ THỐNG THÔNG TIN'
  elif text.startswith('ĐỀ TÀI:'):new='ĐỀ TÀI: QUẢN LÍ QUÁN CAFE'
  elif text.startswith('Sinh viên thực hiện:'):new='Sinh viên thực hiện: NGUYỄN MINH PHÚ'
  elif text.startswith('Mã số sinh viên:'):new='Mã số sinh viên: 6551071065'
  elif text.startswith('Khóa:'):new='Khóa: 64'
  elif text.startswith('Tp. Hồ Chí Minh,'):new='Tp. Hồ Chí Minh, năm 2026'
  if new and ts:
   ts[0].text=new
   for t in ts[1:]:t.text=''
  if text.startswith('HỌC PHẦN:'):
   for sz in para.xpath('.//w:sz|.//w:szCs',namespaces=NS):sz.set(q('val'),'26')
 # Original first run contains the editable cover text and university logo.
 runs=p.findall(q('r'))
 for r in runs[1:]:p.remove(r)
 for e in p.iter():
  for key,val in list(e.attrib.items()):
   if key in ['{'+R+'}embed','{'+R+'}id'] and val=='rId10':e.set(key,logo_id)
 # Native vector ornament; no full-page picture is inserted.
 r=E.SubElement(p,q('r'));pict=E.SubElement(r,q('pict'))
 shape=E.SubElement(pict,'{'+V+'}shape',id='CoverFrame'+str(idx+1),coordorigin='0,0',coordsize='1440,1939',path=vpath,fillcolor='#000000',stroked='f',style='position:absolute;margin-left:84.0pt;margin-top:44.6pt;width:457.15pt;height:745.55pt;z-index:-251658240;mso-position-horizontal-relative:page;mso-position-vertical-relative:page')
 # VML winding fill preserves white spaces enclosed by the traced contours.
 E.SubElement(shape,'{'+V+'}fill',color='#000000')
 covers.append(p)

old_first=list(body)[:4]
boundary=cp(old_first[-1])
for child in list(boundary):
 if child.tag!=q('pPr'):boundary.remove(child)
for p in old_first:body.remove(p)
br=E.Element(q('p'));rr=E.SubElement(br,q('r'));E.SubElement(rr,q('br'),{q('type'):'page'})
for i,p in enumerate([covers[0],br,covers[1],boundary]):body.insert(i,p)
# Match the supplied template's cover margins only.
sects=d.xpath('//w:body//w:sectPr',namespaces=NS)
cover_mar=sects[0].find(q('pgMar'))
cover_mar.set(q('left'),'1701');cover_mar.set(q('top'),'1418')
cover_mar.set(q('right'),'1418');cover_mar.set(q('bottom'),'1418')

paras=body.findall(q('p'));ack=next(i for i,p in enumerate(paras) if ''.join(p.xpath('.//w:t/text()',namespaces=NS))=='LỜI MỞ ĐẦU')
texts=[
 'LỜI CẢM ƠN',
 'Em xin gửi lời cảm ơn đến cô ThS. Phạm Thị Miên, giảng viên học phần Phân tích thiết kế hệ thống thông tin. Những kiến thức của học phần là cơ sở để em thực hiện bài tập lớn với đề tài quản lí quán cafe.',
 'Qua quá trình thực hiện đề tài CoffeeManager, em có dịp vận dụng kiến thức về phân tích chức năng, biểu đồ luồng dữ liệu và thiết kế cơ sở dữ liệu vào một bài toán quản lí cụ thể. Việc xây dựng báo cáo cũng giúp em nhận ra những điểm còn thiếu trong cách mô tả nghiệp vụ và tổ chức hệ thống.',
 'Do kiến thức và kinh nghiệm còn hạn chế, báo cáo khó tránh khỏi thiếu sót. Em mong nhận được ý kiến góp ý của cô để hoàn thiện bài làm và rút kinh nghiệm cho những đề tài sau. Em xin chân thành cảm ơn cô.'
]
for p,text in zip(paras[ack:ack+4],texts):
 ts=p.xpath('.//w:t',namespaces=NS);ts[0].text=text
 for t in ts[1:]:t.text=''
pg=E.Element(q('pgNumType'),{q('fmt'):'upperRoman',q('start'):'1'})
sects[1].insert(next(i for i,c in enumerate(sects[1]) if c.tag==q('cols')),pg)
# Front matter footer is already independent; insert the same PAGE field as body.
front=xml(parts['word/footer1.xml']);back=xml(parts['word/footer2.xml'])
for child in list(front):front.remove(child)
for child in back:front.append(cp(child))
parts['word/footer1.xml']=dump(front)
parts['word/document.xml']=dump(d)
with ZipFile(DOC,'w',ZIP_DEFLATED) as z:
 for n,b in parts.items():z.writestr(n,b)
print(json.dumps({'vector_contours':len(paths),'vector_path_chars':len(vpath),'cover_text_editable':True,'sections':len(sects)},ensure_ascii=False))
