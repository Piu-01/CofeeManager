param([string]$Root,[string]$Output)
$ErrorActionPreference='Stop';$root=$Root
. (Join-Path $Root 'tools/database/DatabaseTestTools.ps1')
[void][IO.Directory]::CreateDirectory($Output)
$run=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db=New-IsolatedDatabase 'G02' $run $true
& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db | Out-Null
& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db -Version 2 -File (Join-Path $Root 'database/migrations/002_auth_and_bootstrap.sql') | Out-Null
& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db -Version 3 -File (Join-Path $Root 'database/migrations/003_reference_catalogs.sql') | Out-Null
$c=Open-TestConnection $db
$result=[ordered]@{Id='T08-K06.b.demoRerun.currentChecksum';Database=$db;Expected='AlreadyApplied; custom name/support and complete seed/history unchanged';Actual=$null;Status='BLOCKED'}
try{
 Assert-TestTarget $c 'G02'
 Run-TestSql $c "UPDATE dbo.DANHMUC_THUCHI SET TenDMTC=N'Custom payroll preserved' WHERE MaDMTC='TC005';UPDATE dbo.PaymentMethodSupport SET IsSupported=0 WHERE MaPT='PT001'"
 $before=Snapshot-TestRows $c
 $runResult=& (Join-Path $Root 'tools/database/Apply-Migration.ps1') -Database $db -Version 3 -File (Join-Path $Root 'database/migrations/003_reference_catalogs.sql')
 $text=$runResult|ConvertTo-Json -Depth 6
 [IO.File]::WriteAllText((Join-Path $Output 'runner.json'),$text,[Text.UTF8Encoding]::new($false))
 if($text -notmatch 'AlreadyApplied'){throw 'Runner did not report AlreadyApplied'}
 if($before -cne (Snapshot-TestRows $c)){throw 'Persistent data/history changed'}
 $result.Status='PASS';$result.Actual='AlreadyApplied, complete DB snapshot unchanged including custom payroll name, disabledPT001 and seed financial history'
}catch{$result.Status='FAIL';$result.Actual=$_.Exception.Message}
finally{$c.Dispose();[IO.File]::WriteAllText((Join-Path $Output 'results.json'),($result|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))}
if($result.Status -ne 'PASS'){exit 1}
