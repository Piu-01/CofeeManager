-- Chính sách hạng thận trọng: 4 hạng, trần giảm 8% (chốt 21/11/2026).
-- Cài mới dùng seed đã cập nhật; DB đã chạy dùng migration này để tái phân hạng.
-- Không đụng điểm, lịch sử điểm, hóa đơn hay snapshot ưu đãi.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=36)
 THROW 52307,N'Cần schema036 và runner migration.',1;
IF OBJECT_ID(N'dbo.LoyaltyPolicyState',N'U') IS NULL OR OBJECT_ID(N'dbo.HANGTHANHVIEN',N'U') IS NULL
 OR OBJECT_ID(N'dbo.KHACHHANG',N'U') IS NULL
 THROW 52307,N'Thiếu bảng chính sách hạng; cần đối chiếu trước nâng cấp.',1;
GO
BEGIN TRY
 BEGIN TRAN;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 52004,N'Chính sách đang được sử dụng.',1;

 DECLARE @target TABLE(Code char(5) PRIMARY KEY,MinimumPoints int NOT NULL,DiscountPercent int NOT NULL);
 INSERT @target VALUES('DONG',0,0),('BAC',2000,3),('VANG',10000,5),('KIM',30000,8);

 -- 1) Dời mọi ngưỡng hiện có sang ngưỡng tạm để không vướng UQ_HTV_Nguong.
 DECLARE @moves TABLE(Code char(5) PRIMARY KEY,TemporaryThreshold int UNIQUE);
 DECLARE @candidate int=2147483647,@moveCode char(5);
 WHILE EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @moves m WHERE m.Code=h.MaHang))
 BEGIN
  SELECT TOP(1) @moveCode=MaHang FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @moves m WHERE m.Code=h.MaHang) ORDER BY MaHang;
  WHILE EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu=@candidate)
     OR EXISTS(SELECT 1 FROM @target WHERE MinimumPoints=@candidate)
     OR EXISTS(SELECT 1 FROM @moves WHERE TemporaryThreshold=@candidate)
  BEGIN
   IF @candidate=0 THROW 52307,N'Không còn ngưỡng tạm cho chính sách.',1;
   SET @candidate-=1;
  END;
  INSERT @moves VALUES(@moveCode,@candidate);
 END;
 UPDATE h SET DiemToiThieu=m.TemporaryThreshold FROM dbo.HANGTHANHVIEN h JOIN @moves m ON m.Code=h.MaHang;

 -- 2) Chèn hạng đích còn thiếu, sau đó đưa tất cả về ngưỡng/% cuối.
 INSERT dbo.HANGTHANHVIEN(MaHang,DiemToiThieu,PhanTramGiam)
 SELECT Code,MinimumPoints,DiscountPercent FROM @target t
 WHERE NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE h.MaHang=t.Code);
 UPDATE h SET DiemToiThieu=t.MinimumPoints,PhanTramGiam=t.DiscountPercent
 FROM dbo.HANGTHANHVIEN h JOIN @target t ON t.Code=h.MaHang;

 -- 3) Tái phân hạng khách theo chính sách mới (điểm không đổi).
 UPDATE k SET MaHang=p.Code
 FROM dbo.KHACHHANG k CROSS APPLY(SELECT TOP(1) Code FROM @target WHERE MinimumPoints<=k.DiemTichLuy ORDER BY MinimumPoints DESC) p
 WHERE k.MaHang<>p.Code;

 -- 4) Xóa hạng ngoài chính sách; lúc này không còn khách tham chiếu nên FK an toàn.
 DELETE h FROM dbo.HANGTHANHVIEN h WHERE NOT EXISTS(SELECT 1 FROM @target t WHERE t.Code=h.MaHang);

 -- 5) Bump rowversion để preview/hóa đơn đang mở phải xác nhận lại số mới.
 UPDATE dbo.LoyaltyPolicyState SET Id=1 WHERE Id=1;
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK; THROW;
END CATCH;
GO
