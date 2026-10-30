$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$manifest=Join-Path $root 'docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/manual-g04-20261024/manifest.json'
$m=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
if($m.Database -cne 'CoffeeManager_Test_G04_20261024_152759_12204643_manual' -or $m.Cleaned){throw 'Unexpected fixture'}
Add-Type -AssemblyName System.Security.Cryptography.ProtectedData
$bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $env:LOCALAPPDATA 'CoffeeManager/development/connection.dpapi')),[Text.Encoding]::UTF8.GetBytes('CoffeeManager.Development.Connection.v1'),[Security.Cryptography.DataProtectionScope]::CurrentUser)
$previousRuntime=$env:G04_AUTH_RUNTIME
try {
 $b=[System.Data.Common.DbConnectionStringBuilder]::new();$b.set_ConnectionString([Text.Encoding]::UTF8.GetString($bytes))
 if($b['Data Source'] -cne 'localhost' -or $b['Initial Catalog'] -cne 'CoffeeManager_Development'){throw 'Unexpected saved profile'}
 $b['Initial Catalog']=$m.Database;$env:G04_AUTH_RUNTIME=$b.get_ConnectionString()
 $sourceFiles=@(Get-ChildItem "$root/src" -File -Recurse | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'})
 $before=@($sourceFiles | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}})
 $before | Export-Csv "$PSScriptRoot/source-before.csv" -NoTypeInformation
 dotnet run --project "$PSScriptRoot/AuthRegression.csproj" -- $manifest "$PSScriptRoot/results.json" *> "$PSScriptRoot/runner.log"
 $runnerExit=$LASTEXITCODE
 $changed=@($before | Where-Object {(Get-FileHash -LiteralPath $_.Path).Hash -cne $_.Hash})
 [ordered]@{ProductSourceFiles=$before.Count;ChangedProductFiles=$changed.Count;ExitCode=$runnerExit} | ConvertTo-Json | Set-Content "$PSScriptRoot/source-audit.json"
 Get-Content "$PSScriptRoot/runner.log" -Tail 12
 if($runnerExit -ne 0 -or $changed.Count -ne 0){throw 'Regression failed; see preserved artifacts'}
}finally{[Array]::Clear($bytes,0,$bytes.Length);$env:G04_AUTH_RUNTIME=$previousRuntime}
