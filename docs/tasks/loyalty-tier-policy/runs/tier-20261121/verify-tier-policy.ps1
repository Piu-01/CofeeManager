param(
 [string]$Root='D:\Workspace\Nam_3\HK1\CoffeeManager',
 [string]$OldSeed='C:\Users\Lenovo\AppData\Local\Temp\insert-data-head.sql'
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$script:fail=0
$N=[System.DBNull]::Value
function Say([string]$s){ Write-Output $s }
function Check([string]$code,[bool]$ok,[string]$detail){
 if($ok){ Say "PASS|$code|$detail" } else { $script:fail++; Say "FAIL|$code|$detail" }
}
function Conn([string]$db){
 $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
 $b['Data Source']='localhost'; $b['Initial Catalog']=$db; $b['Integrated Security']=$true
 $b['Encrypt']=$true; $b['TrustServerCertificate']=$true; $b['Connect Timeout']=15
 $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString); $c.Open(); return $c
}
function RunSql($c,[string]$sql){
 foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')){
  if([string]::IsNullOrWhiteSpace($batch)){continue}
  $cmd=$c.CreateCommand(); $cmd.CommandText=$batch; $cmd.CommandTimeout=180
  try{[void]$cmd.ExecuteNonQuery()} finally{$cmd.Dispose()}
 }
}
function Scalar($c,[string]$sql){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 try{return $cmd.ExecuteScalar()} finally{$cmd.Dispose()}
}
function Rows($c,[string]$sql){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 $r=$cmd.ExecuteReader(); $list=[System.Collections.ArrayList]::new()
 try{
  while($r.Read()){ $o=[ordered]@{}; for($i=0;$i -lt $r.FieldCount;$i++){ $v=$r.GetValue($i); if($v -is [System.DBNull]){$v=$null}; $o[$r.GetName($i)]=$v }
   [void]$list.Add([pscustomobject]$o) }
 } finally { $r.Close(); $cmd.Dispose() }
 return $list.ToArray()
}
function P([string]$name,$type,$value){
 $p=[System.Data.SqlClient.SqlParameter]::new($name,$type)
 if($null -eq $value){ $p.Value=[System.DBNull]::Value } else { $p.Value=$value }
 return $p
}
function PDec([string]$name,[decimal]$value){
 $p=[System.Data.SqlClient.SqlParameter]::new($name,[System.Data.SqlDbType]::Decimal)
 $p.Precision=38; $p.Scale=18; $p.Value=$value; return $p
}
function One($c,[string]$sql,[System.Data.SqlClient.SqlParameter[]]$ps){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 if($ps){ foreach($p in $ps){ [void]$cmd.Parameters.Add($p) } }
 $r=$cmd.ExecuteReader()
 try{ if(-not $r.Read()){ return $null }; $o=[ordered]@{}; for($i=0;$i -lt $r.FieldCount;$i++){ $v=$r.GetValue($i); if($v -is [System.DBNull]){$v=$null}; $o[$r.GetName($i)]=$v }; return [pscustomobject]$o }
 finally{ $r.Close(); $cmd.Dispose() }
}
function Many($c,[string]$sql,[System.Data.SqlClient.SqlParameter[]]$ps){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 if($ps){ foreach($p in $ps){ [void]$cmd.Parameters.Add($p) } }
 $r=$cmd.ExecuteReader(); $list=[System.Collections.ArrayList]::new()
 try{ while($r.Read()){ $o=[ordered]@{}; for($i=0;$i -lt $r.FieldCount;$i++){ $v=$r.GetValue($i); if($v -is [System.DBNull]){$v=$null}; $o[$r.GetName($i)]=$v }; [void]$list.Add([pscustomobject]$o) } }
 finally{ $r.Close(); $cmd.Dispose() }
 return $list.ToArray()
}
function ApplyMigrations($db,[int]$from,[int]$to){
 for($v=$from;$v -le $to;$v++){
  $file=Get-ChildItem (Join-Path $Root 'database\migrations') -Filter ('{0:D3}_*.sql' -f $v)
  if($null -eq $file){ throw "missing migration $v" }
  $out=& (Join-Path $Root 'tools\database\Apply-Migration.ps1') -Database $db -Version $v -File $file.FullName
  $status=if($out -is [array]){$out[-1].Status}else{$out.Status}
  Say "MIGRATION|$v|$status"
  if($status -notin @('Applied','AlreadyApplied')){ throw "migration $v status $status" }
 }
}
function LoadSchemaAndSeed($c,[string]$seedPath){
 $schema=[IO.File]::ReadAllText((Join-Path $Root 'database\DB_Schema.sql'),[Text.Encoding]::UTF8)
 $schema=[regex]::Replace($schema,'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
 RunSql $c $schema
 $seed=[IO.File]::ReadAllText($seedPath,[Text.Encoding]::UTF8)
 $seed=[regex]::Replace($seed,'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
 RunSql $c $seed
}
function EnableAdmin($c){
 [void](Scalar $c "UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE TenDangNhap='admin'; SELECT 1")
 $row=One $c "SELECT t.MaTK Actor,t.SecurityStamp Stamp FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.TenDangNhap='admin' AND t.HoatDong=1 AND n.DangLamViec=1" $null
 if($null -eq $row){ throw 'admin not usable' }
 return $row
}
function TierList($c){ (Rows $c "SELECT RTRIM(MaHang) MaHang,DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN ORDER BY DiemToiThieu" | ForEach-Object { "$($_.MaHang)|$($_.DiemToiThieu)|$($_.PhanTramGiam)" }) -join ';' }
function TierMismatch($c){ [int](Scalar $c "SELECT COUNT(*) FROM dbo.KHACHHANG k WHERE k.MaHang<>(SELECT TOP(1) h.MaHang FROM dbo.HANGTHANHVIEN h WHERE h.DiemToiThieu<=k.DiemTichLuy ORDER BY h.DiemToiThieu DESC)") }
function CustomerMap($c){ (Rows $c "SELECT MaKH,CONVERT(varchar(10),DiemTichLuy) Points,RTRIM(MaHang) Tier FROM dbo.KHACHHANG ORDER BY MaKH" | ForEach-Object { "$($_.MaKH)|$($_.Points)|$($_.Tier)" }) -join ';' }
function PromoSnapshot($c){ (Rows $c "SELECT RTRIM(MaKM) Code,CONVERT(varchar(10),NgayBD,23) StartDate,CONVERT(varchar(10),NgayKT,23) EndDate,PhanTramGiam Discount,CONVERT(bigint,Revision) Revision FROM dbo.KHUYENMAI ORDER BY MaKM" | ForEach-Object { "$($_.Code)|$($_.StartDate)|$($_.EndDate)|$($_.Discount)|$($_.Revision)" }) -join ';' }
function PromoList($c,$actor,$stamp){
 $ps=@((P '@Actor' ([System.Data.SqlDbType]::Int) $actor),(P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp))
 $rows=Many $c 'EXEC dbo.sp_PromotionList @Actor=@Actor,@Stamp=@Stamp' $ps
 return (($rows | ForEach-Object { "$($_.Code)|$($_.StartDate)|$($_.EndDate)|$($_.Discount)|$(if($null -ne $_.Revision){[System.BitConverter]::ToInt64([byte[]]$_.Revision,0)}else{-1})" }) -join ';')
}
function Preview($c,$actor,$stamp,[decimal]$money,[decimal]$ship,[string]$date,[string]$cust,[string]$promo){
 $ps=@(
  (P '@Actor' ([System.Data.SqlDbType]::Int) $actor),
  (P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),
  (PDec '@Money' $money),(PDec '@Ship' $ship),
  (P '@BusinessDate' ([System.Data.SqlDbType]::Date) ([datetime]$date)),
  (P '@Customer' ([System.Data.SqlDbType]::VarChar) $(if([string]::IsNullOrEmpty($cust)){$N}else{$cust})),
  (P '@Promotion' ([System.Data.SqlDbType]::VarChar) $(if([string]::IsNullOrEmpty($promo)){$N}else{$promo}))
 )
 return One $c 'EXEC dbo.sp_DiscountPreview @Actor=@Actor,@Stamp=@Stamp,@Money=@Money,@Ship=@Ship,@BusinessDate=@BusinessDate,@Customer=@Customer,@Promotion=@Promotion' $ps
}
function CreateOrder($c,$actor,$stamp,[string]$code,[string]$channel,[string]$cust,[string]$promo,[string]$product,[int]$qty,[decimal]$ship){
 $ps=@(
  (P '@Actor' ([System.Data.SqlDbType]::Int) $actor),
  (P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),
  (P '@Code' ([System.Data.SqlDbType]::VarChar) $code),
  (P '@RequestId' ([System.Data.SqlDbType]::UniqueIdentifier) ([guid]::NewGuid())),
  (P '@Channel' ([System.Data.SqlDbType]::NVarChar) $channel),
  (P '@TableCode' ([System.Data.SqlDbType]::VarChar) $N),
  (P '@Customer' ([System.Data.SqlDbType]::VarChar) $(if([string]::IsNullOrEmpty($cust)){$N}else{$cust})),
  (P '@Promotion' ([System.Data.SqlDbType]::VarChar) $(if([string]::IsNullOrEmpty($promo)){$N}else{$promo})),
  (P '@Partner' ([System.Data.SqlDbType]::VarChar) $N),
  (P '@Recipient' ([System.Data.SqlDbType]::NVarChar) $N),
  (P '@Phone' ([System.Data.SqlDbType]::VarChar) $N),
  (P '@Address' ([System.Data.SqlDbType]::NVarChar) $N),
  (PDec '@Ship' $ship),
  (P '@Product' ([System.Data.SqlDbType]::VarChar) $product),
  (P '@Quantity' ([System.Data.SqlDbType]::BigInt) ([long]$qty)),
  (P '@Note' ([System.Data.SqlDbType]::NVarChar) $N)
 )
 return One $c 'EXEC dbo.sp_TaoHoaDon @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@RequestId=@RequestId,@Channel=@Channel,@TableCode=@TableCode,@Customer=@Customer,@Promotion=@Promotion,@Partner=@Partner,@Recipient=@Recipient,@Phone=@Phone,@Address=@Address,@Ship=@Ship,@Product=@Product,@Quantity=@Quantity,@Note=@Note' $ps
}
function CheckoutPreview($c,$actor,$stamp,[string]$code){
 $rev=[byte[]](Scalar $c "SELECT Revision FROM dbo.HOADON WHERE MaHD='$code'")
 $ps=@((P '@Actor' ([System.Data.SqlDbType]::Int) $actor),(P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),(P '@Code' ([System.Data.SqlDbType]::VarChar) $code),(P '@ExpectedRevision' ([System.Data.SqlDbType]::Binary) $rev))
 return One $c 'EXEC dbo.sp_SalesCheckoutPreview @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@ExpectedRevision=@ExpectedRevision' $ps
}
function PayOrder($c,$actor,$stamp,[string]$code,[byte[]]$revision,[byte[]]$token,[decimal]$amount,[string]$method){
 $dt=[System.Data.DataTable]::new()
 [void]$dt.Columns.Add('MaPT',[string]); [void]$dt.Columns.Add('SoTien',[decimal]); [void]$dt.Columns.Add('MaThamChieu',[string])
 $r=$dt.NewRow(); $r['MaPT']=$method; $r['SoTien']=$amount; $r['MaThamChieu']=$N; $dt.Rows.Add($r)
 $pay=[System.Data.SqlClient.SqlParameter]::new('@ThanhToan',[System.Data.SqlDbType]::Structured)
 $pay.TypeName='dbo.DanhSachThanhToan'; $pay.Value=$dt
 $ps=@(
  (P '@Actor' ([System.Data.SqlDbType]::Int) $actor),
  (P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),
  (P '@Code' ([System.Data.SqlDbType]::VarChar) $code),
  (P '@ExpectedRevision' ([System.Data.SqlDbType]::Binary) $revision),
  (P '@RequestId' ([System.Data.SqlDbType]::UniqueIdentifier) ([guid]::NewGuid())),
  (P '@ExpectedPreviewToken' ([System.Data.SqlDbType]::Binary) $token),
  $pay
 )
 return One $c 'EXEC dbo.sp_ThanhToanHoaDon @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@ExpectedRevision=@ExpectedRevision,@RequestId=@RequestId,@ExpectedPreviewToken=@ExpectedPreviewToken,@ThanhToan=@ThanhToan' $ps
}
function CustomerState($c,[string]$code){ One $c "SELECT CONVERT(bigint,DiemTichLuy) Points,RTRIM(MaHang) Tier,CONVERT(bigint,Revision) Rev FROM dbo.KHACHHANG WHERE MaKH='$code'" $null }
function InvoiceState($c,[string]$code){ One $c "SELECT GiamGia,TrangThai FROM dbo.HOADON WHERE MaHD='$code'" $null }
function SnapshotText($c,[string]$code){ (Rows $c "SELECT Kind,Source,ISNULL(RTRIM(TierCode),'') TierCode,ISNULL(RTRIM(PromotionCode),'') Promo,PercentDiscount,Discount FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD='$code' " | ForEach-Object { "$($_.Kind)|$($_.Source)|$($_.TierCode)|$($_.Promo)|$($_.PercentDiscount)|$($_.Discount)" }) -join ';' }

$runStamp=Get-Date -Format 'yyyyMMdd_HHmmss'
$fresh="CoffeeManager_Test_TierPolicy_${runStamp}_fresh"
$upg="CoffeeManager_Test_TierPolicy_${runStamp}_upgrade"
$today=(Get-Date).ToString('yyyy-MM-dd')
$master=Conn 'master'
try{
 # ---------------- S1: fresh install with new seed + 001..037 ----------------
 Say "DB|$fresh"
 RunSql $master "CREATE DATABASE [$fresh];"
 $c=Conn $fresh
 try{
  LoadSchemaAndSeed $c (Join-Path $Root 'database\Insert_Data.sql'); Say 'SEED_OK|fresh'
  $c.Dispose()
  ApplyMigrations $fresh 1 37
  $c=Conn $fresh
  $admin=EnableAdmin $c
  Check 'TP-01' ((TierList $c) -eq 'DONG|0|0;BAC|2000|3;VANG|10000|5;KIM|30000|8') ('tiers=' + (TierList $c))
  Check 'TP-02' ((TierMismatch $c) -eq 0) ('tier mismatch=' + (TierMismatch $c))
  $expectedMap='KH001|3433|BAC;KH002|916|DONG;KH003|6910|BAC;KH004|215|DONG;KH005|1800|DONG;KH006|9200|BAC;KH007|350|DONG;KH008|50|DONG;KH009|14500|VANG;KH010|2500|BAC'
  Check 'TP-03' ((CustomerMap $c) -eq $expectedMap) ('customers=' + (CustomerMap $c))
  $inv=InvoiceState $c 'HD20250401'
  Check 'TP-04' (($inv.GiamGia -eq 12500) -and ($inv.TrangThai -eq 'Da thanh toan')) ('HD20250401=' + $inv.GiamGia + '/' + $inv.TrangThai)
  $p1=Preview $c $admin.Actor $admin.Stamp 250000 0 $today 'KH001' $null
  Check 'TP-05' (($p1.Source -eq 'Tier') -and ($p1.Tier.Trim() -eq 'BAC') -and ($p1.PercentDiscount -eq 3) -and ($p1.Discount -eq 7500) -and ($p1.Payable -eq 242500)) ('preview BAC 250000 -> ' + $p1.Source + '/' + $p1.Tier + '/' + $p1.PercentDiscount + '%/' + $p1.Discount + '/' + $p1.Payable)
  $promoBefore=PromoSnapshot $c
  $plBefore=PromoList $c $admin.Actor $admin.Stamp
  [void](Scalar $c "INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('KM20261121',N'Tier policy test 10',N'2026-01-01',N'2026-12-31',10),(N'KM20261122',N'Tier policy test 4',N'2026-01-01',N'2026-12-31',4); SELECT 1")
  $p2=Preview $c $admin.Actor $admin.Stamp 250000 0 $today 'KH001' 'KM20261121'
  Check 'TP-06' (($p2.Source -eq 'Promotion') -and ($p2.PercentDiscount -eq 10) -and ($p2.Discount -eq 25000) -and ($p2.Payable -eq 225000) -and ($p2.Tier.Trim() -eq 'BAC')) ('promo10 KH001 -> ' + $p2.Source + '/' + $p2.PercentDiscount + '%/' + $p2.Discount + '/' + $p2.Payable)
  $p3=Preview $c $admin.Actor $admin.Stamp 250000 0 $today 'KH009' 'KM20261122'
  Check 'TP-07' (($p3.Source -eq 'Promotion') -and ($p3.PercentDiscount -eq 4) -and ($p3.Discount -eq 10000) -and ($p3.Payable -eq 240000) -and ($p3.Tier.Trim() -eq 'VANG')) ('promo4 KH009(VANG5) -> ' + $p3.Source + '/' + $p3.PercentDiscount + '%/' + $p3.Discount + '/' + $p3.Payable)
  $promoAfterPreview=PromoSnapshot $c
  $seedOnlyAfter=(($promoAfterPreview.Split(';') | Where-Object { $_ -notlike 'KM2026*' }) -join ';')
  Check 'TP-08' ($seedOnlyAfter -eq $promoBefore) ('seed promotion rows stable after previews; fixture rows=' + (($promoAfterPreview.Split(';') | Where-Object { $_ -like 'KM2026*' }) -join ','))
  $plAfterInsert=PromoList $c $admin.Actor $admin.Stamp
  Check 'TP-09' (($plAfterInsert -ne $plBefore) -and ($plAfterInsert.Split(';').Count -eq ($plBefore.Split(';').Count + 2))) ('promotion list gained exactly fixture rows')

  # Checkout A: KH001 tier 3%
  [void](CreateOrder $c $admin.Actor $admin.Stamp 'HD20261121' 'Mang di' 'KH001' $null 'SP008' 5 0)
  $ca=CheckoutPreview $c $admin.Actor $admin.Stamp 'HD20261121'
  Check 'TP-10' (($ca.Source -eq 'Tier') -and ($ca.PercentDiscount -eq 3) -and ($ca.Discount -eq 7500) -and ($ca.Payable -eq 242500) -and ($ca.Points -eq 242)) ('checkout A preview=' + $ca.Source + '/' + $ca.Discount + '/' + $ca.Payable + '/pts' + $ca.Points)
  [void](PayOrder $c $admin.Actor $admin.Stamp 'HD20261121' $ca.Revision $ca.PreviewToken ([decimal]$ca.Payable) 'PT001')
  $k1=CustomerState $c 'KH001'; $ia=InvoiceState $c 'HD20261121'
  Check 'TP-11' (($ia.TrangThai -eq 'Da thanh toan') -and ($ia.GiamGia -eq 7500) -and ($k1.Points -eq 3675) -and ($k1.Tier -eq 'BAC')) ('A paid giam=' + $ia.GiamGia + ' KH001=' + $k1.Points + '/' + $k1.Tier)
  Check 'TP-12' ((SnapshotText $c 'HD20261121') -eq 'Checkout|Tier|BAC||3|7500') ('snapshot A=' + (SnapshotText $c 'HD20261121'))

  # Checkout B: KH005 DONG 0% then upgrade to BAC via points
  [void](CreateOrder $c $admin.Actor $admin.Stamp 'HD20261122' 'Mang di' 'KH005' $null 'SP008' 5 0)
  $cb=CheckoutPreview $c $admin.Actor $admin.Stamp 'HD20261122'
  Check 'TP-13' (($cb.Source -eq 'Tier') -and ($cb.PercentDiscount -eq 0) -and ($cb.Discount -eq 0) -and ($cb.Payable -eq 250000) -and ($cb.Points -eq 250)) ('checkout B preview=' + $cb.Source + '/' + $cb.Discount + '/' + $cb.Payable + '/pts' + $cb.Points)
  [void](PayOrder $c $admin.Actor $admin.Stamp 'HD20261122' $cb.Revision $cb.PreviewToken ([decimal]$cb.Payable) 'PT001')
  $k5=CustomerState $c 'KH005'
  Check 'TP-14' (($k5.Points -eq 2050) -and ($k5.Tier -eq 'BAC')) ('KH005 after B=' + $k5.Points + '/' + $k5.Tier)

  # Checkout C: KH001 with 10% promotion replaces tier 3%
  [void](CreateOrder $c $admin.Actor $admin.Stamp 'HD20261123' 'Mang di' 'KH001' 'KM20261121' 'SP008' 5 0)
  $cc=CheckoutPreview $c $admin.Actor $admin.Stamp 'HD20261123'
  Check 'TP-15' (($cc.Source -eq 'Promotion') -and ($cc.PercentDiscount -eq 10) -and ($cc.Discount -eq 25000) -and ($cc.Payable -eq 225000) -and ($cc.Points -eq 225)) ('checkout C preview=' + $cc.Source + '/' + $cc.PercentDiscount + '/pts' + $cc.Points)
  [void](PayOrder $c $admin.Actor $admin.Stamp 'HD20261123' $cc.Revision $cc.PreviewToken ([decimal]$cc.Payable) 'PT001')
  $kc=CustomerState $c 'KH001'
  Check 'TP-16' (($cc.Payable -eq 225000) -and ($kc.Points -eq 3900) -and ($kc.Tier -eq 'BAC')) ('C paid KH001=' + $kc.Points + '/' + $kc.Tier)
  Check 'TP-17' ((SnapshotText $c 'HD20261123') -eq 'Checkout|Promotion|BAC|KM20261121|10|25000') ('snapshot C=' + (SnapshotText $c 'HD20261123'))
  Check 'TP-18' ((PromoList $c $admin.Actor $admin.Stamp) -eq $plAfterInsert) ('promotion list unchanged by checkouts')
  $inv=InvoiceState $c 'HD20250401'
  Check 'TP-19' (($inv.GiamGia -eq 12500) -and ((Scalar $c "SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD IN('HD20261121','HD20261122','HD20261123')") -eq 3)) ('legacy invoice/points history ok')
 } finally { try{$c.Dispose()}catch{} }

 # ---------------- S2: upgrade simulation with HEAD seed (10 tiers) + 001..036 then 037 ----------------
 Say "DB|$upg"
 RunSql $master "CREATE DATABASE [$upg];"
 $c=Conn $upg
 try{
  LoadSchemaAndSeed $c $OldSeed; Say 'SEED_OK|upgrade'
  $c.Dispose()
  ApplyMigrations $upg 1 36
  $c=Conn $upg
  $admin=EnableAdmin $c
  $tiersPre=TierList $c; $custPre=CustomerMap $c; $promoPre=PromoSnapshot $c; $plPre=PromoList $c $admin.Actor $admin.Stamp
  $snapPre=SnapshotText $c 'HD20250401'; $invPre=InvoiceState $c 'HD20250401'
  $revPre=Scalar $c 'SELECT CONVERT(bigint,Revision) FROM dbo.LoyaltyPolicyState WHERE Id=1'
  $pPre=Preview $c $admin.Actor $admin.Stamp 250000 0 $today 'KH001' $null
  Check 'TP-20' (($pPre.Source -eq 'Tier') -and ($pPre.Tier.Trim() -eq 'BKIM') -and ($pPre.PercentDiscount -eq 10) -and ($pPre.Discount -eq 25000) -and ($pPre.Payable -eq 225000)) ('pre-037 preview=' + $pPre.Tier + '/' + $pPre.PercentDiscount + '%/' + $pPre.Discount)
  Check 'TP-21' (($tiersPre.Split(';').Count -eq 10) -and ($tiersPre -like 'DONG|0|0;*')) ('pre-037 tiers=' + $tiersPre)
  $c.Dispose()
  ApplyMigrations $upg 37 37
  $c=Conn $upg
  $admin=EnableAdmin $c
  $tiersPost=TierList $c
  Check 'TP-22' ($tiersPost -eq 'DONG|0|0;BAC|2000|3;VANG|10000|5;KIM|30000|8') ('post-037 tiers=' + $tiersPost)
  Check 'TP-23' ((TierMismatch $c) -eq 0) ('tier mismatch=' + (TierMismatch $c))
  $pointsPre=@{}; foreach($x in $custPre.Split(';')){ $f=$x.Split('|'); $pointsPre[$f[0]]=$f[1] }
  $pointsChanged=0; $tierChanged=0
  foreach($x in (CustomerMap $c).Split(';')){ $f=$x.Split('|'); if($pointsPre[$f[0]] -ne $f[1]){$pointsChanged++}; $old=($custPre.Split(';') | Where-Object { $_.StartsWith($f[0]+'|') }).Split('|')[2]; if($old -ne $f[2]){$tierChanged++} }
  Check 'TP-24' (($pointsChanged -eq 0) -and ($tierChanged -gt 0)) ('points changed=' + $pointsChanged + ' tiers changed=' + $tierChanged)
  Check 'TP-25' ((PromoSnapshot $c) -eq $promoPre) ('promotion rows unchanged by 037')
  Check 'TP-26' ((PromoList $c $admin.Actor $admin.Stamp) -eq $plPre) ('promotion list output unchanged by 037')
  $invPost=InvoiceState $c 'HD20250401'
  Check 'TP-27' (($invPost.GiamGia -eq $invPre.GiamGia) -and ($invPost.TrangThai -eq $invPre.TrangThai) -and ((SnapshotText $c 'HD20250401') -eq $snapPre)) ('legacy invoice/snapshot unchanged')
  $pPost=Preview $c $admin.Actor $admin.Stamp 250000 0 $today 'KH001' $null
  Check 'TP-28' (($pPost.Source -eq 'Tier') -and ($pPost.Tier.Trim() -eq 'BAC') -and ($pPost.PercentDiscount -eq 3) -and ($pPost.Discount -eq 7500) -and ($pPost.Payable -eq 242500)) ('post-037 preview=' + $pPost.Tier + '/' + $pPost.PercentDiscount + '%/' + $pPost.Discount + '/' + $pPost.Payable)
  $revPost=Scalar $c 'SELECT CONVERT(bigint,Revision) FROM dbo.LoyaltyPolicyState WHERE Id=1'
  Check 'TP-29' ($revPost -ne $revPre) ('policy revision ' + $revPre + ' -> ' + $revPost)
  $c.Dispose()
  ApplyMigrations $upg 37 37
  $c=Conn $upg
  Check 'TP-30' ((TierList $c) -eq $tiersPost) ('re-run 037 keeps tiers')
  Check 'TP-31' ((Scalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -eq 37) ('schema version 37')
 } finally { try{$c.Dispose()}catch{} }
} finally {
 foreach($db in @($fresh,$upg)){
  try{
   $master.ChangeDatabase('master')
   RunSql $master "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db];"
   Say "CLEANUP_OK|$db"
  } catch { Say ("CLEANUP_FAIL|$db|"+$_.Exception.Message) }
 }
 $master.Dispose()
}
Say ("SUMMARY|FAIL="+$script:fail)
if($script:fail -gt 0){ exit 1 } else { exit 0 }
