$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$out=$PSScriptRoot
$frozen=@('tools/database/Invoke-G03Technical.ps1','tools/database/Test-G03.ps1') | ForEach-Object {[pscustomobject]@{Path=$_;Hash=(Get-FileHash (Join-Path $root $_)).Hash}}
$frozen | Export-Csv (Join-Path $out 'runner-before.csv') -NoTypeInformation
# Chuẩn bị cả hai bản sao trước lượt test; không thay source/test gốc để tiêm lỗi.
foreach($mode in @('sql-fail','unit-fail')) {
 $copy=Join-Path $out $mode
 if(Test-Path $copy){throw "Regression copy already exists: $copy"}
 New-Item $copy -ItemType Directory | Out-Null
 foreach($folder in @('tools/database','database','tests/CafeManager.Tests','src/CafeManager.Models','src/CafeManager.Business','src/CafeManager.DataAccess')) {
  foreach($file in Get-ChildItem (Join-Path $root $folder) -Recurse -File | Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]'}) {
   $relative=[IO.Path]::GetRelativePath($root,$file.FullName)
   $dest=Join-Path $copy $relative
   New-Item (Split-Path $dest) -ItemType Directory -Force | Out-Null
   Copy-Item -LiteralPath $file.FullName -Destination $dest
  }
 }
 Copy-Item (Join-Path $root 'global.json') $copy
 if($mode -eq 'sql-fail') {
  Add-Content (Join-Path $copy 'database/tests/t09/acceptance-contract.sql') "`nGO`nTHROW 52991, N'G03-B01 controlled SQL failure', 1;"
 } else {
  @'
namespace CafeManager.Tests.G03;
public class RunnerControlledFailure
{
    [Xunit.Fact]
    public void ControlledFailure() => Xunit.Assert.Fail("G03-B01 controlled unit failure");
}
'@ | Set-Content (Join-Path $copy 'tests/CafeManager.Tests/G03/RunnerControlledFailure.cs')
 }
}
$results=@()
foreach($mode in @('pass','sql-fail','unit-fail')) {
 $work=if($mode -eq 'pass'){$root}else{Join-Path $out $mode}
 $log=Join-Path $out "$mode.log"
 & (Join-Path $PSHOME 'pwsh.exe') -NoProfile -File (Join-Path $work 'tools/database/Test-G03.ps1') -ApprovedAcceptance *> $log
 $code=$LASTEXITCODE
 $runs=Join-Path $work 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs'
 $run=Get-ChildItem $runs -Directory -Filter 'acceptance_*' | Sort-Object Name -Descending | Select-Object -First 1
 if($null -eq $run){throw "No manifest for $mode"}
 $m=Get-Content (Join-Path $run.FullName 'manifest.json') -Raw | ConvertFrom-Json
 $sql=@(Get-ChildItem $run.FullName -Filter '*-sql.json' | ForEach-Object {Get-Content $_.FullName -Raw | ConvertFrom-Json})
 $checks=($m.Created.Count -eq 2 -and $m.Cleaned.Count -eq 2 -and $m.Retained.Count -eq 0 -and $m.SourceUnchanged -and $sql.Count -eq 5 -and (Test-Path (Join-Path $run.FullName $m.Unit.Trx)))
 $checks=$checks -and $(if($mode -eq 'pass'){$code -eq 0 -and $m.Status -eq 'Succeeded' -and $m.SqlFailed -eq 0 -and $m.Unit.Status -eq 'PASS' -and $m.Unit.ExitCode -eq 0}else{$code -ne 0 -and $m.Status -eq 'Failed'})
 if($mode -eq 'sql-fail'){$checks=$checks -and $m.SqlFailed -eq 1 -and $m.Unit.Status -eq 'PASS' -and @($sql | Where-Object Status -eq 'FAIL').Count -eq 1}
 if($mode -eq 'unit-fail'){$checks=$checks -and $m.SqlFailed -eq 0 -and $m.Unit.Status -eq 'FAIL' -and $m.Unit.ExitCode -ne 0}
 $results+= [pscustomobject]@{Case=$mode;Status=$(if($checks){'PASS'}else{'FAIL'});ExitCode=$code;Manifest=$([IO.Path]::GetRelativePath($root,(Join-Path $run.FullName 'manifest.json')));RunnerStatus=$m.Status;SqlFailed=$m.SqlFailed;Unit=$m.Unit;Created=$m.Created.Count;Cleaned=$m.Cleaned.Count;Retained=$m.Retained.Count;SourceUnchanged=$m.SourceUnchanged}
 $results | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'results.json')
 Write-Output "$mode regression: $($results[-1].Status), process=$code, runner=$($m.Status), cleaned=$($m.Cleaned.Count)"
}
$changed=@($frozen | Where-Object {(Get-FileHash (Join-Path $root $_.Path)).Hash -cne $_.Hash})
[ordered]@{RunnerChangedDuringRegression=$changed.Count;Cases=$results.Count;Passed=@($results | Where-Object Status -eq 'PASS').Count} | ConvertTo-Json | Set-Content (Join-Path $out 'summary.json')
if($changed.Count -gt 0 -or @($results | Where-Object Status -ne 'PASS').Count -gt 0){throw 'Regression failed; inspect evidence without changing source.'}
