-- T28 mở rộng: chỉ bổ sung hai procedure đọc, không đổi thực thể/thuộc tính lưu trữ.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=27)
 THROW 53502,N'Cần schema027 và runner migration.',1;
GO
CREATE PROCEDURE dbo.sp_DashboardAnalyticsRead
 @Actor int,@Stamp binary(8),@Month bit,@At datetime2(7)=NULL
WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @read datetime2(7)=SYSDATETIME();
 SET @At=COALESCE(@At,@read);
 IF @Month IS NULL OR @At<'20000101' OR @At>@read OR @@TRANCOUNT<>0
  THROW 53501,N'Kỳ tổng quan hoặc transaction không hợp lệ.',1;
 DECLARE @from datetime2(7)=CONVERT(datetime2(7),CASE WHEN @Month=1 THEN DATEFROMPARTS(YEAR(@At),MONTH(@At),1) ELSE CONVERT(date,@At) END),
  @previousFrom datetime2(7),@previousUntil datetime2(7);
 IF @Month=0 BEGIN SET @previousFrom=DATEADD(day,-1,@from);SET @previousUntil=DATEADD(day,-1,@At);END;
 ELSE IF DAY(@At)<=DAY(EOMONTH(DATEADD(month,-1,CONVERT(date,@At))))
 BEGIN SET @previousFrom=DATEADD(month,-1,@from);SET @previousUntil=DATEADD(month,-1,@At);END;
 CREATE TABLE #indicators([Key] varchar(80) NOT NULL,Value decimal(38,8) NULL);
 CREATE TABLE #rows(PageKey varchar(30) NOT NULL,Code nvarchar(100) NOT NULL,Name nvarchar(200) NOT NULL,
  Bills int NOT NULL,Quantity decimal(38,4) NOT NULL,Gross decimal(38,0) NOT NULL,Discount decimal(38,0) NOT NULL,
  Ship decimal(38,0) NOT NULL,Amount decimal(38,0) NOT NULL,Fees decimal(38,0) NOT NULL,Position int NOT NULL);
 BEGIN TRY
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
  BEGIN TRAN;
  -- Một snapshot header cho toàn bộ phần bán hàng, không JOIN phương thức thanh toán vào dòng món.
  SELECT CASE WHEN h.NgayThanhToan>=@from THEN 'C' ELSE 'P' END Period,
   h.MaHD,h.NgayThanhToan,h.LoaiDon,CONVERT(decimal(38,0),h.TongTien) Gross,
   CONVERT(decimal(38,0),h.GiamGia) Discount,CONVERT(decimal(38,0),h.PhiShip) Ship,
   CONVERT(decimal(38,0),h.ThanhTien) Amount,CONVERT(decimal(38,0),h.PhiDoiTac) Fees
  INTO #bills FROM dbo.HOADON h
  WHERE h.TrangThai=N'Da thanh toan' AND
   ((h.NgayThanhToan>=@from AND h.NgayThanhToan<@At) OR
    (@previousFrom IS NOT NULL AND h.NgayThanhToan>=@previousFrom AND h.NgayThanhToan<@previousUntil));
  SELECT 'C' Period,COUNT(*) Bills,COALESCE(SUM(Amount),0) Amount INTO #sales FROM #bills WHERE Period='C';
  INSERT #sales SELECT 'P',COUNT(*),COALESCE(SUM(Amount),0) FROM #bills WHERE Period='P';
  SELECT l.MaSP,s.TenSP,COUNT(DISTINCT b.MaHD) Bills,SUM(CONVERT(decimal(38,4),l.SoLuong)) Quantity,
   SUM(CONVERT(decimal(38,0),l.SoLuong*l.GiaBan)) Amount
  INTO #products FROM #bills b JOIN dbo.CHITIET_HD l ON l.MaHD=b.MaHD JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP
  WHERE b.Period='C' GROUP BY l.MaSP,s.TenSP;
  SELECT CASE WHEN p.NgayLap>=@from THEN 'C' ELSE 'P' END Period,CONVERT(decimal(38,0),p.SoTien) Amount
  INTO #expenses FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
  WHERE d.Loai=N'Chi' AND ((p.NgayLap>=@from AND p.NgayLap<@At) OR
   (@previousFrom IS NOT NULL AND p.NgayLap>=@previousFrom AND p.NgayLap<@previousUntil));
  DECLARE @revenue decimal(38,8),@previousRevenue decimal(38,8),@bills int,@previousBills int,
   @average decimal(38,8),@previousAverage decimal(38,8),@expense decimal(38,8),@previousExpense decimal(38,8);
  SELECT @revenue=Amount,@bills=Bills FROM #sales WHERE Period='C';
  SELECT @previousRevenue=CASE WHEN @previousFrom IS NULL THEN NULL ELSE Amount END,
   @previousBills=CASE WHEN @previousFrom IS NULL THEN NULL ELSE Bills END FROM #sales WHERE Period='P';
  SET @average=@revenue/NULLIF(@bills,0);SET @previousAverage=@previousRevenue/NULLIF(@previousBills,0);
  SELECT @expense=COALESCE(SUM(Amount),0) FROM #expenses WHERE Period='C';
  SELECT @previousExpense=CASE WHEN @previousFrom IS NULL THEN NULL ELSE COALESCE(SUM(Amount),0) END FROM #expenses WHERE Period='P';
  INSERT #indicators VALUES
   ('invoice-count.current',@bills),('invoice-count.previous',@previousBills),
   ('average.current',@average),('average.previous',@previousAverage),
   ('expense.current',@expense),('expense.previous',@previousExpense),
   ('difference.revenue',@revenue-@previousRevenue),('difference.invoices',@bills-@previousBills),
   ('difference.average',@average-@previousAverage),('difference.expense',@expense-@previousExpense),
   ('change.revenue',CASE WHEN @previousRevenue IS NULL THEN NULL WHEN @previousRevenue=0 AND @revenue=0 THEN 0 ELSE (@revenue/NULLIF(@previousRevenue,0)-1)*100 END),
   ('change.invoices',CASE WHEN @previousBills IS NULL THEN NULL WHEN @previousBills=0 AND @bills=0 THEN 0 ELSE (CONVERT(decimal(38,8),@bills)/NULLIF(@previousBills,0)-1)*100 END),
   ('change.average',CASE WHEN @average IS NULL OR @previousAverage IS NULL THEN NULL WHEN @previousAverage=0 AND @average=0 THEN 0 ELSE (@average/NULLIF(@previousAverage,0)-1)*100 END),
   ('change.expense',CASE WHEN @previousExpense IS NULL THEN NULL WHEN @previousExpense=0 AND @expense=0 THEN 0 ELSE (@expense/NULLIF(@previousExpense,0)-1)*100 END);
  INSERT #indicators SELECT 'quantity.total',COALESCE(SUM(Quantity),0) FROM #products;
  INSERT #indicators SELECT 'orders.open',COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan';
  INSERT #indicators SELECT 'delivery.pending',COUNT(*) FROM dbo.HOADON WHERE LoaiDon=N'Giao hang' AND TrangThai=N'Da thanh toan' AND TrangThaiGiao IN(N'Cho giao',N'Dang giao');
  INSERT #indicators SELECT 'material.zero',COUNT(*) FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0;
  INSERT #indicators SELECT 'debt.current',COALESCE(SUM(CONVERT(decimal(38,0),ConNo)),0) FROM dbo.vw_CongNoNCC WHERE ConNo>0;

  -- Chỉ sinh bucket đã bắt đầu; bucket cuối kỳ được cắt tại cùng mốc giờ/phút.
  ;WITH numbers AS(SELECT 0 n UNION ALL SELECT n+1 FROM numbers WHERE n<30),
  periods AS(SELECT 'C' Period,@from StartAt,@At UntilAt UNION ALL SELECT 'P',@previousFrom,@previousUntil WHERE @previousFrom IS NOT NULL)
  SELECT p.Period,n.n,CASE WHEN @Month=1 THEN DATEADD(day,n.n,p.StartAt) ELSE DATEADD(hour,n.n,p.StartAt) END Bucket,
   CASE WHEN @Month=1 THEN DATEADD(day,n.n+1,p.StartAt) ELSE DATEADD(hour,n.n+1,p.StartAt) END BucketUntil
  INTO #buckets FROM periods p CROSS JOIN numbers n
  WHERE CASE WHEN @Month=1 THEN DATEADD(day,n.n,p.StartAt) ELSE DATEADD(hour,n.n,p.StartAt) END<p.UntilAt OPTION(MAXRECURSION 31);
  INSERT #rows
  SELECT CASE k.Period WHEN 'C' THEN 'trend.current' ELSE 'trend.previous' END,CONVERT(nvarchar(33),k.Bucket,126),
   CASE WHEN @Month=1 THEN CONVERT(nvarchar(10),CONVERT(date,k.Bucket),103) ELSE CONVERT(nvarchar(5),k.Bucket,108) END,
   COUNT(b.MaHD),0,COALESCE(SUM(b.Gross),0),COALESCE(SUM(b.Discount),0),COALESCE(SUM(b.Ship),0),COALESCE(SUM(b.Amount),0),COALESCE(SUM(b.Fees),0),k.n
  FROM #buckets k LEFT JOIN #bills b ON b.Period=k.Period AND b.NgayThanhToan>=k.Bucket AND b.NgayThanhToan<k.BucketUntil
  GROUP BY k.Period,k.Bucket,k.n;
  INSERT #rows
  SELECT 'channels',c.Code,c.Name,COUNT(b.MaHD),0,COALESCE(SUM(b.Gross),0),COALESCE(SUM(b.Discount),0),COALESCE(SUM(b.Ship),0),COALESCE(SUM(b.Amount),0),COALESCE(SUM(b.Fees),0),c.Position
  FROM(VALUES(N'Tai quan',N'Tại quán',0),(N'Mang di',N'Mang đi',1),(N'Giao hang',N'Giao hàng',2))c(Code,Name,Position)
  LEFT JOIN #bills b ON b.Period='C' AND b.LoaiDon=c.Code GROUP BY c.Code,c.Name,c.Position;
  INSERT #indicators SELECT 'share.channel.'+CONVERT(varchar(20),Code),CASE WHEN @revenue=0 THEN NULL ELSE CONVERT(decimal(38,8),Amount)/@revenue*100 END FROM #rows WHERE PageKey='channels';
  INSERT #rows SELECT TOP(5) 'products',MaSP,TenSP,Bills,Quantity,0,0,0,Amount,0,CONVERT(int,ROW_NUMBER() OVER(ORDER BY Quantity DESC,MaSP)) FROM #products ORDER BY Quantity DESC,MaSP;
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH;
 SELECT @from [From],@At Until,@previousFrom PreviousFrom,@previousUntil PreviousUntil,@read ReadAt;
 SELECT [Key],Value FROM #indicators ORDER BY [Key];
 SELECT PageKey,Code,Name,Bills,Quantity,Gross,Discount,Ship,Amount,Fees,Gross-Discount AfterDiscount FROM #rows ORDER BY PageKey,Position;
 -- Tổng vẫn đầy đủ khi Top5 hoặc danh sách bucket rỗng.
 SELECT 'trend.current' PageKey,COALESCE(SUM(Gross),0) Gross,COALESCE(SUM(Discount),0) Discount,COALESCE(SUM(Ship),0) Ship,COALESCE(SUM(Amount),0) Amount,COALESCE(SUM(Fees),0) Fees FROM #bills WHERE Period='C'
 UNION ALL SELECT 'trend.previous',COALESCE(SUM(Gross),0),COALESCE(SUM(Discount),0),COALESCE(SUM(Ship),0),COALESCE(SUM(Amount),0),COALESCE(SUM(Fees),0) FROM #bills WHERE Period='P' AND @previousFrom IS NOT NULL
 UNION ALL SELECT 'channels',COALESCE(SUM(Gross),0),COALESCE(SUM(Discount),0),COALESCE(SUM(Ship),0),COALESCE(SUM(Amount),0),COALESCE(SUM(Fees),0) FROM #bills WHERE Period='C'
 UNION ALL SELECT 'products',0,0,0,COALESCE(SUM(Amount),0),0 FROM #products;
END;
GO
GRANT EXECUTE ON dbo.sp_DashboardAnalyticsRead TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_DashboardAnalyticsSources
 @Actor int,@Stamp binary(8),@Kind varchar(20),@From datetime2(7)=NULL,@Until datetime2(7)=NULL,
 @Channel nvarchar(20)=NULL,@Product varchar(5)=NULL,@Skip int=0
WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @read datetime2(7)=SYSDATETIME();
 IF @Kind IS NULL OR @Kind NOT IN('revenue','invoices','average','trend','channel','product','expense','orders','delivery','material','debt')
  OR @Skip<0 OR @@TRANCOUNT<>0 OR (@Kind NOT IN('orders','delivery','material','debt') AND (@From IS NULL OR @Until IS NULL OR @From>@Until OR @Until>@read))
  OR (@Channel IS NOT NULL AND @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang'))
  OR (@Kind='channel' AND @Channel IS NULL) OR (@Channel IS NOT NULL AND @Kind<>'channel')
  OR (@Kind='product' AND (@Product IS NULL OR LEN(@Product)<>5)) OR (@Product IS NOT NULL AND @Kind<>'product')
  THROW 53501,N'Nguồn hoặc kỳ tổng quan không hợp lệ.',1;
 CREATE TABLE #sources(SourceType varchar(20) NOT NULL,Code varchar(100) NOT NULL,Name nvarchar(200) NOT NULL,
  SourceDate datetime2(7) NULL,Amount decimal(38,0) NULL,Quantity decimal(38,4) NULL);
 BEGIN TRY
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;BEGIN TRAN;
  IF @Kind IN('revenue','invoices','average','trend','channel','product','orders','delivery')
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Invoice',h.MaHD,h.LoaiDon,CASE WHEN @Kind IN('orders','delivery') THEN h.NgayLap ELSE h.NgayThanhToan END,h.ThanhTien
   FROM dbo.HOADON h WHERE
    (@Kind='orders' AND h.TrangThai=N'Chua thanh toan') OR
    (@Kind='delivery' AND h.LoaiDon=N'Giao hang' AND h.TrangThai=N'Da thanh toan' AND h.TrangThaiGiao IN(N'Cho giao',N'Dang giao')) OR
    (@Kind NOT IN('orders','delivery') AND h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND h.NgayThanhToan<@Until
     AND (@Channel IS NULL OR h.LoaiDon=@Channel) AND (@Product IS NULL OR EXISTS(SELECT 1 FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD AND l.MaSP=@Product)));
  IF @Kind='expense'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Finance',p.MaTC,d.TenDMTC,p.NgayLap,p.SoTien FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
   WHERE d.Loai=N'Chi' AND p.NgayLap>=@From AND p.NgayLap<@Until;
  IF @Kind='material'
   INSERT #sources(SourceType,Code,Name,Quantity) SELECT 'Material',MaNVL,TenNVL+N' · '+DonViTinh,SoLuongTon FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0;
  IF @Kind='debt'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Debt',v.MaCN,n.TenNCC,p.NgayNhap,v.ConNo FROM dbo.vw_CongNoNCC v JOIN dbo.NHACUNGCAP n ON n.MaNCC=v.MaNCC JOIN dbo.PHIEUNHAP p ON p.MaPN=v.MaPN WHERE v.ConNo>0;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
 SELECT COUNT(*) TotalCount,@read ReadAt FROM #sources;
 SELECT SourceType,Code,Name,SourceDate,Amount,Quantity FROM #sources ORDER BY SourceDate DESC,SourceType,Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
GRANT EXECUTE ON dbo.sp_DashboardAnalyticsSources TO CafeManagerApp;
