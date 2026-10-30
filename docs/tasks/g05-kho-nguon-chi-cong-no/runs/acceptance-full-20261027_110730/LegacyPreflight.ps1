param([ValidateSet('Mapping17','Empty18')][string]$Case)
$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$upgrade=Get-Content "$root/docs/tasks/g05-kho-nguon-chi-cong-no/runs/development021_20261027_112555_5d4d8de7/manifest.json" -Raw|ConvertFrom-Json
if($upgrade.Database -cne 'CoffeeManager_Development' -or $upgrade.Status -ne 'Succeeded'){throw 'Unexpected approved target'}
$backup=[string]$upgrade.Backup.Path
if($backup -cne 'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\CoffeeManager_Development_before_G05_021_20261027_112555_5d4d8de7.bak'){throw 'Wrong backup'}
$name='CoffeeManager_Test_G05_'+(Get-Date -Format yyyyMMdd_HHmmss)+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)+'_backup'
$manifest=[ordered]@{Database=$name;Backup=$backup;Purpose='Compare complete server-side hashes with approved pre-upgrade backup; no live data changes';Created=$false;Cleaned=$false;Comparisons=@()}
function Save {$manifest|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/legacy-${Case}-results.json" -Encoding utf8}
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
 $copy=Open-TestConnection $name
 if((Read-TestScalar $copy 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -ne 16){throw 'Expected schema16 backup'}
 $target=17;$expected=52607;$column='MaPN';$table='PHIEUTHUCHI'
 if($Case -eq 'Mapping17'){Run-TestSql $copy "DELETE dbo.SystemExpenseCategory WHERE UseCase='SupplierDebt';"}
 else{
  $m17=Get-ChildItem "$root/database/migrations/017_*.sql";& "$root/tools/database/Apply-Migration.ps1" -Database $name -Version 17 -File $m17.FullName|Out-Null
  Run-TestSql $copy "INSERT dbo.PHIEUNHAP(MaPN,NgayNhap,MaNV,MaNCC,TongTien) VALUES('PN99000001','20261027','NV001','NCC01',0);"
  $target=18;$expected=52707;$column='DaChot';$table='PHIEUNHAP'
 }
 $snapshotSql="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaTC,SoTien FROM dbo.PHIEUTHUCHI ORDER BY MaTC FOR JSON PATH) Finance,(SELECT * FROM dbo.CONGNO_NCC ORDER BY MaCN FOR JSON PATH) Debts FOR JSON PATH))),2)"
 $before=[string](Read-TestScalar $copy $snapshotSql);$numbers=@();$message='No error'
 $migration=Get-ChildItem "$root/database/migrations/$('{0:d3}' -f $target)_*.sql"
 try{& "$root/tools/database/Apply-Migration.ps1" -Database $name -Version $target -File $migration.FullName|Out-Null}catch{$err=$_.Exception;$message=$err.Message;while($err -and $err -isnot [Data.SqlClient.SqlException]){$err=$err.InnerException};if($err){$numbers=@($err.Errors|ForEach-Object Number)}}
 $after=[string](Read-TestScalar $copy $snapshotSql);$versionCount=Read-TestScalar $copy "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version=$target";$col=Read-TestScalar $copy "SELECT CASE WHEN COL_LENGTH('dbo.$table','$column') IS NULL THEN 0 ELSE 1 END"
 $manifest.Case=$Case;$manifest.Expected=$expected;$manifest.Observed=$numbers;$manifest.Message=$message;$manifest.BeforeHash=$before;$manifest.AfterHash=$after;$manifest.VersionRecorded=$versionCount;$manifest.ColumnAdded=$col
 $manifest.Status=if($expected -in $numbers -and $before -ceq $after -and $versionCount -eq 0 -and $col -eq 0){'PASS'}else{'FAIL'}
}finally{
 if($copy){$copy.Dispose()};if($live){$live.Dispose()};[Data.SqlClient.SqlConnection]::ClearAllPools()
 if($manifest.Created){if($name -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_backup$'){throw 'Cleanup target mismatch'};Run-TestSql $master "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name];";$manifest.Cleaned=$true}
 $master.Dispose();Save
}
Write-Output "Backup compare=$($manifest.Status); parts=$($manifest.Comparisons.Count); copy cleaned=$($manifest.Cleaned)"

