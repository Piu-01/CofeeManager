$ErrorActionPreference='Stop'
$root=(Resolve-Path .).Path
$out=Join-Path $root 'docs/tasks/localization-vi-en/runs/acceptance-20261110'
. (Join-Path $root 'tools/database/OperationsTools.ps1')
$id='20261110_'+(Get-Date -Format HHmmss)+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$manifest=[ordered]@{RunId=$id;Created=@();Status='Preparing';Purpose='Approved VI/EN acceptance; isolated databases only'}
foreach($language in @('vi','en','clean')) {
  $name="CoffeeManager_Test_LOC_${id}_$language"
  $manifest.Created+=@{Language=$language;Database=$name}
  $manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $out 'manifest.json')
  $mode=if($language-eq'clean'){'Clean'}else{'Demo'}
  & (Join-Path $root 'tools/database/Install-Database.ps1') -Database $name -Mode $mode -ManifestPath (Join-Path $out "$language-install.json")|Out-Null
  & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version 28 -File (Join-Path $root 'database/migrations/028_dashboard_analytics_read.sql')|Out-Null
  $c=Open-OperationConnection 'localhost' $name
  try{Invoke-OperationSql $c 'CREATE TABLE dbo.LocalizationTestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.LocalizationTestMarker VALUES(@run)' @{'@run'=$id}}finally{$c.Dispose()}
}
$manifest.Status='Prepared';$manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $out 'manifest.json')
$manifest.Created|ForEach-Object{"Prepared $($_.Language): $($_.Database)"}
