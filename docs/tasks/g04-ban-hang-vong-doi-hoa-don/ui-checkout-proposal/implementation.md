# Triển khai UI Checkout —25/10/2026

Planbổsung đã được người dùng duyệt. Đã hoàn thành code vàbuild;
đợt testUI mớiđãđượcduyệt/chạy25/10,cóFAILfooterG04-UI02,chưa sửa.
Xem [test-results](test-results.md);các đoạnchờtestbêndưới làlịchsửtriểnkhai.

- CheckoutDialog.Layout.cs: bố cụcheader/body2cột hoặcstack,viewportcuộn,footer
  cố định. Nội dung và tổng từDTO/previewthật,khônghardcodeHD20250411/81000.
- CheckoutDialog.cs:bindsummary/thông báophân bổtiềnmặt/thiếuthối;feedbackkhi đang
  sửaAmount/Reference;formatVNDkhi hiển thị/rờiô;giữinputkhônghợp lệ.Xác nhận khóa
  khi validation không đạt;handlerConfirm vẫn revalidate. Busy/unknown/reconcile,
  request/fence,đóng/Enter/Esc vàphépghiSQL giữ contract.
- OrbitSummaryPanel vàOrbitNotice mới chỉ trình bày,khôngtínhnghiệpvụ/gọiSQL.
- OrbitDataGridView thêmPaymentprofile;nativeediting/paintingchoeditablecells,
  font/màu/focuseditor vàcột.Amountcănphải.Cácprofilecũ khôngđổi default.
- OrbitDialogLayout cóheader/leadingActiontùychọn,giữconstructorcũ tương thích.
- OrbitManagementTheme thêm nétcheckicon;dùngpalette/font/metricscũ,khôngthêm
  bộCheckoutTheme trùnglặp. OrbitField/OrbitButton/StarDustOverlay dùng lại.
- MẫuZIPđãlưu/docs/ui/checkout-confirmation/ kèmmanifesthashvàchỉdẫnphạmvi.
  KhôngđưaHTML/JS/liconbundle của demo vào app hoặc đổiPOS/sidebar.

BuildDebug/Release:0warning/0error;logs/binary tại
`../runs/checkout-ui-20261025/`. Lầncompileđầu phát hiện thiếuDefaultValue trên
OrbitNotice.Kind vànullablewarnings;đã sửa trongpha triển khai,giữbuildloglầnđầu.
Khôngphảikết quảFAILmộtlượtnghiệmthuchức năng,khôngsửaexpectedtestđểnélỗi.

KhôngsửaSQL/migrations/Models/DAL/BLL/role hoặcghiDB trongtriểnkhai này.
Khôngđóngprocesscũcủangười dùng. Launcher manual đãchỉ rõ bảnmới chođợt kếtiếp.
Giữ33/0/23G04;khôngnângPASS từ code/build. Hìnhthứcdo userkiểm;agent chỉkiểm
chứcnăng/layoutkỹthuật125%sau duyệtđợttest.Khôngcommit/push.
