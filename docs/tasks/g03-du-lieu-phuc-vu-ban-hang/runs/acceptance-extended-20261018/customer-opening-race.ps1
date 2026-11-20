. "$PSScriptRoot/customer-race-helpers.ps1"
$c=Connection
try{
 Run-TestSql $c ((Prefix)+"EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Customer opening race final',@Phone=NULL;")
 $kh=Read-TestScalar $c "SELECT MaKH FROM dbo.KHACHHANG WHERE TenKH=N'Customer opening race final'";$revision=Read-TestScalar $c "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.KHACHHANG WHERE MaKH='$kh'"
 $one="DECLARE @r binary(8)=$revision;BEGIN TRAN;EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='$kh',@ExpectedRevision=@r,@Points=100,@Source=N'Session1';"
 $two="DECLARE @r binary(8)=$revision;EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='$kh',@ExpectedRevision=@r,@Points=200,@Source=N'Session2';"
 Two T12-K05-two $one $two 51603 "IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='$kh')<>100 OR (SELECT COUNT(*) FROM dbo.CustomerOpening WHERE MaKH='$kh')<>1 THROW 52999,'Customer double opening',1;"
}finally{$c.Dispose()}
