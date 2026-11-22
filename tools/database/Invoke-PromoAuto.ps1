[CmdletBinding()]param()
# Runs the promo-auto SQL suite on a throwaway SQL Server database. It never touches
# CoffeeManager_Development and never modifies source; evidence is written under
# docs/tasks/promotion-dieu-kien-tu-ap/runs/.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_PromoAuto_$runId"
$output=Join-Path $root "docs/tasks/promotion-dieu-kien-tu-ap/runs/$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database=$name;SchemaVersion=39;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Steps=@();Status='Running';Created=$false;Cleaned=$false}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj|runs)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj','.json' } |
 Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
}
function Invoke-Step($Connection,[string]$Label,[string]$File) {
 $sw=[Diagnostics.Stopwatch]::StartNew()
 try {
  Run-TestSql $Connection ([IO.File]::ReadAllText((Join-Path $root $File)))
  $sw.Stop(); $manifest.Steps+=@{Step=$Label;File=$File;Status='PASS';Ms=$sw.ElapsedMilliseconds}
 } catch {
  $sw.Stop(); $manifest.Steps+=@{Step=$Label;File=$File;Status='FAIL';Ms=$sw.ElapsedMilliseconds;Error=$_.Exception.Message}
  throw
 } finally { Save-Manifest }
}
function Get-SqlError($Exception) {
 $e=$Exception
 while($e -and $e -isnot [System.Data.SqlClient.SqlException]) { $e=$e.InnerException }
 return $e
}
function Invoke-PauseRace($Database,[int]$Actor,[byte[]]$Stamp,[string]$Code) {
 # Two real sessions race the same Pause on the same expected revision.
 $reader=Open-TestConnection $Database
 try { [byte[]]$rev=(Read-TestScalar $reader "SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='$Code';") }
 finally { $reader.Dispose() }
 $Stamp=[byte[]]$Stamp
 $c1=Open-TestConnection $Database; $c2=Open-TestConnection $Database
 try {
  $sql="WAITFOR DELAY '00:00:01'; EXEC dbo.sp_PromotionWrite @Actor=@Actor,@Stamp=@Stamp,@Action='Pause',@Code=@Code,@RequestId=@RequestId,@ExpectedRevision=@Revision;"
  $commands=@($c1,$c2) | ForEach-Object {
   $cmd=$_.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=60
   [void]$cmd.Parameters.AddWithValue('@Actor',$Actor)
   $p=$cmd.Parameters.Add('@Stamp',[System.Data.SqlDbType]::Binary,8); $p.Value=$Stamp
   [void]$cmd.Parameters.AddWithValue('@Code',$Code)
   [void]$cmd.Parameters.AddWithValue('@RequestId',[guid]::NewGuid())
   $p=$cmd.Parameters.Add('@Revision',[System.Data.SqlDbType]::Binary,8); $p.Value=$rev
   $cmd
  }
  $t1=$commands[0].ExecuteNonQueryAsync(); $t2=$commands[1].ExecuteNonQueryAsync()
  $results=@()
  foreach($t in @($t1,$t2)) {
   try { [void]$t.GetAwaiter().GetResult(); $results+='OK' }
   catch { $e=Get-SqlError $_.Exception; if($e -and $e.Number -eq 51603){ $results+='CONFLICT' } else { $results+='ERROR:'+$_.Exception.Message } }
  }
  $commands | ForEach-Object { $_.Dispose() }
  $ok=@($results | Where-Object { $_ -eq 'OK' }).Count
  $conflict=@($results | Where-Object { $_ -eq 'CONFLICT' }).Count
  if($ok -ne 1 -or $conflict -ne 1){ throw "Pause race expected 1 OK + 1 CONFLICT, got: $($results -join ', ')" }
 } finally { $c1.Dispose(); $c2.Dispose() }
}
$before=@(Source-Snapshot); $before | Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-Manifest
try {
 $master=Open-TestConnection 'master'
 try {
  if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name';") -ne 0){throw 'Target already exists'}
  Run-TestSql $master "CREATE DATABASE [$name];"
  $manifest.Created=$true; Save-Manifest
  $master.ChangeDatabase($name)
  Run-TestSql $master "CREATE TABLE dbo.PromoAutoTestMarker(RunId varchar(40) NOT NULL PRIMARY KEY); INSERT dbo.PromoAutoTestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $seed
  for($version=1;$version -le 39;$version++) {
   $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
  }
  Invoke-Step $master 'fixture' 'database/tests/promo-auto/fixture.sql'
  Invoke-Step $master '01-preview-multi' 'database/tests/promo-auto/01-preview-multi.sql'
  Invoke-Step $master '02-conditions' 'database/tests/promo-auto/02-conditions.sql'
  Invoke-Step $master '03-checkout-snapshot' 'database/tests/promo-auto/03-checkout-snapshot.sql'
  Invoke-Step $master '04-report-unchanged' 'database/tests/promo-auto/04-report-unchanged.sql'
  $sw=[Diagnostics.Stopwatch]::StartNew()
  $actor=[int](Read-TestScalar $master "SELECT MaTK FROM dbo.TAIKHOAN WHERE MaNV='NV001';")
  $stamp=[byte[]](Read-TestScalar $master "SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';")
  Invoke-PauseRace $name $actor $stamp 'KM90000011'
  $sw.Stop(); $manifest.Steps+=@{Step='05-pause-race';Status='PASS';Ms=$sw.ElapsedMilliseconds}; Save-Manifest
  $manifest.Status='Succeeded'
 } finally { $master.Dispose() }
} catch {
 $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString()
} finally {
 $after=@(Source-Snapshot); $after | Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if($manifest.Created -and $name -match '^CoffeeManager_Test_PromoAuto_\d{8}_\d{6}_[a-f0-9]{8}$') {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.PromoAutoTestMarker;') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned=$true
  } catch { $manifest['Retained']=@{Database=$name;Reason=$_.Exception.Message} }
  finally { if($cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or -not $manifest.Cleaned){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "PromoAuto evidence: $output; status=$($manifest.Status)"
}
if($manifest.Status -ne 'Succeeded'){throw "PromoAuto suite failed: $output/manifest.json"}
