[CmdletBinding()]param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/development016_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database='CoffeeManager_Development';Status='Running';FixtureWritten=$false;AccountReset=$false;Backup=$null}
function Save-DevelopmentManifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
Save-DevelopmentManifest
$connection=$null
try {
 if(Get-Process -Name 'CafeManager.WinForms' -ErrorAction SilentlyContinue){throw 'Development app is running; close it before applying schema. No process was stopped.'}
 $connection=Open-TestConnection 'CoffeeManager_Development'
 if((Read-TestScalar $connection "SELECT CASE WHEN OBJECT_ID(N'dbo.DevelopmentMarker') IS NULL THEN 0 ELSE 1 END") -ne 1){throw 'Development marker missing'}
 if((Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1") -ne 1){throw 'Development marker mismatch'}
 $manifest['BeforeVersion']=Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations'
 $manifest['BeforeInventory']=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 $manifest['BeforePoints']=[string](Read-TestScalar $connection 'SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)')
 $manifest['BeforeInvoices']=[string](Read-TestScalar $connection 'SELECT (SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH)')
 $manifest['BeforeAccounts']=[string](Read-TestScalar $connection "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES))),2)")
 if([int]$manifest.BeforeVersion -lt 16){
  $backupDir=[string](Read-TestScalar $connection "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
  if([string]::IsNullOrWhiteSpace($backupDir)){throw 'SQL backup directory unavailable; no migration applied'}
  $backupFile=[IO.Path]::Combine($backupDir,"CoffeeManager_Development_before_G04_016_$runId.bak")
  $cmd=$connection.CreateCommand();$cmd.CommandText='BACKUP DATABASE [CoffeeManager_Development] TO DISK=@path WITH COPY_ONLY,CHECKSUM;';$cmd.CommandTimeout=180
  [void]$cmd.Parameters.Add('@path',[Data.SqlDbType]::NVarChar,4000);$cmd.Parameters['@path'].Value=$backupFile
  try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
  $manifest.Backup=@{Path=$backupFile;Policy='Giữ đến khi G04 nghiệm thu và người dùng chấp nhận bản development; chưa tự xóa';Kind='SQL COPY_ONLY CHECKSUM'};Save-DevelopmentManifest
 }
 if([int]$manifest.BeforeVersion -lt 15 -or [int]$manifest.BeforeVersion -gt 16){throw 'Expected G04 schema015 or016; no migration applied'}
 for($v=1;$v -le 16;$v++){
  $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v))
  if($file.Count -ne 1){throw "Missing migration $v"}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 $manifest['AfterVersion']=Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations'
 $manifest['AfterInventory']=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 $manifest['AfterPoints']=[string](Read-TestScalar $connection 'SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)')
 $manifest['AfterInvoices']=[string](Read-TestScalar $connection 'SELECT (SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH)')
 $manifest['AfterAccounts']=[string](Read-TestScalar $connection "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES))),2)")
 if($manifest.BeforeAccounts -cne $manifest.AfterAccounts -or $manifest.BeforeInventory -cne $manifest.AfterInventory -or $manifest.BeforePoints -cne $manifest.AfterPoints -or $manifest.BeforeInvoices -cne $manifest.AfterInvoices){throw 'Development balance/history comparison failed; retain backup, investigate before use'}
 $manifest.Status='Succeeded';Write-Output 'Development schema016; inventory, points/tiers and invoice finances preserved. No seed/account reset/acceptance fixture.'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($null-ne $connection){$connection.Dispose()};Save-DevelopmentManifest;Write-Output "Development evidence: $output"}



