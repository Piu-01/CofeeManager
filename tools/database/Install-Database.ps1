[CmdletBinding()]param(
 [Parameter(Mandatory=$true)][string]$Database,
 [string]$Server='localhost',
 [ValidateSet('Clean','Demo')][string]$Mode='Clean',
 [ValidateRange(1,27)][int]$ThroughVersion=27,
 [Parameter(Mandatory=$true)][string]$ManifestPath
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'OperationsTools.ps1')
Assert-OperationDatabase $Database
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(Test-Path -LiteralPath $ManifestPath){throw 'Manifest phải là file mới.'}
if(-not (Test-Path -LiteralPath (Split-Path ([IO.Path]::GetFullPath($ManifestPath)) -Parent))){throw 'Tạo thư mục manifest trước khi cài.'}
$files=@();for($v=1;$v -le $ThroughVersion;$v++){$f=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($f.Count -ne 1){throw "Migration thiếu/trùng:$v"};$files+=$f[0]}
$schema=[IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql'))
foreach($p in @('(?im)^CREATE DATABASE QuanLyCafe_ERP_V2;\s*$','(?im)^USE QuanLyCafe_ERP_V2;\s*$')){if([regex]::Matches($schema,$p).Count -ne 1){throw 'Baseline target không đúng mẫu đã kiểm.'};$schema=[regex]::Replace($schema,$p,'')}
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$manifest=[ordered]@{RunId=$runId;Server=$Server;Database=$Database;Mode=$Mode;Status='Starting';Created=$false;Migrations=@();Cleanup='Không tự xóa DB cài đặt; kiểm manifest/marker trước xử lý lỗi.'}
function Save-InstallManifest {$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $ManifestPath -Encoding utf8}
$c=$null
try{
 $c=Open-OperationConnection $Server 'master'
 $exists=Invoke-OperationTable $c 'SELECT COUNT(*) N FROM sys.databases WHERE name=@name' @{'@name'=$Database}
 if([int]$exists.Rows[0].N -ne 0){throw 'DB đích đã tồn tại; không cài/seed lại.'}
 Save-InstallManifest
 Invoke-OperationSql $c "CREATE DATABASE [$Database]";$manifest.Created=$true;Save-InstallManifest
 $c.ChangeDatabase($Database)
 Invoke-OperationSql $c 'CREATE TABLE dbo.OperationInstallMarker(RunId varchar(40) NOT NULL PRIMARY KEY,Mode varchar(10) NOT NULL);INSERT dbo.OperationInstallMarker VALUES(@id,@mode)' @{'@id'=$runId;'@mode'=$Mode}
 Invoke-OperationBatches $c $schema
 if($Mode -eq 'Demo'){
  $seed=[IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql'))
  $pattern='(?im)^USE QuanLyCafe_ERP_V2;\s*$';if([regex]::Matches($seed,$pattern).Count -ne 1){throw 'Seed target không đúng mẫu.'}
  Invoke-OperationBatches $c ([regex]::Replace($seed,$pattern,''))
 }
 foreach($file in $files){$v=[int]$file.Name.Substring(0,3);$applied=& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Server $Server -Database $Database -Version $v -File $file.FullName;$manifest.Migrations+= $applied;Save-InstallManifest}
 $manifest.Status='Installed';$manifest['Bootstrap']='Pending: dùng app --setup với identity CafeManagerSetup, không có mật khẩu mặc định.'
}catch{$manifest.Status='Failed';$manifest['ErrorType']=$_.Exception.GetType().Name;throw}
finally{if($c){$c.Dispose()};Save-InstallManifest}
[pscustomobject]@{Database=$Database;Mode=$Mode;Version=$ThroughVersion;Manifest=$ManifestPath;Status=$manifest.Status}
