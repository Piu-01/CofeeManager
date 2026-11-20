"""Author report diagrams and verify DFD boundary conservation, without running SQL."""
from pathlib import Path
import json, textwrap

HERE=Path(__file__).parent
OUT=HERE/'revisions/20261110-content-models/diagrams'
OUT.mkdir(parents=True,exist_ok=True)
graphs={}
def quote(x):return json.dumps(x,ensure_ascii=False)
def wrap(s,width=22):return '\n'.join(textwrap.wrap(s,width=width,break_long_words=False))
class Graph:
 def __init__(self,name,direction='LR',engine='dot'):
  self.name=name;self.engine=engine;self.nodes={};self.edges=[];self.extra=[]
  self.options=f'rankdir={direction}; graph [bgcolor="white", pad="0.12", nodesep="0.35", ranksep="0.75", splines=polyline, overlap=false];'
 def node(self,id,label,kind='process',**attrs):
  shapes={'process':'box','actor':'box','store':'box','entity':'box','relationship':'diamond','attribute':'ellipse'}
  defaults={'label':wrap(label), 'shape':shapes[kind],'fontname':'Times New Roman','fontsize':'20','color':'black','fontcolor':'black','penwidth':'1.4','margin':'0.10,0.08'}
  if kind=='process':defaults['style']='rounded'
  if kind=='store':defaults['shape']='plain';defaults['label']=f'<<TABLE BORDER="0" CELLBORDER="0" CELLSPACING="0"><TR><TD BORDER="1" SIDES="TB" CELLPADDING="8">{wrap(label).replace(chr(10),"<BR/>")}</TD></TR></TABLE>>'
  defaults.update(attrs);self.nodes[id]=(kind,defaults)
 def edge(self,a,b,label='',**attrs):
  x={'label':wrap(label,23),'fontname':'Times New Roman','fontsize':'17','color':'black','fontcolor':'black','penwidth':'1.2','arrowsize':'0.9'};x.update(attrs);self.edges.append((a,b,x))
 def save(self):
  def fmt(v):return v if isinstance(v,str) and v.startswith('<<') else quote(str(v))
  lines=['digraph G {',self.options]
  for id,(_,a) in self.nodes.items():lines.append(quote(id)+' ['+','.join(k+'='+fmt(v) for k,v in a.items())+'];')
  for a,b,x in self.edges:lines.append(quote(a)+' -> '+quote(b)+' ['+','.join(k+'='+fmt(v) for k,v in x.items())+'];')
  lines+=self.extra+['}'];(OUT/(self.name+'.dot')).write_text('\n'.join(lines),encoding='utf-8')
  graphs[self.name]={'engine':self.engine}

ACTORS={'admin':'Người quản lý','cash':'Nhân viên thu ngân','store':'Nhân viên kho'}
STORES={'account':'Tài khoản và quyền','catalog':'Danh mục nền','customer':'Hồ sơ khách hàng và điểm','sale':'Hồ sơ bán hàng','stock':'Hồ sơ kho','debt':'Hồ sơ công nợ','finance':'Chứng từ thu chi','hr':'Hồ sơ nhân sự và tài sản'}
PROCESS={'auth':'Xác thực và quản lý phiên','catalogue':'Quản lý dữ liệu nền và khách hàng','sales':'Bán hàng và thanh toán','inventory':'Quản lý kho','money':'Quản lý thu chi và công nợ','people':'Quản lý nhân sự, tài sản và báo cáo'}
# Each boundary item records input and output at a named child process.
SPEC={
 'auth':{'steps':['Kiểm tra thông tin đăng nhập','Xác lập phiên làm việc','Kiểm tra và kết thúc phiên'], 'flows':[
  ('admin','actor','Thông tin đăng nhập','Kết quả xác thực',0,1),('cash','actor','Thông tin đăng nhập','Kết quả xác thực',0,1),('store','actor','Thông tin đăng nhập','Kết quả xác thực',0,1),
  ('account','store','Hồ sơ xác thực','Trạng thái phiên đã kiểm tra',0,2)],'links':[(0,1,'Tài khoản đã xác thực'),(1,2,'Danh tính và quyền trong phiên')]},
 'catalogue':{'steps':['Cập nhật danh mục và chính sách','Tra cứu và cập nhật khách hàng','Ghi điều chỉnh điểm'], 'flows':[
  ('admin','actor','Yêu cầu cập nhật dữ liệu nền','Kết quả cập nhật dữ liệu nền',0,0),('cash','actor','Yêu cầu tìm hoặc thêm khách','Kết quả tìm hoặc thêm khách',1,1),('store','actor','Yêu cầu tra cứu danh mục','Danh mục được phép xem',0,0),
  ('catalog','store','Danh mục hiện có','Danh mục đã cập nhật',0,0),('customer','store','Hồ sơ khách hàng và điểm hiện có','Hồ sơ khách hàng và điểm đã cập nhật',1,2)],'links':[(1,2,'Khách hàng cần điều chỉnh điểm')]},
 'sales':{'steps':['Lập và sửa đơn đang mở','Xác định ưu đãi và số phải trả','Chốt thanh toán','Tra cứu, in và theo dõi giao hàng'], 'flows':[
  ('admin','actor','Yêu cầu xử lý đơn bán','Kết quả xử lý đơn bán',0,3),('cash','actor','Yêu cầu xử lý đơn bán','Kết quả xử lý đơn bán',0,3),
  ('catalog','store','Menu và chính sách ưu đãi','',1,1),('customer','store','Hồ sơ khách hàng và điểm','Lịch sử điểm từ bán hàng',1,2),('sale','store','Hóa đơn và tiền thu đã lưu','Hóa đơn và tiền thu đã cập nhật',0,2),('stock','store','Công thức và tồn nguyên liệu','Tiêu hao nguyên liệu đã ghi',2,2)],'links':[(0,1,'Đơn hàng cần tính tiền'),(1,2,'Đơn hàng và phân bổ đã xác nhận'),(2,3,'Hóa đơn đã chốt')]},
 'inventory':{'steps':['Quản lý nguyên liệu và tồn đầu kỳ','Ghi nhận nhập hàng','Ghi nhận xuất hàng','Đối chiếu và chốt kiểm kê'], 'flows':[
  ('admin','actor','Yêu cầu xử lý chứng từ kho','Kết quả xử lý chứng từ kho',0,3),('store','actor','Yêu cầu xử lý chứng từ kho','Kết quả xử lý chứng từ kho',0,3),
  ('catalog','store','Hồ sơ nhà cung cấp','',1,1),('stock','store','Tồn và chứng từ kho đã lưu','Tồn và chứng từ kho đã cập nhật',0,3),
  ('money','process','Liên kết chi và nợ đã tạo','Nguồn phiếu nhập cần ghi chi và nợ',1,1)],'links':[(0,1,'Nguyên liệu đủ điều kiện nhập'),(0,2,'Nguyên liệu đủ điều kiện xuất'),(0,3,'Tồn hệ thống tại lúc kiểm kê')]},
 'money':{'steps':['Ghi nhận thu chi thông thường','Ghi chi và nợ từ phiếu nhập','Ghi nhận trả nợ và tra cứu số dư','Ghi chi lương theo bảng đã chốt'], 'flows':[
  ('admin','actor','Yêu cầu thu chi hoặc trả nợ','Kết quả thu chi và trả nợ',0,2),('store','actor','Yêu cầu tra cứu công nợ','Số dư và lịch sử công nợ',2,2),
  ('catalog','store','Danh mục thu chi và phương thức trả','',0,0),('debt','store','Công nợ và lịch sử trả đã lưu','Công nợ và lịch sử trả đã cập nhật',2,2),('finance','store','Chứng từ thu chi đã lưu','Chứng từ thu chi đã ghi',0,0),
  ('inventory','process','Nguồn phiếu nhập cần ghi chi và nợ','Liên kết chi và nợ đã tạo',1,1),('people','process','Nguồn chi lương đã chốt','Liên kết phiếu chi lương',3,3)],'links':[(1,2,'Khoản nợ phát sinh từ nhập hàng'),(1,0,'Khoản trả ngay cần ghi chi'),(2,0,'Khoản trả nợ cần ghi chi'),(3,0,'Khoản chi lương cần ghi')]},
 'people':{'steps':['Quản lý nhân sự, chấm công và lương','Quản lý hồ sơ tài sản','Tổng hợp và đối chiếu báo cáo'], 'flows':[
  ('admin','actor','Yêu cầu nhân sự, tài sản và báo cáo','Hồ sơ và báo cáo theo yêu cầu',0,2),('hr','store','Hồ sơ nhân sự và tài sản đã lưu','Hồ sơ nhân sự và tài sản đã cập nhật',0,1),
  ('sale','store','Dữ liệu bán hàng đã ghi','',2,2),('stock','store','Dữ liệu kho đã ghi','',2,2),('debt','store','Dữ liệu công nợ đã ghi','',2,2),('finance','store','Dữ liệu thu chi đã ghi','',2,2),
  ('money','process','Liên kết phiếu chi lương','Nguồn chi lương đã chốt',0,0)],'links':[(0,2,'Kết quả nhân sự và lương'),(1,2,'Thông tin tài sản')]}
}

# Authentication reads the account store; checking a session does not create
# a business data store containing the password or a fictitious session row.
SPEC['auth']['flows'][-1]=('account','store','Hồ sơ xác thực','',0,2)
for i,f in enumerate(SPEC['auth']['flows'][:3]):
 SPEC['auth']['flows'][i]=(f[0],f[1],'Thông tin đăng nhập và yêu cầu phiên','Kết quả xác thực và trạng thái phiên',0,2)
SPEC['catalogue']['flows'][-1]=('customer','store','Hồ sơ khách hàng và điểm hiện có','Hồ sơ khách hàng đã cập nhật',1,1)
SPEC['catalogue']['flows'].append(('customer','store','','Lịch sử điều chỉnh điểm',2,2))
SPEC['catalogue']['links'] += [(0,1,'Yêu cầu cập nhật hồ sơ khách hàng'),(0,2,'Yêu cầu điều chỉnh điểm đã kiểm quyền')]
SPEC['inventory']['flows'][3]=('stock','store','Tồn và chứng từ kho đã lưu','Tồn đầu kỳ đã ghi',0,0)
SPEC['inventory']['flows'] += [('stock','store','','Phiếu nhập và tồn đã cập nhật',1,1),('stock','store','','Phiếu xuất và tồn đã cập nhật',2,2),('stock','store','','Kết quả kiểm kê đã chốt',3,3)]
SPEC['inventory']['links']=[(0,1,'Yêu cầu nhập và hồ sơ nguyên liệu'),(0,2,'Yêu cầu xuất và tồn hiện có'),(0,3,'Số đếm và tồn hệ thống tại lúc kiểm kê')]
SPEC['people']['flows'][1]=('hr','store','Hồ sơ nhân sự và tài sản đã lưu','Hồ sơ nhân sự và lương đã cập nhật',0,0)
SPEC['people']['flows'].append(('hr','store','','Hồ sơ tài sản đã cập nhật',1,1))
SPEC['people']['links'].append((0,1,'Yêu cầu quản lý tài sản đã kiểm quyền'))
SPEC['catalogue']['flows'].append(('stock','store','Nguyên liệu dùng trong công thức','',0,0))
SPEC['people']['flows'].append(('account','store','Tài khoản và quyền hiện có','Tài khoản và quyền đã cập nhật',0,0))
SPEC['sales']['flows'][2]=('catalog','store','Menu, công thức và chính sách ưu đãi','',1,1)
SPEC['sales']['flows'][5]=('stock','store','Tồn nguyên liệu hiện có','Tiêu hao nguyên liệu đã ghi',2,2)

ctx=Graph('context','LR');ctx.node('system','Hệ thống quản lý quán cafe CoffeeManager')
for a,label in ACTORS.items():
 ctx.node(a,label,'actor')
 ctx.edge(a,'system','Thông tin đăng nhập và yêu cầu nghiệp vụ')
 ctx.edge('system',a,'Kết quả xác thực và kết quả nghiệp vụ')
ctx.extra.append('{rank=same; admin; cash; store;}');ctx.save()

balances=[]
for key,s in SPEC.items():
 for detail in [False,True]:
  g=Graph(('detail-' if detail else 'top-')+key,'LR')
  procs=[f'{key}_{i}' for i in range(len(s['steps']))] if detail else [key]
  for i,p in enumerate(procs):g.node(p,s['steps'][i] if detail else PROCESS[key])
  ins=[];outs=[]
  for peer,kind,incoming,outgoing,si,so in s['flows']:
   label=ACTORS.get(peer) if kind=='actor' else STORES.get(peer) if kind=='store' else PROCESS[peer]
   g.node(peer,label,kind)
   if incoming:g.edge(peer,procs[si] if detail else key,incoming);ins.append((peer,incoming))
   if outgoing:g.edge(procs[so] if detail else key,peer,outgoing);outs.append((peer,outgoing))
  if detail:
   for a,b,label in s['links']:g.edge(procs[a],procs[b],label,weight='3')
   g.extra.append('{rank=same; '+ '; '.join(procs)+';}')
   actors=[p for p,k,*_ in s['flows'] if k=='actor'];stores=[p for p,k,*_ in s['flows'] if k=='store']
   if actors:g.extra.append('{rank=min; '+ '; '.join(actors)+';}')
   if stores:g.extra.append('{rank=max; '+ '; '.join(set(stores))+';}')
  else:
   actors=[p for p,k,*_ in s['flows'] if k=='actor'];stores=[p for p,k,*_ in s['flows'] if k=='store']
   if actors:g.extra.append('{rank=min; '+ '; '.join(actors)+';}')
   if stores:g.extra.append('{rank=max; '+ '; '.join(stores)+';}')
  g.save()
  balances.append({'process':key,'detail':detail,'inputs':ins,'outputs':outs})
 for entries in [balances[-2:]]:
  assert entries[0]['inputs']==entries[1]['inputs'] and entries[0]['outputs']==entries[1]['outputs']
assert {p for s in SPEC.values() for p,k,*_ in s['flows'] if k=='actor'}==set(ACTORS)
for a,b in [('inventory','money'),('people','money')]:
 af=next(f for f in SPEC[a]['flows'] if f[0]==b);bf=next(f for f in SPEC[b]['flows'] if f[0]==a)
 assert af[2]==bf[3] and af[3]==bf[2]

bfd=Graph('bfd','LR');bfd.node('root','Quản lý quán cafe')
subfunctions=[['Đăng nhập','Xác lập phiên','Kiểm tra quyền và đăng xuất'],['Quản lý danh mục và chính sách','Quản lý khách hàng','Điều chỉnh điểm'],['Lập đơn bán','Tính ưu đãi và phân bổ tiền','Thanh toán','Tra cứu, in và giao hàng'],['Quản lý nguyên liệu và mở sổ','Nhập hàng','Xuất hàng','Kiểm kê'],['Ghi thu chi','Ghi chi và nợ từ nhập hàng','Trả nợ và đối chiếu'],['Quản lý nhân sự và lương','Quản lý tài sản','Lập báo cáo']]
for (key,label),subs in zip(PROCESS.items(),subfunctions):
 bfd.node(key,label);bfd.edge('root',key)
 for i,sub in enumerate(subs):
  id=key+str(i);bfd.node(id,sub);bfd.edge(key,id)
bfd.save()

# Chen ER diagrams: entities, relationships and attributes are separate nodes.
# Plain lines express participation; cardinality is not a data-flow direction.
def er(name,entities,relations,attrs):
 g=Graph(name,'TB','neato')
 positions={
 'er-menu':{'category':(1,8),'product':(1,5),'material':(1,1),'zone':(6,8),'table':(6,5)},
 'er-sales':{'invoice':(4.5,6),'line':(1,3),'product':(1,0),'payment':(8,3),'method':(8,0),'customer':(1,9),'table':(4.5,9),'partner':(8,9),'promotion':(0,6)},
 'er-stock':{'supplier':(0,10),'receipt':(0,7),'issue':(8,7),'count':(8,1),'material':(4,4),'invoice':(0,1)},
 'er-finance':{'receipt':(0,10),'debt':(0,6),'repay':(0,2),'voucher':(6,2),'category':(6,10),'asset':(10,6),'payroll':(10,10),'method':(0,-1)},
 'er-people':{'employee':(4,6),'position':(0,9),'account':(8,9),'role':(8,5),'shift':(0,2),'payroll':(8,1)},
 'er-customers':{'tier':(0,5),'customer':(0,1),'point':(6,1),'invoice':(6,5)}
 }[name]
 g.options='graph [bgcolor="white", pad="0.20", overlap=true, splines=polyline, notranslate=true];'
 def at(pos):return f'{pos[0]*1.15},{pos[1]*1.15}!'
 for id,label in entities.items():g.node(id,label,'entity',pos=at(positions[id]),pin='true',fontsize='20')
 for rid,label,a,b,ca,cb in relations:
  pa,pb=positions[a],positions[b];positions[rid]=((pa[0]+pb[0])/2,(pa[1]+pb[1])/2)
  g.node(rid,label,'relationship',fontsize='18',margin='0.03',pos=at(positions[rid]),pin='true')
  first={};last={}
  if name=='er-finance' and a=='voucher':first={'tailport':'sw'}
  if name=='er-finance' and b=='voucher':last={'headport':{'classified':'n','related':'e','immediate':'nw','salary':'ne'}[rid]}
  g.edge(a,rid,'',dir='none',taillabel=ca,labeldistance='2.2',labelangle='30',**first)
  g.edge(rid,b,'',dir='none',headlabel=cb,labeldistance='2.2',labelangle='30',**last)
 # Keep identifier attributes and the attributes of associative relationships.
 # The complete attribute dictionary remains in the report's preceding tables.
 for entity,labels in attrs.items():
  isrel=entity not in entities
  labels=labels if isrel else labels[:1]
  x,y=positions[entity]
  for i,label in enumerate(labels):
   if isrel:
    if name=='er-menu':p=(x+2.1,y+(i-.5)*.8)
    elif name=='er-stock':p=(x+(-1 if x<4 else 1)*1.7,y+(i-(len(labels)-1)/2)*1.0)
    elif name=='er-people':p=(x-2.1,y+(i-1)*.9)
    else:p=(x+1.9,y+i*.8)
   elif name=='er-sales':
    p={'invoice':(4.5,4.8),'line':(-.7,3),'payment':(9.7,3),'customer':(1,10.3),'table':(4.5,10.3),'partner':(8,10.3),'promotion':(0,7.2)}.get(entity,(x,y-1.2))
   elif name=='er-stock':p={'supplier':(0,11.2),'receipt':(1.8,7.6),'issue':(8,8.2),'count':(8,-.2),'material':(4,5.4)}.get(entity,(x,y-1.2))
   elif name=='er-finance':p={'debt':(-1.8,6.8),'repay':(-1.8,2.8),'voucher':(6,.8),'asset':(10,7.2),'payroll':(10,11.2)}.get(entity,(x+1.9,y+.8))
   elif name=='er-people':p=(4,7.3) if entity=='employee' else (x+(-1.8 if x<4 else 1.8),y+1.1)
   elif name=='er-customers':p=(x,y+1.2 if entity in ['tier','invoice'] else y-1.2)
   else:p=(x+1.9,y+.8)
   if name=='er-stock' and isrel:
    p={'received':[(0,4.8),(0,3.8)],'issued':[(8,4.6)],'counted':[(8,3.2),(8,2.2)],'consumed':[(0,3)]}[entity][i]
   id=entity+'_attr'+str(i)
   g.node(id,label,'attribute',fontsize='17',pos=at(p),pin='true')
   if not isrel and label.startswith('Mã '):g.nodes[id][1]['label']='<<U>'+label+'</U>>'
   g.edge(entity,id,'',dir='none',weight='0.2')
 g.save()
er('er-menu',{'category':'Danh mục sản phẩm','product':'Sản phẩm','material':'Nguyên liệu','zone':'Khu vực','table':'Bàn'},[
 ('classifies','Phân loại','category','product','1','N'),('recipe','Pha từ','product','material','N','M'),('contains','Bố trí','zone','table','1','N')],
 {'category':['Mã danh mục'],'product':['Mã sản phẩm','Tên sản phẩm'],'material':['Mã nguyên liệu','Đơn vị tính'],'recipe':['Định mức'],'zone':['Mã khu vực'],'table':['Mã bàn']})
er('er-sales',{'invoice':'Hóa đơn','line':'Dòng món','product':'Sản phẩm','payment':'Khoản thanh toán','method':'Phương thức thanh toán','customer':'Khách hàng','table':'Bàn','partner':'Đối tác giao hàng','promotion':'Khuyến mãi'},[
 ('hasline','Gồm','invoice','line','1','N'),('chooses','Chọn','line','product','N','1'),('paid','Được thu qua','invoice','payment','1','N'),('uses','Sử dụng','payment','method','N','1'),('buys','Mua','customer','invoice','0..1','N'),('serves','Phục vụ tại','table','invoice','0..1','N'),('delivers','Giao','partner','invoice','0..1','N'),('discount','Áp dụng','promotion','invoice','0..1','N')],
 {'invoice':['Mã hóa đơn'],'line':['Mã dòng','Số lượng','Giá bán lịch sử'],'payment':['Mã phân bổ','Số tiền'],'customer':['Mã khách hàng'],'table':['Mã bàn'],'partner':['Mã đối tác'],'promotion':['Mã khuyến mãi']})
er('er-stock',{'supplier':'Nhà cung cấp','receipt':'Phiếu nhập','issue':'Phiếu xuất','count':'Phiếu kiểm kê','material':'Nguyên liệu','invoice':'Hóa đơn'},[
 ('supplies','Cung cấp','supplier','receipt','1','N'),('received','Ghi nhập','receipt','material','N','M'),('issued','Ghi xuất','issue','material','N','M'),('counted','Kiểm đếm','count','material','N','M'),('consumed','Tiêu hao','invoice','material','N','M')],
 {'supplier':['Mã nhà cung cấp'],'receipt':['Mã phiếu nhập'],'issue':['Mã phiếu xuất'],'count':['Mã kiểm kê'],'material':['Mã nguyên liệu'],'received':['Số lượng nhập','Đơn giá nhập'],'issued':['Số lượng xuất'],'counted':['Tồn hệ thống','Tồn thực tế'],'consumed':['Lượng đã dùng']})
er('er-finance',{'receipt':'Phiếu nhập','debt':'Khoản nợ nhà cung cấp','repay':'Lần trả nợ','voucher':'Phiếu thu chi','category':'Danh mục thu chi','asset':'Tài sản','payroll':'Bảng lương','method':'Phương thức thanh toán'},[
 ('arises','Phát sinh','receipt','debt','1','0..1'),('repaid','Được trả qua','debt','repay','1','N'),('records','Ghi tiền cho','voucher','repay','1','0..1'),('classified','Phân loại','category','voucher','1','N'),('related','Liên quan','asset','voucher','0..1','N'),('immediate','Chi trả ngay','receipt','voucher','0..1','0..1'),('salary','Chi lương','payroll','voucher','0..1','0..1'),('via','Dùng','repay','method','N','1')],
 {'debt':['Mã công nợ','Tiền nợ gốc'],'repay':['Mã lần trả'],'voucher':['Mã phiếu','Số tiền'],'asset':['Mã tài sản'],'payroll':['Mã bảng lương']})
er('er-people',{'employee':'Nhân viên','position':'Chức vụ','account':'Tài khoản','role':'Vai trò','shift':'Ca làm việc','payroll':'Bảng lương'},[
 ('appointed','Giữ','employee','position','N','1'),('owns','Sở hữu','employee','account','1','0..1'),('granted','Được cấp','account','role','N','1'),('attendance','Chấm công','employee','shift','N','M'),('salary','Có bảng lương','employee','payroll','1','N')],
 {'employee':['Mã nhân viên','Tên nhân viên'],'account':['Mã tài khoản'],'role':['Mã vai trò'],'position':['Mã chức vụ'],'shift':['Mã ca'],'attendance':['Ngày làm','Giờ vào','Giờ ra'],'payroll':['Mã bảng lương','Tháng và năm','Thực lĩnh']})
er('er-customers',{'tier':'Hạng thành viên','customer':'Khách hàng','point':'Giao dịch điểm','invoice':'Hóa đơn'},[
 ('ranked','Phân hạng','tier','customer','1','N'),('points','Có','customer','point','1','N'),('source','Phát sinh từ','point','invoice','N','0..1')],
 {'tier':['Mã hạng','Mức điểm tối thiểu'],'customer':['Mã khách hàng','Tên khách hàng'],'point':['Mã giao dịch','Số điểm','Lý do'],'invoice':['Mã hóa đơn']})

g=Graph('architecture','TB')
for k,label in [('ui','Giao diện WinForms'),('business','Xử lý nghiệp vụ C#'),('data','Truy cập dữ liệu'),('sql','Cơ sở dữ liệu SQL Server'),('models','Mô hình dữ liệu dùng chung')]:g.node(k,label,'entity')
for a,b,req,res in [('ui','business','Yêu cầu của người dùng','Kết quả và lỗi nghiệp vụ'),('business','data','Yêu cầu đã kiểm quyền','Dữ liệu đã ánh xạ'),('data','sql','Truy vấn và tham số','Kết quả thực thi')]:
 g.edge(a,b,req);g.edge(b,a,res)
for k in ['ui','business','data']:g.edge(k,'models','Sử dụng kiểu dữ liệu',style='dashed')
g.save()
(OUT/'manifest.json').write_text(json.dumps(graphs,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'dfd-specification.json').write_text(json.dumps({'actors':ACTORS,'stores':STORES,'processes':PROCESS,'spec':SPEC,'boundary_checks':balances,'balanced':True},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'diagrams':len(graphs),'balanced_processes':len(SPEC),'actors':list(ACTORS.values())},ensure_ascii=False))
