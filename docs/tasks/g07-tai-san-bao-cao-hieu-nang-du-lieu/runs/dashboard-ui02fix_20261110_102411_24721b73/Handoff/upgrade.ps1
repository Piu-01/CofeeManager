$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/OperationsTools.ps1"
if(Get-Process -Name CafeManager.WinForms -ErrorAction SilentlyContinue){throw 'Đóng app bình thường trước; không tự kill.'}
$manifest=[ordered]@{Server='localhost';Database='CoffeeManager_Development';Status='Preflight';FixtureWritten=$false;AccountReset=$false}
function Save {$manifest|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/upgrade-manifest.json" -Encoding utf8}
function Schema($c){(Invoke-OperationTable $c "SELECT (SELECT t.name TableName,c.column_id,c.name ColumnName,TYPE_NAME(c.user_type_id) TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,cc.definition FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES) SchemaJson").Rows[0].SchemaJson}
$c=$null;Save
try {
 $c=Open-OperationConnection 'localhost' 'CoffeeManager_Development'
 $marker=Invoke-OperationTable $c "SELECT COUNT(*) N FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1"
 if([int]$marker.Rows[0].N -ne 1){throw 'Development marker mismatch'}
 $v=Invoke-OperationTable $c 'SELECT MAX(Version) V FROM dbo.SchemaMigrations';if([int]$v.Rows[0].V -ne 27){throw 'Expected development schema027'}
 $manifest['BeforeVersion']=27
 for($version=1;$version -le 27;$version++){
  $file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $version));if($file.Count -ne 1){throw 'Migration missing'}
  $stored=Invoke-OperationTable $c 'SELECT Checksum FROM dbo.SchemaMigrations WHERE Version=@v' @{'@v'=$version}
  if($stored.Rows.Count -ne 1 -or [string]$stored.Rows[0].Checksum -cne (Get-FileHash $file[0].FullName -Algorithm SHA256).Hash){throw "Checksum mismatch $version"}
 }
 $manifest['BeforeSnapshot']=@(Get-OperationSnapshot $c);$beforeSchema=Schema $c;$beforeSchema|Set-Content "$PSScriptRoot/schema-before.json"
 $backupDir=Invoke-OperationTable $c "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) Path"
 $backup=[IO.Path]::Combine([string]$backupDir.Rows[0].Path,('CoffeeManager_Development_before_dashboard028_'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)+'.bak'))
 $manifest['Backup']=$backup;Save
 & "$root/tools/database/Backup-Database.ps1" -Database CoffeeManager_Development -BackupFile $backup -ManifestPath "$PSScriptRoot/backup-manifest.json" | Out-Null
 & "$root/tools/database/Apply-Migration.ps1" -Database CoffeeManager_Development -Version 28 -File "$root/database/migrations/028_dashboard_analytics_read.sql" | ConvertTo-Json | Set-Content "$PSScriptRoot/028.json"
 $manifest['AfterSnapshot']=@(Get-OperationSnapshot $c);$afterSchema=Schema $c;$afterSchema|Set-Content "$PSScriptRoot/schema-after.json"
 $manifest['DataUnchanged']=(($manifest.BeforeSnapshot|ConvertTo-Json -Depth 5 -Compress) -ceq ($manifest.AfterSnapshot|ConvertTo-Json -Depth 5 -Compress))
 $manifest['SchemaTablesColumnsUnchanged']=($beforeSchema -ceq $afterSchema)
 if(-not $manifest.DataUnchanged -or -not $manifest.SchemaTablesColumnsUnchanged){throw 'Parity failed; keep backup and investigate; no automatic restore'}
 $version=Invoke-OperationTable $c 'SELECT MAX(Version) V FROM dbo.SchemaMigrations';$manifest['AfterVersion']=[int]$version.Rows[0].V
 $manifest.Status='Succeeded'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($c){$c.Dispose()};Save}
