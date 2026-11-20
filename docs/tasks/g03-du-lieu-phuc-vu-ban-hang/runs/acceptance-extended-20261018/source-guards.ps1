. "$PSScriptRoot/source-guards-helpers.ps1"
Case T13-K06-write-overdraw 'Real adjust overdraw rejects and rolls back points/history/receipt' @'
DECLARE @before int,@count int,@cr binary(8),@pr binary(8);SELECT @before=DiemTichLuy,@cr=Revision FROM dbo.KHACHHANG WHERE MaKH='KH001';SELECT @pr=Revision FROM dbo.LoyaltyPolicyState;SELECT @count=COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001';
BEGIN TRY EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Tru',2147483647,N'Overdraw actual write',@cr,@pr,@id;THROW 52999,'Overdraw accepted',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>52002 THROW;END CATCH;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>@before OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001')<>@count OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id) THROW 52999,'Overdraw partial state',1;
'@
Case T09-K03-other-sources-setup 'Valid PX, KK and consumption fixture sources exist' @'
EXEC dbo.sp_MaterialCreate @a,@s,'NVL85',@id,NULL,N'Consumption source',N'Kg';SET @id=NEWID();EXEC dbo.sp_MaterialCreate @a,@s,'NVL86',@id,NULL,N'Count source',N'Kg';SET @id=NEWID();EXEC dbo.sp_MaterialCreate @a,@s,'NVL87',@id,NULL,N'Export source',N'Kg';
INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap)VALUES('PN09999001','NVL85',2,1000),('PN09999001','NVL86',2,1000),('PN09999001','NVL87',2,1000);
INSERT dbo.PHIEUXUAT(MaPX,MaNV,LoaiXuat)VALUES('PX09999001','NV001',N'Huy do hong');INSERT dbo.CHITIET_PX(MaPX,MaNVL,SoLuongXuat)VALUES('PX09999001','NVL87',1);
DECLARE @date date=CONVERT(date,SYSDATETIME());INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV)VALUES('KK09999001',@date,'NV001');INSERT dbo.CHITIET_KK(MaKK,MaNVL,TonHeThong,TonThucTe)VALUES('KK09999001','NVL86',2,2);
INSERT dbo.HOADON(MaHD,MaNV,LoaiDon)VALUES('HD09999301','NV001',N'Mang di');INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong)VALUES('HD09999301','NVL85',1);
'@
foreach($code in @('NVL85','NVL86','NVL87')){
 Case "T09-K03-source-$code" 'Opening rejected for material with transaction source' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='$code';EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='$code',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=5,@Source=N'Not allowed';" 51602
 Case "T09-K08-source-$code" 'Deleting referenced material rejected' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='$code';EXEC dbo.sp_MaterialDelete @a,@s,'$code',@id,@rev;" 51602
}
