# One-time cleanup explicitly approved by user on 18/10/2026.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$allow=@(Import-Csv (Join-Path $PSScriptRoot 'database-inventory.csv') | ForEach-Object Database)
if ($allow.Count -ne 112 -or @($allow | Select-Object -Unique).Count -ne 112 -or @($allow | Where-Object {$_ -cnotmatch '^CoffeeManager_Test_[A-Za-z0-9_]+$'}).Count) { throw 'Invalid exact allowlist' }
$c=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
function Query([string]$sql) { $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=30; $t=[Data.DataTable]::new(); try {$r=$cmd.ExecuteReader(); try {$t.Load($r)} finally {$r.Dispose()}} finally {$cmd.Dispose()}; return ,$t }
function Execute([string]$sql) { $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=60; try {[void]$cmd.ExecuteNonQuery()} finally {$cmd.Dispose()} }
function Quote([string]$name) { return '['+$name.Replace(']',']]')+']' }
$c.Open()
try {
 $current=Query "SELECT name FROM sys.databases WHERE name LIKE 'CoffeeManager[_]Test[_]%';"
 if (Compare-Object ($allow | Sort-Object) (@($current.Rows | ForEach-Object name) | Sort-Object)) {throw 'Inventory changed'}
 $user=Get-LocalUser -Name CafeManagerApp
 $sid=$user.SID.Value
 if ($sid -ne 'S-1-5-21-995916740-4115604996-2892334199-1007') {throw 'Windows identity changed'}
 if (@(Get-CimInstance Win32_Service | Where-Object {$_.StartName -match '(^|\\)CafeManagerApp$'}).Count) {throw 'Service dependency'}
 if (@(Get-ScheduledTask | Where-Object {$_.Principal.UserId -eq $sid -or $_.Principal.UserId -match '(^|\\)CafeManagerApp$'}).Count) {throw 'Task dependency'}
 $profiles=@(Get-CimInstance Win32_UserProfile | Where-Object SID -eq $sid)
 if (@($profiles | Where-Object Loaded).Count) {throw 'Profile loaded'}
 $profiles | Select-Object SID,LocalPath,Loaded | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'cleanup-profile-inventory.json')
 $sessions=Query "SELECT session_id FROM sys.dm_exec_sessions WHERE is_user_process=1 AND (login_name LIKE '%\CafeManagerApp' OR DB_NAME(database_id) LIKE 'CoffeeManager[_]Test[_]%');"
 if ($sessions.Rows.Count) {throw 'Target sessions active; no forced disconnect'}
 $login=$env:COMPUTERNAME+'\CafeManagerApp'
 $preserved=Query "SELECT name,state_desc FROM sys.databases WHERE database_id>4 AND name NOT LIKE 'CoffeeManager[_]Test[_]%';"
 foreach($db in $preserved.Rows) {
  if ($db.state_desc -ne 'ONLINE') { Write-Host ('Skipped dependency inspection: '+$db.name+' '+$db.state_desc); continue }
  $c.ChangeDatabase($db.name)
  $deps=Query "SELECT referenced_database_name FROM sys.sql_expression_dependencies WHERE referenced_database_name LIKE 'CoffeeManager[_]Test[_]%'; SELECT name FROM sys.database_principals WHERE sid=SUSER_SID(N'$login');"
  if ($deps.Rows.Count) {throw "Preserved DB dependency: $($db.name)"}
  # Check principal separately: DataTable.Load reads only first result set.
  $mapping=Query "SELECT name FROM sys.database_principals WHERE sid=SUSER_SID(N'$login');"
  if ($mapping.Rows.Count) {throw "Preserved DB user mapping: $($db.name)"}
 }
 $c.ChangeDatabase('master')
 $owners=Query "SELECT name FROM sys.databases WHERE owner_sid=SUSER_SID(N'$login');"
 if($owners.Rows.Count) {throw 'Login owns database'}
 $log=Join-Path $PSScriptRoot 'cleanup-actions.txt'
 "Approved cleanup started $(Get-Date -Format o)" | Set-Content $log
 foreach($db in $allow) { Execute ('DROP DATABASE '+(Quote $db)+';'); Add-Content $log "DROPPED DB $db" }
 Execute ('DROP LOGIN '+(Quote $login)+';')
 Add-Content $log "DROPPED SQL LOGIN $login"
 Remove-LocalUser -SID $user.SID
 Add-Content $log "REMOVED WINDOWS USER CafeManagerApp $sid"
 $remaining=Query "SELECT name FROM sys.databases WHERE name LIKE 'CoffeeManager[_]Test[_]%';"
 if($remaining.Rows.Count) {throw 'Test DB remains'}
 if(Get-LocalUser -Name CafeManagerApp -ErrorAction SilentlyContinue) {throw 'Windows user remains'}
 $left=Query "SELECT name FROM sys.server_principals WHERE name=N'$login';"
 if($left.Rows.Count) {throw 'SQL login remains'}
 $c.ChangeDatabase('CoffeeManager_Development')
 $ready=Query 'SELECT SchemaReady FROM dbo.DevelopmentMarker WHERE Id=1;'
 if($ready.Rows.Count -ne 1 -or -not $ready.Rows[0].SchemaReady) {throw 'Development marker invalid'}
 Add-Content $log 'VERIFIED: 0 target DBs; Windows user absent; old SQL login absent; development marker ready.'
 'Cleanup PASS: 112 test DBs, old SQL login and Windows account removed; development DB retained.'
} finally {$c.Dispose()}

