$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$db=$m.Database
$results=[Collections.Generic.List[object]]::new()
function Case([string]$Id,[string]$Expected,[string]$Sql,[int]$ErrorNumber=0,[string]$Role='ADMIN',[bool]$Runtime=$false){
 $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new("Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();$r=[ordered]@{Id=$Id;Expected=$Expected;Actual=$null;Status='NOT RUN';Role=$Role;RuntimePrincipal=$Runtime;SqlFile="edge-$Id.sql"}
 try{
  if((Read-TestScalar $c 'SELECT RunId FROM dbo.G03TestMarker') -cne $m.RunId){throw 'Marker mismatch'}
  $prefix="DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='$Role' AND HoatDong=1 ORDER BY MaTK;"
  if($Runtime){$prefix+="EXECUTE AS USER='G03Runtime';"}
  $command=$prefix+"`n"+$Sql;$command|Set-Content "$out/edge-$Id.sql" -Encoding utf8
  if($ErrorNumber){Expect-TestSqlError $c $command $ErrorNumber;$r.Actual="SQL error $ErrorNumber; transaction 0:0"}else{Run-TestSql $c $command;if((Read-TestScalar $c 'DECLARE @t int=@@TRANCOUNT,@x int=XACT_STATE();SELECT CONCAT(@t,'':'',@x);') -ne '0:0'){throw 'Transaction leaked'};$r.Actual='Assertions completed; transaction 0:0'}
  $r.Status='PASS'
 }catch{$r.Status='FAIL';$r.Actual=$_.Exception.Message}
 finally{$c.Dispose();$results.Add($r);$results|ConvertTo-Json -Depth 5|Set-Content "$out/sql-material-edge-results.json" -Encoding utf8}
}



