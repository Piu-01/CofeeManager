[CmdletBinding()]param([string]$RunId)
# Creates an owned G04 UI-test database as a COPY_ONLY backup/restore of the bootstrapped
# development database, then applies migrations 32..35 on the copy only.
# ASCII-only on purpose: Windows PowerShell 5.1 reads no-BOM scripts as ANSI.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
if([string]::IsNullOrWhiteSpace($RunId)){$RunId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)}
$name="CoffeeManager_Test_G04_${RunId}_ui"
$output=Join-Path $root "docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/ui_${RunId}"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$RunId;Database=$name;Server='localhost';Source='CoffeeManager_Development';SourceVersion=$null;SchemaVersion=35;Status='Running'}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
Save-Manifest
$master=$null
try {
 $master=Open-TestConnection 'master'
 if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target already exists'}
 $dev=Open-TestConnection 'CoffeeManager_Development'
 try {
  $manifest.SourceVersion=Read-TestScalar $dev 'SELECT MAX(Version) FROM dbo.SchemaMigrations'
  $accountsBefore=[string](Read-TestScalar $dev "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES))),2)")
  $inventoryBefore=[string](Read-TestScalar $dev 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
  $pointsBefore=[string](Read-TestScalar $dev 'SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)')
  $invoicesBefore=[string](Read-TestScalar $dev 'SELECT (SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH)')
 } finally { $dev.Dispose() }
 $backupDir=[string](Read-TestScalar $master "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
 $dataDir=[string](Read-TestScalar $master "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))")
 $logDir=[string](Read-TestScalar $master "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'))")
 if([string]::IsNullOrWhiteSpace($backupDir) -or [string]::IsNullOrWhiteSpace($dataDir)){throw 'SQL default paths unavailable'}
 $bak=[IO.Path]::Combine($backupDir,"G04Ui_$RunId.bak")
 $manifest.Backup=$bak
 Run-TestSql $master "BACKUP DATABASE [CoffeeManager_Development] TO DISK=N'$bak' WITH COPY_ONLY,INIT,CHECKSUM;"
 $readerCmd=$master.CreateCommand(); $readerCmd.CommandText="RESTORE FILELISTONLY FROM DISK=N'$bak';"
 $reader=$readerCmd.ExecuteReader()
 $logicalData=$null; $logicalLog=$null
 while($reader.Read()){
  $logical=$reader.GetString(0); $type=$reader.GetString(2)
  if($type -eq 'D' -and $null -eq $logicalData){$logicalData=$logical}
  if($type -eq 'L' -and $null -eq $logicalLog){$logicalLog=$logical}
 }
 $reader.Close(); $readerCmd.Dispose()
 if($null -eq $logicalData -or $null -eq $logicalLog){throw 'Backup file list incomplete'}
 $dataFile=[IO.Path]::Combine($dataDir,"$name.mdf"); $logFile=[IO.Path]::Combine($logDir,"${name}_log.ldf")
 Run-TestSql $master "RESTORE DATABASE [$name] FROM DISK=N'$bak' WITH MOVE N'$logicalData' TO N'$dataFile',MOVE N'$logicalLog' TO N'$logFile',RECOVERY;"
 $conn=Open-TestConnection $name
 try {
  Run-TestSql $conn "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$RunId');"
  $inventoryCopy=[string](Read-TestScalar $conn 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
  if($inventoryCopy -cne $inventoryBefore){throw 'Restored copy inventory differs from source'}
  for($version=32;$version -le 35;$version++) {
   $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
  }
  if($inventoryBefore -cne [string](Read-TestScalar $conn 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')){throw 'Migration changed inventory on copy'}
  if($pointsBefore -cne [string](Read-TestScalar $conn 'SELECT (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH)')){throw 'Migration changed points on copy'}
  if($invoicesBefore -cne [string](Read-TestScalar $conn 'SELECT (SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH)')){throw 'Migration changed invoice finances on copy'}
  $accountsAfter=[string](Read-TestScalar $conn "SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES))),2)")
  if($accountsAfter -cne $accountsBefore){throw 'Migration changed accounts on copy'}
  $manifest['OpenOrders']=[int](Read-TestScalar $conn "SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')")
  $manifest['OpenSessions']=[int](Read-TestScalar $conn "SELECT COUNT(*) FROM dbo.PHIEN_BAN WHERE GioRa IS NULL")
  $manifest['Accounts']=[string](Read-TestScalar $conn "SELECT (SELECT TenDangNhap,HoatDong FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH)")
  $manifest['SchemaVersionNow']=Read-TestScalar $conn 'SELECT MAX(Version) FROM dbo.SchemaMigrations'
 } finally { $conn.Dispose() }
 $manifest.Status='Succeeded'
} catch { $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString() }
finally { if($null -ne $master){$master.Dispose()} }
Save-Manifest
Write-Output "UI test database: $name; status=$($manifest.Status); evidence=$output"
if($manifest.Status -ne 'Succeeded'){throw 'UI test database creation failed'}
