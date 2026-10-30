$ErrorActionPreference='Stop'
$root=(Get-Location).Path
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/b05-20261029'
$db='CoffeeManager_Test_G05_20261029_182915_f5617a84_demo'
$connection=Open-TestConnection $db
try {
 if((Read-TestScalar $connection 'SELECT RunId FROM dbo.G05TestMarker') -cne '20261029_182915_f5617a84'){throw 'Wrong marker'}
 $after=[ordered]@{
  Purchases=[int](Read-TestScalar $connection 'SELECT COUNT(*) FROM dbo.PHIEUNHAP')
  Debts=[int](Read-TestScalar $connection 'SELECT COUNT(*) FROM dbo.CONGNO_NCC')
  Finance=[int](Read-TestScalar $connection 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI')
  ActorReceipts=[int](Read-TestScalar $connection 'SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId=9001001')
  Inventory=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
  NativeFinance=[string](Read-TestScalar $connection "SELECT (SELECT p.MaTC,p.SoTien,p.GhiChu,p.OriginKind,r.Operation,r.ActorAccountId FROM dbo.PHIEUTHUCHI p JOIN dbo.G03OperationReceipt r ON r.EntityCode=p.MaTC WHERE r.ActorAccountId=9001001 FOR JSON PATH)")
 }
 $before=Get-Content (Join-Path $run 'native-before.json') -Raw|ConvertFrom-Json
 $after['Result']=if($after.Purchases -eq $before.Purchases -and $after.Debts -eq $before.Debts -and $after.Finance -eq ($before.Finance+1) -and $after.ActorReceipts -eq 1 -and $after.Inventory -ceq $before.Inventory -and ($after.NativeFinance|ConvertFrom-Json).SoTien -eq 2345){'PASS'}else{'FAIL'}
 $after|ConvertTo-Json -Depth 6|Set-Content (Join-Path $run 'native-write-check.json') -Encoding utf8
 if($after.Result -ne 'PASS'){throw 'Unexpected native writes'}
 Write-Output 'Native SQL comparison PASS: one manual 2345 receipt, purchase/debt/inventory unchanged.'
}finally{$connection.Dispose()}
$changes=@(Import-Csv (Join-Path $run 'source-fixed.csv')|Where-Object{(Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -cne $_.Hash})
@{SourceUnchanged=$changes.Count -eq 0;Changed=$changes}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $run 'source-freeze-check.json')
if($changes.Count){throw 'Source changed during regression'}
