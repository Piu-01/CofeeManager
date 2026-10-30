# Lịch sử probe native kỹ thuật — 23/10/2026

Lượt `checkout.json` đã thu được Rows1/Columns3/DeviceDpi120, nhưng process kết thúc
exit1 khi Dispose lần hai. Thông báo thực tế đã trả trong tool output:

```
System.ObjectDisposedException: The CancellationTokenSource has been disposed.
at CafeManager.WinForms.Modules.Sales.CheckoutDialog.Dispose(Boolean disposing)
at System.ComponentModel.Component.Dispose()
at Program.Main(String[] args)
```

JSON TechnicalSuccess ở lượt đó chỉ là dữ liệu trước khi dọn, không chứng nhận
vòng đời process thành công. Trong pha triển khai đã thêm guard dọn tài nguyên một
lần cho dialog/control mới. Lượt `checkout-after-dispose-fix.json` exit0, sau đó
`final-native.json` exit0 và ghi125%/binding/CTA/preview. Không xóa lịch sử lỗi,
không tính các lượt này thành ca nghiệm thuT16/T17 hoặc PASS hình thức.
