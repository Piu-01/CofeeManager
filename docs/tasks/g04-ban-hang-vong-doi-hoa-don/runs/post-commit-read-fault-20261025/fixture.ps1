param([ValidateSet('Arm','Restore')][string]$Mode)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$database='CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout'
$marker='20261025_181739_98e5df59'
$journal=Join-Path $PSScriptRoot 'fault-journal.json'
$connection=Open-TestConnection $database
try {
    if((Read-TestScalar $connection 'SELECT RunId FROM dbo.G04TestMarker') -cne $marker){throw 'Test marker mismatch'}
    if($Mode -eq 'Arm'){
        if(Test-Path $journal){throw 'Journal exists; inspect and restore before another fault'}
        if((Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='HD20250420' AND TrangThai=N'Chua thanh toan' AND ThanhTien=81000") -ne 1){throw 'Expected open test order420'}
        $existing=Read-TestScalar $connection "SELECT COUNT(*) FROM sys.database_permissions WHERE class=1 AND major_id=OBJECT_ID('dbo.sp_InvoiceDetail') AND grantee_principal_id=DATABASE_PRINCIPAL_ID('G04Gui') AND permission_name='EXECUTE'"
        if($existing -ne 0){throw 'Unexpected pre-existing direct permission; do not overwrite'}
        Read-TestScalar $connection "SELECT (SELECT (SELECT MaHD,TrangThai,ThanhTien FROM dbo.HOADON WHERE MaHD='HD20250420' FOR JSON PATH) Invoice,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT * FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD20250420' FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD20250420' FOR JSON PATH) Consumption FOR JSON PATH)" | Set-Content "$PSScriptRoot/before.json" -Encoding utf8
        [ordered]@{Database=$database;Marker=$marker;User='G04Gui';Procedure='dbo.sp_InvoiceDetail';OriginalDirectPermissionCount=0;State='Arming';RestoreCommand='./fixture.ps1 -Mode Restore';Purpose='Approved PC04 post-commit invoice read error only';ReviewBy='2026-10-26'} | ConvertTo-Json | Set-Content $journal -Encoding utf8
        Run-TestSql $connection 'DENY EXECUTE ON OBJECT::dbo.sp_InvoiceDetail TO G04Gui;'
        $entry=Get-Content $journal -Raw|ConvertFrom-Json;$entry.State='Armed';$entry|ConvertTo-Json|Set-Content $journal -Encoding utf8
        Write-Output 'PC04 armed only in isolated test DB: invoice-detail read denied. Restore after observing420 result.'
    } else {
        $entry=Get-Content $journal -Raw|ConvertFrom-Json
        if($entry.Database -cne $database -or $entry.Marker -cne $marker -or $entry.User -cne 'G04Gui' -or $entry.Procedure -cne 'dbo.sp_InvoiceDetail' -or $entry.OriginalDirectPermissionCount -ne 0){throw 'Unexpected journal; refusing permission mutation'}
        if($entry.State -eq 'Restored'){Write-Output 'Already restored';return}
        Run-TestSql $connection 'REVOKE EXECUTE ON OBJECT::dbo.sp_InvoiceDetail FROM G04Gui;'
        if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.database_permissions WHERE class=1 AND major_id=OBJECT_ID('dbo.sp_InvoiceDetail') AND grantee_principal_id=DATABASE_PRINCIPAL_ID('G04Gui') AND permission_name='EXECUTE'") -ne 0){throw 'Permission restoration incomplete'}
        $entry.State='Restored';$entry|ConvertTo-Json|Set-Content $journal -Encoding utf8
        Write-Output 'PC04 direct deny removed; inherited application EXEC permission restored.'
    }
}finally{$connection.Dispose()}
