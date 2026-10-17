param([string]$Root,[string]$Output,[string]$Database)
$ErrorActionPreference='Stop'; . (Join-Path $Root 'tools/database/DatabaseTestTools.ps1')
[void][IO.Directory]::CreateDirectory($Output)
$results=[Collections.Generic.List[object]]::new()
$owner=Open-TestConnection $Database; Assert-TestTarget $owner 'G02'
if((Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.G02TestMarker') -ne 1){throw 'Marker missing'}
$app=Open-TestConnection $Database; Assert-TestTarget $app 'G02'; Run-TestSql $app "EXECUTE AS USER=N'G02_Runtime'"
$actor=Read-TestScalar $owner "SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin'"
$stamp=Read-TestScalar $owner "SELECT sys.fn_varbintohexstr(SecurityStamp) FROM dbo.TAIKHOAN WHERE MaTK=$actor"
$auth="@Actor=$actor,@Stamp=$stamp"
function Check($id,$expected,[scriptblock]$action){try{$actual=& $action;$results.Add([ordered]@{Id=$id;Expected=$expected;Actual=[string]$actual;Status='PASS'})}catch{$results.Add([ordered]@{Id=$id;Expected=$expected;Actual=$_.Exception.Message;Status='FAIL'})}}
function Reject($sql,$number){$before=Snapshot-TestRows $owner;Expect-TestSqlError $app $sql $number;$after=Snapshot-TestRows $owner;if($before -cne $after){throw 'Persistent snapshot changed after rejection'};return "SQL$number; transaction0:0; full table snapshot unchanged"}
try{
 foreach($t in @(
 @{Id='ratio.minus1';Call="DeliveryPartnerCreate $auth,@Code='DT874',@Name=N'Invalid ratio',@Ratio=-1"},
 @{Id='ratio.above100';Call="DeliveryPartnerCreate $auth,@Code='DT874',@Name=N'Invalid ratio',@Ratio=100.01"},
 @{Id='allowance.minus1';Call="PositionCreate $auth,@Code='CV874',@Name=N'Invalid allowance',@Allowance=-1,@Factor=1"},
 @{Id='factor.below1';Call="PositionCreate $auth,@Code='CV874',@Name=N'Invalid factor',@Allowance=0,@Factor=0.99"},
 @{Id='shift.equal';Call="ShiftCreate $auth,@Code='CA874',@Name=N'Invalid shift',@StartTime='06:00',@EndTime='06:00'"}
 )){Check "T08-K03.a.$($t.Id).SQL" 'Reject invalid boundary, no changes after error' {Reject "EXEC dbo.sp_Catalog$($t.Call)" 51501}}
 foreach($phone in @(@{Id='NULL';Sql='NULL'},@{Id='empty';Sql="''"})){
  Check "T08-K02.ncc.$($phone.Id).SQL" 'Reject required phone without writes' {Reject "EXEC dbo.sp_CatalogSupplierCreate $auth,@Code='NCC74',@Name=N'Required phone',@Phone=$($phone.Sql)" 51501}
 }
 Check 'T08-K02.fk.actorMissing.write' 'Unknown actor cannot write' {Reject "EXEC dbo.sp_CatalogSupplierCreate @Actor=-1,@Stamp=$stamp,@Code='NCC74',@Name=N'Forbidden',@Phone='01740000001'" 51505}
 Check 'T08-K02.fk.stampWrong.write' 'Wrong stamp cannot write' {Reject "EXEC dbo.sp_CatalogSupplierCreate @Actor=$actor,@Stamp=0x0000000000000000,@Code='NCC74',@Name=N'Forbidden',@Phone='01740000001'" 51505}
 Check 'T08-K02.fk.invalidCategory' 'Product FK rejects, snapshot unchanged' {$before=Snapshot-TestRows $owner;Expect-TestSqlError $owner "INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('SP874',N'Invalid category','DM000',1000)" 547;if($before -cne (Snapshot-TestRows $owner)){throw 'Snapshot changed'};'SQL547; transaction0:0; full snapshot unchanged'}
 foreach($t in @(
 @{Id='DT0';Call="DeliveryPartnerCreate $auth,@Code='DT874',@Name=N'Ratio zero',@Ratio=0";Query="SELECT CONVERT(varchar(20),ChietKhau) FROM dbo.DOITACGIAOHANG WHERE MaDT='DT874'";Expected='0.00'},
 @{Id='DT100';Call="DeliveryPartnerCreate $auth,@Code='DT875',@Name=N'Ratio hundred',@Ratio=100";Query="SELECT CONVERT(varchar(20),ChietKhau) FROM dbo.DOITACGIAOHANG WHERE MaDT='DT875'";Expected='100.00'},
 @{Id='CV0factor1';Call="PositionCreate $auth,@Code='CV874',@Name=N'Position boundary',@Allowance=0,@Factor=1";Query="SELECT CONCAT(PhuCap,':',HeSoTangCa) FROM dbo.CHUCVU WHERE MaCV='CV874'";Expected='0:1.00'},
 @{Id='CAovernight';Call="ShiftCreate $auth,@Code='CA874',@Name=N'Overnight',@StartTime='22:00',@EndTime='06:00'";Query="SELECT CONCAT(CONVERT(varchar(5),GioBatDau,108),':',CONVERT(varchar(5),GioKetThuc,108)) FROM dbo.CALAMVIEC WHERE MaCa='CA874'";Expected='22:00:06:00'}
 )){Check "T08-K04.$($t.Id).SQL" "Boundary persists exactly $($t.Expected)" {Run-TestSql $app "EXEC dbo.sp_Catalog$($t.Call)";$v=Read-TestScalar $owner $t.Query;if($v -cne $t.Expected){throw "Actual boundary=$v"};"Persisted=$v"}}
 foreach($role in @('ADMIN','CASH','STORE')){foreach($purpose in @('Checkout','Inventory')){
  Check "T08-K08.a.selector.$role.$purpose.SQL" 'Role/purpose restrictions and supported-only projection' {
   $id=Read-TestScalar $owner "SELECT TOP(1) MaTK FROM dbo.TAIKHOAN WHERE MaVT='$role' AND HoatDong=1"
   $s=Read-TestScalar $owner "SELECT sys.fn_varbintohexstr(SecurityStamp) FROM dbo.TAIKHOAN WHERE MaTK=$id"
   $sql="EXEC dbo.sp_PaymentMethodListSupported @Actor=$id,@Stamp=$s,@Purpose='$purpose'"
   $allowed=$role -eq 'ADMIN' -or ($role -eq 'CASH' -and $purpose -eq 'Checkout') -or ($role -eq 'STORE' -and $purpose -eq 'Inventory')
   if(-not $allowed){return Reject $sql 51505}
   $cmd=$app.CreateCommand();$cmd.CommandText=$sql;try{$r=$cmd.ExecuteReader();$codes=@();while($r.Read()){$codes+=$r.GetString(0).Trim()};$r.Close()}finally{$cmd.Dispose()}
   if('PT001' -notin $codes -or 'PT002' -notin $codes -or 'PT010' -in $codes -or 'PT003' -in $codes){throw 'Selector mismatch'}
   return "Supported codes=$($codes -join ','); PT010/PT003 absent"
  }
 }}
}finally{$app.Dispose();$owner.Dispose();[IO.File]::WriteAllText((Join-Path $Output 'results.json'),($results|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))}
if(@($results|Where-Object Status -eq 'FAIL').Count){exit 1}
