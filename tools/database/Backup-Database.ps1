[CmdletBinding()]param(
 [Parameter(Mandatory=$true)][string]$Database,[string]$Server='localhost',
 [Parameter(Mandatory=$true)][string]$BackupFile,
 [Parameter(Mandatory=$true)][string]$ManifestPath
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'OperationsTools.ps1')
Assert-OperationDatabase $Database
if(Test-Path -LiteralPath $ManifestPath){throw 'Manifest phải là file mới.'}
$c=$null
$result=[ordered]@{Server=$Server;Database=$Database;Status='Starting';BackupFile=$BackupFile;NewFileVerified=$false;Kind='COPY_ONLY CHECKSUM';StartedAt=[DateTime]::UtcNow.ToString('o');Snapshot=@()}
try{
 $c=Open-OperationConnection $Server $Database
 Assert-NewServerFile $c $BackupFile
 $result.NewFileVerified=$true
 # Dữ liệu snapshot chỉ có ý nghĩa parity khi operator đã dừng các writer.
 $result.Snapshot=@(Get-OperationSnapshot $c)
 Invoke-OperationSql $c "BACKUP DATABASE [$Database] TO DISK=@path WITH COPY_ONLY,CHECKSUM;" @{'@path'=$BackupFile}
 Invoke-OperationSql $c 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM,FILE=1;' @{'@path'=$BackupFile}
 $h=Invoke-OperationTable $c 'RESTORE HEADERONLY FROM DISK=@path;' @{'@path'=$BackupFile}
 if($h.Rows.Count -ne 1 -or [string]$h.Rows[0].DatabaseName -cne $Database -or -not [bool]$h.Rows[0].IsCopyOnly -or -not [bool]$h.Rows[0].HasBackupChecksums){throw 'Backup metadata không khớp nguồn/checksum/copy-only.'}
 $result['BackupSetGUID']=[string]$h.Rows[0].BackupSetGUID
 $result['AfterSnapshot']=@(Get-OperationSnapshot $c)
 $result['SourceStable']=(($result.Snapshot|ConvertTo-Json -Depth 5 -Compress) -ceq ($result.AfterSnapshot|ConvertTo-Json -Depth 5 -Compress))
 if(-not $result.SourceStable){throw 'Nguồn thay đổi trong backup; không đủ bằng chứng snapshot parity. Dừng writer rồi tạo backup mới.'}
 $result.Status='Verified'
}catch{$result.Status='Failed';$result['ErrorType']=$_.Exception.GetType().Name;throw}
finally{if($c){$c.Dispose()};$result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ManifestPath -Encoding utf8}
[pscustomobject]@{Status=$result.Status;Manifest=$ManifestPath}
