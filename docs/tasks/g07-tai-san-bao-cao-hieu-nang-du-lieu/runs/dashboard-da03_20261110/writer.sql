SET NOCOUNT ON;
SET XACT_ABORT OFF;
DECLARE @a int,@s binary(8);SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1;
DECLARE @start datetime2(7)=SYSDATETIME();
WHILE (SELECT StartedAt FROM dbo.G07RaceGate WHERE Id=1) IS NULL
BEGIN
 IF DATEDIFF(second,@start,SYSDATETIME())>120
 BEGIN INSERT dbo.G07RaceLog(Session,Iteration,ErrorNumber,Message) VALUES('writer',0,0,N'gate timeout');RETURN;END
 WAITFOR DELAY '00:00:00.050';
END
DECLARE @i int=1,@total int=$(Iterations),@code char(10),@req uniqueidentifier,@rev binary(8),
 @err int,@msg nvarchar(400),@token binary(32),@payable decimal(38,0);
DECLARE @c TABLE(Kind varchar(20),Code char(10));
DECLARE @pay dbo.DanhSachThanhToan;
WHILE @i<=@total AND (SELECT StopAt FROM dbo.G07RaceGate WHERE Id=1) IS NULL
BEGIN
 BEGIN TRY
  DELETE @c;INSERT @c EXEC dbo.sp_CapMa 'HOADON';SELECT @code=Code FROM @c;
  SET @req=NEWID();
  EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@req,@Channel=N'Mang di',@Product='SP999',@Quantity=1,@Note=N'Race';
  SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
  SET @req=NEWID();
  EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@req,@Action='Add',@Product='SP999',@Quantity=1,@Note=N'Race';
  IF @i%10=0
  BEGIN
   SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
   SELECT @token=PreviewToken,@payable=Payable FROM dbo.fn_CheckoutPreview(@code);
   DELETE @pay;INSERT @pay(MaPT,SoTien) VALUES('PT001',@payable);
   SET @req=NEWID();
   EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@rev,@RequestId=@req,@ExpectedPreviewToken=@token,@ThanhToan=@pay;
  END
 END TRY BEGIN CATCH
  SELECT @err=ERROR_NUMBER(),@msg=ERROR_MESSAGE();
  INSERT dbo.G07RaceLog(Session,Iteration,ErrorNumber,Message) VALUES('writer',@i,@err,@msg);
 END CATCH;
 SET @i+=1;
END
INSERT dbo.G07RaceLog(Session,Iteration,ErrorNumber,Message) VALUES('writer',-1,0,CONCAT(N'done loops=',@i-1));
