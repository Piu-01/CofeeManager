param(
 [string]$Db='CoffeeManager_Test_038_accept',
 [string]$ReferenceDb='CoffeeManager_Development'
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
function Scalar($c,[string]$sql){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 try{return $cmd.ExecuteScalar()} finally{$cmd.Dispose()}
}
function One($c,[string]$sql,[System.Data.SqlClient.SqlParameter[]]$ps){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 if($ps){ foreach($p in $ps){ [void]$cmd.Parameters.Add($p) } }
 $r=$cmd.ExecuteReader()
 try{ if(-not $r.Read()){ return $null }; $o=[ordered]@{}; for($i=0;$i -lt $r.FieldCount;$i++){ $v=$r.GetValue($i); if($v -is [System.DBNull]){$v=$null}; $o[$r.GetName($i)]=$v }; $o['__FieldCount']=$r.FieldCount; return [pscustomobject]$o }
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
function P([string]$name,$type,$value){
 $p=[System.Data.SqlClient.SqlParameter]::new($name,$type)
 if($null -eq $value){ $p.Value=[System.DBNull]::Value } else { $p.Value=$value }
 return $p
}
function PDec([string]$name,[decimal]$value){
 $p=[System.Data.SqlClient.SqlParameter]::new($name,[System.Data.SqlDbType]::Decimal)
 $p.Precision=38; $p.Scale=18; $p.Value=$value; return $p
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
function CreateOrder($c,$actor,$stamp,[string]$code,[string]$channel,[string]$cust,[string]$promo,[string]$product,[int]$qty,[decimal]$ship,[string]$recipient,[string]$phone,[string]$address){
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
  (P '@Recipient' ([System.Data.SqlDbType]::NVarChar) $(if([string]::IsNullOrEmpty($recipient)){$N}else{$recipient})),
  (P '@Phone' ([System.Data.SqlDbType]::VarChar) $(if([string]::IsNullOrEmpty($phone)){$N}else{$phone})),
  (P '@Address' ([System.Data.SqlDbType]::NVarChar) $(if([string]::IsNullOrEmpty($address)){$N}else{$address})),
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
function PayOrder($c,$actor,$stamp,[string]$code,[byte[]]$revision,[byte[]]$token,[decimal]$amount,[guid]$requestId){
 $dt=[System.Data.DataTable]::new()
 [void]$dt.Columns.Add('MaPT',[string]); [void]$dt.Columns.Add('SoTien',[decimal]); [void]$dt.Columns.Add('MaThamChieu',[string])
 $r=$dt.NewRow(); $r['MaPT']='PT001'; $r['SoTien']=$amount; $r['MaThamChieu']=$N; $dt.Rows.Add($r)
 $pay=[System.Data.SqlClient.SqlParameter]::new('@ThanhToan',[System.Data.SqlDbType]::Structured)
 $pay.TypeName='dbo.DanhSachThanhToan'; $pay.Value=$dt
 $ps=@(
  (P '@Actor' ([System.Data.SqlDbType]::Int) $actor),
  (P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),
  (P '@Code' ([System.Data.SqlDbType]::VarChar) $code),
  (P '@ExpectedRevision' ([System.Data.SqlDbType]::Binary) $revision),
  (P '@RequestId' ([System.Data.SqlDbType]::UniqueIdentifier) $requestId),
  (P '@ExpectedPreviewToken' ([System.Data.SqlDbType]::Binary) $token),
  $pay
 )
 return One $c 'EXEC dbo.sp_ThanhToanHoaDon @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@ExpectedRevision=@ExpectedRevision,@RequestId=@RequestId,@ExpectedPreviewToken=@ExpectedPreviewToken,@ThanhToan=@ThanhToan' $ps
}
function OrderRevision($c,[string]$code){ [byte[]](Scalar $c "SELECT Revision FROM dbo.HOADON WHERE MaHD='$code'") }
function CustomerState($c,[string]$code){ One $c "SELECT CONVERT(bigint,DiemTichLuy) Points,RTRIM(MaHang) Tier FROM dbo.KHACHHANG WHERE MaKH='$code'" $null }
function PointsHistoryCount($c,[string]$code){ [int](Scalar $c "SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='$code'") }
function PaymentCount($c,[string]$code){ [int](Scalar $c "SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$code'") }
function Stock($c){ (Many $c "SELECT MaNVL,CONVERT(decimal(18,4),SoLuongTon) SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL" $null | ForEach-Object { "$($_.MaNVL)=$([string]::Format([Globalization.CultureInfo]::InvariantCulture,'{0:0.0000}',$_.SoLuongTon))" }) -join ';' }
function SnapshotText($c,[string]$code){
 (Many $c "SELECT Kind,ISNULL(Source,'') Source,ISNULL(RTRIM(TierCode),'') TierCode,ISNULL(RTRIM(PromotionCode),'') Promo,PercentDiscount,TierPercent,PromotionPercent,Discount,CASE WHEN PolicyRevision IS NULL THEN 0 ELSE 1 END HasPolicy FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD='$code' ORDER BY Kind" $null |
  ForEach-Object { "$($_.Kind)|$($_.Source)|$($_.TierCode)|$($_.Promo)|$($_.PercentDiscount)|$($_.TierPercent)|$($_.PromotionPercent)|$($_.Discount)|$($_.HasPolicy)" }) -join ';'
}
function TxState($c){ [string](Scalar $c "SELECT CAST(@@TRANCOUNT AS varchar(10))+'/'+CAST(XACT_STATE() AS varchar(10))") }
function InvoiceDetailSets($c,$actor,$stamp,[string]$code){
 $ps=@((P '@Actor' ([System.Data.SqlDbType]::Int) $actor),(P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),(P '@Code' ([System.Data.SqlDbType]::VarChar) $code))
 $cmd=$c.CreateCommand(); $cmd.CommandText='EXEC dbo.sp_InvoiceDetail @Actor=@Actor,@Stamp=@Stamp,@Code=@Code'; $cmd.CommandTimeout=180
 foreach($p in $ps){ [void]$cmd.Parameters.Add($p) }
 $r=$cmd.ExecuteReader(); $sets=[System.Collections.ArrayList]::new()
 try{
  do{
   $rows=[System.Collections.ArrayList]::new()
   while($r.Read()){ $o=[ordered]@{}; for($i=0;$i -lt $r.FieldCount;$i++){ $v=$r.GetValue($i); if($v -is [System.DBNull]){$v=$null}; $o[$r.GetName($i)]=$v }; [void]$rows.Add([pscustomobject]$o) }
   [void]$sets.Add($rows.ToArray())
  } while($r.NextResult())
 } finally{ $r.Close(); $cmd.Dispose() }
 return $sets
}
function TrySql($c,[scriptblock]$action){
 try{ & $action; return @{Ok=$true} }
 catch{
  $ex=$_.Exception
  while($null -ne $ex.InnerException){ $ex=$ex.InnerException }
  return @{Ok=$false;Number=$ex.Number;Message=$ex.Message}
 }
}
 function Dump($c,[string]$sql){
 $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=180
 $r=$cmd.ExecuteReader(); $sb=[Text.StringBuilder]::new()
 try{
  $set=0
  while($true){
   $set++; $row=0; $sb.Append("SET${set}:")|Out-Null
   while($r.Read()){
    $row++
    for($i=0;$i -lt $r.FieldCount;$i++){
     if($r.GetName($i) -eq 'ReadAt'){ continue }
     $v=$r.GetValue($i)
     if($v -is [System.DBNull]){ $s='NULL' } elseif($v -is [decimal]){ $s=$v.ToString([Globalization.CultureInfo]::InvariantCulture) } else { $s=[string]$v }
     $sb.Append($r.GetName($i)+'='+$s+'|')|Out-Null
    }
   }
   $sb.Append(';')|Out-Null
   if(-not $r.NextResult()){ break }
  }
 } finally{ $r.Close(); $cmd.Dispose() }
 return $sb.ToString()
}
function PolicyReplaceSame($c,$actor,$stamp){
 $dt=[System.Data.DataTable]::new()
 [void]$dt.Columns.Add('Code',[string]); [void]$dt.Columns.Add('MinimumPoints',[long]); [void]$dt.Columns.Add('DiscountPercent',[long])
 foreach($t in (Many $c "SELECT RTRIM(MaHang) Code,DiemToiThieu MinimumPoints,PhanTramGiam DiscountPercent FROM dbo.HANGTHANHVIEN ORDER BY MaHang" $null)){ $dt.Rows.Add($t.Code,$t.MinimumPoints,$t.DiscountPercent)|Out-Null }
 $tvp=[System.Data.SqlClient.SqlParameter]::new('@Tiers',[System.Data.SqlDbType]::Structured); $tvp.TypeName='dbo.LoyaltyTierInput'; $tvp.Value=$dt
 $ps=@((P '@Actor' ([System.Data.SqlDbType]::Int) $actor),(P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),$tvp)
 $pv=One $c 'EXEC dbo.sp_LoyaltyPolicyPreview @Actor=@Actor,@Stamp=@Stamp,@Tiers=@Tiers' $ps
 $tvp2=[System.Data.SqlClient.SqlParameter]::new('@Tiers',[System.Data.SqlDbType]::Structured); $tvp2.TypeName='dbo.LoyaltyTierInput'; $tvp2.Value=$dt
 $ps2=@((P '@Actor' ([System.Data.SqlDbType]::Int) $actor),(P '@Stamp' ([System.Data.SqlDbType]::Binary) $stamp),$tvp2,
  (P '@ExpectedRevision' ([System.Data.SqlDbType]::Binary) ([byte[]]$pv.Revision)),
  (P '@PayloadHash' ([System.Data.SqlDbType]::Binary) ([byte[]]$pv.PayloadHash)),
  (P '@RequestId' ([System.Data.SqlDbType]::UniqueIdentifier) ([guid]::NewGuid())))
 return One $c 'EXEC dbo.sp_LoyaltyPolicyReplace @Actor=@Actor,@Stamp=@Stamp,@Tiers=@Tiers,@ExpectedRevision=@ExpectedRevision,@PayloadHash=@PayloadHash,@RequestId=@RequestId' $ps2
}
function TokenText($t){ if($null -eq $t){ return '' }; return [BitConverter]::ToString([byte[]]$t) }

$today=(Get-Date).ToString('yyyy-MM-dd')
$c=Conn $Db
try{
 $admin=One $c "SELECT t.MaTK Actor,t.SecurityStamp Stamp FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.TenDangNhap='admin' AND t.HoatDong=1 AND n.DangLamViec=1" $null
 if($null -eq $admin){ throw 'admin account not usable' }
 $actor=$admin.Actor; $stamp=[byte[]]$admin.Stamp
 Say "DB|$Db|actor=$actor"
 Say ("VERSION|" + (Scalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations') + "|" + (Scalar $c 'SELECT Checksum FROM dbo.SchemaMigrations WHERE Version=38'))
 $tiers=(Many $c "SELECT RTRIM(MaHang) MaHang,DiemToiThieu,PhanTramGiam FROM dbo.HANGTHANHVIEN ORDER BY DiemToiThieu" $null | ForEach-Object { "$($_.MaHang)|$($_.DiemToiThieu)|$($_.PhanTramGiam)" }) -join ';'
 Check 'ST-00' (($tiers -eq 'DONG|0|0;BAC|2000|3;VANG|10000|5;KIM|30000|8') -and ([int](Scalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -eq 38)) ('schema v38; tiers=' + $tiers)

 # ---------------- ST-01..ST-09: preview formula ----------------
 $p=Preview $c $actor $stamp 250000 0 $today 'KH001' 'KM26HRO'
 Check 'ST-01' (($p.Source -eq 'TierAndPromotion') -and ($p.Tier.Trim() -eq 'BAC') -and ($p.Promotion.Trim() -eq 'KM26HRO') -and ($p.PercentDiscount -eq 13) -and ($p.TierPercent -eq 3) -and ($p.PromotionPercent -eq 10) -and ($p.Discount -eq 32500) -and ($p.Payable -eq 217500)) ('KH001(BAC3)+KM26HRO(10) 250000 -> ' + $p.Source + '/' + $p.PercentDiscount + '%=' + $p.TierPercent + '+' + $p.PromotionPercent + '/' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 250000 0 $today 'KH001' $null
 Check 'ST-02' (($p.Source -eq 'Tier') -and ($p.PercentDiscount -eq 3) -and ($p.TierPercent -eq 3) -and ($p.PromotionPercent -eq 0) -and ($p.Discount -eq 7500) -and ($p.Payable -eq 242500)) ('tier only -> ' + $p.Source + '/' + $p.PercentDiscount + '/' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 250000 0 $today $null 'KM26HRO'
 Check 'ST-03' (($p.Source -eq 'Promotion') -and ($null -eq $p.Tier) -and ($p.PercentDiscount -eq 10) -and ($p.TierPercent -eq 0) -and ($p.PromotionPercent -eq 10) -and ($p.Discount -eq 25000) -and ($p.Payable -eq 225000)) ('promo only -> ' + $p.Source + '/' + $p.PercentDiscount + '/' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 250000 0 $today $null $null
 Check 'ST-04' (($p.Source -eq 'None') -and ($p.PercentDiscount -eq 0) -and ($p.TierPercent -eq 0) -and ($p.PromotionPercent -eq 0) -and ($p.Discount -eq 0) -and ($p.Payable -eq 250000)) ('none -> ' + $p.Source + '/' + $p.PercentDiscount + '/' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 250000 30000 $today 'KH001' 'KM26HRO'
 Check 'ST-05' (($p.PercentDiscount -eq 13) -and ($p.Discount -eq 32500) -and ($p.Payable -eq 247500)) ('ship excluded from discount -> discount=' + $p.Discount + ' payable=' + $p.Payable)
 $r=TrySql $c { Preview $c $actor $stamp 250000 0 $today 'KH001' 'KM24BF' }
 Check 'ST-06a' ((-not $r.Ok) -and ($r.Number -eq 52001)) ('expired KM24BF rejected by preview validation err=' + $r.Number)
 $fn=One $c "SELECT Source,TierPercent,PromotionPercent,PercentDiscount,Discount,Payable FROM dbo.fn_DiscountPreview(250000,0,'$today','KH001','KM26HRO')" $null
 Check 'ST-06b' (($fn.Source -eq 'TierAndPromotion') -and ($fn.TierPercent -eq 3) -and ($fn.PromotionPercent -eq 10) -and ($fn.PercentDiscount -eq 13) -and ($fn.Discount -eq 32500) -and ($fn.Payable -eq 217500)) ('fn breakdown=' + $fn.TierPercent + '+' + $fn.PromotionPercent + '=' + $fn.PercentDiscount + '/' + $fn.Discount)
 $ins=TrySql $c { [void](Scalar $c "IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KMSTK100') INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('KMSTK100',N'Stacking test 100','2026-01-01','2026-12-31',100); SELECT 1") }
 $p=Preview $c $actor $stamp 250000 0 $today 'KH018' 'KMSTK100'
 Check 'ST-07' (($p.Source -eq 'TierAndPromotion') -and ($p.Tier.Trim() -eq 'KIM') -and ($p.TierPercent -eq 8) -and ($p.PromotionPercent -eq 100) -and ($p.PercentDiscount -eq 100) -and ($p.Discount -eq 250000) -and ($p.Payable -eq 0)) ('KIM8+100% capped -> ' + $p.Source + '/' + $p.TierPercent + '+' + $p.PromotionPercent + '=' + $p.PercentDiscount + '/' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 33333 0 $today 'KH001' 'KM26HRO'
 Check 'ST-08a' (($p.Discount -eq 4333) -and ($p.Payable -eq 29000)) ('round once 33333x13% -> ' + $p.Discount + '/' + $p.Payable)
 $p=Preview $c $actor $stamp 33335 0 $today 'KH001' 'KM26HRO'
 Check 'ST-08b' (($p.Discount -eq 4334) -and ($p.Payable -eq 29001)) ('round once 33335x13% -> ' + $p.Discount + '/' + $p.Payable)
 $k1=CustomerState $c 'KH001'
 Check 'ST-09' (($k1.Points -eq 8281) -and ($k1.Tier -eq 'BAC')) ('preview has no side effects; KH001=' + $k1.Points + '/' + $k1.Tier)

 # ---------------- ST-10: two promotion codes blocked at SQL ----------------
 $r=TrySql $c { CreateOrder $c $actor $stamp 'HD26990001' 'Mang di' 'KH001' 'KM26HRO,KM26STU' 'SP008' 5 0 $null $null $null }
 Check 'ST-10' ((-not $r.Ok) -and ($r.Number -eq 52105) -and ([int](Scalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='HD26990001'") -eq 0)) ('two codes rejected err=' + $r.Number + ' rows=0')

 # ---------------- ST-11/12: checkout A stacked 13% + replay ----------------
 $stockBefore=Stock $c
 [void](CreateOrder $c $actor $stamp 'HD26990002' 'Mang di' 'KH001' 'KM26HRO' 'SP008' 5 0 $null $null $null)
 $a=CheckoutPreview $c $actor $stamp 'HD26990002'
 Check 'ST-11a' (($a.Source -eq 'TierAndPromotion') -and ($a.PercentDiscount -eq 13) -and ($a.TierPercent -eq 3) -and ($a.PromotionPercent -eq 10) -and ($a.Discount -eq 32500) -and ($a.Payable -eq 217500) -and ($a.Points -eq 217) -and ([byte[]]$a.PreviewToken).Length -eq 32 -and $a.__FieldCount -eq 14) ('preview A=' + $a.Source + '/' + $a.PercentDiscount + '%/' + $a.Discount + '/' + $a.Payable + '/pts' + $a.Points + '/token' + ([byte[]]$a.PreviewToken).Length + '/cols' + $a.__FieldCount)
 $revA=OrderRevision $c 'HD26990002'; $reqA=[guid]::NewGuid()
 [void](PayOrder $c $actor $stamp 'HD26990002' $revA ([byte[]]$a.PreviewToken) ([decimal]$a.Payable) $reqA)
 $inv=One $c "SELECT TrangThai,GiamGia,PhiShip,TongTien FROM dbo.HOADON WHERE MaHD='HD26990002'" $null
 $k1=CustomerState $c 'KH001'
 Check 'ST-11b' (($inv.TrangThai -eq 'Da thanh toan') -and ($inv.GiamGia -eq 32500) -and ($inv.PhiShip -eq 0) -and ($k1.Points -eq 8498) -and ($k1.Tier -eq 'BAC') -and ((PointsHistoryCount $c 'HD26990002') -eq 1) -and ((PaymentCount $c 'HD26990002') -eq 1)) ('paid A: giam=' + $inv.GiamGia + ' KH001=' + $k1.Points + '/' + $k1.Tier + ' hist=' + (PointsHistoryCount $c 'HD26990002') + ' pays=' + (PaymentCount $c 'HD26990002'))
 Check 'ST-11c' ((SnapshotText $c 'HD26990002') -eq 'Checkout|TierAndPromotion|BAC|KM26HRO|13|3|10|32500|1') ('snapshot A=' + (SnapshotText $c 'HD26990002'))
 $stockAfter=Stock $c
 Check 'ST-11d' (($stockAfter -ne $stockBefore) -and ((TxState $c) -eq '0/0') -and ([int](Scalar $c "SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD26990002'") -gt 0) -and ([int](Scalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='HD26990002'") -eq 1)) ('stock consumed; single invoice row; tx=' + (TxState $c))
 [void](PayOrder $c $actor $stamp 'HD26990002' $revA ([byte[]]$a.PreviewToken) ([decimal]$a.Payable) $reqA)
 $k1=CustomerState $c 'KH001'
 Check 'ST-12' (($k1.Points -eq 8498) -and ((PaymentCount $c 'HD26990002') -eq 1) -and ((PointsHistoryCount $c 'HD26990002') -eq 1) -and ((Stock $c) -eq $stockAfter)) ('replay request id: no duplicate points/payment/stock')

 # ---------------- ST-13: delivery order, ship not discounted / not in points ----------------
 [void](CreateOrder $c $actor $stamp 'HD26990003' 'Giao hang' 'KH001' 'KM26HRO' 'SP008' 5 30000 'Khach Test' '0901234567' 'So 1 Duong Test')
 $b=CheckoutPreview $c $actor $stamp 'HD26990003'
 Check 'ST-13a' (($b.Source -eq 'TierAndPromotion') -and ($b.Discount -eq 32500) -and ($b.Payable -eq 247500) -and ($b.Points -eq 217)) ('delivery preview=' + $b.Source + '/' + $b.Discount + '/' + $b.Payable + '/pts' + $b.Points)
 [void](PayOrder $c $actor $stamp 'HD26990003' (OrderRevision $c 'HD26990003') ([byte[]]$b.PreviewToken) ([decimal]$b.Payable) ([guid]::NewGuid()))
 $inv=One $c "SELECT TrangThai,GiamGia,PhiShip FROM dbo.HOADON WHERE MaHD='HD26990003'" $null
 $k1=CustomerState $c 'KH001'
 Check 'ST-13b' (($inv.TrangThai -eq 'Da thanh toan') -and ($inv.GiamGia -eq 32500) -and ($inv.PhiShip -eq 30000) -and ($k1.Points -eq 8715) -and ((SnapshotText $c 'HD26990003') -eq 'Checkout|TierAndPromotion|BAC|KM26HRO|13|3|10|32500|1')) ('paid B: giam=' + $inv.GiamGia + ' ship=' + $inv.PhiShip + ' KH001=' + $k1.Points + ' snapshot=' + (SnapshotText $c 'HD26990003'))

 # ---------------- ST-14: promotion edited after preview invalidates token ----------------
 [void](Scalar $c "IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KMSTK10') INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES('KMSTK10',N'Stacking test 10','2026-01-01','2026-12-31',10); UPDATE dbo.KHUYENMAI SET PhanTramGiam=10 WHERE MaKM='KMSTK10'; SELECT 1")
 [void](CreateOrder $c $actor $stamp 'HD26990004' 'Mang di' 'KH001' 'KMSTK10' 'SP008' 5 0 $null $null $null)
 $c1=CheckoutPreview $c $actor $stamp 'HD26990004'
 [void](Scalar $c "UPDATE dbo.KHUYENMAI SET PhanTramGiam=11 WHERE MaKM='KMSTK10'; SELECT 1")
 $c2=CheckoutPreview $c $actor $stamp 'HD26990004'
 Check 'ST-14a' (((TokenText $c1.PreviewToken) -ne (TokenText $c2.PreviewToken)) -and ($c1.PercentDiscount -eq 13) -and ($c2.PercentDiscount -eq 14) -and ($c2.Discount -eq 35000) -and ($c2.Payable -eq 215000)) ('promo 10->11%: 13%->' + $c2.PercentDiscount + '%; token changed')
 $r=TrySql $c { PayOrder $c $actor $stamp 'HD26990004' (OrderRevision $c 'HD26990004') ([byte[]]$c1.PreviewToken) ([decimal]$c1.Payable) ([guid]::NewGuid()) }
 Check 'ST-14b' ((-not $r.Ok) -and ($r.Number -eq 52201) -and ((PaymentCount $c 'HD26990004') -eq 0) -and ((PointsHistoryCount $c 'HD26990004') -eq 0) -and ([int](Scalar $c "SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD26990004'") -eq 0)) ('stale token rejected err=' + $r.Number + ' no leaked rows')
 [void](PayOrder $c $actor $stamp 'HD26990004' (OrderRevision $c 'HD26990004') ([byte[]]$c2.PreviewToken) ([decimal]$c2.Payable) ([guid]::NewGuid()))
 Check 'ST-14c' (((SnapshotText $c 'HD26990004') -eq 'Checkout|TierAndPromotion|BAC|KMSTK10|14|3|11|35000|1') -and ((One $c "SELECT GiamGia FROM dbo.HOADON WHERE MaHD='HD26990004'" $null).GiamGia -eq 35000)) ('paid with new token: snapshot=' + (SnapshotText $c 'HD26990004'))

 # ---------------- ST-14d: promotion expired after order creation ----------------
 [void](CreateOrder $c $actor $stamp 'HD26990007' 'Mang di' 'KH001' 'KMSTK10' 'SP008' 5 0 $null $null $null)
 [void](Scalar $c "UPDATE dbo.KHUYENMAI SET NgayKT='2026-11-20' WHERE MaKM='KMSTK10'; SELECT 1")
 $r=TrySql $c { CheckoutPreview $c $actor $stamp 'HD26990007' }
 Check 'ST-14d' ((-not $r.Ok) -and ($r.Number -eq 51105) -and ((PaymentCount $c 'HD26990007') -eq 0)) ('expired promo blocks checkout preview err=' + $r.Number)

 # ---------------- ST-15: tier policy revision invalidates token ----------------
 [void](CreateOrder $c $actor $stamp 'HD26990005' 'Mang di' 'KH001' $null 'SP008' 5 0 $null $null $null)
 $d1=CheckoutPreview $c $actor $stamp 'HD26990005'
 $pol=PolicyReplaceSame $c $actor $stamp
 $d2=CheckoutPreview $c $actor $stamp 'HD26990005'
 Check 'ST-15a' (((TokenText $d1.PreviewToken) -ne (TokenText $d2.PreviewToken)) -and ($d2.PercentDiscount -eq 3) -and ($d2.Discount -eq 7500)) ('policy revision change: token changed; amounts stay 3%/7500')
 $r=TrySql $c { PayOrder $c $actor $stamp 'HD26990005' (OrderRevision $c 'HD26990005') ([byte[]]$d1.PreviewToken) ([decimal]$d1.Payable) ([guid]::NewGuid()) }
 Check 'ST-15b' ((-not $r.Ok) -and ($r.Number -eq 52201)) ('stale policy token rejected err=' + $r.Number)
 [void](PayOrder $c $actor $stamp 'HD26990005' (OrderRevision $c 'HD26990005') ([byte[]]$d2.PreviewToken) ([decimal]$d2.Payable) ([guid]::NewGuid()))
 Check 'ST-15c' ((SnapshotText $c 'HD26990005') -eq 'Checkout|Tier|BAC||3|3|0|7500|1') ('paid D: snapshot=' + (SnapshotText $c 'HD26990005'))

 # ---------------- ST-16: invoice detail returns breakdown; legacy stays NULL ----------------
 $sets=InvoiceDetailSets $c $actor $stamp 'HD26990002'
 $s4=$sets[3][0]
 Check 'ST-16a' (($s4.Kind -eq 'Checkout') -and ($s4.Source -eq 'TierAndPromotion') -and ($s4.PercentDiscount -eq 13) -and ($s4.TierPercent -eq 3) -and ($s4.PromotionPercent -eq 10) -and ($s4.Points -eq 217)) ('detail A set4=' + $s4.Kind + '/' + $s4.Source + '/' + $s4.PercentDiscount + '=' + $s4.TierPercent + '+' + $s4.PromotionPercent + '/pts' + $s4.Points)
 $sets=InvoiceDetailSets $c $actor $stamp 'HD20250401'
 $s4=$sets[3][0]
 Check 'ST-16b' (($s4.Kind -eq 'LegacyUnknown') -and ($null -eq $s4.Source) -and ($null -eq $s4.PercentDiscount) -and ($null -eq $s4.TierPercent) -and ($null -eq $s4.PromotionPercent)) ('legacy detail=' + $s4.Kind + '/' + $s4.Source + '/' + $s4.PercentDiscount + '/' + $s4.TierPercent + '/' + $s4.PromotionPercent)
 $stampHexForList=([BitConverter]::ToString([byte[]]$stamp)).Replace('-','')
 $listRow=One $c "EXEC dbo.sp_InvoiceList @Actor=$actor,@Stamp=0x$stampHexForList,@DateBasis='Paid',@FromDate='2026-01-01',@ToDate='$today',@Status=NULL,@Channel=NULL,@Customer=NULL,@Search=N'HD26990002',@Skip=0" $null
 Check 'ST-16c' (($null -ne $listRow) -and ($listRow.Code.Trim() -eq 'HD26990002') -and ($listRow.Payable -eq 217500)) ('invoice list still reads paid stacked invoice: ' + $(if($null -ne $listRow){ $listRow.Code + '/' + $listRow.Payable + '/' + $listRow.Status } else { 'no row' }))

 # ---------------- ST-17: snapshot cap and immutability ----------------
 [void](CreateOrder $c $actor $stamp 'HD26990008' 'Giao hang' 'KH018' 'KMSTK100' 'SP008' 5 30000 'Khach Test' '0901234567' 'So 1 Duong Test')
 $f1=CheckoutPreview $c $actor $stamp 'HD26990008'
 Check 'ST-17a' (($f1.Source -eq 'TierAndPromotion') -and ($f1.TierPercent -eq 8) -and ($f1.PromotionPercent -eq 100) -and ($f1.PercentDiscount -eq 100) -and ($f1.Discount -eq 250000) -and ($f1.Payable -eq 30000) -and ($f1.Points -eq 0)) ('cap 100% checkout preview=' + $f1.PercentDiscount + '%=' + $f1.TierPercent + '+' + $f1.PromotionPercent + '/' + $f1.Discount + '/' + $f1.Payable + '/pts' + $f1.Points)
 [void](PayOrder $c $actor $stamp 'HD26990008' (OrderRevision $c 'HD26990008') ([byte[]]$f1.PreviewToken) ([decimal]$f1.Payable) ([guid]::NewGuid()))
 Check 'ST-17b' ((SnapshotText $c 'HD26990008') -eq 'Checkout|TierAndPromotion|KIM|KMSTK100|100|8|100|250000|1') ('capped snapshot=' + (SnapshotText $c 'HD26990008'))
 $r1=TrySql $c { [void](Scalar $c "INSERT dbo.HoaDonUuDaiSnapshot(MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,TierPercent,PromotionPercent,Discount,RecordedAt,PolicyRevision) VALUES('HD26990006','Checkout','TierAndPromotion','BAC','KM26HRO',13,3,10,32500,SYSDATETIME(),0x0000000000000001); SELECT 1") }
 $r2=TrySql $c { [void](Scalar $c "UPDATE dbo.HoaDonUuDaiSnapshot SET PercentDiscount=20 WHERE MaHD='HD26990002' AND Kind='Checkout'; SELECT 1") }
 Check 'ST-17c' ((-not $r1.Ok) -and (-not $r2.Ok) -and ((SnapshotText $c 'HD26990002') -eq 'Checkout|TierAndPromotion|BAC|KM26HRO|13|3|10|32500|1') -and ((TxState $c) -eq '0/0')) ('direct insert/update blocked err=' + $r1.Number + '/' + $r2.Number + '; snapshot intact')

 # ---------------- ST-18: legacy invoice untouched + snapshot counts ----------------
 $legacy=One $c "SELECT GiamGia,TrangThai,TongTien FROM dbo.HOADON WHERE MaHD='HD20250401'" $null
 $ls=One $c "SELECT Kind,Source,PercentDiscount,TierPercent,PromotionPercent,Discount,RecordedAt FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD='HD20250401'" $null
 Check 'ST-18' (($legacy.GiamGia -eq 12500) -and ($legacy.TongTien -eq 125000) -and ($legacy.TrangThai -eq 'Da thanh toan') -and ($ls.Kind -eq 'LegacyUnknown') -and ($null -eq $ls.Source) -and ($null -eq $ls.PercentDiscount) -and ($null -eq $ls.TierPercent) -and ($null -eq $ls.PromotionPercent) -and ($ls.Discount -eq 12500) -and ([datetime]$ls.RecordedAt -eq [datetime]'2026-10-23T15:53:42.1362922')) ('legacy invoice/snapshot untouched: giam=' + $legacy.GiamGia + ' kind=' + $ls.Kind + ' recorded=' + ([datetime]$ls.RecordedAt).ToString('yyyy-MM-dd HH:mm:ss.fffffff'))

 # ---------------- ST-19: permissions and query grants ----------------
 $perm=$true
 foreach($n in @('sp_DiscountPreview','sp_SalesCheckoutPreview','sp_ThanhToanHoaDon','sp_InvoiceDetail')){
  if([int](Scalar $c "SELECT HAS_PERMS_BY_NAME('dbo.$n','OBJECT','EXECUTE')") -ne 1){ $perm=$false }
 }
 Check 'ST-19' $perm 'EXECUTE granted on all four altered procedures'

 # ---------------- ST-20: reports unchanged on identical data (A/B vs v37) ----------------
 try{
  $ref=Conn $ReferenceDb
  try{
   $refAdmin=One $ref "SELECT t.MaTK Actor,t.SecurityStamp Stamp FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.TenDangNhap='admin' AND t.HoatDong=1 AND n.DangLamViec=1" $null
   if($null -eq $refAdmin){ throw 'reference admin not usable' }
   $refVer=[int](Scalar $ref 'SELECT MAX(Version) FROM dbo.SchemaMigrations')
   $stampHex=([BitConverter]::ToString([byte[]]$stamp)).Replace('-','')
   $refStampHex=([BitConverter]::ToString([byte[]]$refAdmin.Stamp)).Replace('-','')
   $salesNew=Dump $c "EXEC dbo.sp_ReportSales @Actor=$actor,@Stamp=0x$stampHex,@From='2026-01-01',@To='2026-11-20',@Group='Day'"
   $salesOld=Dump $ref "EXEC dbo.sp_ReportSales @Actor=$($refAdmin.Actor),@Stamp=0x$refStampHex,@From='2026-01-01',@To='2026-11-20',@Group='Day'"
   $payNew=Dump $c "EXEC dbo.sp_ReportPayments @Actor=$actor,@Stamp=0x$stampHex,@From='2026-01-01',@To='2026-11-20'"
   $payOld=Dump $ref "EXEC dbo.sp_ReportPayments @Actor=$($refAdmin.Actor),@Stamp=0x$refStampHex,@From='2026-01-01',@To='2026-11-20'"
   $defNew=[string](Scalar $c "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',OBJECT_DEFINITION(OBJECT_ID('dbo.sp_ReportRead'))),2)")
   $defOld=[string](Scalar $ref "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',OBJECT_DEFINITION(OBJECT_ID('dbo.sp_ReportRead'))),2)")
   Check 'ST-20a' ($salesNew -eq $salesOld) ('sp_ReportSales identical (v' + $refVer + ' vs v38), length=' + $salesNew.Length)
   Check 'ST-20b' ($payNew -eq $payOld) ('sp_ReportPayments identical, length=' + $payNew.Length)
   Check 'ST-20c' ($defNew -eq $defOld) ('sp_ReportRead definition untouched by 038')
  } finally{ $ref.Dispose() }
 }
 catch{
  $script:fail++
  Say ('FAIL|ST-20|report A/B error: ' + $_.Exception.Message)
 }

 Say ("SUMMARY|FAIL="+$script:fail)
} finally { $c.Dispose() }
if($script:fail -gt 0){ exit 1 } else { exit 0 }
