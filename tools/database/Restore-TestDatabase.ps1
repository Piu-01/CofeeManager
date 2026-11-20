[CmdletBinding()]param(
 [Parameter(Mandatory=$true)][string]$BackupManifest,
 [Parameter(Mandatory=$true)][string]$Database,
 [Parameter(Mandatory=$true)][ValidatePattern('^\d{8}_\d{6}_[a-f0-9]{8}$')][string]$RunId,
 [Parameter(Mandatory=$true)][string]$ManifestPath,
 [string]$Server='localhost'
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'OperationsTools.ps1')
Assert-OperationDatabase $Database
if($Database -cnotmatch ('^CoffeeManager_Test_G08_'+[regex]::Escape($RunId)+'_restore[a-z0-9]*$')){throw 'Restore chỉ nhận DB test mới của runID này.'}
if(Test-Path -LiteralPath $ManifestPath){throw 'Manifest phải là file mới.'}
$backup=Get-Content -LiteralPath $BackupManifest -Raw|ConvertFrom-Json
if($backup.Status -ne 'Verified' -or -not $backup.SourceStable -or $backup.Server -cne $Server){throw 'Backup chưa verify/nguồn chưa ổn định hoặc khác server.'}
$manifest=[ordered]@{RunId=$RunId;Server=$Server;Database=$Database;SourceDatabase=$backup.Database;BackupSetGUID=$backup.BackupSetGUID;Status='Starting';RestoreStarted=$false;Created=$false;Files=@()}
function Save-RestoreManifest {$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ManifestPath -Encoding utf8}
$c=$null
try{
 $c=Open-OperationConnection $Server 'master'
 $existing=Invoke-OperationTable $c 'SELECT COUNT(*) N FROM sys.databases WHERE name=@name' @{'@name'=$Database}
 if([int]$existing.Rows[0].N -ne 0){throw 'Đích tồn tại; không REPLACE/ghi đè.'}
 $headers=Invoke-OperationTable $c 'RESTORE HEADERONLY FROM DISK=@path' @{'@path'=$backup.BackupFile}
 if($headers.Rows.Count -ne 1 -or [string]$headers.Rows[0].BackupSetGUID -cne [string]$backup.BackupSetGUID -or [string]$headers.Rows[0].DatabaseName -cne [string]$backup.Database){throw 'Backup file đã đổi hoặc sai nguồn.'}
 Invoke-OperationSql $c 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM,FILE=1' @{'@path'=$backup.BackupFile}
 $files=Invoke-OperationTable $c 'RESTORE FILELISTONLY FROM DISK=@path WITH FILE=1' @{'@path'=$backup.BackupFile}
 $dirs=Invoke-OperationTable $c "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')) DataPath,CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath')) LogPath"
 $moves=@();$parameters=@{'@path'=$backup.BackupFile};$i=0
 foreach($file in $files.Rows){
  if($file.Type -notin @('D','L')){throw 'Chỉ hỗ trợ backup data/log thường, không tự restore FILESTREAM.'}
  $dir=if($file.Type -eq 'L'){[string]$dirs.Rows[0].LogPath}else{[string]$dirs.Rows[0].DataPath}
  if([string]::IsNullOrWhiteSpace($dir)){throw 'SQL thiếu default data/log path.'}
  $ext=if($file.Type -eq 'L'){'.ldf'}else{'.mdf'};$physical=[IO.Path]::Combine($dir,"${Database}_$i$ext")
  Assert-NewServerFile $c $physical
  $parameters["@logical$i"]=[string]$file.LogicalName;$parameters["@physical$i"]=$physical
  $moves+="MOVE @logical$i TO @physical$i";$manifest.Files+=@{Logical=[string]$file.LogicalName;Physical=$physical};$i++
 }
 if($moves.Count -eq 0){throw 'Backup không có file.'}
 $manifest.RestoreStarted=$true;Save-RestoreManifest
 Invoke-OperationSql $c ("RESTORE DATABASE [$Database] FROM DISK=@path WITH FILE=1,CHECKSUM,RECOVERY,"+($moves -join ',')) $parameters
 $manifest.Created=$true;Save-RestoreManifest;$c.ChangeDatabase($Database)
 $actualFiles=Invoke-OperationTable $c 'SELECT physical_name FROM sys.database_files'
 if(@(Compare-Object @($manifest.Files|ForEach-Object Physical) @($actualFiles.Rows|ForEach-Object physical_name)).Count){throw 'Physical file mapping mismatch.'}
 Invoke-OperationSql $c "IF OBJECT_ID('dbo.G08TestMarker') IS NOT NULL DROP TABLE dbo.G08TestMarker;CREATE TABLE dbo.G08TestMarker(RunId varchar(40) NOT NULL PRIMARY KEY);INSERT dbo.G08TestMarker VALUES(@run)" @{'@run'=$RunId}
 $manifest['Snapshot']=@(Get-OperationSnapshot $c)
 $manifest['Parity']=(($backup.Snapshot|ConvertTo-Json -Depth 5 -Compress) -ceq ($manifest.Snapshot|ConvertTo-Json -Depth 5 -Compress))
 if(-not $manifest.Parity){throw 'Restore snapshot không khớp backup.'}
 $manifest.Status='Restored'
}catch{$manifest.Status='Failed';$manifest['ErrorType']=$_.Exception.GetType().Name;throw}
finally{if($c){$c.Dispose()};Save-RestoreManifest}
[pscustomobject]@{Status=$manifest.Status;Database=$Database;Manifest=$ManifestPath}
