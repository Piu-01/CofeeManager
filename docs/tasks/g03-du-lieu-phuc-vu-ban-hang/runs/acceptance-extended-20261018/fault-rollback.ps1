$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$db=$m.Database
$b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new("Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False")
$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();$results=[Collections.Generic.List[object]]::new()
$prefix="DECLARE @a int,@s binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;"
function Record([string]$Id,[string]$Sql,[int]$ErrorNumber=0){
 $r=[ordered]@{Id=$Id;Expected=if($ErrorNumber){"SQL $ErrorNumber; rollback invariants checked separately"}else{'All assertions hold; transaction 0:0'};Status='NOT RUN';Actual=$null}
 try{($prefix+$Sql)|Set-Content "$out/$Id.sql" -Encoding utf8;if($ErrorNumber){Expect-TestSqlError $c ($prefix+$Sql) $ErrorNumber}else{Run-TestSql $c ($prefix+$Sql)};$r.Status='PASS';$r.Actual='Assertions/error expectation completed'}catch{$r.Status='FAIL';$r.Actual=$_.Exception.Message}finally{$results.Add($r);$results|ConvertTo-Json -Depth 5|Set-Content "$out/fault-rollback-results.json" -Encoding utf8}
}
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G03TestMarker') -cne $m.RunId){throw 'Marker mismatch'}
 Run-TestSql $c ($prefix+"DECLARE @id uniqueidentifier=NEWID();EXEC dbo.sp_MaterialCreate @a,@s,'NVL99',@id,NULL,N'Fault material',N'Kg';")
 Run-TestSql $c "CREATE TRIGGER dbo.G03TestFaultMaterial ON dbo.MaterialOpening AFTER INSERT AS THROW 52990,'Injected material fault',1;"
 try{Record T09-K02-fault "DECLARE @rev binary(8),@id uniqueidentifier='11111111-1111-1111-1111-111111111109';SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99';EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL99',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=5,@Source=N'Fault';" 52990}
 finally{Run-TestSql $c 'DROP TRIGGER dbo.G03TestFaultMaterial;'}
 Record T09-K02-invariants "IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99')<>0 OR EXISTS(SELECT 1 FROM dbo.MaterialOpening WHERE MaNVL='NVL99') OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId='11111111-1111-1111-1111-111111111109') THROW 52999,'Opening partial state',1;"
 $recipeBefore=Read-TestScalar $c "SELECT (SELECT MaNVL,DinhMuc FROM dbo.CONGTHUC WHERE MaSP='SP996' ORDER BY MaNVL FOR JSON PATH)"
 Run-TestSql $c "CREATE TRIGGER dbo.G03TestFaultRecipe ON dbo.CONGTHUC AFTER DELETE AS THROW 52990,'Injected recipe fault',1;"
 try{Record T10-K03-fault "DECLARE @rev binary(8),@id uniqueidentifier='11111111-1111-1111-1111-111111111110',@r dbo.RecipeLineInput;SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP996';INSERT @r VALUES('NVL01',0.1);EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP996',@id,@r,@rev;" 52990}
 finally{Run-TestSql $c 'DROP TRIGGER dbo.G03TestFaultRecipe;'}
 $recipeAfter=Read-TestScalar $c "SELECT (SELECT MaNVL,DinhMuc FROM dbo.CONGTHUC WHERE MaSP='SP996' ORDER BY MaNVL FOR JSON PATH)"
 Record T10-K03-invariants "IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId='11111111-1111-1111-1111-111111111110') THROW 52999,'Recipe receipt survived',1;"
 if($recipeBefore -cne $recipeAfter){$results.Add(@{Id='T10-K03-recipe-compare';Status='FAIL';Expected='Identical old recipe';Actual='Changed'})}else{$results.Add(@{Id='T10-K03-recipe-compare';Status='PASS';Expected='Identical old recipe';Actual=$recipeAfter})}
 Run-TestSql $c ($prefix+"DECLARE @id uniqueidentifier=NEWID();EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Fault customer',@Phone=NULL;")
 $kh=Read-TestScalar $c "SELECT MaKH FROM dbo.KHACHHANG WHERE TenKH=N'Fault customer'"
 Run-TestSql $c "CREATE TRIGGER dbo.G03TestFaultCustomer ON dbo.CustomerOpening AFTER INSERT AS THROW 52990,'Injected customer fault',1;"
 try{Record T12-K05-fault "DECLARE @rev binary(8),@id uniqueidentifier='11111111-1111-1111-1111-111111111112';SELECT @rev=Revision FROM dbo.KHACHHANG WHERE MaKH='$kh';EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='$kh',@ExpectedRevision=@rev,@Points=300,@Source=N'Fault';" 52990}
 finally{Run-TestSql $c 'DROP TRIGGER dbo.G03TestFaultCustomer;'}
 Record T12-K05-invariants "IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='$kh')<>0 OR EXISTS(SELECT 1 FROM dbo.CustomerOpening WHERE MaKH='$kh') OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId='11111111-1111-1111-1111-111111111112') THROW 52999,'Customer partial state',1;"
 $policyBefore=Read-TestScalar $c "SELECT (SELECT MaHang,DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN ORDER BY MaHang FOR JSON PATH)"
 $customersBefore=Read-TestScalar $c "SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)"
 Run-TestSql $c "CREATE TRIGGER dbo.G03TestFaultPolicy ON dbo.HANGTHANHVIEN AFTER UPDATE AS IF EXISTS(SELECT 1 FROM inserted WHERE DiemToiThieu=0) THROW 52990,'Injected after-policy before reclassification',1;"
 try{Record T13-K05-fault @'
DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);
INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @rev binary(8),@hash binary(32),@id uniqueidentifier='11111111-1111-1111-1111-111111111113';SELECT @rev=Revision,@hash=PayloadHash FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@rev,@hash,@id;
'@ 52990}
 finally{Run-TestSql $c 'DROP TRIGGER dbo.G03TestFaultPolicy;'}
 $policyAfter=Read-TestScalar $c "SELECT (SELECT MaHang,DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN ORDER BY MaHang FOR JSON PATH)"
 $customersAfter=Read-TestScalar $c "SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)"
 Record T13-K05-invariants "IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId='11111111-1111-1111-1111-111111111113') THROW 52999,'Policy receipt survived',1;"
 $results.Add(@{Id='T13-K05-policy-customer-compare';Status=if($policyBefore -ceq $policyAfter -and $customersBefore -ceq $customersAfter){'PASS'}else{'FAIL'};Expected='Policy and all customer points/tiers unchanged';Actual='Compared ordered JSON before/after'})
}finally{$results|ConvertTo-Json -Depth 5|Set-Content "$out/fault-rollback-results.json" -Encoding utf8;$c.Dispose()}
