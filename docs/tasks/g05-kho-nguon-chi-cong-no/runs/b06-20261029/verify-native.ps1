$ErrorActionPreference='Stop'
$root=(Get-Location).Path;$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/b06-20261029'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$c=Open-TestConnection 'CoffeeManager_Test_G05_20261029_194514_8bfcd1ce_demo'
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne '20261029_194514_8bfcd1ce'){throw 'Wrong marker'}
 $after=[ordered]@{Purchases=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUNHAP');Issues=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUXUAT');Debts=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.CONGNO_NCC');Finance=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI');ActorReceipts=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId IN(9001001,9001002)');Inventory=[string](Read-TestScalar $c 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')}
 $details=[string](Read-TestScalar $c "SELECT (SELECT r.Operation,r.EntityCode,p.TongTien,p.DaChot PurchaseClosed,d.MaNVL,d.SoLuong,d.DonGiaNhap,f.MaTC,f.SoTien,f.OriginKind,n.MaCN,n.TienNo,x.DaChot IssueClosed,x.LyDo,dx.SoLuongXuat FROM dbo.G03OperationReceipt r LEFT JOIN dbo.PHIEUNHAP p ON p.MaPN=r.EntityCode LEFT JOIN dbo.CHITIET_PN d ON d.MaPN=p.MaPN LEFT JOIN dbo.PHIEUTHUCHI f ON f.MaPN=p.MaPN LEFT JOIN dbo.CONGNO_NCC n ON n.MaPN=p.MaPN LEFT JOIN dbo.PHIEUXUAT x ON x.MaPX=r.EntityCode LEFT JOIN dbo.CHITIET_PX dx ON dx.MaPX=x.MaPX WHERE r.ActorAccountId=9001001 ORDER BY r.Operation FOR JSON PATH)")
 $after['Details']=$details|ConvertFrom-Json
 $before=Get-Content (Join-Path $run 'native-before.json') -Raw|ConvertFrom-Json
 $stocksBefore=$before.Inventory|ConvertFrom-Json;$stocksAfter=$after.Inventory|ConvertFrom-Json
 $stockOk=$true;foreach($stock in $stocksBefore){$expected=$stock.SoLuongTon;if($stock.MaNVL -eq 'NVL12'){$expected+=1};if(($stocksAfter|Where-Object MaNVL -eq $stock.MaNVL).SoLuongTon -ne $expected){$stockOk=$false}}
 $pn=$after.Details|Where-Object Operation -eq PurchaseCreate;$px=$after.Details|Where-Object Operation -eq StockIssueCreate
 $over=Get-Content (Join-Path $run 'native-overstock-sql.json') -Raw|ConvertFrom-Json
 $ok=$after.Purchases -eq ($before.Purchases+1) -and $after.Issues -eq ($before.Issues+1) -and $after.Debts -eq ($before.Debts+1) -and $after.Finance -eq ($before.Finance+1) -and $after.ActorReceipts -eq 2 -and $stockOk -and $pn.TongTien -eq 20000 -and $pn.SoLuong -eq 2 -and $pn.DonGiaNhap -eq 10000 -and $pn.SoTien -eq 4000 -and $pn.TienNo -eq 16000 -and $pn.OriginKind -eq 'PurchaseImmediate' -and $pn.PurchaseClosed -and $px.IssueClosed -and $px.SoLuongXuat -eq 1 -and $over.Issues -eq $before.Issues -and $over.IssueReceipts -eq 0 -and $over.Stock -eq 31
 $after['Result']=if($ok){'PASS'}else{'FAIL'};$after['AllInventoryDeltasCorrect']=$stockOk
 $after|ConvertTo-Json -Depth 6|Set-Content (Join-Path $run 'native-write-check.json')
 if(-not $ok){throw 'Unexpected native data'}
}finally{$c.Dispose()}
$changes=@(Import-Csv (Join-Path $run 'source-fixed.csv')|Where-Object{(Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -cne $_.Hash})
@{SourceUnchanged=$changes.Count -eq 0;Changed=$changes}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $run 'source-freeze-check.json')
if($changes.Count){throw 'Source changed'}
$pendingRoot=Join-Path $env:LOCALAPPDATA 'CoffeeManager/Pending'
$pending=@('PurchaseCreate','StockIssueCreate')|ForEach-Object{@{Operation=$_;PayloadPresent=Test-Path -LiteralPath (Join-Path $pendingRoot "9001001_$_.bin")}}
$pending|ConvertTo-Json|Set-Content (Join-Path $run 'native-pending-check.json')
Write-Output 'Native data PASS: purchase+2, issue-1, inventory29=>30; one chi4000, debt16000, two receipts; overstock rolled back.'
