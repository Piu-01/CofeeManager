param([string]$Root,[string]$Output,[string]$Database)
$ErrorActionPreference='Stop'; . (Join-Path $Root 'tools/database/DatabaseTestTools.ps1')
[void][IO.Directory]::CreateDirectory($Output)
$results=[Collections.Generic.List[object]]::new()
$owner=Open-TestConnection $Database;Assert-TestTarget $owner 'G02'
if((Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.G02TestMarker') -ne 1){throw 'Missing marker'}
$app=Open-TestConnection $Database;Run-TestSql $app "EXECUTE AS USER=N'G02_Runtime'"
$actor=Read-TestScalar $owner "SELECT MaTK FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin'"
$stamp=Read-TestScalar $owner "SELECT sys.fn_varbintohexstr(SecurityStamp) FROM dbo.TAIKHOAN WHERE MaTK=$actor"
try{
 foreach($cat in @(
 @{Kind='Supplier';Code='NCC90';Extra=",@Phone='01760000001'"},
 @{Kind='PaymentMethod';Code='PT990';Extra=',@Supported=0'},
 @{Kind='DeliveryPartner';Code='DT990';Extra=',@Ratio=0'},
 @{Kind='ExpenseCategory';Code='TC990';Extra=",@ExpenseType=N'Chi'"},
 @{Kind='Position';Code='CV990';Extra=',@Allowance=0,@Factor=1'},
 @{Kind='Shift';Code='CA990';Extra=",@StartTime='22:00',@EndTime='06:00'"}
 )){
  foreach($test in @(@{Id='malformed';Code='BAD00';Error=51506},@{Id='duplicate';Code=$cat.Code;Error=2627})){
   $id="T08-K02.$($cat.Kind).$($test.Id).SQL.confirmed"
   try{$before=Snapshot-TestRows $owner;Expect-TestSqlError $app "EXEC dbo.sp_Catalog$($cat.Kind)Create @Actor=$actor,@Stamp=$stamp,@Code='$($test.Code)',@Name=N'Code boundary $($cat.Kind)'$($cat.Extra)" $test.Error
    if($before -cne (Snapshot-TestRows $owner)){throw 'Persistent rows/revisions changed'}
    $results.Add([ordered]@{Id=$id;Expected="Reject SQL$($test.Error), no partial write";Actual="SQL$($test.Error); full DB snapshot unchanged; transaction0:0";Status='PASS'})
   }catch{$results.Add([ordered]@{Id=$id;Expected="Reject SQL$($test.Error), no partial write";Actual=$_.Exception.Message;Status='FAIL'})}
  }
 }
}finally{$owner.Dispose();$app.Dispose();[IO.File]::WriteAllText((Join-Path $Output 'results.json'),($results|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))}
if(@($results|Where-Object Status -eq 'FAIL').Count){exit 1}
