[CmdletBinding()]param([Parameter(Mandatory=$true)][switch]$Apply)
# Chi chay sau khi duoc giao nang development (duyet 24/11/2026); khong tu ap dung.
if(-not $Apply){throw 'Cần được giao nâng development trước khi chạy -Apply.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/promotion-mot-mon-mot-km/runs/dev044_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database='CoffeeManager_Development';Status='Running';Backup=$null}
$connection=$null
# Hash JSON o SQL (khong doc chunk dau) tren du lieu nghiep vu lien quan; khong ghi credential.
$snapshotSql=@'
SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
SELECT (SELECT MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,PhiShip,TongTien,GiamGia,ThanhTien,TrangThai,
  NgayThanhToan,PhanTramPhiDoiTac,PhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) Invoices,
(SELECT * FROM dbo.CHITIET_HD ORDER BY MaHD,MaDong FOR JSON PATH,INCLUDE_NULL_VALUES) Lines,
(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaHD,MaPT FOR JSON PATH,INCLUDE_NULL_VALUES) Payments,
(SELECT * FROM dbo.HoaDonUuDaiSnapshot ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) DiscountSnapshots,
(SELECT * FROM dbo.HoaDonUuDaiApDung ORDER BY MaHD,MaKM FOR JSON PATH,INCLUDE_NULL_VALUES) AppliedPromotions,
(SELECT MaKM,TenKM,LoaiUuDai,PhamVi,DoiTuongKhach,PhanTramGiam,NgayBD,NgayKT,TrangThai FROM dbo.KHUYENMAI ORDER BY MaKM FOR JSON PATH,INCLUDE_NULL_VALUES) Promotions,
(SELECT MaKM,MaSP FROM dbo.KHUYENMAI_MON ORDER BY MaKM,MaSP FOR JSON PATH) PromotionItems,
(SELECT MaKM,MaSP,SoLuong FROM dbo.KHUYENMAI_MON_TANG ORDER BY MaKM,MaSP FOR JSON PATH) PromotionGifts,
(SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,
(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Inventory,
(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES) Consumption,
(SELECT MaGD,MaHD,MaKH,NgayGD,Loai,SoDiem,LyDo FROM dbo.LICHSU_DIEM ORDER BY MaGD FOR JSON PATH,INCLUDE_NULL_VALUES) PointHistory
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER))),2);
'@
try {
 if(Get-Process -Name 'CafeManager.WinForms' -ErrorAction SilentlyContinue){throw 'Đóng app development trước; script không tự dừng process.'}
 $connection=Open-TestConnection 'CoffeeManager_Development'
 if((Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND SchemaReady=1") -ne 1){throw 'Development marker không khớp'}
 $version=[int](Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations')
 if($version -ne 43){throw 'Cần development đúng schema 043 trước khi nâng 044.'}
 $manifest['BeforeVersion']=$version; $manifest['BeforeHash']=Read-TestScalar $connection $snapshotSql
 # Xác minh checksum 1..43 trước backup/nâng.
 for($v=1;$v -le 43;$v++) {
  $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v)); if($file.Count -ne 1){throw "Thiếu migration $v"}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 $backupDir=[string](Read-TestScalar $connection "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
 if([string]::IsNullOrWhiteSpace($backupDir)){throw 'Thiếu đường dẫn backup SQL; không nâng schema'}
 $backupFile=[IO.Path]::Combine($backupDir,"CoffeeManager_Development_before044_$runId.bak")
 $backupManifest=Join-Path $env:LOCALAPPDATA "CoffeeManager/backups/before044_$runId.json"
 $backupTool=Join-Path $env:LOCALAPPDATA 'CoffeeManager/backups/tools/Backup-Database.ps1'
 if(-not (Test-Path -LiteralPath $backupTool)){ $backupTool=Join-Path $PSScriptRoot 'Backup-Database.ps1' }
 & $backupTool -Database 'CoffeeManager_Development' -BackupFile $backupFile -ManifestPath $backupManifest | Out-Null
 $manifest.Backup=@{Path=$backupFile;Manifest=$backupManifest;Kind='COPY_ONLY CHECKSUM + VERIFYONLY';Policy='Giữ đến khi nghiệm thu; không tự xóa'}
 $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8
 $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter '044_*.sql'); if($file.Count -ne 1){throw 'Thiếu migration 044'}
 & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version 44 -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 $manifest['AfterVersion']=[int](Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations')
 $manifest['AfterHash']=Read-TestScalar $connection $snapshotSql
 if($manifest.BeforeHash -cne $manifest.AfterHash){throw 'Dữ liệu gốc thay đổi; giữ backup và điều tra trước khi dùng app'}
 $manifest['Objects']=[ordered]@{
  fn_UuDaiChon=(Read-TestScalar $connection "SELECT OBJECT_ID(N'dbo.fn_UuDaiChon',N'TF')")
  Constraint=(Read-TestScalar $connection "SELECT OBJECT_ID(N'dbo.CK_DiscountSnapshot_Known',N'C')")
  Grants=[int](Read-TestScalar $connection @"
SELECT COUNT(*) FROM sys.database_permissions p
JOIN sys.database_principals r ON r.principal_id=p.grantee_principal_id
WHERE r.name='CafeManagerApp' AND p.permission_name='EXECUTE' AND p.state IN('G','W')
 AND p.major_id IN(OBJECT_ID(N'dbo.sp_DiscountPreview'),OBJECT_ID(N'dbo.sp_ThanhToanHoaDon'),
  OBJECT_ID(N'dbo.sp_InvoiceDetail'),OBJECT_ID(N'dbo.sp_SalesReadCore'));
"@)
  LegacyPaidMatch=[int](Read-TestScalar $connection @"
SELECT COUNT(*) FROM (
 SELECT TOP(5) h.MaHD,h.GiamGia,(SELECT COALESCE(SUM(d.Discount),0) FROM dbo.fn_HoaDonGiamGia(h.MaHD) d) Calc
 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' ORDER BY h.MaHD DESC) x
WHERE x.GiamGia<>x.Calc;
"@)
 }
 if($manifest.Objects.fn_UuDaiChon -eq $null -or $manifest.Objects.Constraint -eq $null){throw 'Thiếu object 044 sau nâng.'}
 if($manifest.Objects.Grants -ne 4){throw 'Thiếu GRANT EXECUTE cho CafeManagerApp.'}
 if($manifest.Objects.LegacyPaidMatch -ne 0){throw 'Hóa đơn đã trả lệch giảm giá sau nâng.'}
 $manifest.Status='Succeeded'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($null -ne $connection){$connection.Dispose()};$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $output 'manifest.json') -Encoding utf8;Write-Output "Development evidence: $output"}
