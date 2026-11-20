from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
from shutil import copy2
from lxml import etree
import re,json,hashlib,sys

sys.stdout.reconfigure(encoding='utf-8')
task=Path(__file__).resolve().parent;root=task.parents[2]
path=root/'artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx'
run=task/'revisions/20261110-heading-black';run.mkdir(exist_ok=True,parents=True)
backup=run/'before.docx'
if backup.exists():raise RuntimeError('Backup exists; inspect before repeating.')
copy2(path,backup)
W='http://schemas.openxmlformats.org/wordprocessingml/2006/main'
W14='http://schemas.microsoft.com/office/word/2010/wordml'
ns={'w':W,'w14':W14}
def t(v):return '{'+W+'}'+v
def get_child(parent,name):
    x=parent.find(t(name))
    if x is None:x=etree.SubElement(parent,t(name))
    return x
def black(rpr,fill=False):
    for x in list(rpr):
        if etree.QName(x).localname in ['color','textFill','textOutline']:rpr.remove(x)
    c=etree.SubElement(rpr,t('color'));c.set(t('val'),'000000')
    if fill:
        f=etree.SubElement(rpr,'{'+W14+'}textFill');s=etree.SubElement(f,'{'+W14+'}solidFill');c=etree.SubElement(s,'{'+W14+'}srgbClr');c.set('{'+W14+'}val','000000')
with ZipFile(backup) as z:
    data={n:z.read(n) for n in z.namelist()}
styles=etree.fromstring(data['word/styles.xml']);ids=set()
for s in styles.findall(t('style')):
    name=s.find(t('name'));label='' if name is None else name.get(t('val'),'')
    if re.search(r'heading\s*\d',label,re.I) or s.find('./'+t('pPr')+'/'+t('outlineLvl')) is not None:
        ids.add(s.get(t('styleId')))
        link=s.find(t('link'))
        if link is not None:ids.add(link.get(t('val')))
for s in styles.findall(t('style')):
    if s.get(t('styleId')) in ids:
        black(get_child(s,'rPr'))
        for auto in list(s.findall(t('autoRedefine'))):s.remove(auto)
data['word/styles.xml']=etree.tostring(styles,encoding='UTF-8',xml_declaration=True,standalone=True)
d=etree.fromstring(data['word/document.xml']);before_text=d.xpath('//w:t/text()',namespaces=ns);fixed=[]
for p in d.iter(t('p')):
    name=p.find('./'+t('pPr')+'/'+t('pStyle'));sid=None if name is None else name.get(t('val'))
    text=''.join(p.xpath('.//w:t/text()',namespaces=ns))
    if sid not in ids and not re.match(r'^\s*\d+(?:\.\d+)+(?:\.)?\s+',text):continue
    black(get_child(get_child(p,'pPr'),'rPr'),True)
    for r in p.iter(t('r')):black(get_child(r,'rPr'),True)
    fixed.append(text)
assert before_text==d.xpath('//w:t/text()',namespaces=ns)
data['word/document.xml']=etree.tostring(d,encoding='UTF-8',xml_declaration=True,standalone=True)
if 'word/numbering.xml' in data:
    numbering=etree.fromstring(data['word/numbering.xml'])
    for lvl in numbering.iter(t('lvl')):black(get_child(lvl,'rPr'),True)
    data['word/numbering.xml']=etree.tostring(numbering,encoding='UTF-8',xml_declaration=True,standalone=True)
with ZipFile(path,'w',ZIP_DEFLATED) as z:
    for n,b in data.items():z.writestr(n,b)
(run/'fixed-headings.json').write_text(json.dumps({'heading_style_ids':sorted(ids),'heading_paragraphs':fixed,'text_unchanged':True,'backup':str(backup)},ensure_ascii=False,indent=2),encoding='utf-8')
print('Fixed',len(fixed),'paragraphs and',len(ids),'heading/linked styles; all text unchanged.')
