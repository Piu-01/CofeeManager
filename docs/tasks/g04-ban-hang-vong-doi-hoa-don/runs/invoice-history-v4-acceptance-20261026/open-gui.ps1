$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:CHECKOUT_SQL_RUNTIME
try {
    $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
    if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
    $b['Initial Catalog']='CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout';$env:CHECKOUT_SQL_RUNTIME=$b.get_ConnectionString()
    $env:CAFEMANAGER_APP_CONNECTION=$b.get_ConnectionString()
    $exe=(Resolve-Path (Join-Path $PSScriptRoot '../invoice-history-v4-implementation-20261026/Debug/CafeManager.WinForms.exe')).Path
    $process=Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -WindowStyle Normal -PassThru
    [pscustomobject]@{ProcessId=$process.Id;Executable=$exe;Database=$b['Initial Catalog'];Mode='Normal Login, no bypass';RetainReason='User functional/visual check V4';ReviewBy='2026-10-27'} | ConvertTo-Json | Set-Content "$PSScriptRoot/gui-process.json"
    Get-Content "$PSScriptRoot/gui-process.json"
} finally {[Array]::Clear($bytes,0,$bytes.Length);$env:CHECKOUT_SQL_RUNTIME=$previousRuntime;Remove-Item Env:CAFEMANAGER_APP_CONNECTION -ErrorAction SilentlyContinue}

