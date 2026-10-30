$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$m=Get-Content "$PSScriptRoot/manifest.json" -Raw | ConvertFrom-Json
if($m.Cleaned -or $m.Database -cne "CoffeeManager_Test_G04_$($m.Marker)_checkout"){throw 'Unexpected isolated fixture'}
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:CHECKOUT_SQL_RUNTIME
try {
    $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
    if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
    $b['Initial Catalog']=$m.Database; $env:CHECKOUT_SQL_RUNTIME=$b.get_ConnectionString()
    dotnet "$PSScriptRoot/sql-bin/CheckoutSqlTests.dll" "$PSScriptRoot/manifest.json" "$PSScriptRoot/sql-results.json" *> "$PSScriptRoot/sql-run.log"
    if($LASTEXITCODE -ne 0){throw 'SQL regression failed; inspect log/results'}
    Get-Content "$PSScriptRoot/sql-run.log" -Tail 5
} finally {[Array]::Clear($bytes,0,$bytes.Length);$env:CHECKOUT_SQL_RUNTIME=$previousRuntime}
