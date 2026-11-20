-- G04-POS-06: gợi ý ghi chú món từ lịch sử dòng hóa đơn (đọc, không ghi).
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=30)
 THROW 52107,N'Cần schema030 và runner migration.',1;
GO
CREATE PROCEDURE dbo.sp_SalesNoteSuggestions @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'',@Take int=30 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @Take NOT BETWEEN 1 AND 50 THROW 52101,N'Số gợi ý không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 -- Ghi chú đã dùng nhiều lần đứng trước; chỉ trả chuỗi, không lộ dòng/đơn nào.
 SELECT TOP(@Take) x.Note
 FROM (SELECT LTRIM(RTRIM(c.GhiChu)) Note,COUNT_BIG(*) Uses
       FROM dbo.CHITIET_HD c
       WHERE c.GhiChu IS NOT NULL AND LTRIM(RTRIM(c.GhiChu))<>N''
         AND (@Search=N'' OR LTRIM(RTRIM(c.GhiChu)) LIKE N'%'+@Search+N'%' ESCAPE N'~')
       GROUP BY LTRIM(RTRIM(c.GhiChu))) x
 ORDER BY x.Uses DESC,x.Note;
END;
GO
