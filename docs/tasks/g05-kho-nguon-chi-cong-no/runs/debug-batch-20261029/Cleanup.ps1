$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$db='CoffeeManager_Test_G05_20261029_134725_96d98d09_demo';$marker='20261029_134725_96d98d09';$meta=Get-Content "$PSScriptRoot/gui-fixture.json" -Raw|ConvertFrom-Json
if($meta.Database -cne $db -or $meta.Marker -cne $marker){throw 'Wrong fixture metadata'}
if(Get-Process -Id $meta.GuiProcessId -ErrorAction SilentlyContinue){throw 'GUI still running; close own GUI before cleanup'}
$c=Open-TestConnection $db
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $marker){throw 'Wrong database marker'}
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile("$root/tools/database/Update-DevelopmentG05.ps1",[ref]$tokens,[ref]$errors)
 $assignment=$ast.Find({param($n)$n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -eq '$moneySql'},$true)
 $literal=$assignment.Right.Find({param($n)$n -is [Management.Automation.Language.StringConstantExpressionAst]},$true)
 $financial=[string](Read-TestScalar $c $literal.Value);$previous=Get-Content "$PSScriptRoot/hash-regression.json" -Raw|ConvertFrom-Json
 @{FinancialSourcesUnchangedDuringNative=($financial -ceq $previous.AfterTailInsertHash);Hash=$financial;Purchases=Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUNHAP';Stocktakes=Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUKIEMKE'}|ConvertTo-Json|Set-Content "$PSScriptRoot/native-write-check.json" -Encoding utf8
 $environment=@{SQL=[string](Read-TestScalar $c 'SELECT @@VERSION');Schema=Read-TestScalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations';SDK=(& dotnet --version);Database=$db;Marker=$marker;Dpi=120;DpiSource='layout/component/diagnostic probes; no Windows Scale change';GuiProcessId=$meta.GuiProcessId}
 $environment|ConvertTo-Json|Set-Content "$PSScriptRoot/environment.json" -Encoding utf8
}finally{$c.Dispose();[Data.SqlClient.SqlConnection]::ClearAllPools()}
$master=Open-TestConnection master
try{
 Run-TestSql $master "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$db];"
 $names=@('CoffeeManager_Test_G05_20261029_134725_96d98d09_clean',$db)
 $remaining=@(foreach($name in $names){if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name='$name'") -ne 0){$name}})
}finally{$master.Dispose()}
$temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\');$target=[IO.Path]::GetFullPath([string]$meta.PrivateDirectory);$expected=[IO.Path]::GetFullPath((Join-Path $temp ('CoffeeManager_G05_'+$marker)))
if($target -cne $expected -or -not $target.StartsWith($temp+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Private cleanup target outside expected temp path'}
if((Get-Content -LiteralPath (Join-Path $target 'marker.txt') -Raw).Trim() -cne $marker){throw 'Private marker mismatch'}
Remove-Item -LiteralPath $target -Recurse -Force
@{Created=2;Cleaned=2-$remaining.Count;Remaining=$remaining;GuiExited=$true;PrivateCredentialDirectoryDeleted=(-not(Test-Path -LiteralPath $target));DevelopmentWritten=$false;DevelopmentBackupTouched=$false;PendingFilesDeleted=0}|ConvertTo-Json -Depth 5|Set-Content "$PSScriptRoot/resource-audit.json" -Encoding utf8
Write-Output "Test DB cleanup:2 created, $($remaining.Count) remaining; private credentials deleted."
