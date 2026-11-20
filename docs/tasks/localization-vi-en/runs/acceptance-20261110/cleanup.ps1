$ErrorActionPreference='Stop'
$out=(Resolve-Path 'docs/tasks/localization-vi-en/runs/acceptance-20261110').Path
$manifest=Get-Content (Join-Path $out 'manifest.json') -Raw|ConvertFrom-Json
. ./tools/database/OperationsTools.ps1
$cleanup=@()
foreach($item in $manifest.Created) {
    $name=[string]$item.Database
    if($name -notmatch '^CoffeeManager_Test_LOC_20261110_\d{6}_[a-f0-9]{8}_(vi|en|clean)$'){throw 'Refusing cleanup outside this test namespace'}
    $installed=Get-Content (Join-Path $out ($item.Language+'-install.json')) -Raw|ConvertFrom-Json
    if(!$installed.Created -or $installed.Database -ne $name){throw 'Install manifest mismatch'}
    $c=Open-OperationConnection 'localhost' $name
    try {
        $marker=Invoke-OperationTable $c 'SELECT RunId FROM dbo.LocalizationTestMarker'
        $install=Invoke-OperationTable $c 'SELECT RunId FROM dbo.OperationInstallMarker'
        if([string]$marker.Rows[0].RunId -cne $manifest.RunId -or [string]$install.Rows[0].RunId -cne $installed.RunId){throw 'Ownership marker mismatch'}
        $state=Invoke-OperationTable $c 'SELECT @@TRANCOUNT TxCount,XACT_STATE() TxState,(SELECT MAX(Version) FROM dbo.SchemaMigrations) SchemaVersion'
        $cleanup+=@{Database=$name;BeforeDrop=@{TransactionCount=[int]$state.Rows[0].TxCount;TransactionState=[int]$state.Rows[0].TxState;SchemaVersion=[int]$state.Rows[0].SchemaVersion};OwnershipVerified=$true}
    } finally {$c.Dispose()}
    $master=Open-OperationConnection 'localhost' 'master'
    try {
        Invoke-OperationSql $master "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]"
        $check=Invoke-OperationTable $master 'SELECT COUNT(*) N FROM sys.databases WHERE name=@name' @{'@name'=$name}
        if([int]$check.Rows[0].N -ne 0){throw 'Cleanup did not remove owned test database'}
        $cleanup[-1].Removed=$true
    }finally{$master.Dispose()}
}
$cleanup|ConvertTo-Json -Depth 6|Set-Content (Join-Path $out 'cleanup.json')
$manifest.Status='Tested; three UI bugs recorded; owned test databases removed'
$manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $out 'manifest.json')
"Removed $($cleanup.Count) owned test databases after verifying both markers."
