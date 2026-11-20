$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$manifest=Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$tracked=@('src/CafeManager.WinForms/Modules/Sales/SalesControl.cs','src/CafeManager.WinForms/Forms/OrbitConfirmationDialog.cs')
$hashes=[ordered]@{}
foreach($f in $tracked){$hashes[$f]=(Get-FileHash (Join-Path $root $f) -Algorithm SHA256).Hash}
$hashes | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'source-before.json') -Encoding utf8
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:POSNEW_SQL_RUNTIME
try {
    $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
    if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
    $b['Initial Catalog']=$manifest.Database;$env:POSNEW_SQL_RUNTIME=$b.get_ConnectionString()
    $out="$PSScriptRoot/bin/Debug/net10.0-windows"
    dotnet "$out/PosNewOrder.dll" "$PSScriptRoot" *> "$PSScriptRoot/run.log"
    $result=$LASTEXITCODE
    if(Test-Path "$PSScriptRoot/results.json"){$r=Get-Content "$PSScriptRoot/results.json" -Raw | ConvertFrom-Json; $r.Results | Group-Object Outcome | Select-Object Name,Count; $r.Results | ConvertTo-Json -Depth 6}
    exit $result
} finally {
    [Array]::Clear($bytes,0,$bytes.Length);$env:POSNEW_SQL_RUNTIME=$previousRuntime
    $after=[ordered]@{}
    foreach($f in $tracked){$after[$f]=(Get-FileHash (Join-Path $root $f) -Algorithm SHA256).Hash}
    $after | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'source-after.json') -Encoding utf8
}
