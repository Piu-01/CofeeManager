$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$bad=$m.Database.Replace('_demo','_clean');$results=[Collections.Generic.List[object]]::new()
$migrationFiles=Get-ChildItem "$root/database/migrations" -Filter '*.sql'|Sort-Object Name
function AddResult($id,$expected,$actual,$status){$results.Add(@{Id=$id;Expected=$expected;Actual=$actual;Status=$status});$results|ConvertTo-Json -Depth 5|Set-Content "$out/migration-results.json" -Encoding utf8}
function Apply($db,$v){& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $migrationFiles[$v-1].FullName}
function Reject($v,$number){$observed=$false;try{Apply $bad $v|Out-Null}catch{$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException};if($e -and $number -in @($e.Errors|ForEach-Object Number)){$observed=$true}else{throw}};if(-not $observed){throw 'Expected migration rejection missing'}}
$c=Open-TestConnection $m.Database;$admin=Open-TestConnection master;$created=$false
try{
 $before=Read-TestScalar $c "SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)"
 $rerun=Apply $m.Database 4
 $after=Read-TestScalar $c "SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)"
 AddResult Migration-rerun 'AlreadyApplied and all inventory balances unchanged' $rerun.Status $(if($rerun.Status -eq 'AlreadyApplied' -and $before -ceq $after){'PASS'}else{'FAIL'})
 $copy=Join-Path $out 'checksum-negative.sql';[IO.File]::WriteAllText($copy,[IO.File]::ReadAllText($migrationFiles[8].FullName)+"`n-- checksum negative fixture")
 $rejected=$false;try{& "$root/tools/database/Apply-Migration.ps1" -Database $m.Database -Version 9 -File $copy|Out-Null}catch{if($_.Exception.Message -like '*different checksum*'){$rejected=$true}else{throw}}
 AddResult Migration-checksum 'Changed checksum rejected before DDL' "$rejected" $(if($rejected){'PASS'}else{'FAIL'})
 if((Read-TestScalar $admin "SELECT COUNT(*) FROM sys.databases WHERE name=N'$bad'") -ne 0){throw 'Target exists'}
 Run-TestSql $admin "CREATE DATABASE [$bad]";$created=$true;$admin.ChangeDatabase($bad)
 Run-TestSql $admin "CREATE TABLE dbo.G03TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G03TestMarker VALUES('$($m.RunId)');"
 $schema=[regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','');Run-TestSql $admin $schema
 for($v=1;$v -le 3;$v++){Apply $bad $v|Out-Null}
 Run-TestSql $admin "INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh)VALUES('NVL01',N'',N'Kg');"
 Reject 4 51607
 $ok=(Read-TestScalar $admin "SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=4) AND OBJECT_ID('dbo.MaterialOpening') IS NULL AND EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE TenNVL=N'') THEN 1 ELSE 0 END")
 AddResult T09-preflight 'Invalid legacy name rejects004 without normalization/DDL/version' "Invariant=$ok" $(if($ok -eq 1){'PASS'}else{'FAIL'})
 Run-TestSql $admin "UPDATE dbo.NGUYENVATLIEU SET TenNVL=N'Fixture stage2' WHERE MaNVL='NVL01';";Apply $bad 4|Out-Null
 Run-TestSql $admin "INSERT dbo.DANHMUC_SP(MaDM,TenDM)VALUES('DM001',N'Fixture');INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai)VALUES('SP001',N'No recipe','DM001',10000,N'Dang ban');"
 Reject 5 51707
 $ok=Read-TestScalar $admin "SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=5) AND EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP001' AND TrangThai=N'Dang ban') THEN 1 ELSE 0 END"
 AddResult T10-preflight 'Selling legacy product without recipe rejects005; no auto stop' "Invariant=$ok" $(if($ok -eq 1){'PASS'}else{'FAIL'})
 Run-TestSql $admin "UPDATE dbo.SANPHAM SET TrangThai=N'Ngung ban' WHERE MaSP='SP001';";Apply $bad 5|Out-Null;Apply $bad 6|Out-Null
 Run-TestSql $admin "INSERT dbo.HANGTHANHVIEN(MaHang,DiemToiThieu,PhanTramGiam)VALUES('BAC',300,3);"
 Reject 7 51907
 $ok=Read-TestScalar $admin "SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=7) AND NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu=0) AND OBJECT_ID('dbo.CustomerOpening') IS NULL THEN 1 ELSE 0 END"
 AddResult T12-preflight 'Missing zero legacy tier rejects007; does not change policy' "Invariant=$ok" $(if($ok -eq 1){'PASS'}else{'FAIL'})
}catch{AddResult Migration-infrastructure 'Remaining checks execute' $_.Exception.Message 'FAIL';throw}
finally{
 $c.Dispose()
 if($created){try{if((Read-TestScalar $admin 'SELECT RunId FROM dbo.G03TestMarker') -cne $m.RunId){throw 'Marker mismatch'};$admin.ChangeDatabase('master');Run-TestSql $admin "ALTER DATABASE [$bad] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$bad];";@{Database=$bad;Marker=$m.RunId;Created=$true;Cleaned=$true}|ConvertTo-Json|Set-Content "$out/preflight-resource.json" -Encoding utf8}catch{@{Database=$bad;Created=$true;Cleaned=$false;Error=$_.Exception.Message}|ConvertTo-Json|Set-Content "$out/preflight-resource.json" -Encoding utf8}}
 $admin.Dispose()
}
