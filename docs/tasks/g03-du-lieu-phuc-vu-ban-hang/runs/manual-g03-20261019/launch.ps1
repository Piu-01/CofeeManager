$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$path=Join-Path $PSScriptRoot 'manifest.json'
$m=Get-Content $path -Raw | ConvertFrom-Json
if($m.Database -cne 'CoffeeManager_Test_G03_20261019_215457_29e552a9_demo' -or $m.Cleaned){throw 'Unexpected test target'}
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
 $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
 if($b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected credential profile'}
 $b['Initial Catalog']=$m.Database
 $psi=[Diagnostics.ProcessStartInfo]::new((Join-Path $root 'src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe'))
 $psi.UseShellExecute=$false;$psi.Environment['CAFEMANAGER_APP_CONNECTION']=$b.get_ConnectionString()
 $psi.Environment.Remove('CAFEMANAGER_USE_DEVELOPMENT') | Out-Null
 $process=[Diagnostics.Process]::Start($psi)
 $m.ProcessId=$process.Id;$m.State='Awaiting manual login; test database only'
 $m | ConvertTo-Json | Set-Content $path
 Write-Output "Test app PID $($process.Id); target $($m.Database)"
}finally{[Array]::Clear($bytes,0,$bytes.Length)}


