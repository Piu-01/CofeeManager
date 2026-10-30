$ErrorActionPreference='Stop'
$root=(Get-Location).Path;$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/native-followup-20261029'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$meta=Get-Content (Join-Path $run 'gui-fixture.json') -Raw|ConvertFrom-Json
if($meta.Database -cne 'CoffeeManager_Test_G05_20261029_195906_57af788d_demo' -or $meta.Marker -cne '20261029_195906_57af788d'){throw 'Wrong fixture'}
$c=Open-TestConnection $meta.Database
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $meta.Marker){throw 'Wrong DB marker'}
 $sql="SELECT (SELECT (SELECT COUNT(*) FROM dbo.PHIEUNHAP) Purchases,(SELECT COUNT(*) FROM dbo.PHIEUXUAT) Issues,(SELECT COUNT(*) FROM dbo.CONGNO_NCC) Debts,(SELECT COUNT(*) FROM dbo.PHIEUTHUCHI) Finance,(SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId IN(9001001,9001002)) Receipts,JSON_QUERY((SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)) Inventory,JSON_QUERY((SELECT * FROM dbo.vw_CongNoNCC WHERE MaCN='CN20250405' FOR JSON PATH)) Debt,JSON_QUERY((SELECT t.MaTC,p.SoTien,p.OriginKind,t.MaPT,p.MaNV FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI p ON p.MaTC=t.MaTC WHERE t.MaCN='CN20250405' ORDER BY t.MaTT FOR JSON PATH)) Payments FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)"
 $after=([string](Read-TestScalar $c $sql))|ConvertFrom-Json
 $before=Get-Content (Join-Path $run 'native-before.json') -Raw|ConvertFrom-Json
 $stockOk=$true;foreach($stock in $before.Inventory){$expected=$stock.SoLuongTon;if($stock.MaNVL -eq 'NVL12'){$expected+=1};if(($after.Inventory|Where-Object MaNVL -eq $stock.MaNVL).SoLuongTon -ne $expected){$stockOk=$false}}
 $ok=$after.Purchases -eq $before.Purchases+1 -and $after.Issues -eq $before.Issues+1 -and $after.Debts -eq $before.Debts+1 -and $after.Finance -eq $before.Finance+2 -and $after.Receipts -eq 4 -and $stockOk -and $after.Debt[0].TienNo -eq 21000 -and $after.Debt[0].DaTra -eq 21000 -and $after.Debt[0].ConNo -eq 0 -and $after.Payments.Count -eq 2 -and ($after.Payments|Measure-Object SoTien -Sum).Sum -eq 21000 -and @($after.Payments|Where-Object{$_.OriginKind -ne 'SupplierDebt' -or $_.MaNV -ne 'NV991'}).Count -eq 0
 $after|Add-Member Result $(if($ok){'PASS'}else{'FAIL'});$after|ConvertTo-Json -Depth 8|Set-Content (Join-Path $run 'native-final-check.json');if(-not $ok){throw 'Unexpected native writes'}
 # Hai nhánh boundary còn thiếu; principal runtime chỉ có quyền procedure của app.
 Run-TestSql $c "IF USER_ID('G05FollowupRuntime') IS NULL BEGIN CREATE USER G05FollowupRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G05FollowupRuntime;END;"
 $snapshot=Snapshot-TestRows $c
 $cases=@(
  @{Id='T19-K02.Name51';Number=51501;Sql="DECLARE @s binary(8),@name nvarchar(max)=REPLICATE(N'x',51);SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=9001001;EXEC dbo.sp_CatalogExpenseCategoryCreate @Actor=9001001,@Stamp=@s,@Code='TC999',@Name=@name,@ExpenseType=N'Chi';"},
  @{Id='T20-K02.ActorMissing';Number=51505;Sql="DECLARE @id uniqueidentifier=NEWID(),@lines dbo.PurchaseLinesInput;INSERT @lines VALUES('NVL12',1,7000);EXEC dbo.sp_LapPhieuNhap @Actor=2147483647,@Stamp=0x0000000000000000,@RequestId=@id,@Supplier='NCC07',@Date='20261030',@Immediate=0,@ExpectedTotal=7000,@Lines=@lines;"}
 )
 $results=@();foreach($case in $cases){
  # Lấy stamp trước EXECUTE AS, không mở quyền đọc credential cho runtime.
  $sql=$case.Sql
  if($case.Id -like 'T19*'){$sql=$sql.Replace('EXEC dbo.sp_CatalogExpenseCategoryCreate',"EXECUTE AS USER='G05FollowupRuntime';EXEC dbo.sp_CatalogExpenseCategoryCreate")}else{$sql="EXECUTE AS USER='G05FollowupRuntime';"+$sql}
  try{Expect-TestSqlError $c $sql $case.Number}finally{Run-TestSql $c 'REVERT;'}
  $unchanged=(Snapshot-TestRows $c) -ceq $snapshot
  $results+=@{Id=$case.Id;Error=$case.Number;DataUnchanged=$unchanged;Transaction=[string](Read-TestScalar $c "SELECT CONCAT(@@TRANCOUNT,':',XACT_STATE())");Result=$(if($unchanged){'PASS'}else{'FAIL'})}
 }
 $results|ConvertTo-Json -Depth 5|Set-Content (Join-Path $run 'boundary-results.json');if(@($results|Where-Object Result -ne PASS).Count){throw 'Boundary mutation'}
}finally{$c.Dispose()}
$changed=@(Import-Csv (Join-Path $run 'source-fixed.csv')|Where-Object{(Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -cne $_.Hash})
@{SourceUnchanged=$changed.Count -eq 0;Changed=$changed}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $run 'source-freeze-check.json');if($changed.Count){throw 'Source changed during native test'}
Write-Output 'Native final deltas PASS; runtime name51/actor missing boundaries PASS.'
