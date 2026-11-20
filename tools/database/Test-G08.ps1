[CmdletBinding()]param([switch]$Technical,[switch]$RunAcceptance,[switch]$OperationsSmoke)
$ErrorActionPreference='Stop'
if(($Technical -and $RunAcceptance) -or (-not $Technical -and -not $RunAcceptance)){throw 'Chọn Technical hoặc RunAcceptance; acceptance chỉ sau duyệt đợt test G08.'}
if($OperationsSmoke -and -not $Technical){throw 'OperationsSmoke chỉ là kiểm kỹ thuật triển khai, không nghiệm thu.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'OperationsTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$phase=if($Technical){'technical'}else{'acceptance'}
$output=Join-Path $root "docs/tasks/g08-van-hanh-nghiem-thu-ban-giao/runs/${phase}_$runId"
New-Item -ItemType Directory -Path $output | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind=$phase;Server='localhost';Status='Running';Created=@();Cleaned=@();Retained=@();SourceUnchanged=$false}
function Save-G08Manifest {$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'manifest.json') -Encoding utf8}
function Get-G08Source {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests'),(Join-Path $root 'tools') -Recurse -File |
 Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.csproj','.sql','.ps1'} |
 Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}
}
$before=@(Get-G08Source);$before|Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-G08Manifest
try{
 & dotnet build (Join-Path $root 'tools/CafeManager.G08Probe/CafeManager.G08Probe.csproj') -c Debug -m:1 -nr:false -v minimal *> (Join-Path $output 'build.log')
 if($LASTEXITCODE -ne 0){throw 'Build probe failed.'}
 foreach($mode in @('Clean','Demo')){
  $name="CoffeeManager_Test_G08_${runId}_$($mode.ToLowerInvariant())";$installManifest=Join-Path $output "$mode-install.json"
  # Ghi tên dự kiến trước CREATE; cleanup còn cần marker do Install tạo thực sự.
 $manifest.Created+=@{Database=$name;InstallManifest=$installManifest;Kind='Install'};Save-G08Manifest
  & (Join-Path $PSScriptRoot 'Install-Database.ps1') -Database $name -Mode $mode -ManifestPath $installManifest | Out-Null
  $c=Open-OperationConnection 'localhost' $name
  try{
   Invoke-OperationSql $c 'CREATE TABLE dbo.G08TestMarker(RunId varchar(40) NOT NULL PRIMARY KEY);INSERT dbo.G08TestMarker VALUES(@run)' @{'@run'=$runId}
   if($RunAcceptance -and $mode -eq 'Demo'){Invoke-OperationBatches $c ([IO.File]::ReadAllText((Join-Path $root 'database/tests/g08/e2e-fixture.sql')))}
  }finally{$c.Dispose()}
  $probe=Join-Path $root 'tools/CafeManager.G08Probe/bin/Debug/net10.0-windows/CafeManager.G08Probe.exe'
  & $probe $name $runId (Join-Path $output "$mode-probe.json") $phase $mode *> (Join-Path $output "$mode-probe.log")
  if($LASTEXITCODE -ne 0){throw "Probe $mode failed; xem JSON ErrorType/SqlNumber."}
  if($OperationsSmoke -and $mode -eq 'Demo'){
   $c=Open-OperationConnection 'localhost' $name
   try{$dir=Invoke-OperationTable $c "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) BackupPath";$backupRoot=[string]$dir.Rows[0].BackupPath}finally{$c.Dispose()}
   if([string]::IsNullOrWhiteSpace($backupRoot)){throw 'SQL không có default backup path.'}
   $backupFile=[IO.Path]::Combine($backupRoot,"CoffeeManager_G08_$runId.bak")
   $manifest['BackupFile']=$backupFile;$manifest['BackupRoot']=$backupRoot;Save-G08Manifest
   $backupManifest=Join-Path $output 'backup.json'
   & (Join-Path $PSScriptRoot 'Backup-Database.ps1') -Database $name -BackupFile $backupFile -ManifestPath $backupManifest|Out-Null
   $restoreName="CoffeeManager_Test_G08_${runId}_restore";$restoreManifest=Join-Path $output 'restore.json'
   $manifest.Created+=@{Database=$restoreName;InstallManifest=$restoreManifest;Kind='Restore'};Save-G08Manifest
   & (Join-Path $PSScriptRoot 'Restore-TestDatabase.ps1') -BackupManifest $backupManifest -Database $restoreName -RunId $runId -ManifestPath $restoreManifest|Out-Null
  }
 }
 $manifest.Status='Succeeded'
 if($RunAcceptance){$manifest['Native']='NOT RUN; chạy native-test-guide và publish-guide riêng, không suy PASS từ runner này.'}
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message}
finally{
 $after=@(Get-G08Source);$after|Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest.SourceUnchanged=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if(-not $manifest.SourceUnchanged){$manifest.Status='Failed'}
 foreach($item in $manifest.Created){
  $cleanup=$null
  try{
   if(-not (Test-Path -LiteralPath $item.InstallManifest)){throw 'Không có manifest cài đặt; không tự dọn.'}
   $installed=Get-Content -LiteralPath $item.InstallManifest -Raw|ConvertFrom-Json
   if(-not $installed.Created){
    if($item.Kind -eq 'Restore' -and $installed.RestoreStarted){throw 'Restore lỗi sau khi bắt đầu; giữ đích/file theo manifest để operator kiểm, không tự dọn thiếu marker.'}
    continue
   }
   $name=$item.Database
   if($name -cnotmatch ('^CoffeeManager_Test_G08_'+[regex]::Escape($runId)+'_(clean|demo|restore)$') -or $installed.Database -cne $name -or $installed.Server -cne 'localhost'){throw 'Cleanup target mismatch.'}
   $cleanup=Open-OperationConnection 'localhost' $name
   if($item.Kind -eq 'Restore'){
    $marker=Invoke-OperationTable $cleanup 'SELECT RunId FROM dbo.G08TestMarker'
    $actual=Invoke-OperationTable $cleanup 'SELECT physical_name FROM sys.database_files'
    if($marker.Rows.Count -ne 1 -or [string]$marker.Rows[0].RunId -cne $runId -or @(Compare-Object @($installed.Files|ForEach-Object Physical) @($actual.Rows|ForEach-Object physical_name)).Count){throw 'Restore ownership/file mismatch.'}
   }else{
    $marker=Invoke-OperationTable $cleanup 'SELECT RunId FROM dbo.OperationInstallMarker'
    if($marker.Rows.Count -ne 1 -or [string]$marker.Rows[0].RunId -cne [string]$installed.RunId){throw 'Install ownership mismatch.'}
   }
   $cleanup.ChangeDatabase('master');Invoke-OperationSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]"
   $manifest.Cleaned+=$name
  }catch{$manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message}}
  finally{if($cleanup){$cleanup.Dispose()}}
 }
 if($manifest.Contains('BackupFile')){
  try{
   $resolvedBackup=[IO.Path]::GetFullPath($manifest.BackupFile);$resolvedRoot=[IO.Path]::GetFullPath($manifest.BackupRoot).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
   $backupOwnership=Get-Content -LiteralPath (Join-Path $output 'backup.json') -Raw|ConvertFrom-Json
   if(-not $backupOwnership.NewFileVerified -or $backupOwnership.BackupFile -cne $resolvedBackup -or $backupOwnership.Server -cne 'localhost'){throw 'Chưa xác minh backup file mới của lượt; không tự xóa.'}
   if(-not $resolvedBackup.StartsWith($resolvedRoot,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolvedBackup) -cne "CoffeeManager_G08_$runId.bak"){throw 'Backup cleanup path mismatch.'}
   # File thuộc filesystem server: dọn bằng SQL service với exact path, không đổi ACL.
   $backupCleanup=Open-OperationConnection 'localhost' 'master'
   try{
    Invoke-OperationSql $backupCleanup 'EXEC master.dbo.xp_delete_file 0,@path;' @{'@path'=$resolvedBackup}
    $remaining=Invoke-OperationTable $backupCleanup 'EXEC master.dbo.xp_fileexist @path;' @{'@path'=$resolvedBackup}
    if([int]$remaining.Rows[0][0] -ne 0){throw 'SQL backup file vẫn còn; cần operator dọn exact file.'}
   }finally{$backupCleanup.Dispose()}
   $manifest['BackupCleaned']=$true
  }catch{$manifest.Retained+=@{File=$manifest.BackupFile;Reason=$_.Exception.Message}}
 }
 if($manifest.Retained.Count){$manifest.Status='Failed'}
 Save-G08Manifest
 Write-Output "G08 $phase : $($manifest.Status); created=$($manifest.Created.Count),cleaned=$($manifest.Cleaned.Count),retained=$($manifest.Retained.Count); $output"
}
if($manifest.Status -ne 'Succeeded'){throw 'G08 runner failed; xem manifest.'}
