-- T20 sau migration 043: luồng nhập 2 bước thay contract một bước cũ.
-- Giữ mã ca T20-K05.SQL-replay để đối chiếu lịch sử: trả một phần vẫn đúng chi/nợ và replay không nhân đôi.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G05_%' OR OBJECT_ID('dbo.G05TestMarker') IS NULL THROW 53900,N'Chỉ dùng fixture G05.',1;
DECLARE @actor int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1),@stamp binary(8),@id uniqueidentifier=NEWID(),@countId uniqueidentifier=NEWID(),@postId uniqueidentifier=NEWID(),@code varchar(10),@revision binary(8),@ok bit,@err int,@today datetime2(0)=CONVERT(datetime2(0),SYSDATETIME());
SELECT @stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@actor;
IF @actor IS NULL THROW 53900,N'Thiếu actor fixture.',1;
DECLARE @lines dbo.PurchaseLinesInput;INSERT @lines VALUES('NVL01',1,10000);
DECLARE @receive dbo.PurchaseReceiveInput;INSERT @receive VALUES('NVL01',1,NULL);
DECLARE @before decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01');
EXEC dbo.sp_TaoPhieuNhap @actor,@stamp,@id,'NCC01',@today,NULL,4000,10000,@lines;
EXEC dbo.sp_TaoPhieuNhap @actor,@stamp,@id,'NCC01',@today,NULL,4000,10000,@lines;
SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
SELECT @revision=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code;
SET @ok=CASE WHEN @code IS NOT NULL AND (SELECT COUNT(*) FROM dbo.PHIEUNHAP WHERE MaPN=@code)=1 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before AND NOT EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WHERE MaPN=@code) AND NOT EXISTS(SELECT 1 FROM dbo.CONGNO_NCC WHERE MaPN=@code) THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20-K05.SQL-draft',@ok,N'Draft created once, no stock/expense/debt.';
EXEC dbo.sp_LuuKiemNhan @actor,@stamp,@countId,@code,@revision,@receive;
SELECT @revision=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code;
EXEC dbo.sp_ChotNhanHang @actor,@stamp,@postId,@code,@revision,4000;
EXEC dbo.sp_ChotNhanHang @actor,@stamp,@postId,@code,@revision,4000;
SET @ok=CASE WHEN (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before+1 AND (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN=@code AND SoTien=4000)=1 AND (SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaPN=@code AND TienNo=6000)=1 AND (SELECT TrangThai FROM dbo.PHIEUNHAP WHERE MaPN=@code)='DaChot' THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20-K05.SQL-replay',@ok,N'PN/chi4000/no6000, stock +1 exactly once, posted once.';
