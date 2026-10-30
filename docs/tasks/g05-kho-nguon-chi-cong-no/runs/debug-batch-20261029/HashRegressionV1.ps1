$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$database='CoffeeManager_Test_G05_20261029_134725_96d98d09_demo';$marker='20261029_134725_96d98d09';$c=Open-TestConnection $database
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne $marker){throw 'Wrong test DB marker'}
 $tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile("$root/tools/database/Update-DevelopmentG05.ps1",[ref]$tokens,[ref]$parseErrors)
 if($parseErrors.Count){throw 'Upgrade script parse failed'}
 $assignment=$ast.Find({param($node)$node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$moneySql'},$true)
 $sql=[string]$assignment.Right.SafeGetValue()
 $prefix="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),("
 if(-not $sql.StartsWith($prefix) -or -not $sql.EndsWith('))),2);')){throw 'Unexpected source SQL expression'}
 $legacy=$sql.Substring($prefix.Length);$legacy=$legacy.Substring(0,$legacy.Length-8)+';'
 $length=[int](Read-TestScalar $c ('SELECT LEN(('+ $legacy.TrimEnd(';') +'))'))
 $before=[string](Read-TestScalar $c $sql);$same=[string](Read-TestScalar $c $sql);$chunkBefore=[string](Read-TestScalar $c $legacy)
 Run-TestSql $c "IF USER_ID('G05Runtime') IS NULL CREATE USER G05Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G05Runtime;"
 Run-TestSql $c "DECLARE @a int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1),@s binary(8),@request uniqueidentifier=NEWID(),@date datetime2(0)=SYSDATETIME();SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@a;EXECUTE AS USER='G05Runtime';BEGIN TRY EXEC dbo.sp_FinanceCreate @a,@s,@request,@date,'TC001',1000,N'G05 tail hash regression',NULL;REVERT;END TRY BEGIN CATCH REVERT;THROW;END CATCH;"
 $after=[string](Read-TestScalar $c $sql);$chunkAfter=[string](Read-TestScalar $c $legacy)
 $result=[ordered]@{Part='B04.FullFinancialSourcesHash';Result=if($before -match '^[A-F0-9]{64}$' -and $before -ceq $same -and $before -cne $after -and $length -gt $chunkBefore.Length -and $chunkBefore -ceq $chunkAfter){'PASS'}else{'FAIL'};Source='tools/database/Update-DevelopmentG05.ps1 moneySql AST; upgrade script not executed';Database=$database;PayloadLength=$length;LegacyFirstChunkLength=$chunkBefore.Length;LegacyChunkUnchanged=($chunkBefore -ceq $chunkAfter);BeforeHash=$before;RepeatHash=$same;AfterTailInsertHash=$after}
 $result|ConvertTo-Json -Depth 5|Set-Content "$PSScriptRoot/hash-regression.json" -Encoding utf8;Write-Output "Hash regression=$($result.Result); JSON length=$length; old chunk=$($chunkBefore.Length)"
}finally{$c.Dispose()}
