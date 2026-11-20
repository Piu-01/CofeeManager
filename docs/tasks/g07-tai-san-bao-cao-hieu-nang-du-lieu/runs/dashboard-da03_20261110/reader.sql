SET NOCOUNT ON;
SET XACT_ABORT OFF;
DECLARE @a int,@s binary(8);SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1;
DECLARE @from datetime2(7)=CONVERT(datetime2(7),CONVERT(date,SYSDATETIME())),@start datetime2(7)=SYSDATETIME(),@i int=0;
WHILE (SELECT StartedAt FROM dbo.G07RaceGate WHERE Id=1) IS NULL
BEGIN
 IF DATEDIFF(second,@start,SYSDATETIME())>120 RETURN;
 WAITFOR DELAY '00:00:00.050';
END
WHILE (SELECT StopAt FROM dbo.G07RaceGate WHERE Id=1) IS NULL AND @i<$(Iterations)
BEGIN
 BEGIN TRY
  DECLARE @until datetime2(7)=DATEADD(second,-1,SYSDATETIME());
  -- INSERT EXEC mo transaction an => procedure chan @@TRANCOUNT<>0, phai goi truc tiep.
  EXEC dbo.sp_DashboardAnalyticsSources @Actor=@a,@Stamp=@s,@Kind='product',@From=@from,@Until=@until,@Product='SP999';
  SET @i+=1;
 END TRY BEGIN CATCH
  DECLARE @err int=ERROR_NUMBER(),@msg nvarchar(400)=ERROR_MESSAGE();
  INSERT dbo.G07RaceLog(Session,Iteration,ErrorNumber,Message) VALUES('reader',@i,@err,@msg);
  SET @i+=1;
 END CATCH;
END
INSERT dbo.G07RaceLog(Session,Iteration,ErrorNumber,Message) VALUES('reader',-1,0,CONCAT(N'done loops=',@i));
