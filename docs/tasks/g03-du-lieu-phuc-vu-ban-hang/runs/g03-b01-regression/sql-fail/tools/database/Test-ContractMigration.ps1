#requires -Version 5.1
[CmdletBinding()]param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$cases=[Collections.Generic.List[object]]::new()
$connection=$null; $emptyConnection=$null; $database=$null; $empty=$null
$setupError=$null; $started=(Get-Date).ToString('o')
$directory=Join-Path $root 'docs\tasks\g01-nen-tang-ky-thuat-du-lieu\t03-contract-migration-cap-ma'
$history=Join-Path $directory 'runs'; [void][IO.Directory]::CreateDirectory($history)
function Save-Progress([string]$Stage) {
    $progress=[ordered]@{RunId=$runId;Stage=$Stage;Databases=@($database,$empty);Cases=@($cases.ToArray());UpdatedAt=(Get-Date).ToString('o')}
    [IO.File]::WriteAllText((Join-Path $history "$runId.progress.json"),($progress | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    Write-Host $Stage
}
function Case([string]$Id,[string]$Expected,[scriptblock]$Action) {
    Save-Progress "START $Id"
    $status='PASS'; $actual='Assertions passed.'
    try { & $Action } catch { $status='FAIL'; $actual=$_.Exception.Message }
    $cases.Add([pscustomobject]@{Id=$Id;Status=$status;Expected=$Expected;Actual=$actual})
    Save-Progress "$status $Id"
}
try {
    $database=New-IsolatedDatabase 'T03' $runId $true $false {param($name) $script:database=$name; Save-Progress "SETUP $name"}
    $connection=Open-TestConnection $database; Assert-TestTarget $connection 'T03'
    $before=Snapshot-TestRows $connection
    $migration=& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database
    Case 'T03-K01-seed' 'Migration preserves all baseline rows and trusted FK.' {
        if ($before -cne (Snapshot-TestRows $connection)) { throw 'Baseline rows changed.' }
        Run-TestSql $connection "IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 52001,'FK changed',1;"
    }
    $empty=New-IsolatedDatabase 'T03' $runId $false $true {param($name) $script:empty=$name; Save-Progress "SETUP $name"}
    $emptyConnection=Open-TestConnection $empty
    Case 'T03-K01-empty' 'Migration applies to empty schema.' {
        [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $empty)
        if ((Read-TestScalar $emptyConnection 'SELECT COUNT(*) FROM dbo.SchemaMigrations;') -ne 1) { throw 'Missing version.' }
    }
    Case 'T03-K01-rollback' 'Late DDL failure rolls back object and version.' {
        $temp=Join-Path ([IO.Path]::GetTempPath()) ("CoffeeManager_Migration_$runId.sql")
        try {
            [IO.File]::WriteAllText($temp,"CREATE TABLE dbo.T03_Fault(Id INT PRIMARY KEY);`nGO`nTHROW 52903,'Injected DDL failure',1;",[Text.Encoding]::UTF8)
            $failed=$false
            try { [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database -Version 2 -File $temp) } catch { $failed=$true }
            if (-not $failed) { throw 'Expected DDL failure.' }
            Run-TestSql $connection "IF OBJECT_ID(N'dbo.T03_Fault') IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=2) THROW 52001,'Partial migration',1;"
        } finally { if(Test-Path $temp) { Remove-Item -LiteralPath $temp } }
    }
    Case 'T03-K02-repeat' 'Repeat returns AlreadyApplied and retains sequence current value.' {
        $seq=Read-TestScalar $connection "SELECT COALESCE(CONVERT(BIGINT,current_value),0) FROM sys.sequences WHERE name='SeqHoaDon';"
        $repeat=& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database
        if($repeat.Status -ne 'AlreadyApplied') { throw 'Not idempotent.' }
        if($seq -ne (Read-TestScalar $connection "SELECT COALESCE(CONVERT(BIGINT,current_value),0) FROM sys.sequences WHERE name='SeqHoaDon';")) { throw 'Sequence changed on repeat.' }
    }
    Case 'T03-K02-checksum' 'Same version with altered bytes is refused.' {
        $temp=Join-Path ([IO.Path]::GetTempPath()) ("CoffeeManager_Checksum_$runId.sql")
        try {
            [IO.File]::WriteAllText($temp,'-- altered migration',[Text.Encoding]::UTF8)
            $refused=$false
            try { [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database -File $temp) } catch { if($_.Exception.Message -like '*checksum*') {$refused=$true} else {throw} }
            if(-not $refused) { throw 'Checksum mismatch accepted.' }
        } finally { if(Test-Path $temp) { Remove-Item -LiteralPath $temp } }
    }
    Case 'T03-K03-concurrent' 'Two overlapping sessions return 100 unique HD codes absent from seed.' {
        $other=Open-TestConnection $database
        $commands=@(); $readers=[Collections.Generic.List[object]]::new()
        try {
            foreach($c in @($connection,$other)) {
                $cmd=$c.CreateCommand(); $cmd.CommandTimeout=60
                $cmd.CommandText="WAITFOR DELAY '00:00:01'; CREATE TABLE #codes(Loai VARCHAR(20),Ma CHAR(10)); DECLARE @i INT=0; WHILE @i<50 BEGIN INSERT #codes EXEC dbo.sp_CapMa 'HOADON'; SET @i+=1; END; SELECT Ma FROM #codes;"
                $commands+=$cmd
            }
            $tasks=@($commands[0].ExecuteReaderAsync(),$commands[1].ExecuteReaderAsync())
            $codes=@()
            foreach($task in $tasks) {
                $reader=$task.GetAwaiter().GetResult(); $readers.Add($reader)
                while($reader.Read()) { $codes+=$reader.GetString(0) }
                $reader.Dispose(); [void]$readers.Remove($reader)
            }
            if($codes.Count -ne 100 -or @($codes | Select-Object -Unique).Count -ne 100) {throw 'Duplicate/missing concurrent codes.'}
            foreach($code in $codes) { if($code -notmatch '^HD\d{8}$' -or (Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='$code';") -ne 0) {throw 'Invalid/seed-colliding code.'} }
            $script:concurrentCodes=$codes
        } finally { foreach($r in $readers){$r.Dispose()}; foreach($cmd in $commands){$cmd.Dispose()}; $other.Dispose() }
    }
    Case 'T03-K04-limits' 'DM/NV last code accepted, next call fails explicitly; unknown source refused.' {
        foreach($source in @(@('SeqDanhMuc','DANHMUC_SP','DM999'),@('SeqNhanVien','NHANVIEN','NV999'))) {
            Run-TestSql $connection "ALTER SEQUENCE dbo.$($source[0]) RESTART WITH 999;"
            $cmd=$connection.CreateCommand(); $cmd.CommandText="EXEC dbo.sp_CapMa '$($source[1])';"
            try {$reader=$cmd.ExecuteReader(); try { [void]$reader.Read(); if($reader.GetString(1) -ne $source[2]){throw 'Last code incorrect.'} } finally {$reader.Dispose()} } finally {$cmd.Dispose()}
            Expect-TestSqlError $connection "EXEC dbo.sp_CapMa '$($source[1])';" 51302
        }
        Expect-TestSqlError $connection "EXEC dbo.sp_CapMa 'INVALID';" 51301
    }
    Case 'T03-K05-gap' 'Rolled back invoice absent; later code differs.' {
        Run-TestSql $connection "DECLARE @a TABLE(Loai VARCHAR(20),Ma CHAR(10)); BEGIN TRAN; INSERT @a EXEC dbo.sp_CapMa 'HOADON'; DECLARE @old CHAR(10)=(SELECT Ma FROM @a); INSERT dbo.HOADON(MaHD,MaNV,LoaiDon) VALUES(@old,'NV004',N'Mang di'); ROLLBACK; DECLARE @b TABLE(Loai VARCHAR(20),Ma CHAR(10)); INSERT @b EXEC dbo.sp_CapMa 'HOADON'; IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@old) OR (SELECT Ma FROM @b)=@old THROW 52005,'Sequence rollback reused code',1;"
    }
    Case 'T03-K06-permissions' 'App user reads views and executes existing procedures with ownership chain.' {
        Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root 'database\tests\t03\permissions.sql'),[Text.Encoding]::UTF8))
    }
    Case 'T03-K07-direct-writes' 'All protected table writes fail with permission error and unchanged rows.' {
        $snap=Snapshot-TestRows $connection
        $writes=@('UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=0','UPDATE dbo.KHACHHANG SET DiemTichLuy=0','UPDATE dbo.KHACHHANG SET MaHang=''DONG''','UPDATE dbo.HOADON SET TrangThai=N''Huy''','DELETE dbo.HOADON','INSERT dbo.HOADON(MaHD,MaNV,LoaiDon) VALUES(''Z3FORBID01'',''NV001'',N''Mang di'')','INSERT dbo.PHIEUTHUCHI(MaTC,MaDMTC,MaNV,SoTien) VALUES(''Z3FORBID01'',''TC011'',''NV001'',1)','UPDATE dbo.PHIEUTHUCHI SET SoTien=1','DELETE dbo.PHIEUTHUCHI')
        $writes+=@(
            "INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong) VALUES('Z3PAY00001','NVL01',1)",
            "INSERT dbo.LICHSU_DIEM(MaKH,Loai,SoDiem,LyDo) VALUES('KH004',N'Cong',1,N'Test forbidden')",
            "INSERT dbo.CHITIET_THANHTOAN(MaHD,MaPT,SoTien) VALUES('Z3PAY00001','PT001',1)",
            "INSERT dbo.THANHTOAN_CONGNO(MaCN,MaTC,MaPT) VALUES('CN20250401','Z3CASH0001','PT001')",
            "INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV) VALUES('Z3FORBID02',CONVERT(DATE,SYSDATETIME()),'NV001')"
        )
        foreach($table in @('TIEUHAO_HOADON','LICHSU_DIEM','CHITIET_THANHTOAN','THANHTOAN_CONGNO','PHIEUKIEMKE')) {
            $writes+="DELETE dbo.$table"; $writes+="UPDATE dbo.$table SET "+@{TIEUHAO_HOADON='SoLuong=1';LICHSU_DIEM='SoDiem=1';CHITIET_THANHTOAN='SoTien=1';THANHTOAN_CONGNO="MaPT='PT001'";PHIEUKIEMKE='DaDieuChinh=0'}[$table]
        }
        foreach($write in $writes) {
            Run-TestSql $connection "EXECUTE AS USER=N'T03_App';"
            try {Expect-TestSqlError $connection $write 229} finally {Run-TestSql $connection 'REVERT;'}
        }
        if($snap -cne (Snapshot-TestRows $connection)){throw 'Protected writes changed data.'}
    }
} catch {$setupError=$_.Exception.Message} finally {
    if($connection){$connection.Dispose()}; if($emptyConnection){$emptyConnection.Dispose()}
    $directory=Join-Path $root 'docs\tasks\g01-nen-tang-ky-thuat-du-lieu\t03-contract-migration-cap-ma'
    $history=Join-Path $directory 'runs'; [void][IO.Directory]::CreateDirectory($history)
    $report=[ordered]@{RunId=$runId;StartedAt=$started;CompletedAt=(Get-Date).ToString('o');Databases=@($database,$empty);SetupError=$setupError;Cases=@($cases.ToArray());ConcurrentCodes=$script:concurrentCodes;SeededSnapshotBefore=$before;SourceCommit='uncommitted';SchemaHash=(Get-FileHash (Join-Path $root 'database\DB_Schema.sql')).Hash;SeedHash=(Get-FileHash (Join-Path $root 'database\Insert_Data.sql')).Hash;MigrationHash=(Get-FileHash (Join-Path $root 'database\migrations\001_contract_and_code_generation.sql')).Hash;ManualCases=@('T03-K08','T03-K09')}
    $json=$report | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText((Join-Path $history "$runId.json"),$json,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $directory 'test-results.json'),$json,[Text.UTF8Encoding]::new($false))
    $md=@('# T03 - test results',"Run: $runId; database: $database / $empty",'| Case | Status | Expected | Actual |','|---|---|---|---|')
    foreach($case in $cases){$md+="| $($case.Id) | $($case.Status) | $($case.Expected) | $($case.Actual.Replace('|','/')) |"}
    $md+='| T03-K08 | NOT RUN | Review principals/config/log | Manual review required |'
    $md+='| T03-K09 | NOT RUN | Compare contract/schema/Models | Manual review required |'
    if($setupError){$md+="Setup blocker: $setupError"}
    [IO.File]::WriteAllText((Join-Path $directory 'test-results.md'),($md -join "`n"),[Text.UTF8Encoding]::new($false))
}
if($setupError -or @($cases | Where-Object Status -eq 'FAIL').Count){exit 1}
