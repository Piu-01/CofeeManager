$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_G06_${runId}_legacy"
$result=[ordered]@{RunId=$runId;Database=$name;Cases=@();Created=@();Cleaned=@();Retained=@()}
function Save{$result|ConvertTo-Json -Depth 10|Set-Content (Join-Path $PSScriptRoot 'legacy-results.json') -Encoding utf8}
function Migrate([int]$v){$file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v)); & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version $v -File $file[0].FullName}
function RejectMigration([int]$v,[string]$case,[int]$expected){$err=$null;try{Migrate $v|Out-Null}catch{$err=$_.Exception.ToString()};$count=Read-TestScalar $c "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version=$v"; $column= switch($v){22{'Revision'}23{'Revision'}24{'SnapshotKind'}};$table=switch($v){22{'NHANVIEN'}23{'CHAMCONG'}24{'BANGLUONG'}};$col=Read-TestScalar $c "SELECT COL_LENGTH('dbo.$table','$column')";$pass=$err -and $err.Contains([string]$expected) -and $count -eq 0 -and $col -is [DBNull];$result.Cases+=@{Case=$case;Expected=$expected;Error=$err;MigrationRows=$count;ColumnExists=($col -isnot [DBNull]);Status=$(if($pass){'PASS'}else{'FAIL'})};Save}
$c=$null
try{
 $c=Open-TestConnection 'master';if(Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'"){throw 'Exists'};Run-TestSql $c "CREATE DATABASE [$name]";$result.Created+=@{Database=$name;Marker=$runId};$c.ChangeDatabase($name);Run-TestSql $c "CREATE TABLE dbo.G06TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G06TestMarker VALUES('$runId')";Save
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v -le 21;$v++){Migrate $v|Out-Null}
 Run-TestSql $c "UPDATE dbo.NHANVIEN SET TenNV=N'' WHERE MaNV='NV010'";RejectMigration 22 'EmptyLegacyName' 53107;Run-TestSql $c "UPDATE dbo.NHANVIEN SET TenNV=N'Fixture legacy' WHERE MaNV='NV010'"
 Run-TestSql $c "UPDATE dbo.NHANVIEN SET SDT='abc' WHERE MaNV='NV010'";RejectMigration 22 'LegacyPhone' 53107;Run-TestSql $c "UPDATE dbo.NHANVIEN SET SDT=NULL WHERE MaNV='NV010'"
 Run-TestSql $c "UPDATE dbo.NHANVIEN SET DangLamViec=0 WHERE MaNV='NV001'";RejectMigration 22 'RetiredActiveAccount' 53107;Run-TestSql $c "UPDATE dbo.NHANVIEN SET DangLamViec=1 WHERE MaNV='NV001'"
 Migrate 22|Out-Null
 Run-TestSql $c "INSERT dbo.CHAMCONG(MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai) VALUES('NV010','20260810','CA001','20260810 08:00','20260810 16:00',N'Di lam'),('NV010','20260810','CA002','20260810 09:00','20260810 17:00',N'Di lam')";RejectMigration 23 'LegacyOverlap' 53207;Run-TestSql $c "DELETE dbo.CHAMCONG WHERE MaNV='NV010' AND NgayLam='20260810'";Migrate 23|Out-Null
 Migrate 24|Out-Null
 $already=Migrate 24;$copy=Join-Path $PSScriptRoot '024_checksum_fixture.sql';$orig=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter '024_*.sql')[0];[IO.File]::WriteAllText($copy,[IO.File]::ReadAllText($orig.FullName)+"`r`n-- isolated checksum fixture`r`n")
 $err=$null;try{& (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version 24 -File $copy|Out-Null}catch{$err=$_.Exception.Message}
 $result.Cases+=@{Case='RerunChecksum';Status=$(if($already.Status -eq 'AlreadyApplied' -and $err -like '*different checksum*'){'PASS'}else{'FAIL'});Rerun=$already.Status;Mismatch=$err}
 $result.Status='Completed'
}catch{$result.Status='Error';$result.Error=$_.Exception.ToString()}
finally{
 if($c){$c.Dispose()};if($result.Created.Count){try{$c=Open-TestConnection $name;if($name -notmatch '^CoffeeManager_Test_G06_\d{8}_\d{6}_[a-f0-9]{8}_legacy$' -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G06TestMarker') -cne $runId){throw 'Cleanup marker'};$c.ChangeDatabase('master');Run-TestSql $c "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]";$result.Cleaned+=@($name)}catch{$result.Retained+=@{Database=$name;Reason=$_.Exception.Message}}finally{if($c){$c.Dispose()}}};Save
}
$result|ConvertTo-Json -Depth 3

