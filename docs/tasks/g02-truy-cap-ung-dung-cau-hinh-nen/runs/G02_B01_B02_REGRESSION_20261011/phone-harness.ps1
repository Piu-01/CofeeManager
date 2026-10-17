param([string]$Root,[string]$Output,[string]$Database)
$ErrorActionPreference='Stop'; . (Join-Path $Root 'tools/database/DatabaseTestTools.ps1')
[void][IO.Directory]::CreateDirectory($Output)
$owner=Open-TestConnection $Database;Assert-TestTarget $owner 'G02'
if((Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.G02TestMarker') -ne 1){throw 'Missing marker'}
$app=Open-TestConnection $Database;Run-TestSql $app "EXECUTE AS USER=N'G02_Runtime'"
$actor=Read-TestScalar $owner "SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin'"
$stamp=Read-TestScalar $owner "SELECT sys.fn_varbintohexstr(SecurityStamp) FROM dbo.TAIKHOAN WHERE MaTK=$actor"
$results=[Collections.Generic.List[object]]::new()
function Check($id,$expected,[scriptblock]$testAction){try{$actual=& $testAction;$results.Add([ordered]@{Id=$id;Expected=$expected;Actual=[string]$actual;Status='PASS'})}catch{$results.Add([ordered]@{Id=$id;Expected=$expected;Actual=$_.Exception.Message;Status='FAIL'})}}
function Reject($sql,$numbers){$before=Snapshot-TestRows $owner;$failed=$false
 try{Run-TestSql $app $sql}catch{$e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e -or -not @($e.Errors|Where-Object {$_.Number -in $numbers}).Count){throw};$failed=$true;$n=$e.Number}
 if(-not $failed){throw 'Expected rejection absent'}
 if((Read-TestScalar $app "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)") -ne '0:0'){throw 'Transaction not idle'}
 if($before -cne (Snapshot-TestRows $owner)){throw 'Data or revision changed after rejection'}
 return "SQL$n; transaction0:0; full database snapshot unchanged"
}
try{
 Run-TestSql $app "EXEC dbo.sp_CatalogSupplierCreate @Actor=$actor,@Stamp=$stamp,@Code='NCC86',@Name=N'Phone duplicate fixture',@Phone='01860000001'"
 foreach($action in @('Create','Update')){
  if($action -eq 'Update'){Run-TestSql $app "EXEC dbo.sp_CatalogSupplierCreate @Actor=$actor,@Stamp=$stamp,@Code='NCC87',@Name=N'Phone regression',@Phone='01870000001'"}
  $revision=if($action -eq 'Update'){Read-TestScalar $owner "SELECT sys.fn_varbintohexstr(CatalogRevision) FROM dbo.NHACUNGCAP WHERE MaNCC='NCC87'"}else{'NULL'}
  foreach($case in @(@{Id='NULL';Value='NULL';Numbers=@(51501)},@{Id='empty';Value="''";Numbers=@(51501)},@{Id='duplicate';Value="'01860000001'";Numbers=@(2601,2627)},@{Id='length16';Value="'0187000000000001'";Numbers=@(51501)})){
   Check "B02.Phone.$action.$($case.Id)" 'Reject invalid/duplicate phone; no truncation or partial write' {
    Reject "EXEC dbo.sp_CatalogSupplier$action @Actor=$actor,@Stamp=$stamp,@Code='NCC87',@Name=N'Phone regression',@ExpectedRevision=$revision,@Phone=$($case.Value)" $case.Numbers
   }
  }
  if($action -eq 'Create'){
   Check 'B02.Phone.Create.valid15.leading0' 'Phone15 including leading0 persists exactly' {
    Run-TestSql $app "EXEC dbo.sp_CatalogSupplierCreate @Actor=$actor,@Stamp=$stamp,@Code='NCC88',@Name=N'Phone length15',@Phone='018800000000001'"
    $v=Read-TestScalar $owner "SELECT SDT FROM dbo.NHACUNGCAP WHERE MaNCC='NCC88'";if($v -cne '018800000000001'){throw 'Phone changed'};return 'Length15 and leading0 preserved'
   }
  }else{
   Check 'B02.Phone.Update.valid15.leading0' 'Update to phone15 preserves exact string' {
    Run-TestSql $app "EXEC dbo.sp_CatalogSupplierUpdate @Actor=$actor,@Stamp=$stamp,@Code='NCC87',@Name=N'Phone regression updated',@ExpectedRevision=$revision,@Phone='018700000000001'"
    $v=Read-TestScalar $owner "SELECT SDT FROM dbo.NHACUNGCAP WHERE MaNCC='NCC87'";if($v -cne '018700000000001'){throw 'Phone changed'};return 'Update length15 and leading0 preserved'
   }
  }
 }
}finally{$owner.Dispose();$app.Dispose();[IO.File]::WriteAllText((Join-Path $Output 'results.json'),($results|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))}
if(@($results|Where-Object Status -eq 'FAIL').Count){exit 1}
