$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../../../../../tools/database/DatabaseTestTools.ps1')
$m=Get-Content (Join-Path $PSScriptRoot 'native-manifest.json') -Raw|ConvertFrom-Json
function Open-G06Connection([string]$db) {
 $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new();$b['Data Source']='localhost';$b['Initial Catalog']=$db;$b['Integrated Security']=$true;$b['Encrypt']=$true;$b['TrustServerCertificate']=$true;$b['Pooling']=$false
 $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();return $c
}
$owner=Open-G06Connection $m.Database
try {
 if((Read-TestScalar $owner 'SELECT RunId FROM dbo.G06TestMarker') -cne $m.RunId){throw 'Marker mismatch'}
 Run-TestSql $owner "DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1),@h varchar(255)=(SELECT MatKhauHash FROM dbo.TAIKHOAN WHERE MaTK=1),@r uniqueidentifier=NEWID(),@s2 binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=2); EXEC dbo.sp_AccountWrite @Actor=1,@Stamp=@s,@Action='Reset',@RequestId=@r,@Employee='NV004',@ExpectedStamp=@s2,@NewHash=@h; SET @r=NEWID();SELECT @s2=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=2;EXEC dbo.sp_AccountWrite @Actor=1,@Stamp=@s,@Action='Change',@RequestId=@r,@Employee='NV004',@ExpectedStamp=@s2,@Role='CASH',@Active=1;SET @r=NEWID();SELECT @s2=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=3; EXEC dbo.sp_AccountWrite @Actor=1,@Stamp=@s,@Action='Reset',@RequestId=@r,@Employee='NV010',@ExpectedStamp=@s2,@NewHash=@h;SET @r=NEWID();SELECT @s2=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=3;EXEC dbo.sp_AccountWrite @Actor=1,@Stamp=@s,@Action='Change',@RequestId=@r,@Employee='NV010',@ExpectedStamp=@s2,@Role='STORE',@Active=1;"
 function Read-State($c){$q=$c.CreateCommand();$q.CommandText='SELECT @@TRANCOUNT TranCount,XACT_STATE() XactState,(SELECT open_transaction_count FROM sys.dm_exec_sessions WHERE session_id=@@SPID) OpenTransactions;';try{$r=$q.ExecuteReader();[void]$r.Read();$o=[ordered]@{};for($i=0;$i -lt $r.FieldCount;$i++){$o[$r.GetName($i)]=$r.GetValue($i)};$r.Dispose();return [pscustomobject]$o}finally{$q.Dispose()}}
 $cases=@(
 @{Id='T24-K02';Sub='SqlName51';Actor=1;Expected=53101;Sql="EXEC dbo.sp_EmployeeWrite @Actor=@a,@Stamp=@s,@Action='Create',@RequestId=@r,@Name=@n,@Contract=N'Full-time',@MonthlyRate=0,@HourlyRate=0,@Position='CV001'"},
 @{Id='T24-K06';Sub='SetupDenied';Actor=1;Expected=229;Sql="EXEC dbo.sp_SetupBootstrap @Mode='ExistingEmployee',@Username=N'reopen',@NewHash='test hash not submitted',@MaNV='NV002'"},
 @{Id='T24-K10';Sub='CashSQL';Actor=2;Expected=51605;Sql='EXEC dbo.sp_EmployeeList @Actor=@a,@Stamp=@s'},
 @{Id='T24-K10';Sub='StoreSQL';Actor=3;Expected=51605;Sql='EXEC dbo.sp_EmployeeList @Actor=@a,@Stamp=@s'},
 @{Id='T26-K07';Sub='DirectPayrollDml';Actor=1;Expected=229;Sql='UPDATE dbo.BANGLUONG SET Thuong=Thuong+1 WHERE MaBL=1'},
 @{Id='T26-K12';Sub='CashSQL';Actor=2;Expected=51605;Sql='EXEC dbo.sp_PayrollList @Actor=@a,@Stamp=@s,@Month=9,@Year=2026'},
 @{Id='T26-K12';Sub='StoreSQL';Actor=3;Expected=51605;Sql='EXEC dbo.sp_PayrollList @Actor=@a,@Stamp=@s,@Month=9,@Year=2026'}
 )
 $results=@()
 foreach($case in $cases){
  $q=$owner.CreateCommand();$q.CommandText='SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@a';[void]$q.Parameters.AddWithValue('@a',$case.Actor);$stamp=$q.ExecuteScalar();$q.Dispose()
  $c=Open-G06Connection $m.Database
  try {
   Run-TestSql $c "EXECUTE AS USER='G06Runtime';";$before=Read-State $c;$number=$null
   $q=$c.CreateCommand();$q.CommandText=$case.Sql;[void]$q.Parameters.AddWithValue('@a',$case.Actor);[void]$q.Parameters.AddWithValue('@s',$stamp);[void]$q.Parameters.AddWithValue('@r',[guid]::NewGuid());[void]$q.Parameters.AddWithValue('@n',('x'*51))
   try{[void]$q.ExecuteNonQuery()}catch{$ex=$_.Exception;while($ex -and -not ($ex -is [System.Data.SqlClient.SqlException])){$ex=$ex.InnerException};if($ex){$number=$ex.Number}else{throw}}finally{$q.Dispose()}
   $after=Read-State $c;$pass=($number -eq $case.Expected -and $before.TranCount -eq 0 -and $after.TranCount -eq 0 -and $after.OpenTransactions -eq 0 -and $before.XactState -eq $after.XactState)
   $results += [pscustomobject]@{Id=$case.Id;Subcase=$case.Sub;ExpectedError=$case.Expected;ActualError=$number;Before=$before;After=$after;Status=$(if($pass){'PASS'}else{'FAIL'});Criterion='Expected rejection; no explicit/open session transaction; compare XactState to same-connection baseline'}
  }finally{$c.Dispose()}
 }
 $results|ConvertTo-Json -Depth 7|Set-Content (Join-Path $PSScriptRoot 'transaction-followup-v2.json') -Encoding utf8
 $results|Select-Object Id,Subcase,ActualError,Status|Format-Table
}finally{$owner.Dispose()}

