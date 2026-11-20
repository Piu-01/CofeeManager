param([string]$Name='source-before.csv')
$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
Get-ChildItem "$root/src","$root/database","$root/tools" -Recurse -File |
 Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
 Get-FileHash -Algorithm SHA256 |
 Select-Object @{n='Path';e={$_.Path.Substring($root.Length+1)}},Hash |
 Sort-Object Path | Export-Csv (Join-Path $PSScriptRoot $Name) -NoTypeInformation
git -C $root rev-parse HEAD | Set-Content (Join-Path $PSScriptRoot 'head.txt')
git -C $root status --short | Set-Content (Join-Path $PSScriptRoot 'git-status.txt')
