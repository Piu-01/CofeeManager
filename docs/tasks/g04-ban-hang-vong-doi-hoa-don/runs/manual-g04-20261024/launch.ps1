param([string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$path = Join-Path $PSScriptRoot 'manifest.json'
$m = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
if ($m.Database -cnotmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_manual$' -or !$m.Created -or $m.Cleaned -or $m.State -notlike 'Ready*') { throw 'Fixture not ready' }
$exe = if ($ExecutablePath) { [IO.Path]::GetFullPath($ExecutablePath) } else { Join-Path $root 'docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/checkout-submit-fix-20261025/Debug/CafeManager.WinForms.exe' }
if (!(Test-Path $exe)) { throw 'Build Debug first' }
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$plain = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')), [Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'), [Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
    $b = [System.Data.Common.DbConnectionStringBuilder]::new()
    $b.set_ConnectionString([Text.Encoding]::UTF8.GetString($plain))
    if ($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development') { throw 'Unexpected saved connection' }
    $b['Initial Catalog'] = $m.Database
    $start = [Diagnostics.ProcessStartInfo]::new($exe); $start.UseShellExecute=$false
    $start.Environment['CAFEMANAGER_APP_CONNECTION']=$b.get_ConnectionString()
    $start.Environment.Remove('CAFEMANAGER_USE_DEVELOPMENT') | Out-Null
    $p = [Diagnostics.Process]::Start($start)
    $m.ProcessIds=@($m.ProcessIds)+$p.Id
    $m | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output "G04 TEST PID $($p.Id); DB $($m.Database); normal login required"
} finally { [Array]::Clear($plain,0,$plain.Length) }
