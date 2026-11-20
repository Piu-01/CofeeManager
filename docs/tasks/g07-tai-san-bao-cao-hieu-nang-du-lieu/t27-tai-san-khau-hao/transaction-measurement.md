# Phân biệt phép đo transaction trong harness G07-B01

31/10/2026, SQL Server localhost17.0.1000.7. Lượt regression_20261031_200218_8ec08ae7 giữ nguyên139ca tích hợp PASS, nhưng34ca mới FAIL vì phép đo sau lời gọi dùng SELECT trực tiếp trả0:1, kể cả write thành công. Chưa sửa source sản phẩm từ kết quả này.

Kiểm tra read-only trên **connection mới vào master**, không gọi procedure sản phẩm, không BEGIN TRAN:

```sql
SELECT @@TRANCOUNT AS TranCount, XACT_STATE() AS XactState,
       CONCAT(@@TRANCOUNT,':',XACT_STATE()) AS Combined;
DECLARE @n int=@@TRANCOUNT,@s int=XACT_STATE();
SELECT @n AS SavedTranCount,@s AS SavedXactState;
```

Actual từ engine: SELECT trực tiếp `TranCount=0, XactState=1, Combined=0:1`; lưu vào biến trước SELECT `SavedTranCount=0, SavedXactState=0`. Không suy rò transaction của sản phẩm từ số1 chỉ xuất hiện khi đọc trực tiếp trong SELECT. Không kết luận sâu hơn về nguyên nhân engine nếu chưa có bằng chứng.

Chỉnh riêng harness giữa hai lượt: chụp @@TRANCOUNT/XACT_STATE vào biến trước SELECT, trước và sau cùng một lời gọi trên cùng connection runtime. Expected **vẫn0:0**, không đổi sang chấp nhận0:1 hoặc bỏ kiểm transaction. Migration027 và source sản phẩm không đổi. Lượt cũ/34FAIL không xóa hay ghi đè; lượt mới có source hash riêng để xác minh lại receipt/row/phiếu và transaction.
