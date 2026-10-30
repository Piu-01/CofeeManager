$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'OrderingHarness.ps1')
function OrderedPair([string]$first,[string]$second,[string]$flag,[int[]]$expected) {
 $a=$null;$b=$null;$ca=$null;$cb=$null
 try {
  $a=Open-TestConnection $c.Database;$b=Open-TestConnection $c.Database
  $spid=[int](Read-TestScalar $a 'SELECT @@SPID');$ca=$a.CreateCommand();$cb=$b.CreateCommand();$ca.CommandText=$first;$cb.CommandText=$second;$ca.CommandTimeout=$cb.CommandTimeout=30
  $pendingA=$ca.BeginExecuteNonQuery();$observed=$false
  for($i=0;$i -lt 80;$i++) {
   $signal=Read-TestScalar $c "SELECT COUNT(*) FROM sys.dm_exec_sessions s WHERE s.session_id=$spid AND SUBSTRING(s.context_info,1,2)=$flag AND EXISTS(SELECT 1 FROM sys.dm_tran_session_transactions t WHERE t.session_id=s.session_id AND t.is_user_transaction=1)"
   if([int]$signal -eq 1){$observed=$true;break};[Threading.Thread]::Sleep(50)
  }
  if(-not $observed){throw 'First transaction did not reach held-lock barrier'}
  $script:evidence['BarrierObserved']=$true;$pendingB=$cb.BeginExecuteNonQuery();$actual=@();$exceptions=@()
  foreach($item in @(@($ca,$pendingA),@($cb,$pendingB))) {
   try{[void]$item[0].EndExecuteNonQuery($item[1]);$actual+=0}catch{$exceptions+=$_.Exception.ToString();$e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e){throw};$actual += [int]$e.Number}
  }
  $script:evidence['Expected']=$expected;$script:evidence['Actual']=$actual;$script:evidence['Exceptions']=$exceptions;$script:evidence['SessionStates']=@((CleanState $a),(CleanState $b))
  if($actual.Count -ne $expected.Count){throw 'Missing result'}
  for($i=0;$i -lt $expected.Count;$i++){if($actual[$i] -ne $expected[$i]){throw "Ordered result mismatch at$i"}}
 }finally{foreach($x in @($ca,$cb,$a,$b)){if($x){$x.Dispose()}}}
}
$c=$null;Save
try {
 $c=NewDb 'demo' 16;Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 Run-TestSql $c @'
CREATE TABLE dbo.G04Frozen(Code char(10) PRIMARY KEY,Rev binary(8),Token binary(32),Amount decimal(18,0),PolicyRev binary(8),Discount decimal(18,0),Points bigint,InitialBalance int);
CREATE TABLE dbo.G04RecipeExpected(Code char(10),MaNVL char(5),OldQty decimal(18,4),NewQty decimal(18,4),BeforeStock decimal(18,4),PRIMARY KEY(Code,MaNVL));
'@
 $actor="DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';"
 $policy=$actor+@'
DECLARE @tiers dbo.LoyaltyTierInput,@policyRevision binary(8),@json nvarchar(max),@hash binary(32);
INSERT @tiers SELECT RTRIM(MaHang),DiemToiThieu,CASE WHEN PhanTramGiam<100 THEN PhanTramGiam+1 ELSE 100 END FROM dbo.HANGTHANHVIEN;
SELECT @policyRevision=Revision FROM dbo.LoyaltyPolicyState WHERE Id=1;
SET @json=(SELECT Code,MinimumPoints,DiscountPercent FROM @tiers ORDER BY Code FOR JSON PATH);
SET @hash=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@json));
EXEC dbo.sp_LoyaltyPolicyReplace @a,@s,@tiers,@policyRevision,@hash,@id;
'@
 $recipe=$actor+@'
DECLARE @recipe dbo.RecipeLineInput,@revision binary(8);
INSERT @recipe SELECT RTRIM(MaNVL),DinhMuc+0.0010 FROM dbo.CONGTHUC WHERE MaSP='SP001';
SELECT @revision=Revision FROM dbo.SANPHAM WHERE MaSP='SP001';
EXEC dbo.sp_ProductReplaceRecipe @Actor=@a,@Stamp=@s,@Code='SP001',@RequestId=@id,@Recipe=@recipe,@ExpectedRevision=@revision;
'@
 foreach($kind in @('Policy','Recipe')) {
  foreach($writeFirst in @($true,$false)) {
   $suffix=if($kind -eq 'Policy'){if($writeFirst){'01'}else{'02'}}else{if($writeFirst){'03'}else{'04'}}
   $code='HD860000'+$suffix;$label=if($writeFirst){'writer first'}else{'checkout first'}
   Case "T15 policy-recipe concurrency $kind $label" {
    Run-TestSql $c "UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=10000;EXEC dbo.G04New '$code',@customer='KH001';INSERT dbo.G04Frozen SELECT h.MaHD,h.Revision,p.PreviewToken,p.Payable,p.PolicyRevision,p.Discount,p.Points,k.DiemTichLuy FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH WHERE h.MaHD='$code'; INSERT dbo.G04RecipeExpected SELECT '$code',r.MaNVL,r.DinhMuc,r.DinhMuc+0.0010,n.SoLuongTon FROM dbo.CONGTHUC r JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=r.MaNVL WHERE r.MaSP='SP001';"
    $checkout=$actor+"DECLARE @r binary(8),@t binary(32),@amount decimal(18,0),@pay dbo.DanhSachThanhToan;SELECT @r=Rev,@t=Token,@amount=Amount FROM dbo.G04Frozen WHERE Code='$code';INSERT @pay VALUES('PT001',@amount,NULL);EXEC dbo.sp_ThanhToanHoaDon @a,@s,'$code',@r,@id,@t,@pay;"
    if($writeFirst) {
     if($kind -eq 'Policy'){Run-TestSql $c "CREATE TRIGGER dbo.G04Barrier ON dbo.LoyaltyPolicyState AFTER UPDATE AS BEGIN SET CONTEXT_INFO 0x7F01;WAITFOR DELAY '00:00:02';END;";$writer=$policy;$expected=@(0,52201)}
     else{Run-TestSql $c "CREATE TRIGGER dbo.G04Barrier ON dbo.SANPHAM AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaSP='SP001') BEGIN SET CONTEXT_INFO 0x7F01;WAITFOR DELAY '00:00:02';END;END;";$writer=$recipe;$expected=@(0,0)}
     try{OrderedPair $writer $checkout '0x7F01' $expected}finally{Run-TestSql $c 'DROP TRIGGER dbo.G04Barrier;'}
    }else{
     Run-TestSql $c "CREATE TRIGGER dbo.G04Barrier ON dbo.HOADON AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='$code' AND TrangThai=N'Chua thanh toan') BEGIN SET CONTEXT_INFO 0x7F02;WAITFOR DELAY '00:00:02';END;END;"
     $writer=if($kind -eq 'Policy'){$policy}else{$recipe}
     try{OrderedPair $checkout $writer '0x7F02' @(0,0)}finally{Run-TestSql $c 'DROP TRIGGER dbo.G04Barrier;'}
    }
    if($kind -eq 'Policy' -and $writeFirst) {
     Run-TestSql $c "IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='$code' AND TrangThai=N'Chua thanh toan') OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$code') OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD='$code') OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD='$code') OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE EntityCode='$code' AND Operation='SalesCheckout') THROW 52990,'Policy-first rejection leaked',1;"
     $script:evidence['ConflictHasNoFinance']=$true;Run-TestSql $c "EXEC dbo.G04Pay '$code';";$script:evidence['ExplicitNewPreviewCheckout']='Paid after caller explicitly requested new preview'
    }
    if($kind -eq 'Policy' -and -not $writeFirst){Run-TestSql $c "IF EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot s JOIN dbo.G04Frozen f ON f.Code=s.MaHD WHERE s.MaHD='$code' AND (s.PolicyRevision<>f.PolicyRev OR s.Discount<>f.Discount)) THROW 52990,'Policy changed committed snapshot',1;"}
    if($kind -eq 'Recipe'){
     $qty=if($writeFirst){'NewQty'}else{'OldQty'}
     Run-TestSql $c "IF EXISTS(SELECT 1 FROM dbo.G04RecipeExpected e LEFT JOIN dbo.TIEUHAO_HOADON x ON x.MaHD=e.Code AND x.MaNVL=e.MaNVL JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=e.MaNVL WHERE e.Code='$code' AND (x.MaHD IS NULL OR x.SoLuong<>e.$qty OR n.SoLuongTon<>e.BeforeStock-e.$qty)) THROW 52990,'Recipe snapshot or stock order mismatch',1;"
     $script:evidence['ConsumptionUsed']=$qty
    }
    Run-TestSql $c "IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='$code' AND TrangThai=N'Da thanh toan') OR (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$code')<>1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='$code')<>1 OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE EntityCode='$code' AND Operation='SalesCheckout')<>1 THROW 52990,'Checkout not exactly once',1;IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>(SELECT InitialBalance FROM dbo.G04Frozen WHERE Code='$code')+(SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD='$code') THROW 52990,'Lost customer points',1;"
   }
  }
 }
 $m.Status=if(@($m.Cases|Where-Object {$_.Status -eq 'FAIL'}).Count){'CompletedWithFailures'}else{'Succeeded'}
}catch{$m.Status='Aborted';$m['Error']=$_.Exception.ToString()}
finally{if($c){DisposeDb $c};Save;Write-Output "Ordering: $($m.Status),cases=$($m.Cases.Count),cleaned=$($m.Cleaned.Count)"}
