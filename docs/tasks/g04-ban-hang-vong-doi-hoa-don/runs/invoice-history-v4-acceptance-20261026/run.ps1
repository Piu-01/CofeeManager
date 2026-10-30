$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:CHECKOUT_SQL_RUNTIME
try {
    $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
    if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
    $b['Initial Catalog']='CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout';$env:CHECKOUT_SQL_RUNTIME=$b.get_ConnectionString()
    dotnet "$PSScriptRoot/bin/Acceptance.dll" "$PSScriptRoot" *> "$PSScriptRoot/run.log"
    $result=$LASTEXITCODE
    if(Test-Path "$PSScriptRoot/results.json"){$r=Get-Content "$PSScriptRoot/results.json" -Raw | ConvertFrom-Json; $r.Results | Group-Object Outcome | Select-Object Name,Count; $r.Results | Where-Object Outcome -eq FAIL | ConvertTo-Json -Depth 6}
    exit $result
} finally {[Array]::Clear($bytes,0,$bytes.Length);$env:CHECKOUT_SQL_RUNTIME=$previousRuntime}
