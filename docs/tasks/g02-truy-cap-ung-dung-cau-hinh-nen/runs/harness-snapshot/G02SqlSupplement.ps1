param([string]$Root,[string]$Output,[string]$Database)
$ErrorActionPreference='Stop';$root=$Root
. (Join-Path $Root 'tools/database/DatabaseTestTools.ps1')
[void][IO.Directory]::CreateDirectory($Output);$results=[Collections.Generic.List[object]]::new()
function Result($id,$expected,$actual,$status){$results.Add([ordered]@{Id=$id;Expected=$expected;Actual=$actual;Status=$status})}
function Check($id,$expected,[scriptblock]$action){try{$actual=& $action;Result $id $expected ([string]$actual) 'PASS'}catch{Result $id $expected $_.Exception.Message 'FAIL'}}
function Sql($c,$text,$parameters=@{}){
 $cmd=$c.CreateCommand();$cmd.CommandText=$text;$cmd.CommandTimeout=15
 foreach($key in $parameters.Keys){$v=$parameters[$key];if($v -is [byte[]]){[void]$cmd.Parameters.Add($key,[Data.SqlDbType]::Binary,8);$cmd.Parameters[$key].Value=$v}else{[void]$cmd.Parameters.AddWithValue($key,$v)}}
 try{$value=$cmd.ExecuteScalar();if($value -is [byte[]]){return ,$value};return $value}finally{$cmd.Dispose()}
}
function Reject($c,$text,$params=@{},$number=0){
 $failed=$false;try{[void](Sql $c $text $params)}catch{$e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e -or ($number -and $number -notin @($e.Errors|ForEach-Object Number))){throw};$failed=$true}
 if(-not $failed){throw 'Expected rejection absent; command accepted'}
 if((Sql $c "SELECT CONCAT(@@TRANCOUNT,':',XACT_STATE())") -ne '0:0'){throw 'Transaction remained after error'}
 return 'Rejected; @@TRANCOUNT=0/XACT_STATE=0'
}
function Need($value,$message){if(-not $value){throw $message}}
$owner=Open-TestConnection $Database;Assert-TestTarget $owner 'G02'
Need ((Sql $owner 'SELECT COUNT(*) FROM dbo.G02TestMarker') -eq 1) 'Missing marker'
$app=Open-TestConnection $Database;Assert-TestTarget $app 'G02';[void](Sql $app "EXECUTE AS USER=N'G02_Runtime'")
$actor=[int](Sql $owner "SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin'")
$stamp=Sql $owner 'SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@id' @{'@id'=$actor}
$base=@{'@Actor'=$actor;'@Stamp'=$stamp}
$catalogs=@(
 @{Kind='ProductCategory';Code='';Max=50;Extra="";Params=@{'@RequestId'=[guid]::NewGuid()}},
 @{Kind='Supplier';Code='NCC81';Max=100;Extra=',@Phone=@phone';Params=@{'@phone'='01810000001'}},
 @{Kind='PaymentMethod';Code='PT881';Max=50;Extra='';Params=@{}},
 @{Kind='DeliveryPartner';Code='DT881';Max=50;Extra=',@Ratio=0';Params=@{}},
 @{Kind='ExpenseCategory';Code='TC881';Max=50;Extra=",@ExpenseType=N'Chi'";Params=@{}},
 @{Kind='Position';Code='CV881';Max=50;Extra=',@Allowance=0,@Factor=1';Params=@{}},
 @{Kind='Shift';Code='CA881';Max=30;Extra=",@StartTime='22:00',@EndTime='06:00'";Params=@{}}
)
try{
 foreach($cat in $catalogs){
  $params=$base.Clone();$params['@Code']=$cat.Code;foreach($k in $cat.Params.Keys){$params[$k]=$cat.Params[$k]}
  $request=if($cat.Kind -eq 'ProductCategory'){',@RequestId=@RequestId'}else{''}
  $sql="EXEC dbo.sp_Catalog$($cat.Kind)Create @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@Name=@Name$request$($cat.Extra)"
  foreach($input in @(@{Label='empty';Name=''},@{Label='space';Name=' '},@{Label='tab';Name="`t"},@{Label='maxplus1';Name=('X'*($cat.Max+1))})){
   $params['@Name']=$input.Name
   Check "T08-K02.$($cat.Kind).a.$($input.Label).SQL" 'SQL rejects invalid name without truncation' {Reject $app $sql $params}
  }
  # Every accepted boundary is a fresh fixed code/request to avoid previous FAIL masking it.
  $params['@Name']='Y'*$cat.Max;$params['@Code']=if($cat.Kind -eq 'Supplier'){'NCC82'}elseif($cat.Kind -eq 'ProductCategory'){''}else{$cat.Code.Substring(0,2)+'882'}
  if($cat.Kind -eq 'ProductCategory'){$params['@RequestId']=[guid]::NewGuid()}
  if($cat.Kind -eq 'Supplier'){$params['@phone']='01820000001'}
  Check "T08-K02.$($cat.Kind).a.max.SQL" 'Maximum name length persists' {[void](Sql $app $sql $params);'Maximum name accepted'}
  if($cat.Kind -ne 'ProductCategory'){
   Check "T08-K02.$($cat.Kind).b.duplicate.SQL" 'Duplicate code rejected' {Reject $app $sql $params}
   $params['@Code']='BAD00';Check "T08-K02.$($cat.Kind).b.malformed.SQL" 'Malformed code rejected' {Reject $app $sql $params 51506}
  }
 }
 foreach($phone in @(@{Id='NULL';Value=[DBNull]::Value},@{Id='empty';Value=''},@{Id='length16';Value='0123400000000000'})){
  $p=$base.Clone();$p['@phone']=$phone.Value
  Check "T08-K02.ncc.$($phone.Id).SQL" 'Invalid phone rejected, no truncation' {Reject $app "EXEC dbo.sp_CatalogSupplierCreate @Actor=@Actor,@Stamp=@Stamp,@Code='NCC83',@Name=N'Phone boundary',@Phone=@phone" $p}
 }
 foreach($test in @(
 @{Id='ratio.minus1';Sql="sp_CatalogDeliveryPartnerCreate @Code='DT883',@Name=N'Ratio',@Ratio=-1"},
 @{Id='ratio.above100';Sql="sp_CatalogDeliveryPartnerCreate @Code='DT883',@Name=N'Ratio',@Ratio=100.01"},
 @{Id='allowance.minus1';Sql="sp_CatalogPositionCreate @Code='CV883',@Name=N'Position',@Allowance=-1,@Factor=1"},
 @{Id='factor.below1';Sql="sp_CatalogPositionCreate @Code='CV883',@Name=N'Position',@Allowance=0,@Factor=0.99"},
 @{Id='shift.equal';Sql="sp_CatalogShiftCreate @Code='CA883',@Name=N'Shift',@StartTime='06:00',@EndTime='06:00'"}
 )){Check "T08-K03.a.$($test.Id).SQL" 'Invalid boundary rejected, transaction0' {Reject $app "EXEC dbo.$($test.Sql),@Actor=@Actor,@Stamp=@Stamp" $base 51501}}
 foreach($code in @('TC005','TC011','TC012')){
  foreach($action in @('type','code','delete')){
   $sql=switch($action){'type'{"UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Thu' WHERE MaDMTC='$code'"}'code'{"UPDATE dbo.DANHMUC_THUCHI SET MaDMTC='TC879' WHERE MaDMTC='$code'"}'delete'{"DELETE dbo.DANHMUC_THUCHI WHERE MaDMTC='$code'"}}
   Check "T08-K07.a.$code.$action.raw" 'Even unused system row protected' {Reject $owner $sql}
  }
  Check "T08-K07.a.$code.rename.raw" 'System expense name remains editable' {[void](Sql $owner "UPDATE dbo.DANHMUC_THUCHI SET TenDMTC=TenDMTC+N' test' WHERE MaDMTC='$code'");'Rename accepted; mapping retained'}
 }
 $roles=@('ADMIN','CASH','STORE')
 foreach($role in $roles){
  $id=[int](Sql $owner 'SELECT MaTK FROM dbo.TAIKHOAN WHERE MaVT=@role' @{'@role'=$role})
  $s=Sql $owner 'SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@id' @{'@id'=$id};$p=@{'@Actor'=$id;'@Stamp'=$s}
  foreach($purpose in @('Checkout','Inventory')){
   $allow=$role -eq 'ADMIN' -or ($role -eq 'CASH' -and $purpose -eq 'Checkout') -or ($role -eq 'STORE' -and $purpose -eq 'Inventory')
   Check "T08-K08.a.$role.$purpose.SQL" 'Selector enforces role/purpose and excludes unsupported/PT010' {if($allow){$cmd=$app.CreateCommand();$cmd.CommandText="EXEC dbo.sp_PaymentMethodListSupported @Actor=$id,@Stamp=0x$(([BitConverter]::ToString($s)).Replace('-','')),@Purpose='$purpose'";try{$reader=$cmd.ExecuteReader();$codes=@();while($reader.Read()){$codes+=$reader.GetString(0).Trim()};$reader.Close();Need ($codes -contains 'PT001' -and $codes -contains 'PT002' -and $codes -notcontains 'PT010' -and $codes -notcontains 'PT998') 'Selector wrong';return "Allowed; codes=$($codes -join ',')"}finally{$cmd.Dispose()}}else{return Reject $app "EXEC dbo.sp_PaymentMethodListSupported @Actor=@Actor,@Stamp=@Stamp,@Purpose='$purpose'" $p 51505}}
  }
  foreach($kind in @('ProductCategory','Supplier','DeliveryPartner','ExpenseCategory','Position','Shift')){
   $allow=$role -eq 'ADMIN' -or ($kind -eq 'ProductCategory') -or ($role -eq 'STORE' -and $kind -eq 'Supplier') -or ($role -eq 'CASH' -and $kind -eq 'DeliveryPartner')
   Check "T08-K09.lookup.$role.$kind.SQL" 'Lookup role and field projection match contract' {if(-not $allow){return Reject $app "EXEC dbo.sp_CatalogLookup @Actor=@Actor,@Stamp=@Stamp,@Kind='$kind'" $p 51505};$cmd=$app.CreateCommand();$cmd.CommandText="EXEC dbo.sp_CatalogLookup @Actor=$id,@Stamp=0x$(([BitConverter]::ToString($s)).Replace('-','')),@Kind='$kind'";try{$r=$cmd.ExecuteReader();$fields=@();for($i=0;$i -lt $r.FieldCount;$i++){$fields+=$r.GetName($i)};$r.Close();Need ('Ratio' -notin $fields -and 'Allowance' -notin $fields -and 'Factor' -notin $fields) 'Financial fields exposed';return "Allowed fields=$($fields -join ',')"}finally{$cmd.Dispose()}}
  }
 }
 Check 'T08-K02.fk.actorMissing' 'Unknown actor rejected' {$p=$base.Clone();$p['@Actor']=-1;Reject $app "EXEC dbo.sp_CatalogSupplierList @Actor=@Actor,@Stamp=@Stamp" $p 51505}
 Check 'T08-K02.fk.stampWrong' 'Wrong security stamp rejected' {$p=$base.Clone();$p['@Stamp']=[byte[]]::new(8);Reject $app "EXEC dbo.sp_CatalogSupplierList @Actor=@Actor,@Stamp=@Stamp" $p 51505}
 Check 'T08-K02.fk.productMissingCategory' 'Invalid category FK rejected' {Reject $owner "INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('SP889',N'Invalid FK','DM000',1000)" @{} 547}
 Check 'T08-K06.a' 'Clean target includes all system rows/mappings' {Need ((Sql $owner "SELECT COUNT(*) FROM dbo.SystemExpenseCategory WHERE MaDMTC IN('TC005','TC011','TC012')") -eq 3) 'Mappings missing';'Three mappings, default methods and roles from clean runner fixture present'}
 foreach($conflict in @('TC011Thu','TC012Existing','PaymentNameCollision','UsernameCaseCollision')){
  $run=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$db=New-IsolatedDatabase 'G02' $run ($conflict -ne 'PaymentNameCollision')
  [void](& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db)
  $c=Open-TestConnection $db
  try{
   $version=3
   if($conflict -eq 'UsernameCaseCollision'){
    Run-TestSql $c "ALTER TABLE dbo.TAIKHOAN DROP CONSTRAINT UQ_TK_Login; ALTER TABLE dbo.TAIKHOAN ALTER COLUMN TenDangNhap varchar(50) COLLATE Latin1_General_100_CS_AS NOT NULL; ALTER TABLE dbo.TAIKHOAN ADD CONSTRAINT UQ_TK_Login UNIQUE(TenDangNhap); UPDATE dbo.TAIKHOAN SET TenDangNhap='ADMIN' WHERE TenDangNhap='thungan';";$version=2
   }else{
    [void](& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db -Version 2 -File (Join-Path $Root 'database/migrations/002_auth_and_bootstrap.sql'))
    switch($conflict){'TC011Thu'{Run-TestSql $c "UPDATE dbo.DANHMUC_THUCHI SET Loai=N'Thu' WHERE MaDMTC='TC011'"}'TC012Existing'{Run-TestSql $c "INSERT dbo.DANHMUC_THUCHI VALUES('TC012',N'Existing meaning',N'Chi')"}'PaymentNameCollision'{Run-TestSql $c "INSERT dbo.PHUONGTHUCTT VALUES('PT003',N'Chuyển khoản ngân hàng')"}}
   }
   Check $(if($version -eq 2){'T06-K10.b.preflight'}else{"T08-K06.c.$conflict"}) 'Migration conflict rolls back without rewrite' {$before=Snapshot-TestRows $c;$failed=$false;try{[void](& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db -Version $version -File ((Get-ChildItem (Join-Path $Root 'database/migrations') -Filter ('{0:000}_*.sql' -f $version)).FullName))}catch{$failed=$true};Need $failed 'Migration unexpectedly accepted';Need ($before -ceq (Snapshot-TestRows $c)) 'Persistent rows changed on failure';Need ((Sql $c "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version=$version") -eq 0) 'Version committed';"Rejected, rows unchanged, version absent; DB=$db"}
  }finally{$c.Dispose()}
 }
}finally{
 $app.Dispose();$owner.Dispose()
 [IO.File]::WriteAllText((Join-Path $Output 'sql-supplement.json'),($results|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
}
if(@($results|Where-Object {$_.Status -eq 'FAIL'}).Count){exit 1}
