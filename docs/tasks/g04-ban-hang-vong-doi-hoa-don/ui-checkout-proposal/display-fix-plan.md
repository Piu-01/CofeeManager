# G04-UI03 — Sửa lại nội dung Checkout theo mẫu ZIP

Người dùng báo bản mới không giống mẫu và gửi ảnh lỗi25/10/2026.
Người dùng đã duyệt đợt sửa và regression. Đã triển khai UI03, kiểm kỹ thuật
60/60 + component32/32 + SQL bridge7/7 PASS; hình thức do người dùng xác nhận.
Xem `../t16-pos-huy-don/display-fix-test-results.md`. Phần dưới giữ hồ sơ điều tra.

## Bằng chứng và trách nhiệm

Đúng bản footer-fix-20261025 đang chạy; không gán lỗi cho dùng nhầm bản.
Ảnh người dùng: bảng chiếm gần hếtbody, ôTiền mặt khách đưa/notice không hiện,
header/phân nhóm chưa sátPNG,editorcombo native có nềntrắng. FooterUI02 đã sửa
không giải quyết phầnbody. Các test32/32 trước không chứng nhận giống mẫu.

Probe đúngDLLfooter,Darkmode nhưProgram,DPI120,form1150×750:
gridheight1408,ôtiềnkháchđưaY1805,noticeY1900,đều bị viewport/formclipping.
Đo trước vàsau focusMethod/chọnblank đều như vậy. SourceinputTableAutoSize=true,
gridrowPercent100 vàDockFill trongbodyAutoSize tạo kíchthước không bị chặn.
Managedcolorcombo sauinit=#1C1823 thay vìInput#17131D;ảnhngười dùng còncho thấy
nềnnative trắng. Không suy màu property là màu renderer đã vẽ: cần xử lý cùng
lifecycle/stylecell/editor. Chưa chốt một nguyên nhânrenderer cụ thể từ property.

## Đợt sửa được đề xuất

1. **Body bám mẫu:** bảng phân bổ một dòng có chiều cao gọn theo header+row,
   chỉ tăng đến số dòng tối đa rồi cuộn bên tronggrid; bỏ phối hợpPercent100/
   AutoSize gâygiãnvôhạn. Khối kháchđưa/notice đứng ngay dưới,khôngbịđẩykhỏi
   vùng nhìn thấy ởsizechuẩn. Giữ2cột khi rộng,stack/cuộn cóchủđích khi hẹp.
2. **Tổng kết/header sát mẫu:** dùng kích thước/tỷ lệlogical từHTMLZIP;
   thông tinưuđãi nằm dướiƯuđãi,khôngphát sinh hàngChươngtrình làmđộidài;
   mãHD/kênh nhận diện rõ,nhómphải trả vàkháchđưa/thốithiếu nhưPNG.
   Nativechrome vẫn theo plan đãduyệt;khôngthayPOS/sidebar hoặcnhúngWebView.
3. **Editor theoOrbit:** profilePayment ởsharedgrid style cảcell/editor và
   vòngđờikhởi tạo/đổiô;khôngchỉ gánBackColor một lần. Chọn cáchnative/owner-draw
   tối thiểu để bỏ nềntrắng;giữmappingCode/Name,keyboard/dropdown/tab/edit.
   Mọi extension nằm trong sharedcontrol/themehiệncó,khoanhPaymentprofile;
   khôngđổicácprofileReadonly khác vì vấnđềmànthanh toán.
4. **Thông báo rõ:** methodtrống báoChọn phương thức thanh toán thay vìThiếu mã
   nguồn;giữnguyên điều kiệnchặn/ConfirmvàBLL revalidate. Nêu rõCashn/a khibankonly.

File dự kiến:CheckoutDialog.Layout.cs,OrbitSummaryPanel.cs,OrbitDataGridView.cs;
CheckoutDialog.cs chỉ wiring/nhãnvalidation;Theme chỉ bổ sungmetrics/stylecầnthiết.
Không sửaSQL/DAL/BLL/role,request/transaction/preview/reconcile hoặcgiátrị tài chính.

## Regression sau duyệt debug

- Đóngbăngbinary/source mới,giữmọiFAIL/ảnhnguồn trước sửa. NativeDarkmode đúngnhưapp.
- Đo bounds **cảtoolbar/grid/cash/notice/summary/footer**,trước/sau focus/edit,
  một vànhiềudòng ở1280×720,1366×768,sizehẹp/workingareaclamp tại125%.
  Cakhôngđủchiềucao cần kiểmcuộnđúngvùng vàkhảnăngtiếpcậncontrols.
- Nhập vàcommitmethod/amount/reference,thêm/xóa,tiền81000/khách100000,
  thiếu/abc/overflow/methodtrống,mixed/bank/0₫,busy/unknown,cancel/dispose,
  footerkhôngtáilỗi. Khẳngđịnhrõhandlercomponent hay physicalinput.
- Smokeprofilegrid/dialogdùngchung hiệncó;đối chiếu tiền kho điểm khi chốtDBtest,
  khôngsuyGUI từgọiSQLtrực tiếp vàkhôngchốt đơnmanual đểxemmẫu.
- Agent kiểmchức năng/layoutkỹthuật125%;người dùng xácnhận hìnhthức nhưphâncông.
  KhôngchụpảnhQAso sánh hoặcđổiScale100/150;khôngbáo“giốngmẫu”từbuild/metrics.

Rủiro:owner-drawlàm mất editing/keyboard;boundheightkhi nhiều dòng cắt editor;
summaryresize lại đẩybody;đều phải cócasephủ trước bàn giao.
