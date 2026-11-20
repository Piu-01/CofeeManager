$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$path=Join-Path $PSScriptRoot 'native-manifest.json';$m=Get-Content $path -Raw|ConvertFrom-Json
$exe=Join-Path $PSScriptRoot 'bin/Debug/net10.0-windows/G06NativeAcceptance3.exe'
$stopped=@();Get-Process G06NativeAcceptance3 -ErrorAction SilentlyContinue|ForEach-Object{if($_.Path -ine $exe){throw 'Unexpected test process path'};$stopped+=$_.Id;Stop-Process -Id $_.Id}
$db=$m.Database;if($db -cne 'CoffeeManager_Test_G06_20261030_160836_ea0f560f_demo'){throw 'Exact cleanup allowlist'}
$c=Open-TestConnection $db
try{if((Read-TestScalar $c 'SELECT RunId FROM dbo.G06TestMarker') -cne $m.RunId){throw 'Cleanup marker mismatch'};$nativeRows=Read-TestScalar $c "SELECT COUNT(*) FROM dbo.NHANVIEN WHERE TenNV=N'Native G06 Test'";$c.ChangeDatabase('master');Run-TestSql $c "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$db];";$m.Cleaned=@($db);$m.Retained=@();$m.Status='Cleaned';$m|Add-Member -NotePropertyName StoppedNativeTestProcesses -NotePropertyValue $stopped -Force;$m|Add-Member -NotePropertyName NativeEnteredRows -NotePropertyValue $nativeRows -Force;$m|ConvertTo-Json -Depth 8|Set-Content $path -Encoding utf8}finally{$c.Dispose()}
$before=Import-Csv (Join-Path $PSScriptRoot 'source-before.csv');$after=@(Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests'),(Join-Path $root 'tools') -Recurse -File|Where-Object{$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj','.ps1'}|Sort-Object FullName|ForEach-Object{[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$after|Export-Csv (Join-Path $PSScriptRoot 'source-final.csv') -NoTypeInformation
$diff=@(Compare-Object $before $after -Property Path,Hash)
$c=Open-TestConnection 'master';try{$remaining=Read-TestScalar $c "SELECT (SELECT name FROM sys.databases WHERE name LIKE 'CoffeeManager[_]Test[_]G06[_]%' FOR JSON PATH)"}finally{$c.Dispose()}
@{SourceUnchanged=($diff.Count -eq 0);Difference=$diff;RemainingG06Databases=$remaining;NativeInput='BLOCKED: tool targets disabled root; modal indices unavailable; coordinate/key/type actions leave name empty';NativeEnteredRows=$nativeRows}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $PSScriptRoot 'final-audit.json') -Encoding utf8
Get-Content (Join-Path $PSScriptRoot 'final-audit.json')

