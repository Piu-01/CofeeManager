[CmdletBinding()]param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/development_$runId"
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
 if([int]$manifest.BeforeVersion -lt 9){
  $backupDir=[string](Read-TestScalar $connection "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
  if([string]::IsNullOrWhiteSpace($backupDir)){throw 'SQL backup directory unavailable; no migration applied'}
  $backupFile=[IO.Path]::Combine($backupDir,"CoffeeManager_Development_before_G03_$runId.bak")
  $cmd=$connection.CreateCommand();$cmd.CommandText='BACKUP DATABASE [CoffeeManager_Development] TO DISK=@path WITH COPY_ONLY,CHECKSUM;';$cmd.CommandTimeout=180
  [void]$cmd.Parameters.Add('@path',[Data.SqlDbType]::NVarChar,4000);$cmd.Parameters['@path'].Value=$backupFile
  try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
  $manifest.Backup=@{Path=$backupFile;Policy='Giữ đến khi G03 nghiệm thu và người dùng chấp nhận bản development; chưa tự xóa';Kind='SQL COPY_ONLY CHECKSUM'};Save-DevelopmentManifest
 }
 $files=@('004_inventory_materials.sql','005_products_recipes.sql','006_tables_areas.sql','007_customers.sql','008_promotions_loyalty.sql','009_promotion_legacy_codes.sql')
 for($v=4;$v -le 9;$v++){
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database 'CoffeeManager_Development' -Version $v -File (Join-Path $root ('database/migrations/'+$files[$v-4])) | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 $manifest['AfterVersion']=Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations'
 $manifest['AfterInventory']=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 $manifest['AfterPoints']=[string](Read-TestScalar $connection 'SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)')
 $manifest['AfterInvoices']=[string](Read-TestScalar $connection 'SELECT (SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH)')
 if($manifest.BeforeInventory -cne $manifest.AfterInventory -or $manifest.BeforePoints -cne $manifest.AfterPoints -or $manifest.BeforeInvoices -cne $manifest.AfterInvoices){throw 'Development balance/history comparison failed; retain backup, investigate before use'}
 $manifest.Status='Succeeded';Write-Output 'Development schema009; inventory, points/tiers and invoice finances preserved. No seed/account reset/acceptance fixture.'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($null-ne $connection){$connection.Dispose()};Save-DevelopmentManifest;Write-Output "Development evidence: $output"}

