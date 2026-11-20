. "$PSScriptRoot/final-helpers.ps1"
foreach($price in @('0','-1','0.5')){
 Case "T10-K02-price-$price" 'Invalid price rejected' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',0.01);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Invalid price',@Category='DM001',@Price=$price,@Status=N'Dang ban';" 51701
}
foreach($quantity in @('0','-1','0.00001','100000000000000')){
 Case "T10-K02-qty-$quantity" 'Invalid recipe quantity rejected' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',$quantity);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Invalid qty',@Category='DM001',@Price=10000,@Status=N'Dang ban';" 51701
}
Case T10-K02-duplicate 'Duplicate recipe ingredient rejected' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',0.01),('NVL01',0.02);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Duplicate',@Category='DM001',@Price=10000,@Status=N'Dang ban';" 51701
Case T10-K02-fk 'Invalid material FK rejected' "DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL00',0.01);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Invalid FK',@Category='DM001',@Price=10000,@Status=N'Dang ban';" 51701
Case T11-K02-empty 'Empty area name rejected' "EXEC dbo.sp_AreaCreate @a,@s,'KV999',@id,NULL,N' ';" 51801
Case T11-K02-long 'Overlength table name rejected' "DECLARE @n nvarchar(max)=REPLICATE(N'X',51);EXEC dbo.sp_TableCreate @a,@s,'B0999',@id,NULL,@n,'KV001';" 51801
Case T11-K02-fk 'Invalid area FK rejected' "EXEC dbo.sp_TableCreate @a,@s,'B0999',@id,NULL,N'Test','KV000';" 51801
Case T11-K06-paid 'Table with paid history cannot delete' "SELECT @rev=Revision FROM dbo.BAN WHERE MaBan='B0001';EXEC dbo.sp_TableDelete @a,@s,'B0001',@id,@rev;" 547
Case T11-K06-cancel 'Table with cancelled history cannot delete' "SELECT @rev=Revision FROM dbo.BAN WHERE MaBan='B0003';EXEC dbo.sp_TableDelete @a,@s,'B0003',@id,@rev;" 547
Case T13-K02-fk-customer 'Unknown customer rejected' "EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-18','KH000',NULL;" 52001
Case T13-K02-fk-promotion 'Unknown promotion rejected' "EXEC dbo.sp_DiscountPreview @a,@s,100,10,'2026-10-18',NULL,'KM00000000';" 52001
Case T13-K04-percent-negative 'Negative percent rejected' "DECLARE @t dbo.LoyaltyTierInput;INSERT @t VALUES('DONG',0,-1);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;" 52001
Case T13-K04-code-long 'Overlength tier code rejected before cast' "DECLARE @t dbo.LoyaltyTierInput;INSERT @t VALUES('ABCDEF',0,0);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;" 52001
Case T13-K04-points-overflow 'Threshold exceeding int rejected before cast' "DECLARE @t dbo.LoyaltyTierInput;INSERT @t VALUES('DONG',0,0),('VIP',2147483648,10);EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;" 52001
Case T13-K05-stale 'Policy changes invalidate earlier preview' @'
DECLARE @t dbo.LoyaltyTierInput;INSERT @t SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @p TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);INSERT @p EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@t;
DECLARE @r binary(8),@h binary(32);SELECT @r=Revision,@h=PayloadHash FROM @p;EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@t,@r,@h,@id;
SET @id=NEWID();EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@t,@r,@h,@id;
'@ 51603
Case T13-K07-nonexistent 'Nonexistent order FK rejected' "INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD00000000',N'Cong',1,N'Invalid FK');" 547
Case T13-K07-wrong-customer 'Paid order with zero earned points still rejects wrong customer' @'
INSERT dbo.HOADON(MaHD,MaNV,MaKH,LoaiDon)VALUES('HD09999201','NV001','KH002',N'Mang di');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan)VALUES('HD09999201','SP995',1,500);
DECLARE @tt dbo.DanhSachThanhToan;INSERT @tt(MaPT,SoTien)VALUES('PT001',500);EXEC dbo.sp_ThanhToanHoaDon 'HD09999201',@tt;
INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD09999201',N'Cong',1,N'Wrong customer');
'@ 51012
Case T13-K09-date-rate 'Date, percentage and name changes preserve paid snapshot' @'
DECLARE @before nvarchar(max)=(SELECT MaHD,GiamGia,ThanhTien,PhiShip,PhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH),@code varchar(10);
SELECT TOP(1) @code=k.MaKM,@rev=k.Revision FROM dbo.KHUYENMAI k JOIN dbo.HOADON h ON h.MaKM=k.MaKM WHERE h.TrangThai=N'Da thanh toan';
EXEC dbo.sp_PromotionUpdate @a,@s,@code,@id,@rev,N'Changed name and period','2026-10-18','2026-10-18',0;
IF @before<>(SELECT MaHD,GiamGia,ThanhTien,PhiShip,PhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) THROW 52999,'Paid snapshot changed',1;
'@
