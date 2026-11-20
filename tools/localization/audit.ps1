param([string]$Root = (Join-Path $PSScriptRoot '../..'))
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path
dotnet run --project (Join-Path $PSScriptRoot 'LocalizationAudit.csproj') -- audit $Root
if ($LASTEXITCODE -ne 0) { throw 'Localization audit failed; see the violations above.' }
