$ErrorActionPreference='Stop'
$root=(Get-Location).Path;$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/b07-20261030'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$meta=Get-Content (Join-Path $run 'gui-fixture.json') -Raw|ConvertFrom-Json
if($meta.Database -cne 'CoffeeManager_Test_G05_20261030_035847_cdac8a8e_demo' -or $meta.Marker -cne '20261030_035847_cdac8a8e'){throw 'Wrong target'}
$c=Open-TestConnection $meta.Database
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $meta.Marker){throw 'Wrong marker'}
 $sql="SELECT (SELECT (SELECT COUNT(*) FROM dbo.PHIEUNHAP) Purchases,(SELECT COUNT(*) FROM dbo.PHIEUXUAT) Issues,(SELECT COUNT(*) FROM dbo.CONGNO_NCC) Debts,(SELECT COUNT(*) FROM dbo.PHIEUTHUCHI) Finance,(SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId IN(9001001,9001002)) Receipts,JSON_QUERY((SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)) Inventory FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)"
 $after=([string](Read-TestScalar $c $sql))|ConvertFrom-Json
 $before=Get-Content (Join-Path $run 'native-before.json') -Raw|ConvertFrom-Json
 $same=($before|ConvertTo-Json -Depth 8 -Compress) -ceq ($after|ConvertTo-Json -Depth 8 -Compress)
 $after|Add-Member DataUnchanged $same;$after|ConvertTo-Json -Depth 8|Set-Content (Join-Path $run 'native-write-check.json')
 if(-not $same){throw 'Unexpected finance/inventory/source counts'}
}finally{$c.Dispose()}
$changed=@(Import-Csv (Join-Path $run 'source-fixed.csv')|Where-Object{(Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -cne $_.Hash})
@{SourceUnchanged=($changed.Count -eq 0);Changed=@($changed.Path)}|ConvertTo-Json|Set-Content (Join-Path $run 'source-freeze-check.json')
if($changed.Count){throw 'Source changed during regression'}
Write-Output 'Native counts, operation receipts and inventory unchanged; source freeze PASS.'
