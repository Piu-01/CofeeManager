. "$PSScriptRoot/material-edge-helpers.ps1"
Case T09-K04-maximum 'Maximum decimal18,4 persists exactly' @'
EXEC dbo.sp_MaterialCreate @a,@s,'NVL88',@id,NULL,N'Maximum test',N'Kg';SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88';SET @id=NEWID();
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL88',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=99999999999999.9999,@Source=N'Maximum';
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL88')<>99999999999999.9999 THROW 52999,'Precision lost',1;
'@
Case T09-K04-overflow 'Quantity over decimal18,4 maximum rejected' "EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL88',@RequestId=@id,@Quantity=100000000000000,@Source=N'Overflow';" 51601
Case T09-K06-before-source 'Unit changes before any source allowed and stock remains zero' @'
EXEC dbo.sp_MaterialCreate @a,@s,'NVL89',@id,NULL,N'Unit test',N'Kg';SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89';SET @id=NEWID();EXEC dbo.sp_MaterialUpdate @a,@s,'NVL89',@id,@rev,N'Unit test',N'g';
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89' AND DonViTinh=N'g' AND SoLuongTon=0) THROW 52999,'Unit conversion corrupted stock',1;
DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL89',1);SET @id=NEWID();EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Unit recipe test',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
'@
Case T09-K06-recipe 'Unit changes after recipe blocked without opening marker' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89';EXEC dbo.sp_MaterialUpdate @a,@s,'NVL89',@id,@rev,N'Unit test',N'Kg';" 51602
Case T09-K08-recipe 'Delete recipe-referenced material blocked' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89';EXEC dbo.sp_MaterialDelete @a,@s,'NVL89',@id,@rev;" 51602
Case T09-K03-transaction-setup 'Valid baseline purchase increases stock after material creation' @'
EXEC dbo.sp_MaterialCreate @a,@s,'NVL90',@id,NULL,N'Transaction test',N'Kg';
INSERT dbo.PHIEUNHAP(MaPN,MaNV,MaNCC)VALUES('PN09999001','NV001','NCC01');INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap)VALUES('PN09999001','NVL90',1,1000);
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL90')<>1 THROW 52999,'Baseline source failed',1;
'@
Case T09-K03-after-transaction 'Opening after transaction rejected without preexisting opening marker' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL90';EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL90',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=2,@Source=N'Not allowed';" 51602
Case T09-K06-transaction 'Unit change after transaction blocked' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL90';EXEC dbo.sp_MaterialUpdate @a,@s,'NVL90',@id,@rev,N'Transaction test',N'g';" 51602
Case T09-K08-transaction 'Delete material with transaction source blocked' "SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL90';EXEC dbo.sp_MaterialDelete @a,@s,'NVL90',@id,@rev;" 51602
