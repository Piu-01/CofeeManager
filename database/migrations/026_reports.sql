-- T28: aggregate theo nguồn; read-only, không nhân món x phương thức.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=25) THROW 53502,N'Cần schema025.',1;
GO
CREATE VIEW dbo.vw_ReportStockEvents AS
 WITH receipts AS(SELECT Operation,EntityCode,MIN(CompletedAt) At FROM dbo.G03OperationReceipt
 WHERE Operation IN('PurchaseCreate','StockIssueCreate','SalesCheckout','StocktakeCommit') GROUP BY Operation,EntityCode)
 SELECT l.MaNVL Material,'Purchase' SourceType,p.MaPN Code,COALESCE(r.At,CONVERT(datetime2(7),p.NgayChot)) At,
 p.NgayNhap SourceDate,CONVERT(decimal(38,4),l.SoLuong) Quantity,CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END) FallbackTime
 FROM dbo.PHIEUNHAP p JOIN dbo.CHITIET_PN l ON l.MaPN=p.MaPN LEFT JOIN receipts r ON r.Operation='PurchaseCreate' AND r.EntityCode=p.MaPN WHERE p.DaChot=1 AND p.CutoverKind='New'
 UNION ALL
 SELECT l.MaNVL,'Issue',p.MaPX,COALESCE(r.At,CONVERT(datetime2(7),p.NgayChot)),p.NgayXuat,-CONVERT(decimal(38,4),l.SoLuongXuat),CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END)
 FROM dbo.PHIEUXUAT p JOIN dbo.CHITIET_PX l ON l.MaPX=p.MaPX LEFT JOIN receipts r ON r.Operation='StockIssueCreate' AND r.EntityCode=p.MaPX WHERE p.DaChot=1 AND p.CutoverKind='New'
 UNION ALL
 SELECT l.MaNVL,'Invoice',h.MaHD,COALESCE(r.At,CONVERT(datetime2(7),h.NgayThanhToan)),h.NgayThanhToan,-CONVERT(decimal(38,4),l.SoLuong),CONVERT(bit,CASE WHEN r.At IS NULL THEN 1 ELSE 0 END)
 FROM dbo.HOADON h JOIN dbo.TIEUHAO_HOADON l ON l.MaHD=h.MaHD LEFT JOIN receipts r ON r.Operation='SalesCheckout' AND r.EntityCode=h.MaHD
 JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD AND s.Kind='Checkout' WHERE h.TrangThai=N'Da thanh toan'
 UNION ALL
 SELECT l.MaNVL,'Stocktake',p.MaKK,r.At,CONVERT(datetime2(0),p.NgayKK),CONVERT(decimal(38,4),l.TonThucTe-l.TonHeThong),CONVERT(bit,0)
 FROM dbo.PHIEUKIEMKE p JOIN dbo.CHITIET_KK l ON l.MaKK=p.MaKK LEFT JOIN receipts r ON r.Operation='StocktakeCommit' AND r.EntityCode=p.MaKK WHERE p.DaDieuChinh=1 AND p.CutoverKind='New';
GO
CREATE PROCEDURE dbo.sp_ReportRead @Actor int,@Stamp binary(8),@Kind varchar(20),@From date,@To date,
 @Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Kind IS NULL OR @Kind NOT IN('SalesDay','SalesChannel','Products','Payments','Inventory','Debts','CashFlow','Labor','Assets')
 OR @From IS NULL OR @To IS NULL OR @From>@To OR @Skip<0
 OR (@Channel IS NOT NULL AND (@Kind NOT IN('SalesDay','SalesChannel','Products','Payments') OR @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang')))
 OR (@Method IS NOT NULL AND @Kind<>'Payments') OR (@Supplier IS NOT NULL AND @Kind<>'Debts')
 OR (@Employee IS NOT NULL AND @Kind<>'Labor') OR (@Material IS NOT NULL AND @Kind<>'Inventory') OR (@Asset IS NOT NULL AND @Kind<>'Assets') THROW 53501,N'Loại báo cáo hoặc bộ lọc không hợp lệ.',1;
 DECLARE @until datetime2(7)=NULL,@at datetime2(7)=SYSDATETIME(),@basis nvarchar(200),@note nvarchar(1000)=N'',@complete bit=1;
 IF @To<'99991231' SET @until=DATEADD(day,1,CONVERT(datetime2(7),@To));
 CREATE TABLE #rows(Code nvarchar(100) NOT NULL,Name nvarchar(200) NOT NULL,RowDate datetime2(7) NULL,CountValue int NOT NULL DEFAULT 0,
 Quantity decimal(38,4) NOT NULL DEFAULT 0,Gross decimal(38,0) NOT NULL DEFAULT 0,Discount decimal(38,0) NOT NULL DEFAULT 0,Ship decimal(38,0) NOT NULL DEFAULT 0,
 Amount decimal(38,0) NOT NULL DEFAULT 0,Fees decimal(38,0) NOT NULL DEFAULT 0,Inflow decimal(38,0) NOT NULL DEFAULT 0,Outflow decimal(38,0) NOT NULL DEFAULT 0,
 Opening decimal(38,4) NULL,Purchases decimal(38,4) NOT NULL DEFAULT 0,Issues decimal(38,4) NOT NULL DEFAULT 0,Consumption decimal(38,4) NOT NULL DEFAULT 0,
 Adjustment decimal(38,4) NOT NULL DEFAULT 0,Closing decimal(38,4) NULL,CurrentStock decimal(38,4) NOT NULL DEFAULT 0,
 Hours decimal(38,4) NOT NULL DEFAULT 0,Committed decimal(38,0) NOT NULL DEFAULT 0,Paid decimal(38,0) NOT NULL DEFAULT 0,
 Depreciation decimal(38,0) NOT NULL DEFAULT 0,Accumulated decimal(38,0) NOT NULL DEFAULT 0,Remaining decimal(38,0) NOT NULL DEFAULT 0,Coverage nvarchar(300) NOT NULL DEFAULT N'');
 IF @Kind IN('SalesDay','SalesChannel','Products','Payments')
 BEGIN
  SET @basis=N'Ngày thanh toán hóa đơn';
  SELECT h.MaHD,h.NgayThanhToan,h.LoaiDon,h.TongTien,h.GiamGia,h.PhiShip,h.ThanhTien,h.PhiDoiTac INTO #bills FROM dbo.HOADON h
  WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND (@until IS NULL OR h.NgayThanhToan<@until) AND (@Channel IS NULL OR h.LoaiDon=@Channel);
  IF @Kind IN('SalesDay','SalesChannel')
   INSERT #rows(Code,Name,CountValue,Gross,Discount,Ship,Amount,Fees)
   SELECT CASE WHEN @Kind='SalesDay' THEN CONVERT(nvarchar(10),NgayThanhToan,23) ELSE LoaiDon END,
   CASE WHEN @Kind='SalesDay' THEN CONVERT(nvarchar(10),NgayThanhToan,103) ELSE LoaiDon END,COUNT(*),SUM(CONVERT(decimal(38,0),TongTien)),SUM(CONVERT(decimal(38,0),GiamGia)),SUM(CONVERT(decimal(38,0),PhiShip)),SUM(CONVERT(decimal(38,0),ThanhTien)),SUM(CONVERT(decimal(38,0),PhiDoiTac))
   FROM #bills GROUP BY CASE WHEN @Kind='SalesDay' THEN CONVERT(nvarchar(10),NgayThanhToan,23) ELSE LoaiDon END,CASE WHEN @Kind='SalesDay' THEN CONVERT(nvarchar(10),NgayThanhToan,103) ELSE LoaiDon END;
  IF @Kind='Products'
  BEGIN
   SET @note=N'Tiền món theo giá snapshot, trước giảm toàn bill; tên món là tên hiện tại.';
   INSERT #rows(Code,Name,CountValue,Quantity,Amount)
   SELECT l.MaSP,s.TenSP,COUNT(DISTINCT l.MaHD),SUM(CONVERT(decimal(38,4),l.SoLuong)),SUM(CONVERT(decimal(38,0),l.SoLuong*l.GiaBan))
   FROM #bills h JOIN dbo.CHITIET_HD l ON l.MaHD=h.MaHD JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP GROUP BY l.MaSP,s.TenSP;
  END;
  IF @Kind='Payments'
   INSERT #rows(Code,Name,CountValue,Amount) SELECT t.MaPT,p.TenPT,COUNT(DISTINCT t.MaHD),SUM(CONVERT(decimal(38,0),t.SoTien))
   FROM #bills h JOIN dbo.CHITIET_THANHTOAN t ON t.MaHD=h.MaHD JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE @Method IS NULL OR t.MaPT=@Method GROUP BY t.MaPT,p.TenPT;
 END;
 IF @Kind='Debts'
 BEGIN
  SET @basis=N'Công nợ tại cuối ngày đến; ngày nguồn nhập và ngày phiếu chi trả nợ';
  INSERT #rows(Code,Name,RowDate,Gross,Paid,Remaining)
  SELECT c.MaCN,n.TenNCC,p.NgayNhap,c.TienNo,COALESCE(a.Paid,0),c.TienNo-COALESCE(a.Paid,0) FROM dbo.CONGNO_NCC c JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN JOIN dbo.NHACUNGCAP n ON n.MaNCC=p.MaNCC
  OUTER APPLY(SELECT SUM(CONVERT(decimal(38,0),tc.SoTien)) Paid FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=c.MaCN AND (@until IS NULL OR tc.NgayLap<@until)) a
  WHERE (@until IS NULL OR p.NgayNhap<@until) AND (@Supplier IS NULL OR p.MaNCC=@Supplier);
 END;
 IF @Kind='CashFlow'
 BEGIN
  SET @basis=N'Thu bán: ngày thanh toán; thu/chi khác: ngày phiếu';SET @note=N'Dòng tiền quản trị, đối tác chưa đối soát; không phải số dư ngân hàng hoặc lợi nhuận chính xác. Dữ liệu LegacyUnknown chưa đủ liên kết nguồn.';
  INSERT #rows(Code,Name,CountValue,Inflow,Outflow,Amount)
  SELECT Code,Name,COUNT(*),SUM(Inflow),SUM(Outflow),SUM(Inflow)-SUM(Outflow) FROM(
   SELECT CONVERT(nvarchar(100),'Sales') Code,CONVERT(nvarchar(200),N'Thu bán hàng') Name,CONVERT(decimal(38,0),t.SoTien) Inflow,CONVERT(decimal(38,0),0) Outflow
   FROM dbo.HOADON h JOIN dbo.CHITIET_THANHTOAN t ON t.MaHD=h.MaHD WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND (@until IS NULL OR h.NgayThanhToan<@until)
   UNION ALL
   SELECT CONVERT(nvarchar(100),p.OriginKind+':'+d.Loai),CONVERT(nvarchar(200),p.OriginKind+N' · '+d.Loai),CASE WHEN d.Loai=N'Thu' THEN CONVERT(decimal(38,0),p.SoTien) ELSE 0 END,CASE WHEN d.Loai=N'Chi' THEN CONVERT(decimal(38,0),p.SoTien) ELSE 0 END
   FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until)
  ) a GROUP BY Code,Name;
 END;
 IF @Kind='Labor'
 BEGIN
  SET @basis=N'Giờ: NgayLam; lương chốt: NgayChot; thực chi: ngày phiếu';SET @note=N'Kỳ công, kỳ bảng lương và thời điểm chốt/chi là các nguồn độc lập. Giờ tăng ca theo snapshot New; LegacyUnknown không suy từ hồ sơ hiện tại.';
  INSERT #rows(Code,Name,CountValue,Hours,Committed,Paid,Coverage,Quantity,Adjustment)
  SELECT n.MaNV,n.TenNV,COALESCE(c.Incomplete,0),COALESCE(c.Hours,0),COALESCE(b.Amount,0),COALESCE(p.Amount,0),CASE WHEN COALESCE(b.LegacyCount,0)>0 THEN N'Có bảng lương LegacyUnknown; giờ snapshot chỉ gồm New' ELSE N'Giờ snapshot chỉ gồm New' END,COALESCE(b.RegularHours,0),COALESCE(b.OvertimeHours,0) FROM dbo.NHANVIEN n
  OUTER APPLY(SELECT SUM(CONVERT(decimal(38,4),CASE WHEN GioRa>GioVao AND TrangThai=N'Di lam' THEN DATEDIFF_BIG(second,GioVao,GioRa) ELSE 0 END))/3600 Hours,SUM(CASE WHEN TrangThai=N'Di lam' AND (GioVao IS NULL OR GioRa IS NULL) THEN 1 ELSE 0 END) Incomplete FROM dbo.CHAMCONG WHERE MaNV=n.MaNV AND NgayLam>=@From AND (@until IS NULL OR NgayLam<@until)) c
  OUTER APPLY(SELECT SUM(CONVERT(decimal(38,0),ThucLinh)) Amount,SUM(CASE WHEN SnapshotKind='LegacyUnknown' THEN 1 ELSE 0 END) LegacyCount,SUM(CASE WHEN SnapshotKind='New' THEN CONVERT(decimal(28,4),RegularHours) ELSE 0 END) RegularHours,SUM(CASE WHEN SnapshotKind='New' THEN CONVERT(decimal(28,4),OvertimeHours) ELSE 0 END) OvertimeHours FROM dbo.BANGLUONG WHERE MaNV=n.MaNV AND NgayChot>=@From AND (@until IS NULL OR NgayChot<@until)) b
  OUTER APPLY(SELECT SUM(CONVERT(decimal(38,0),p.SoTien)) Amount FROM dbo.BANGLUONG l JOIN dbo.PHIEUTHUCHI p ON p.MaBL=l.MaBL WHERE l.MaNV=n.MaNV AND p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until)) p
  WHERE (@Employee IS NULL OR n.MaNV=@Employee) AND (c.Hours IS NOT NULL OR b.Amount IS NOT NULL OR p.Amount IS NOT NULL);
 END;
 IF @Kind='Assets'
 BEGIN
  SET @basis=N'Tháng mua và kỳ khấu hao; chi theo ngày phiếu';SET @note=N'Khấu hao THAM KHẢO theo hồ sơ hiện tại, không tự ghi phiếu chi hoặc tính như giá vốn/dòng tiền.';
  DECLARE @before date=CASE WHEN @From>'00010101' THEN DATEADD(day,-1,@From) ELSE NULL END;
  -- Kỳ khấu hao tính trọn các tháng được chọn, không chia theo ngày.
  IF @From>'00010131' SET @before=DATEADD(day,-1,DATEFROMPARTS(YEAR(@From),MONTH(@From),1)); ELSE SET @before=NULL;
  INSERT #rows(Code,Name,RowDate,Gross,Outflow,Depreciation,Accumulated,Remaining)
  SELECT a.MaTS,a.TenTS,a.NgayMua,a.GiaTri,COALESCE(p.Amount,0),e.Accumulated-COALESCE(b.Accumulated,0),e.Accumulated,a.GiaTri-e.Accumulated FROM dbo.TAISAN_CODINH a
  CROSS APPLY dbo.fn_AssetDepreciation(a.GiaTri,a.SoThangKH,a.NgayMua,@To) e OUTER APPLY dbo.fn_AssetDepreciation(a.GiaTri,a.SoThangKH,a.NgayMua,@before) b
  OUTER APPLY(SELECT SUM(CONVERT(decimal(38,0),p.SoTien)) Amount FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE p.MaTS=a.MaTS AND d.Loai=N'Chi' AND p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until)) p WHERE @Asset IS NULL OR a.MaTS=@Asset;
 END;
 IF @Kind='Inventory'
 BEGIN
  SET @basis=N'Thời điểm ghi kho sau mốc mở sổ; ngày nguồn chứng từ hiển thị riêng';SET @note=N'Không dựng tồn trước mở sổ. Closing tại ngày đến khác Current đọc hiện tại; không cộng lại lịch sử legacy đã nằm trong mốc.';
  SELECT e.* INTO #events FROM dbo.vw_ReportStockEvents e WHERE @Material IS NULL OR e.Material=@Material;
  INSERT #rows(Code,Name,RowDate,Opening,Purchases,Issues,Consumption,Adjustment,Closing,CurrentStock,Coverage)
  SELECT n.MaNVL,n.TenNVL+N' ('+n.DonViTinh+N')',o.RecordedAt,
   CASE WHEN o.RecordedAt<@From THEN o.Quantity+COALESCE(CONVERT(decimal(28,4),e.BeforePeriod),0) ELSE NULL END,COALESCE(e.Purchases,0),COALESCE(e.Issues,0),COALESCE(e.Consumption,0),COALESCE(e.Adjustment,0),
   CASE WHEN o.RecordedAt IS NOT NULL AND (@until IS NULL OR o.RecordedAt<@until) THEN o.Quantity+COALESCE(CONVERT(decimal(28,4),e.BeforeEnd),0) ELSE NULL END,n.SoLuongTon,
   CASE WHEN o.RecordedAt IS NULL THEN N'Incomplete: chưa mở sổ' WHEN o.RecordedAt>=@From THEN N'Incomplete: kỳ bắt đầu trước mốc mở sổ'
    WHEN COALESCE(e.MissingTime,0)>0 THEN N'Incomplete: thiếu thời điểm ghi kho' WHEN COALESCE(e.FallbackCount,0)>0 THEN N'Incomplete: có timestamp fallback'
    WHEN o.Quantity+COALESCE(CONVERT(decimal(28,4),e.AllChange),0)<>n.SoLuongTon THEN N'Incomplete: nguồn sau mốc chưa khớp tồn hiện tại' ELSE N'Complete' END
  FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL
  OUTER APPLY(SELECT SUM(CASE WHEN e.At>=eo.RecordedAt AND e.At<@From THEN e.Quantity ELSE 0 END) BeforePeriod,
   SUM(CASE WHEN e.At>=eo.RecordedAt AND (@until IS NULL OR e.At<@until) THEN e.Quantity ELSE 0 END) BeforeEnd,
   SUM(CASE WHEN e.At>=eo.RecordedAt THEN e.Quantity ELSE 0 END) AllChange,
   SUM(CASE WHEN e.At IS NULL THEN 1 ELSE 0 END) MissingTime,SUM(CASE WHEN e.At>=eo.RecordedAt AND e.FallbackTime=1 THEN 1 ELSE 0 END) FallbackCount,
   SUM(CASE WHEN e.At>=eo.RecordedAt AND e.At>=@From AND (@until IS NULL OR e.At<@until) AND e.SourceType='Purchase' THEN e.Quantity ELSE 0 END) Purchases,
   -SUM(CASE WHEN e.At>=eo.RecordedAt AND e.At>=@From AND (@until IS NULL OR e.At<@until) AND e.SourceType='Issue' THEN e.Quantity ELSE 0 END) Issues,
   -SUM(CASE WHEN e.At>=eo.RecordedAt AND e.At>=@From AND (@until IS NULL OR e.At<@until) AND e.SourceType='Invoice' THEN e.Quantity ELSE 0 END) Consumption,
   SUM(CASE WHEN e.At>=eo.RecordedAt AND e.At>=@From AND (@until IS NULL OR e.At<@until) AND e.SourceType='Stocktake' THEN e.Quantity ELSE 0 END) Adjustment FROM #events e JOIN dbo.MaterialOpening eo ON eo.MaNVL=e.Material WHERE e.Material=n.MaNVL) e
  WHERE @Material IS NULL OR n.MaNVL=@Material;
  IF EXISTS(SELECT 1 FROM #rows WHERE Coverage<>N'Complete') SET @complete=0;
 END;
 SELECT @Kind Kind,@From FromDate,@To ToDate,@at ReadAt,COUNT(*) TotalCount,@basis DateBasis,@note Note,@complete Complete FROM #rows;
 SELECT COALESCE(SUM(Gross),0) Gross,COALESCE(SUM(Discount),0) Discount,COALESCE(SUM(Ship),0) Ship,COALESCE(SUM(Amount),0) Amount,COALESCE(SUM(Fees),0) Fees,
 COALESCE(SUM(Inflow),0) Inflow,COALESCE(SUM(Outflow),0) Outflow,COALESCE(SUM(Committed),0) Committed,COALESCE(SUM(Paid),0) Paid,COALESCE(SUM(Depreciation),0) Depreciation,COALESCE(SUM(Accumulated),0) Accumulated,COALESCE(SUM(Remaining),0) Remaining FROM #rows;
 SELECT Code,Name,RowDate,CountValue,Quantity,Gross,Discount,Ship,Amount,Fees,Inflow,Outflow,Opening,Purchases,Issues,Consumption,Adjustment,Closing,CurrentStock,Hours,Committed,Paid,Depreciation,Accumulated,Remaining,Coverage FROM #rows ORDER BY Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_ReportDashboard @Actor int,@Stamp binary(8) AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';DECLARE @today date=CONVERT(date,SYSDATETIME()),@until datetime2(7)=DATEADD(day,1,CONVERT(datetime2(7),CONVERT(date,SYSDATETIME())));
 SELECT @today BusinessDate,SYSDATETIME() ReadAt,
 (SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=@today AND NgayThanhToan<@until) Revenue,
 (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan') OpenOrders,
 (SELECT COALESCE(SUM(CONVERT(decimal(38,0),ConNo)),0) FROM dbo.vw_CongNoNCC) Debt;
END;
GO
CREATE PROCEDURE dbo.sp_ReportOptions @Actor int,@Stamp binary(8) AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT 'Method' Kind,MaPT Code,TenPT Name FROM dbo.PHUONGTHUCTT UNION ALL SELECT 'Supplier',MaNCC,TenNCC FROM dbo.NHACUNGCAP
 UNION ALL SELECT 'Employee',MaNV,TenNV FROM dbo.NHANVIEN UNION ALL SELECT 'Material',MaNVL,TenNVL FROM dbo.NGUYENVATLIEU UNION ALL SELECT 'Asset',MaTS,TenTS FROM dbo.TAISAN_CODINH;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportDashboard TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ReportOptions TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportSourceList @Actor int,@Stamp binary(8),@Kind varchar(20),@From date,@To date,@GroupCode nvarchar(100),
 @Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Kind IS NULL OR @Kind NOT IN('SalesDay','SalesChannel','Products','Payments','Inventory','Debts','CashFlow','Labor','Assets') OR @From IS NULL OR @To IS NULL OR @From>@To OR @Skip<0 OR @GroupCode IS NULL OR LEN(@GroupCode)>100
 OR (@Channel IS NOT NULL AND (@Kind NOT IN('SalesDay','SalesChannel','Products','Payments') OR @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang')))
 OR (@Method IS NOT NULL AND @Kind<>'Payments') OR (@Supplier IS NOT NULL AND @Kind<>'Debts') OR (@Employee IS NOT NULL AND @Kind<>'Labor') OR (@Material IS NOT NULL AND @Kind<>'Inventory') OR (@Asset IS NOT NULL AND @Kind<>'Assets') THROW 53501,N'Bộ lọc chứng từ nguồn không hợp lệ.',1;
 DECLARE @until datetime2(7)=NULL;IF @To<'99991231' SET @until=DATEADD(day,1,CONVERT(datetime2(7),@To));
 CREATE TABLE #sources(SourceType varchar(20),Code varchar(36),Name nvarchar(200),SourceDate datetime2(7) NULL,Amount decimal(38,0) NULL,Quantity decimal(38,4) NULL);
 IF @Kind IN('SalesDay','SalesChannel','Products','Payments')
  INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
  SELECT 'Invoice',h.MaHD,h.LoaiDon,h.NgayThanhToan,CASE WHEN @Kind='Payments' THEN COALESCE(p.Amount,0) ELSE h.ThanhTien END
  FROM dbo.HOADON h OUTER APPLY(SELECT SUM(CONVERT(decimal(38,0),t.SoTien)) Amount FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD AND t.MaPT=@GroupCode AND (@Method IS NULL OR t.MaPT=@Method)) p
  WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND (@until IS NULL OR h.NgayThanhToan<@until) AND (@Channel IS NULL OR h.LoaiDon=@Channel)
  AND ((@Kind='SalesDay' AND CONVERT(date,h.NgayThanhToan)=TRY_CONVERT(date,@GroupCode,23)) OR (@Kind='SalesChannel' AND h.LoaiDon=@GroupCode)
   OR (@Kind='Products' AND EXISTS(SELECT 1 FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD AND l.MaSP=@GroupCode)) OR (@Kind='Payments' AND p.Amount IS NOT NULL));
 IF @Kind='Debts'
  INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Debt',c.MaCN,n.TenNCC,p.NgayNhap,c.TienNo FROM dbo.CONGNO_NCC c JOIN dbo.PHIEUNHAP p ON p.MaPN=c.MaPN JOIN dbo.NHACUNGCAP n ON n.MaNCC=p.MaNCC
  WHERE c.MaCN=@GroupCode AND (@until IS NULL OR p.NgayNhap<@until) AND (@Supplier IS NULL OR p.MaNCC=@Supplier);
 IF @Kind='Assets'
  INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Asset',MaTS,TenTS,NgayMua,GiaTri FROM dbo.TAISAN_CODINH WHERE MaTS=@GroupCode AND (@Asset IS NULL OR MaTS=@Asset);
 IF @Kind='Inventory'
 BEGIN
  INSERT #sources(SourceType,Code,Name,SourceDate,Quantity) SELECT e.SourceType,e.Code,n.TenNVL,e.At,e.Quantity FROM dbo.vw_ReportStockEvents e JOIN dbo.MaterialOpening o ON o.MaNVL=e.Material JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=e.Material
  WHERE e.Material=@GroupCode AND (@Material IS NULL OR e.Material=@Material) AND e.At>=o.RecordedAt AND e.At>=@From AND (@until IS NULL OR e.At<@until);
  INSERT #sources(SourceType,Code,Name,SourceDate,Quantity) SELECT 'Opening',o.MaNVL,o.Source,o.RecordedAt,o.Quantity FROM dbo.MaterialOpening o WHERE o.MaNVL=@GroupCode AND (@Material IS NULL OR o.MaNVL=@Material);
 END;
 IF @Kind='CashFlow'
 BEGIN
  IF @GroupCode='Sales'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Invoice',h.MaHD,h.LoaiDon,h.NgayThanhToan,SUM(CONVERT(decimal(38,0),t.SoTien)) FROM dbo.HOADON h JOIN dbo.CHITIET_THANHTOAN t ON t.MaHD=h.MaHD WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND (@until IS NULL OR h.NgayThanhToan<@until) GROUP BY h.MaHD,h.LoaiDon,h.NgayThanhToan;
  ELSE INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Finance',p.MaTC,d.TenDMTC,p.NgayLap,p.SoTien FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE p.OriginKind+':'+d.Loai=@GroupCode AND p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until);
 END;
 IF @Kind='Labor' AND (@Employee IS NULL OR @Employee=@GroupCode)
 BEGIN
  INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Payroll',CONVERT(varchar(36),b.MaBL),CONVERT(nvarchar(200),N'Kỳ '+CONVERT(varchar(2),b.Thang)+'/'+CONVERT(varchar(4),b.Nam)+N' · '+b.SnapshotKind),b.NgayChot,b.ThucLinh FROM dbo.BANGLUONG b WHERE b.MaNV=@GroupCode AND b.NgayChot>=@From AND (@until IS NULL OR b.NgayChot<@until);
  INSERT #sources(SourceType,Code,Name,SourceDate,Amount) SELECT 'Finance',p.MaTC,N'Chi lương · kỳ '+CONVERT(varchar(2),b.Thang)+'/'+CONVERT(varchar(4),b.Nam),p.NgayLap,p.SoTien FROM dbo.BANGLUONG b JOIN dbo.PHIEUTHUCHI p ON p.MaBL=b.MaBL WHERE b.MaNV=@GroupCode AND p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until);
  INSERT #sources(SourceType,Code,Name,SourceDate,Quantity) SELECT 'Attendance',RTRIM(c.MaNV)+':'+CONVERT(varchar(8),c.NgayLam,112)+':'+RTRIM(c.MaCa),c.TrangThai,CONVERT(datetime2(7),c.NgayLam),CONVERT(decimal(28,4),CASE WHEN c.GioRa>c.GioVao AND c.TrangThai=N'Di lam' THEN DATEDIFF_BIG(second,c.GioVao,c.GioRa)/3600.0 ELSE NULL END) FROM dbo.CHAMCONG c WHERE c.MaNV=@GroupCode AND c.NgayLam>=@From AND (@until IS NULL OR c.NgayLam<@until);
 END;
 SELECT COUNT(*) TotalCount,SYSDATETIME() ReadAt FROM #sources;
 SELECT SourceType,Code,Name,SourceDate,Amount,Quantity FROM #sources ORDER BY SourceDate DESC,SourceType,Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportSourceList TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportProducts @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Products',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportProducts TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportPayments @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Payments',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportPayments TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportInventory @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Inventory',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportInventory TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportDebts @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Debts',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportDebts TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportCashFlow @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'CashFlow',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportCashFlow TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportLabor @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Labor',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportLabor TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportAssets @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_ReportRead @Actor,@Stamp,'Assets',@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportAssets TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ReportSales @Actor int,@Stamp binary(8),@From date,@To date,@Channel nvarchar(20)=NULL,@Method varchar(5)=NULL,@Supplier varchar(5)=NULL,@Employee varchar(5)=NULL,@Material varchar(5)=NULL,@Asset varchar(5)=NULL,@Skip int=0,@Group varchar(10)='Day' AS
BEGIN
 SET NOCOUNT ON;
 IF @Group IS NULL OR @Group NOT IN('Day','Channel') THROW 53501,N'Nhóm báo cáo không hợp lệ.',1;
 DECLARE @kind varchar(20)=CASE WHEN @Group='Day' THEN 'SalesDay' ELSE 'SalesChannel' END;
 EXEC dbo.sp_ReportRead @Actor,@Stamp,@kind,@From,@To,@Channel,@Method,@Supplier,@Employee,@Material,@Asset,@Skip;
END;
GO
GRANT EXECUTE ON dbo.sp_ReportSales TO CafeManagerApp;





