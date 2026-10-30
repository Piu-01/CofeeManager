$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$upgrade=Get-Content "$root/docs/tasks/g05-kho-nguon-chi-cong-no/runs/development021_20261027_112555_5d4d8de7/manifest.json" -Raw|ConvertFrom-Json
if($upgrade.Database -cne 'CoffeeManager_Development' -or $upgrade.Status -ne 'Succeeded'){throw 'Unexpected approved target'}
$backup=[string]$upgrade.Backup.Path
if($backup -cne 'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\CoffeeManager_Development_before_G05_021_20261027_112555_5d4d8de7.bak'){throw 'Wrong backup'}
$name='CoffeeManager_Test_G05_'+(Get-Date -Format yyyyMMdd_HHmmss)+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)+'_backup'
$manifest=[ordered]@{Database=$name;Backup=$backup;Purpose='Compare complete server-side hashes with approved pre-upgrade backup; no live data changes';Created=$false;Cleaned=$false;Comparisons=@()}
function Save {$manifest|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/development-backup-verification.json" -Encoding utf8}
function ReadTable($connection,[string]$sql){$cmd=$connection.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180;try{$a=[Data.SqlClient.SqlDataAdapter]::new($cmd);$t=[Data.DataTable]::new();[void]$a.Fill($t);return ,$t}finally{$cmd.Dispose()}}
$master=Open-TestConnection master;$copy=$null;$live=$null
try{
 if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name='$name'") -ne 0){throw 'Test target exists'}
 $header=ReadTable $master "RESTORE HEADERONLY FROM DISK=N'$backup';"
 if($header.Rows.Count -ne 1 -or $header.Rows[0].DatabaseName -cne 'CoffeeManager_Development'){throw 'Backup identity mismatch'}
 $files=ReadTable $master "RESTORE FILELISTONLY FROM DISK=N'$backup';"
 if($files.Rows.Count -ne 2 -or @($files.Rows|Where-Object Type -notin @('D','L')).Count){throw 'Unexpected backup file layout'}
 $data=[string](Read-TestScalar $master "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))")
 $log=[string](Read-TestScalar $master "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'))")
 $moves=@($files.Rows|ForEach-Object{$dir=if($_.Type -eq 'D'){$data}else{$log};$ext=if($_.Type -eq 'D'){'.mdf'}else{'.ldf'};"MOVE N'$($_.LogicalName)' TO N'$([IO.Path]::Combine($dir,$name+$ext))'"})
 Run-TestSql $master "RESTORE DATABASE [$name] FROM DISK=N'$backup' WITH $($moves -join ','),RECOVERY,CHECKSUM;"
 $manifest.Created=$true;Save
 $copy=Open-TestConnection $name;$live=Open-TestConnection CoffeeManager_Development
 if((Read-TestScalar $copy 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -ne 16 -or (Read-TestScalar $live 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -ne 21){throw 'Schema identity mismatch'}
 $queries=[ordered]@{
  Inventory='SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES'
  Points='SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH,INCLUDE_NULL_VALUES'
  Accounts='SELECT * FROM dbo.TAIKHOAN ORDER BY MaTK FOR JSON PATH,INCLUDE_NULL_VALUES'
  Invoices='SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES'
  Finance='SELECT MaTC,NgayLap,MaDMTC,MaNV,MaTS,SoTien,GhiChu FROM dbo.PHIEUTHUCHI ORDER BY MaTC FOR JSON PATH,INCLUDE_NULL_VALUES'
  Purchases='SELECT MaPN,NgayNhap,MaNV,MaNCC,TongTien FROM dbo.PHIEUNHAP ORDER BY MaPN FOR JSON PATH,INCLUDE_NULL_VALUES'
  Debts='SELECT * FROM dbo.CONGNO_NCC ORDER BY MaCN FOR JSON PATH,INCLUDE_NULL_VALUES'
  Payments='SELECT * FROM dbo.THANHTOAN_CONGNO ORDER BY MaTT FOR JSON PATH,INCLUDE_NULL_VALUES'
 }
 foreach($part in $queries.Keys){$sql="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),($($queries[$part])))),2)";$before=[string](Read-TestScalar $copy $sql);$after=[string](Read-TestScalar $live $sql);$manifest.Comparisons+=@{Part=$part;BeforeHash=$before;AfterHash=$after;Equal=($before -ceq $after)}}
 $manifest.Status=if(@($manifest.Comparisons|Where-Object Equal -eq $false).Count){'FAIL'}else{'PASS'}
}finally{
 if($copy){$copy.Dispose()};if($live){$live.Dispose()};[Data.SqlClient.SqlConnection]::ClearAllPools()
 if($manifest.Created){if($name -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_backup$'){throw 'Cleanup target mismatch'};Run-TestSql $master "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name];";$manifest.Cleaned=$true}
 $master.Dispose();Save
}
Write-Output "Backup compare=$($manifest.Status); parts=$($manifest.Comparisons.Count); copy cleaned=$($manifest.Cleaned)"
