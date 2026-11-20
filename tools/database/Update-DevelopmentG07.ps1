[CmdletBinding()]param([Parameter(Mandatory=$true)][switch]$Apply)
# Chỉ chạy sau khi được giao nâng development; chuẩn bị review không tự áp dụng.
if(-not $Apply){throw 'Cần được giao nâng development trước khi chạy -Apply.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$target=if(Test-Path (Join-Path $root 'database/migrations/027_asset_write_lock.sql')){27}else{26}
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/developmentG07_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database='CoffeeManager_Development';Status='Running';FixtureWritten=$false;AccountReset=$false;Backup=$null}
$connection=$null
# Hash ở SQL bao trọn JSON, không đọc riêng chunk đầu; không ghi credential vào artifact.
$snapshotSql=@'
SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
SELECT (SELECT MaNV,TenNV,SDT,LoaiHopDong,LuongCung,LuongTheoGio,MaCV,DangLamViec FROM dbo.NHANVIEN ORDER BY MaNV FOR JSON PATH,INCLUDE_NULL_VALUES) Employees,
(SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES) Accounts,
(SELECT MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai FROM dbo.CHAMCONG ORDER BY MaNV,NgayLam,MaCa FOR JSON PATH,INCLUDE_NULL_VALUES) Attendance,
(SELECT MaBL,MaNV,Thang,Nam,NgayChot,LuongCoBan,SoGioLam,DonGiaGio,TienTangCa,PhuCap,Thuong,KhauTru,ThucLinh FROM dbo.BANGLUONG ORDER BY MaBL FOR JSON PATH,INCLUDE_NULL_VALUES) Payroll,
(SELECT * FROM dbo.PHIEUTHUCHI ORDER BY MaTC FOR JSON PATH,INCLUDE_NULL_VALUES) Finance,
(SELECT * FROM dbo.CONGNO_NCC ORDER BY MaCN FOR JSON PATH,INCLUDE_NULL_VALUES) Debts,
(SELECT * FROM dbo.THANHTOAN_CONGNO ORDER BY MaTT FOR JSON PATH,INCLUDE_NULL_VALUES) DebtPayments,
(SELECT MaTS,TenTS,NgayMua,GiaTri,SoThangKH FROM dbo.TAISAN_CODINH ORDER BY MaTS FOR JSON PATH) Assets,
(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Inventory,
(SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,
(SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Invoices
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER))),2);
'@
try {
 if(Get-Process -Name 'CafeManager.WinForms' -ErrorAction SilentlyContinue){throw 'Đóng app development trước; script không tự dừng process.'}
 $connection=Open-TestConnection 'CoffeeManager_Development'
 if((Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1") -ne 1){throw 'Development marker không khớp'}
 $version=[int](Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations')
 if($version -lt 24 -or $version -gt $target){throw 'Cần development đã được duyệt nâng G06/schema024; G07 chỉ nâng024 đến target đã chuẩn bị'}
 $manifest['BeforeVersion']=$version; $manifest['BeforeHash']=Read-TestScalar $connection $snapshotSql
 # Xác minh checksum các migration đã áp dụng trước backup/nâng phiên bản.
 for($v=1;$v -le $version;$v++) {
  $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v)); if($file.Count -ne 1){throw "Thiếu migration$v"}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 if($version -lt $target) {
  $backupDir=[string](Read-TestScalar $connection "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
  if([string]::IsNullOrWhiteSpace($backupDir)){throw 'Thiếu đường backup SQL; không nâng schema'}
  $backupFile=[IO.Path]::Combine($backupDir,"CoffeeManager_Development_before_G07_$runId.bak")
  $cmd=$connection.CreateCommand();$cmd.CommandText='BACKUP DATABASE [CoffeeManager_Development] TO DISK=@path WITH COPY_ONLY,CHECKSUM;';$cmd.CommandTimeout=180
  [void]$cmd.Parameters.Add('@path',[Data.SqlDbType]::NVarChar,4000);$cmd.Parameters['@path'].Value=$backupFile
  try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
  $manifest.Backup=@{Path=$backupFile;Kind='COPY_ONLY CHECKSUM';Policy='Giữ đến khi nghiệm thu/chấp nhận bản development; không tự xóa'}
  $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8
 }
 for($v=$version+1;$v -le $target;$v++) {
  $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw "Thiếu migration$v"}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 $manifest['AfterVersion']=Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations'; $manifest['AfterHash']=Read-TestScalar $connection $snapshotSql
 if($manifest.BeforeHash -cne $manifest.AfterHash){throw 'Dữ liệu gốc thay đổi; giữ backup và điều tra trước khi dùng app'}
 $manifest.Status='Succeeded'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($null -ne $connection){$connection.Dispose()};$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $output 'manifest.json') -Encoding utf8;Write-Output "Development evidence: $output"}

