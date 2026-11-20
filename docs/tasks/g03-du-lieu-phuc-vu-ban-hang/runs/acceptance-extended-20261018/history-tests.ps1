. "$PSScriptRoot/history-helpers.ps1"
Case T10-K05-stop 'Stop selling removes selector entry and preserves existing invoice lines' @'
DECLARE @before nvarchar(max)=(SELECT MaHD,MaDong,SoLuong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP996' ORDER BY MaDong FOR JSON PATH),@r dbo.RecipeLineInput;
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP996';
EXEC dbo.sp_ProductUpdate @Actor=@a,@Stamp=@s,@Code='SP996',@RequestId=@id,@Recipe=@r,@ExpectedRevision=@rev,@Name=N'Concurrent recipe v3',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
DECLARE @rows TABLE(Code char(5),Name nvarchar(50),Category char(5),Price decimal(18,0),Status nvarchar(20),Revision binary(8));
INSERT @rows EXEC dbo.sp_ProductList @a,@s,N'SP996',0,NULL,1;
IF EXISTS(SELECT 1 FROM @rows) THROW 52999,'Stopped product still selectable',1;
IF @before<>(SELECT MaHD,MaDong,SoLuong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP996' ORDER BY MaDong FOR JSON PATH) THROW 52999,'Existing lines changed',1;
'@
Case T10-K06-price 'Changing menu price preserves all historical snapshot prices' @'
DECLARE @before nvarchar(max)=(SELECT MaDong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP001' ORDER BY MaDong FOR JSON PATH),@r dbo.RecipeLineInput;
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP001';
EXEC dbo.sp_ProductUpdate @Actor=@a,@Stamp=@s,@Code='SP001',@RequestId=@id,@Recipe=@r,@ExpectedRevision=@rev,@Name=N'Price test',@Category='DM001',@Price=12345,@Status=N'Dang ban';
IF @before<>(SELECT MaDong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP001' ORDER BY MaDong FOR JSON PATH) THROW 52999,'History price changed',1;
'@
Case T10-K07-snapshot 'Replacing recipe preserves consumption snapshots and stock' @'
DECLARE @before nvarchar(max)=(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH),@stock nvarchar(max)=(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH),@r dbo.RecipeLineInput;
INSERT @r VALUES('NVL01',0.01);SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP001';EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP001',@id,@r,@rev;
IF @before<>(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) OR @stock<>(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) THROW 52999,'Consumption recalculated',1;
'@
Case T10-K10-delete-history 'Product with paid invoices cannot delete' "DECLARE @r dbo.RecipeLineInput;SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP996';EXEC dbo.sp_ProductDelete @a,@s,'SP996',@id,@r,@rev;" 51702
Case T10-K10-delete-unused 'Unused product deletes atomically, receipt remains' @'
DECLARE @r dbo.RecipeLineInput;EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP994',@RequestId=@id,@Recipe=@r,@Name=N'Unused',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP994';SET @id=NEWID();EXEC dbo.sp_ProductDelete @a,@s,'SP994',@id,@r,@rev;
IF EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP994') OR NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND EntityCode='SP994') THROW 52999,'Delete receipt mismatch',1;
'@
Case T11-K04-view 'Paid/cancelled do not occupy table; open does' "IF (SELECT COUNT(*) FROM dbo.vw_TrangThaiBan WHERE TrangThai=N'Dang phuc vu')<>1 OR NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0003' AND MaHD='HD20250410') THROW 52999,'Occupancy wrong',1;"
Case T11-K07-refresh 'After second session cancels order select-current sees empty table' @'
DECLARE @before TABLE(Code char(5),Name nvarchar(50),Area char(5),Status nvarchar(30),OrderCode char(10),Revision binary(8),ReadAt datetime2(7));
INSERT @before EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';
IF NOT EXISTS(SELECT 1 FROM @before WHERE OrderCode='HD20250410') THROW 52999,'Initial order wrong',1;
EXEC dbo.sp_HuyHoaDon 'HD20250410';
DELETE @before;INSERT @before EXEC dbo.sp_TableSelectCurrent @a,@s,'B0003';
IF NOT EXISTS(SELECT 1 FROM @before WHERE OrderCode IS NULL AND Status=N'Trong') THROW 52999,'Stale order returned',1;
'@
Case T12-K05-repeat-legacy 'Existing legacy customer cannot reopen points' "SELECT @rev=Revision FROM dbo.KHACHHANG WHERE MaKH='KH001';EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='KH001',@ExpectedRevision=@rev,@Points=1,@Source=N'Reopen';" 51902
Case T12-K07-history 'Existing customer source prevents deletion' "SELECT @rev=Revision FROM dbo.KHACHHANG WHERE MaKH='KH001';EXEC dbo.sp_CustomerDelete @a,@s,@id,'KH001',@rev;" 547
Case T12-K01-profile 'Profile changes preserve points and tier' @'
DECLARE @points int,@tier char(5);SELECT @rev=Revision,@points=DiemTichLuy,@tier=MaHang FROM dbo.KHACHHANG WHERE MaKH='KH001';
EXEC dbo.sp_CustomerUpdate @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='KH001',@ExpectedRevision=@rev,@Name=N'Profile rename',@Phone=NULL;
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH='KH001' AND DiemTichLuy=@points AND MaHang=@tier AND SDT IS NULL) THROW 52999,'Profile changed points/tier',1;
'@
