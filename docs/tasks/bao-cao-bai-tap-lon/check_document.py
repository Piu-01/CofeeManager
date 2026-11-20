from pathlib import Path
import re,json,hashlib,sys
from zipfile import ZipFile
from lxml import etree
from pypdf import PdfReader
import pypdfium2 as pdfium
from PIL import Image,ImageDraw
from docx import Document

sys.stdout.reconfigure(encoding='utf-8')
task=Path(__file__).resolve().parent;root=task.parents[2]
out=root/'artifacts/bao-cao-bai-tap-lon';qa=task/'qa'
pdf=qa/'BaoCao_CoffeeManager-review.pdf';r=PdfReader(pdf)
doc=Document(out/'BaoCao_CoffeeManager.docx')
checks={}
checks['a4_all_pdf_pages']=all(abs(float(p.mediabox.width)-595.28)<1 and abs(float(p.mediabox.height)-841.89)<1 for p in r.pages)
checks['three_word_sections']=len(doc.sections)==3
checks['section_dimensions_and_margins']=all(abs(s.page_width.cm-21)<.01 and abs(s.page_height.cm-29.7)<.01 and abs(s.left_margin.cm-3.5)<.01 and abs(s.right_margin.cm-2)<.01 and abs(s.top_margin.cm-2)<.01 and abs(s.bottom_margin.cm-2)<.01 for s in doc.sections)
checks['normal_font']=doc.styles['Normal'].font.name=='Times New Roman' and doc.styles['Normal'].font.size.pt==13
checks['paragraph_spacing']=doc.styles['Normal'].paragraph_format.space_before.pt==6 and doc.styles['Normal'].paragraph_format.space_after.pt==6 and abs(doc.styles['Normal'].paragraph_format.line_spacing-1.15)<.01
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
with ZipFile(out/'BaoCao_CoffeeManager.docx') as z:
 tree=etree.fromstring(z.read('word/document.xml'));fields=tree.xpath('//w:instrText/text()',namespaces=ns)
 checks['three_automatic_contents_fields']=sum('TOC ' in f for f in fields)==3
 checks['automatic_figure_table_numbers']=sum('SEQ ' in f for f in fields)==65
 checks['only_body_footer_numbered']=len([n for n in z.namelist() if n.startswith('word/footer') and n.endswith('.xml') and b'PAGE' in z.read(n)])==1
 checks['body_number_restart_at_one']=bool(tree.xpath('//w:sectPr/w:pgNumType[@w:start="1"]',namespaces=ns))
 checks['no_heading_number_on_conclusion']=any(p.text=='KẾT LUẬN VÀ KIẾN NGHỊ' and p.style.name=='UnnumberedHeading' for p in doc.paragraphs)
text='\n'.join(p.extract_text() or '' for p in r.pages)
checks['no_unresolved_word_fields']=not any(x in text for x in ['Cập nhật trường trong Word','Error!','Bookmark not defined','Lỗi!'])
toc='\n'.join((p.extract_text() or '') for p in r.pages[4:6])
checks['toc_includes_conclusion_and_references']='KẾT LUẬN VÀ KIẾN NGHỊ' in toc and 'TÀI LIỆU THAM KHẢO' in toc
schema=json.loads((task/'schema-baseline.json').read_text(encoding='utf-8'))
checks['baseline_35_tables']=len(schema)==35
spec=json.loads((out/'so-do/diagram-specifications.json').read_text(encoding='utf-8'))
bad=[]
extra={('HoaDonUuDaiSnapshot','MaHD','HOADON','MaHD'),('PHIEUTHUCHI','MaPN','PHIEUNHAP','MaPN'),('PHIEUTHUCHI','MaBL','BANGLUONG','MaBL')}
for item in spec.values():
 for c,f,p,k in item.get('foreign_keys',[]):
  if (c,f,p,k) not in extra and tuple(schema[c]['fk'].get(f,[]))!=(p,k):bad.append([c,f,p,k])
checks['diagram_foreign_keys_match_source']=not bad
manifest=json.loads((task/'source-manifest.json').read_text(encoding='utf-8'))
changed=[x['path'] for x in manifest if hashlib.sha256((root/x['path']).read_bytes()).hexdigest()!=x['sha256']]
checks['source_snapshot_unchanged_since_build']=not changed
native=pdfium.PdfDocument(str(pdf));images=[];page_map=[];bounds=[];changed_images=[]
for i,page in enumerate(native):
 imgpath=qa/f'page-{i+1:02}.png';im=page.render(scale=1.5).to_pil().convert('RGB')
 if not imgpath.exists() or Image.open(imgpath).tobytes()!=im.tobytes():changed_images.append(i+1)
 im.save(imgpath);images.append(im)
 s=' '.join((r.pages[i].extract_text() or '').split());page_map.append({'pdf_page':i+1,'characters':len(s),'start':s[:140]})
 tp=page.get_textpage()
 for c in range(tp.count_chars()):
  ch=tp.get_text_range(c,1)
  if not ch.strip():continue
  left,bottom,right,top=tp.get_charbox(c)
  if left<0 or right>page.get_width()+1 or bottom<0 or top>page.get_height()+1:bounds.append([i+1,c,ch])
checks['rendered_cover_pages_identical']=images[0].tobytes()==images[1].tobytes()
checks['all_text_inside_page']=not bounds
for start in range(0,len(images),4):
 sheet=Image.new('RGB',(1250,1800),'#cccccc');d=ImageDraw.Draw(sheet)
 for j in range(min(4,len(images)-start)):
  im=images[start+j].copy();im.thumbnail((600,850));x=j%2*625+(625-im.width)//2;y=j//2*900+30
  sheet.paste(im,(x,y));d.text((j%2*625+12,j//2*900+7),f'PDF page {start+j+1}',fill='black')
 sheet.save(qa/f'contact-final-{start//4+1:02}.png')
result={'checks':checks,'page_count':len(r.pages),'tables':len(doc.tables),'figures_seq_count':sum('SEQ Hinh' in f for f in fields),'tables_seq_count':sum('SEQ Bang' in f for f in fields),'foreign_key_errors':bad,'source_changes':changed,'out_of_page_characters':bounds,'changed_images_since_previous_render':changed_images}
(qa/'checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
(qa/'page-map.json').write_text(json.dumps(page_map,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
