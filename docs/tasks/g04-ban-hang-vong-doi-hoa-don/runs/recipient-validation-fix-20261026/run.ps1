$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:G04_HEADER_RUNTIME
try {
 $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
 if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
 $b['Initial Catalog']='CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout';$env:G04_HEADER_RUNTIME=$b.get_ConnectionString()
 $before=@(Get-ChildItem "$root/src" -Recurse -File | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'} | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}})
 $before | Export-Csv "$PSScriptRoot/source-before.csv" -NoTypeInformation
 dotnet build "$PSScriptRoot/HeaderRegression.csproj" -p:NuGetAudit=false -o "$PSScriptRoot/ComponentBin" *> "$PSScriptRoot/component-build.log"
 if($LASTEXITCODE -ne 0){Get-Content "$PSScriptRoot/component-build.log" -Tail 12;throw 'Component build failed'}
 dotnet "$PSScriptRoot/ComponentBin/HeaderRegression.dll" $PSScriptRoot *> "$PSScriptRoot/header-runner.log"
 $runnerExit=$LASTEXITCODE
 $changed=@($before | Where-Object {(Get-FileHash -LiteralPath $_.Path).Hash -cne $_.Hash})
 [ordered]@{SourceFiles=$before.Count;ChangedSourceFiles=$changed.Count;ExitCode=$runnerExit} | ConvertTo-Json | Set-Content "$PSScriptRoot/source-audit.json"
 Get-Content "$PSScriptRoot/header-runner.log" -Tail 14
 if($runnerExit -ne 0 -or $changed.Count -ne 0){throw 'Regression did not pass; preserve source and evidence'}
} finally {[Array]::Clear($bytes,0,$bytes.Length);$env:G04_HEADER_RUNTIME=$previousRuntime}


