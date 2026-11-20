SET NOCOUNT ON;
DECLARE @a int,@s binary(8);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap=N'bench.admin';
IF @a IS NULL THROW 53901,N'bench.admin missing',1;
DECLARE @from datetime2(7)=DATEADD(day,-90,CONVERT(datetime2(7),CONVERT(date,SYSDATETIME()))),
 @until datetime2(7)=SYSDATETIME(),@kind varchar(20)='$(Kind)',
 @product varchar(5)=CASE WHEN '$(Kind)'='product' THEN 'SP001' END,
 @channel nvarchar(20)=CASE WHEN '$(Kind)'='channel' THEN N'Mang di' END;
EXECUTE AS USER='G07Runtime';
SET STATISTICS IO ON;SET STATISTICS TIME ON;
DECLARE @rep int=1,@t0 datetime2(7);
WHILE @rep<=5
BEGIN
 SET @t0=SYSDATETIME();
 EXEC dbo.sp_DashboardAnalyticsSources @Actor=@a,@Stamp=@s,@Kind=@kind,@From=@from,@Until=@until,@Channel=@channel,@Product=@product;
 PRINT CONCAT('REP|',@kind,'|',@rep,'|',DATEDIFF(microsecond,@t0,SYSDATETIME()));
 SET @rep+=1;
END;
SET STATISTICS IO OFF;SET STATISTICS TIME OFF;
REVERT;
