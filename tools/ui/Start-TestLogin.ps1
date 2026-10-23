param(
    [string]$ManifestPath,
    [string]$ExecutablePath
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$ManifestPath) { $ManifestPath = Join-Path $taskRoot 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/manual-g03-20261019/manifest.json' }
if (!$ExecutablePath) { $ExecutablePath = Join-Path $taskRoot 'docs/tasks/temporary-test-login/runs/build/Debug/CafeManager.WinForms.exe' }
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.Server -cne 'localhost' -or $manifest.Database -cnotlike 'CoffeeManager_Test_*' -or $manifest.Cleaned -ne $false -or !$manifest.Marker -or $manifest.Marker.Length -gt 40) {
    throw 'Manifest không xác định một DB test local còn dùng được.'
}
if (!(Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) { throw 'Chưa có bản Debug; build bản test trước khi chạy launcher.' }
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$profilePath = Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi'
$plain = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($profilePath), [Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'), [Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
    $target = [System.Data.Common.DbConnectionStringBuilder]::new()
    $target.ConnectionString = [Text.Encoding]::UTF8.GetString($plain)
    if ($target['Data Source'] -cne 'localhost' -or $target['Initial Catalog'] -cne 'CoffeeManager_Development') { throw 'Profile kết nối local không khớp.' }
    $target['Initial Catalog'] = $manifest.Database
    $start = [Diagnostics.ProcessStartInfo]::new([IO.Path]::GetFullPath($ExecutablePath))
    $start.UseShellExecute = $false
    $start.Environment['CAFEMANAGER_APP_CONNECTION'] = $target.ConnectionString
    $start.Environment.Remove('CAFEMANAGER_USE_DEVELOPMENT') | Out-Null
    $start.Environment.Remove('CAFEMANAGER_TEST_MANIFEST') | Out-Null
    $start.Environment['CAFEMANAGER_TEST_LOGIN'] = '1'
    $start.Environment['CAFEMANAGER_TEST_DATABASE'] = $manifest.Database
    $start.Environment['CAFEMANAGER_TEST_MARKER'] = $manifest.Marker
    $process = [Diagnostics.Process]::Start($start)
    Write-Output "CoffeeManager TEST PID $($process.Id); DB: $($manifest.Database). Bấm 'Bỏ qua đăng nhập — TEST'."
} finally {
    [Array]::Clear($plain, 0, $plain.Length)
}
