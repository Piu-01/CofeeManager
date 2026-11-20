. "$PSScriptRoot/policy-race-helpers.ps1"
function PolicySql([bool]$hold){
 $begin=if($hold){'BEGIN TRAN;'}else{''}
 return $begin+@'
DECLARE @tiers dbo.LoyaltyTierInput;INSERT @tiers SELECT RTRIM(MaHang),DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN;
DECLARE @preview TABLE(Revision binary(8),PayloadHash binary(32),AffectedCustomers int);
INSERT @preview EXEC dbo.sp_LoyaltyPolicyPreview @a,@s,@tiers;
DECLARE @r binary(8),@hash binary(32);SELECT @r=Revision,@hash=PayloadHash FROM @preview;
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@r,@hash,@id;
'@
}
$invariant="IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaHang<>CASE WHEN DiemTichLuy>=1000 THEN 'VIP' WHEN DiemTichLuy>=100 THEN 'RICH' ELSE 'DONG' END) THROW 52999,'Policy customer mismatch',1;"
$c=Connection
try{
 $policyFirst=PolicySql $true;$policySecond=PolicySql $false
 Two T13-policy-first-create $policyFirst "EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Policy first create',@Phone=NULL;" 0 ($invariant+"IF (SELECT COUNT(*) FROM dbo.KHACHHANG WHERE TenKH=N'Policy first create' AND DiemTichLuy=0)<>1 THROW 52999,'Create wrong',1;")
 Two T13-create-first-policy "BEGIN TRAN;EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Create first policy',@Phone=NULL;" $policySecond 0 $invariant
 foreach($direction in @('policy-first','open-first')){
  $name="Policy race opening $direction";Run-TestSql $c ((Prefix)+"EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'$name',@Phone=NULL;")
  $kh=Read-TestScalar $c "SELECT MaKH FROM dbo.KHACHHANG WHERE TenKH=N'$name'";$r=Read-TestScalar $c "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.KHACHHANG WHERE MaKH='$kh'"
  $open="DECLARE @r binary(8)=$r;EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='$kh',@ExpectedRevision=@r,@Points=10,@Source=N'Policy concurrency';"
  if($direction -eq 'policy-first'){Two T13-policy-first-open $policyFirst $open 0 $invariant}else{Two T13-open-first-policy ('BEGIN TRAN;'+$open) $policySecond 0 $invariant}
 }
 $adjust=@'
DECLARE @preview TABLE(Balance bigint,CustomerRevision binary(8),PolicyRevision binary(8),Tier char(5));
INSERT @preview EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Cong',1;
DECLARE @cr binary(8),@pr binary(8);SELECT @cr=CustomerRevision,@pr=PolicyRevision FROM @preview;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Cong',1,N'Policy race adjust',@cr,@pr,@id;
'@
 Two T13-policy-first-adjust $policyFirst $adjust 0 $invariant
 Two T13-adjust-first-policy ('BEGIN TRAN;'+$adjust) $policySecond 0 $invariant
 foreach($direction in @('policy-first','checkout-first')){
  $hd=if($direction -eq 'policy-first'){'HD09999101'}else{'HD09999102'}
  Run-TestSql $c "INSERT dbo.HOADON(MaHD,MaNV,MaKH,LoaiDon)VALUES('$hd','NV001','KH001',N'Mang di');INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan)VALUES('$hd','SP995',1,10000);"
  $checkout="DECLARE @tt dbo.DanhSachThanhToan;INSERT @tt(MaPT,SoTien)VALUES('PT001',10000);EXEC dbo.sp_ThanhToanHoaDon '$hd',@tt;"
  $assert=$invariant+"IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='$hd' AND TrangThai=N'Da thanh toan') OR (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$hd')<>1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='$hd')<>1 THROW 52999,'Checkout source mismatch',1;"
  if($direction -eq 'policy-first'){Two T13-policy-first-checkout $policyFirst $checkout 0 $assert}else{Two T13-checkout-first-policy ('BEGIN TRAN;'+$checkout) $policySecond 0 $assert}
 }
}finally{$c.Dispose()}
