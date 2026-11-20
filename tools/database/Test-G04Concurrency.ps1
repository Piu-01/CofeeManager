[CmdletBinding()]param([switch]$ApprovedConcurrency)
if(-not $ApprovedConcurrency){throw 'Dot concurrency G04 phai duoc nguoi dung duyet rieng.'}
# TS04: two real connections create an order on the same table at the same time.
# The winner keeps the table lock, the loser must get 52104 after the winner commits.
# Gate connection holds UPDLOCK on BAN so both commands are provably in flight together.
$ThroughVersion=18
$SchemaVersion=35
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
Add-Type -AssemblyName System.Data
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/concurrency_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='TS04 two-connection table race; technical verdict only';Server='localhost';ThroughVersion=$ThroughVersion;SchemaVersion=$SchemaVersion;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Created=@();Cleaned=@();Retained=@();Status='Running';Checks=@()}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
function Add-Check([string]$Name,[bool]$Ok,[string]$Detail) {
 $manifest.Checks+=@{Check=$Name;Ok=$Ok;Detail=$Detail}
 if(-not $Ok){throw "TS04 check failed: $Name ($Detail)"}
}
function Get-SqlNumbers($ErrorRecord) {
 $e=$ErrorRecord.Exception
 while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException}
 if(-not $e){return @()}
 return @($e.Errors | ForEach-Object { $_.Number })
}
function New-CreateCommand($Connection,[int]$Actor,[byte[]]$Stamp,[string]$Code,[guid]$RequestId) {
 $cmd=$Connection.CreateCommand(); $cmd.CommandTimeout=120
 $cmd.CommandText="EXEC dbo.sp_TaoHoaDon @Actor=@Actor,@Stamp=@Stamp,@Code=@Code,@RequestId=@RequestId,@Channel=N'Tai quan',@TableCode=N'B0007',@Product=N'SP001',@Quantity=1;"
 [void]$cmd.Parameters.Add('@Actor',[System.Data.SqlDbType]::Int); $cmd.Parameters['@Actor'].Value=$Actor
 [void]$cmd.Parameters.Add('@Stamp',[System.Data.SqlDbType]::Binary,8); $cmd.Parameters['@Stamp'].Value=$Stamp
 [void]$cmd.Parameters.Add('@Code',[System.Data.SqlDbType]::VarChar,10); $cmd.Parameters['@Code'].Value=$Code
 [void]$cmd.Parameters.Add('@RequestId',[System.Data.SqlDbType]::UniqueIdentifier); $cmd.Parameters['@RequestId'].Value=$RequestId
 return $cmd
}
Save-Manifest
$name="CoffeeManager_Test_G04_${runId}_ts04"
$gate=$null; $a=$null; $b=$null; $verify=$null
try {
 $master=Open-TestConnection 'master'
 try {
  if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target already exists'}
  Run-TestSql $master "CREATE DATABASE [$name];"
  $manifest.Created+=@{Database=$name;Marker=$runId;Mode='ts04'}; Save-Manifest
  $master.ChangeDatabase($name)
  Run-TestSql $master "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $seed
  $inventoryBefore=[string](Read-TestScalar $master 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
  for($version=1;$version -le $SchemaVersion;$version++) {
   $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
  }
  if($inventoryBefore -cne [string](Read-TestScalar $master 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')){throw 'Migration changed inventory'}
  Run-TestSql $master "UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');"
 } finally { $master.Dispose() }

 $verify=Open-TestConnection $name
 $actor=[int](Read-TestScalar $verify "SELECT MaTK FROM dbo.TAIKHOAN WHERE MaNV='NV001'")
 $stamp=[byte[]](Read-TestScalar $verify "SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'")
 if($null -eq $stamp -or $stamp.Length -ne 8){throw 'Actor stamp missing'}
 $codeA=[string](Read-TestScalar $verify "DECLARE @t TABLE(Loai varchar(20),Ma char(10)); INSERT @t EXEC dbo.sp_CapMa 'HOADON'; SELECT Ma FROM @t;")
 $codeB=[string](Read-TestScalar $verify "DECLARE @t TABLE(Loai varchar(20),Ma char(10)); INSERT @t EXEC dbo.sp_CapMa 'HOADON'; SELECT Ma FROM @t;")
 if($codeA -eq $codeB){throw 'Code generator returned duplicates'}
 if((Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.PHIEN_BAN WHERE MaBan='B0007'") -ne 0){throw 'B0007 must have no prior session'}

 # Gate holds an update lock on the BAN row so both create commands block inside the proc.
 $gate=Open-TestConnection $name
 $gateCmd=$gate.CreateCommand(); $gateCmd.CommandText="SET XACT_ABORT ON; BEGIN TRAN; DECLARE @x char(5); SELECT @x=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan='B0007'; SELECT @x;"
 $gateCode=$gateCmd.ExecuteScalar(); $gateCmd.Dispose()
 if($gateCode -ne 'B0007'){throw 'Gate lock did not acquire BAN row'}
 $a=Open-TestConnection $name; $b=Open-TestConnection $name
 $cmdA=New-CreateCommand $a $actor $stamp $codeA ([guid]::NewGuid())
 $cmdB=New-CreateCommand $b $actor $stamp $codeB ([guid]::NewGuid())
 $asyncA=$cmdA.BeginExecuteNonQuery()
 $asyncB=$cmdB.BeginExecuteNonQuery()
 Start-Sleep -Seconds 3
 Add-Check 'both-commands-in-flight' ((-not $asyncA.IsCompleted) -and (-not $asyncB.IsCompleted)) ("A=$($asyncA.IsCompleted) B=$($asyncB.IsCompleted)")
 $commit=$gate.CreateCommand(); $commit.CommandText='COMMIT;'; [void]$commit.ExecuteNonQuery(); $commit.Dispose()
 $errA=$null; $errB=$null
 try { [void]$cmdA.EndExecuteNonQuery($asyncA) } catch { $errA=$_ }
 try { [void]$cmdB.EndExecuteNonQuery($asyncB) } catch { $errB=$_ }
 $numA=-join (Get-SqlNumbers $errA); $numB=-join (Get-SqlNumbers $errB)
 Add-Check 'one-winner-one-conflict' (($null -eq $errA) -xor ($null -eq $errB)) "A='$numA' B='$numB'"
 if($null -eq $errA){$winner=$codeA;$winnerConn=$a;$loser=$codeB;$loserErr=$numB}
 else {$winner=$codeB;$winnerConn=$b;$loser=$codeA;$loserErr=$numA}
 Add-Check 'loser-error-52104' ($loserErr -eq '52104') "loser=$loser numbers=$loserErr"
 $stateA=[string](Read-TestScalar $a 'DECLARE @t INT=@@TRANCOUNT,@s INT=XACT_STATE(); SELECT CONCAT(@t,'':'',@s);')
 $stateB=[string](Read-TestScalar $b 'DECLARE @t INT=@@TRANCOUNT,@s INT=XACT_STATE(); SELECT CONCAT(@t,'':'',@s);')
 Add-Check 'winner-connection-clean' ($stateA -eq '0:0') "A=$stateA"
 Add-Check 'loser-connection-clean' ($stateB -eq '0:0') "B=$stateB"

 $openOrders=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='B0007' AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')")
 Add-Check 'single-open-order' ($openOrders -eq 1) "open=$openOrders"
 $rows=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD IN ('$codeA','$codeB')")
 Add-Check 'single-order-row' ($rows -eq 1) "rows=$rows winner=$winner loser=$loser"
 $sessionCnt=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.PHIEN_BAN WHERE MaBan='B0007'")
 $openSessions=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.PHIEN_BAN WHERE MaBan='B0007' AND GioRa IS NULL")
 Add-Check 'single-session' (($sessionCnt -eq 1) -and ($openSessions -eq 1)) "sessions=$sessionCnt open=$openSessions"
 $sessionLink=[string](Read-TestScalar $verify "SELECT CONCAT(h.MaPhien,':',p.MaBan,':',p.MaNVMo,':',CASE WHEN p.GioRa IS NULL THEN 'open' ELSE 'closed' END) FROM dbo.HOADON h JOIN dbo.PHIEN_BAN p ON p.MaPhien=h.MaPhien WHERE h.MaHD='$winner'")
 Add-Check 'winner-session-linked' ($sessionLink -match '^\d+:B0007:NV001:open$') "link=$sessionLink"
 $loserLines=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD='$loser'")
 $winnerLines=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD='$winner'")
 Add-Check 'no-loser-rows' (($loserLines -eq 0) -and ($winnerLines -eq 1)) "loser=$loserLines winner=$winnerLines"
 $payments=[int](Read-TestScalar $verify "SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD IN ('$codeA','$codeB')")
 Add-Check 'no-payment-written' ($payments -eq 0) "payments=$payments"
 $inventoryAfter=[string](Read-TestScalar $verify 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 Add-Check 'inventory-unchanged' ($inventoryAfter -ceq $inventoryBefore) 'inventory compared'
 $manifest['Winner']=$winner; $manifest['Loser']=$loser; $manifest['LoserError']=$loserErr
 $manifest.Status='Succeeded'
} catch { $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString() }
finally {
 foreach($c in @($gate,$a,$b,$verify)){if($null -ne $c){try{$c.Dispose()}catch{}}}
 foreach($item in $manifest.Created) {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'; $db=$item.Database
   if($db -notmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_ts04$'){throw 'Cleanup name invalid'}
   $cleanup.ChangeDatabase($db)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G04TestMarker') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db];"
   $manifest.Cleaned+=$db
  } catch { $manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message} }
  finally { if($null -ne $cleanup){$cleanup.Dispose()} }
 }
 if($manifest.Retained.Count -gt 0){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "TS04 evidence: $output; status=$($manifest.Status); retained=$($manifest.Retained.Count)"
}
if($manifest.Status -ne 'Succeeded'){throw "TS04 concurrency run failed: $output/manifest.json"}
