param([Parameter(Mandatory=$true)][string]$Root,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
if(-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Elevated test operator required'}
$root=[IO.Path]::GetFullPath($Root);$Output=[IO.Path]::GetFullPath($Output)
if(-not $Output.StartsWith((Join-Path $root 'docs\tasks\g02-truy-cap-ung-dung-cau-hinh-nen\runs\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Explicit G02 evidence directory required'}
[void][IO.Directory]::CreateDirectory($Output)
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;using System.Text;using System.Collections.Generic;using System.Runtime.InteropServices;
public static class G02WindowsNative {
 public delegate bool Callback(IntPtr h,IntPtr arg);
 [DllImport("user32.dll")]public static extern bool EnumWindows(Callback callback,IntPtr arg);
 [DllImport("user32.dll")]public static extern bool EnumChildWindows(IntPtr parent,Callback callback,IntPtr arg);
 [DllImport("user32.dll")]public static extern int GetWindowThreadProcessId(IntPtr h,out int pid);
 [DllImport("user32.dll")]public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll")]public static extern IntPtr GetDlgItem(IntPtr h,int id);
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,int message,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll")]public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
 [StructLayout(LayoutKind.Sequential)]public struct Rect{public int Left,Top,Right,Bottom;}
 public static string Title(IntPtr h){var b=new StringBuilder(512);GetWindowText(h,b,512);return b.ToString();}
 public static List<IntPtr> Windows(int pid){var list=new List<IntPtr>();EnumWindows((h,a)=>{int p;GetWindowThreadProcessId(h,out p);if(p==pid&&IsWindowVisible(h))list.Add(h);return true;},IntPtr.Zero);return list;}
 public static IntPtr OkButton(IntPtr parent){IntPtr found=IntPtr.Zero;EnumChildWindows(parent,(h,a)=>{if(Title(h).Replace("&","")=="OK"){found=h;return false;}return true;},IntPtr.Zero);return found;}
}
'@
[void][G02WindowsNative]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
$run=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$suffix=$run.Substring($run.Length-8);$names=@(('CMG02S_'+$suffix),('CMG02R_'+$suffix))
$created=[Collections.Generic.List[object]]::new();$processes=[Collections.Generic.List[Diagnostics.Process]]::new();$records=[Collections.Generic.List[object]]::new();$cleanup=[Collections.Generic.List[object]]::new()
$stage='prepare';$db=$null;$owner=$null;$logins=[Collections.Generic.List[string]]::new();$status='BLOCKED';$work=Join-Path $env:LOCALAPPDATA ('Temp\opencode\G02-Windows-'+$run)
function Record($id,$expected,$actual,$outcome='PASS'){$records.Add([ordered]@{Id=$id;Expected=$expected;Actual=$actual;Status=$outcome})}
function Need($value,$message){if(-not $value){throw $message}}
function New-Secret { $bytes=[byte[]]::new(24);$rng=[Security.Cryptography.RandomNumberGenerator]::Create();try{$rng.GetBytes($bytes);return 'G!a9'+[Convert]::ToBase64String($bytes)}finally{$rng.Dispose()} }
function Wait-Value([scriptblock]$Body,[string]$Message,[int]$Ms=30000){$watch=[Diagnostics.Stopwatch]::StartNew();while($watch.ElapsedMilliseconds -lt $Ms){try{$value=& $Body;if($value){return $value}}catch{};Start-Sleep -Milliseconds 100};throw $Message}
function Grant-Folder($path,$sid,$rights){$acl=Get-Acl -LiteralPath $path;$rule=[Security.AccessControl.FileSystemAccessRule]::new($sid,$rights,([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit),[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow);[void]$acl.AddAccessRule($rule);Set-Acl -LiteralPath $path -AclObject $acl}
function Launch($file,$arguments,$account,$password,$runtime){
 $si=[Diagnostics.ProcessStartInfo]::new();$si.FileName=$file;$si.Arguments=$arguments;$si.UseShellExecute=$false;$si.LoadUserProfile=$false;$si.Domain=$env:COMPUTERNAME;$si.UserName=$account;$si.Password=ConvertTo-SecureString $password -AsPlainText -Force;$si.WorkingDirectory=Split-Path $file
 $si.EnvironmentVariables.Clear();foreach($key in @('SystemRoot','WINDIR','SystemDrive','ComSpec','ProgramFiles','ProgramFiles(x86)','ProgramData','PROCESSOR_ARCHITECTURE')){if([Environment]::GetEnvironmentVariable($key)){$si.EnvironmentVariables[$key]=[Environment]::GetEnvironmentVariable($key)}}
 $profile=Join-Path $work ('profiles\'+$account);[void][IO.Directory]::CreateDirectory($profile);$si.EnvironmentVariables['USERPROFILE']=$profile;$si.EnvironmentVariables['TEMP']=$profile;$si.EnvironmentVariables['TMP']=$profile;$si.EnvironmentVariables['PATH']="$env:SystemRoot\System32;C:\Program Files\dotnet";$si.EnvironmentVariables['DOTNET_CLI_TELEMETRY_OPTOUT']='1'
 $si.EnvironmentVariables['CAFEMANAGER_APP_CONNECTION']=if($runtime){"Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False"}else{"Server=localhost,1;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1"}
 try{$p=[Diagnostics.Process]::Start($si);$processes.Add($p);return $p}finally{$si.Password.Dispose()}
}
function Window($p,$pattern){return Wait-Value {if($p.HasExited){throw 'Test process exited before window'};[G02WindowsNative]::Windows($p.Id)|Where-Object {[G02WindowsNative]::Title($_) -like $pattern}|Select-Object -First 1} 'Expected native app window unavailable'}
function Element($handle,$name,$kind){$window=[System.Windows.Automation.AutomationElement]::FromHandle($handle);$conditions=[System.Windows.Automation.Condition[]]@([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$name),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,$kind));return $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.AndCondition]::new($conditions))}
function Set-Input($handle,$name,$text){$e=Element $handle $name ([System.Windows.Automation.ControlType]::Edit);Need ($null -ne $e) ('Input absent: '+$name);([System.Windows.Automation.ValuePattern]$e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($text)}
function Click($element){Need ($null -ne $element) 'Native action absent';([System.Windows.Automation.InvokePattern]$element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()}
function Capture($handle,$name){$r=[G02WindowsNative+Rect]::new();Need ([G02WindowsNative]::GetWindowRect($handle,[ref]$r)) 'Native bounds unavailable';$b=[Drawing.Bitmap]::new($r.Right-$r.Left,$r.Bottom-$r.Top);$g=[Drawing.Graphics]::FromImage($b);try{$dc=$g.GetHdc();try{Need ([G02WindowsNative]::PrintWindow($handle,$dc,2)) 'Capture failed'}finally{$g.ReleaseHdc($dc)};$b.Save((Join-Path $Output ($name+'.png')))}finally{$g.Dispose();$b.Dispose()}}
function Close-App($p,$h){[void][G02WindowsNative]::PostMessage($h,0x0010,[IntPtr]::Zero,[IntPtr]::Zero);Need ($p.WaitForExit(15000)) 'App did not close';Need ($p.ExitCode -eq 0) 'App exit was not successful'}
function Confirm-Ok($p,$h){$button=[G02WindowsNative]::GetDlgItem($h,1);if($button -eq [IntPtr]::Zero){$button=[G02WindowsNative]::OkButton($h)};if($button -ne [IntPtr]::Zero){[void][G02WindowsNative]::PostMessage($button,0x00F5,[IntPtr]::Zero,[IntPtr]::Zero)}else{Click (Element $h 'OK' ([System.Windows.Automation.ControlType]::Button))}}
function Owner-Name($p){$instance=Get-CimInstance Win32_Process -Filter ("ProcessId="+$p.Id);$identity=Invoke-CimMethod -InputObject $instance -MethodName GetOwner;Need ($identity.ReturnValue -eq 0) 'Cannot inspect real process owner';return $identity.Domain+'\'+$identity.User}
function Run-Probe($account,$password,$role){$file=Join-Path $diagnostics ($role+'.json');$p=Launch $probeExe ('"'+$db+'" '+$role+' "'+$file+'"') $account $password $true;Need ($p.WaitForExit(30000)) 'Principal probe timeout';Need (Test-Path $file) 'Principal probe did not write evidence';$data=Get-Content $file -Raw|ConvertFrom-Json;Copy-Item $file (Join-Path $Output ($role+'-principal.json'));Need ($p.ExitCode -eq 0 -and $data.Status -eq 'PASS') ('Principal probe failed: '+$role);Record ('WindowsPrincipal.'+$role) 'Real Windows/SQL identity and separate roles, patched runtime; no impersonation' $data.Checks}
try{
 $stage='create test DB';$db=New-IsolatedDatabase 'G02' $run $false
 & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $db|Out-Null
 & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $db -Version 2 -File (Join-Path $root 'database/migrations/002_auth_and_bootstrap.sql')|Out-Null
 & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $db -Version 3 -File (Join-Path $root 'database/migrations/003_reference_catalogs.sql')|Out-Null
 $owner=Open-TestConnection $db;Assert-TestTarget $owner 'G02';Need ((Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.G02TestMarker') -eq 1) 'Marker absent'
 $stage='prepare deployment';[void][IO.Directory]::CreateDirectory($work);$app=Join-Path $work 'app';$probe=Join-Path $work 'probe';$diagnostics=Join-Path $work 'diagnostics';[void][IO.Directory]::CreateDirectory($app);[void][IO.Directory]::CreateDirectory($probe);[void][IO.Directory]::CreateDirectory($diagnostics)
 Copy-Item (Join-Path $root 'src/CafeManager.WinForms/bin/Debug/net10.0-windows/*') $app -Recurse
 Copy-Item 'C:/Users/Lenovo/AppData/Local/Temp/opencode/G02WindowsPrincipalProbe/bin/Debug/net10.0-windows/*' $probe -Recurse
 $appExe=Join-Path $app 'CafeManager.WinForms.exe';$probeExe=Join-Path $probe 'G02WindowsPrincipalProbe.exe';Need ((Test-Path $appExe) -and (Test-Path $probeExe)) 'Compiled app/probe missing'
 $setupPassword=New-Secret;$runtimePassword=New-Secret;$appPassword=New-Secret
 for($i=0;$i -lt 2;$i++){
  $stage='create temporary Windows identity';Need (-not (Get-LocalUser -Name $names[$i] -ErrorAction SilentlyContinue)) 'Refusing existing Windows account'
  $password=if($i -eq 0){$setupPassword}else{$runtimePassword};$user=New-LocalUser -Name $names[$i] -Password (ConvertTo-SecureString $password -AsPlainText -Force) -AccountExpires (Get-Date).AddDays(1) -UserMayNotChangePassword -Description ('CoffeeManager G02 '+$suffix);$created.Add([ordered]@{Name=$user.Name;Sid=$user.SID.Value});Grant-Folder $work $user.SID ([Security.AccessControl.FileSystemRights]::ReadAndExecute);Grant-Folder $diagnostics $user.SID ([Security.AccessControl.FileSystemRights]::Modify);$profile=Join-Path $work ('profiles\'+$user.Name);[void][IO.Directory]::CreateDirectory($profile);Grant-Folder $profile $user.SID ([Security.AccessControl.FileSystemRights]::Modify)
  $identity=$env:COMPUTERNAME+'\'+$user.Name;$quoted=$identity.Replace(']',']]');Need ((Read-TestScalar $owner "SELECT COUNT(*) FROM sys.server_principals WHERE name=N'$identity'") -eq 0) 'Refusing existing SQL login';Run-TestSql $owner "CREATE LOGIN [$quoted] FROM WINDOWS WITH DEFAULT_DATABASE=[$db];CREATE USER [$quoted] FOR LOGIN [$quoted];";$logins.Add($identity);$role=if($i -eq 0){'CafeManagerSetup'}else{'CafeManagerApp'};Run-TestSql $owner "ALTER ROLE [$role] ADD MEMBER [$quoted]"
 }
 $stage='runtime principal probe before setup';Run-Probe $names[1] $runtimePassword 'runtime'
 $stage='runtime exe setup denied';$denied=Launch $appExe ("--setup --server localhost --database $db") $names[1] $runtimePassword $true;$h=Window $denied '*Thiết lập quản trị*';Need ((Owner-Name $denied) -ieq ($env:COMPUTERNAME+'\'+$names[1])) 'Runtime process owner mismatch';Capture $h 'runtime-setup-denied';Confirm-Ok $denied $h;Need ($denied.WaitForExit(15000)) 'Denied setup process did not exit';Need ((Read-TestScalar $owner 'SELECT State FROM dbo.AppSetupState') -eq 'Pending') 'Denied runtime changed setup';Record 'T06-K02.e.WindowsRuntimeDenied' 'Runtime executable cannot enter bootstrap, SQL229, no write' 'Actual runtime Windows exe rejected; Pending unchanged'
 $stage='clean setup real exe';$setupProcess=Launch $appExe ("--setup --server localhost --database $db") $names[0] $setupPassword $false;$h=Window $setupProcess '*Thiết lập quản trị*';Need ((Owner-Name $setupProcess) -ieq ($env:COMPUTERNAME+'\'+$names[0])) 'Setup process owner mismatch';[void](Wait-Value {$b=Element $h 'Hoàn tất thiết lập' ([System.Windows.Automation.ControlType]::Button);if($b.Current.IsEnabled){$b}} 'Setup not ready')
 Set-Input $h 'Tên nhân viên mới' 'G02 Windows ADMIN';Set-Input $h 'Tên đăng nhập' 'g02.windows.admin';Set-Input $h 'Mật khẩu' $appPassword;Set-Input $h 'Xác nhận mật khẩu' $appPassword
 $masked=Element $h 'Mật khẩu' ([System.Windows.Automation.ControlType]::Edit);$confirmation=Element $h 'Xác nhận mật khẩu' ([System.Windows.Automation.ControlType]::Edit);Need ($masked.Current.IsPassword -and $confirmation.Current.IsPassword) 'Setup password fields unmasked';Capture $h 'setup-masked-before-submit';Click (Element $h 'Hoàn tất thiết lập' ([System.Windows.Automation.ControlType]::Button))
 [void](Wait-Value {(Read-TestScalar $owner 'SELECT State FROM dbo.AppSetupState') -eq 'Completed'} 'Native setup did not commit');$success=Window $setupProcess 'CoffeeManager';Capture $success 'setup-success-popup';Confirm-Ok $setupProcess $success;Need ($setupProcess.WaitForExit(15000)) 'Success OK did not close setup';Need ($setupProcess.ExitCode -eq 0) 'Setup failed exit';Need ((Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.TAIKHOAN') -eq 1 -and (Read-TestScalar $owner 'SELECT COUNT(*) FROM dbo.NHANVIEN WHERE SDT IS NULL') -eq 1) 'Clean bootstrap row counts wrong';$hashBefore=Read-TestScalar $owner 'SELECT MatKhauHash FROM dbo.TAIKHOAN';Record 'T06-K01.a.WindowsCleanSetup' 'Actual setup Windows exe creates exactly1ADMIN/1NVNULL, Completed, OK closes process' 'Committed once; native success OK invoked; process exit0; runtime environment deliberately points at invalid port and was not used'
 $stage='setup principal proof';Run-Probe $names[0] $setupPassword 'setup'
 foreach($variant in @('Restart','LockedAdmin','DepartedAdmin')){
  $stage='durable gate '+$variant;if($variant -eq 'LockedAdmin'){Run-TestSql $owner 'UPDATE dbo.TAIKHOAN SET HoatDong=0'};if($variant -eq 'DepartedAdmin'){Run-TestSql $owner 'UPDATE dbo.TAIKHOAN SET HoatDong=1;UPDATE dbo.NHANVIEN SET DangLamViec=0'}
  $p=Launch $appExe ("--setup --server localhost --database $db") $names[0] $setupPassword $false;$h=Window $p '*Thiết lập quản trị*';[void](Wait-Value {$button=Element $h 'Hoàn tất thiết lập' ([System.Windows.Automation.ControlType]::Button);$texts=[System.Windows.Automation.AutomationElement]::FromHandle($h).FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text));if(-not $button.Current.IsEnabled -and @($texts|Where-Object {$_.Current.Name -like '*Thiết lập đã hoàn tất*'}).Count){$true}} 'Completed gate not loaded');Capture $h ('setup-'+$variant);Close-App $p $h;Need ((Read-TestScalar $owner 'SELECT MatKhauHash FROM dbo.TAIKHOAN') -ceq $hashBefore) 'Bootstrap restart changed hash';Record ('T06-K02.Windows.'+$variant) 'Completed persists across actual exe restart/admin lock/departure; no reset' 'Complete disabled, explanatory state visible, exe exits0, hash unchanged'
 }
 Run-TestSql $owner 'UPDATE dbo.TAIKHOAN SET HoatDong=1;UPDATE dbo.NHANVIEN SET DangLamViec=1'
 $stage='real runtime login';$p=Launch $appExe '' $names[1] $runtimePassword $true;$h=Window $p '*Đăng nhập*';Need ((Owner-Name $p) -ieq ($env:COMPUTERNAME+'\'+$names[1])) 'Runtime identity wrong';Set-Input $h 'Tên đăng nhập' 'g02.windows.admin';Set-Input $h 'Mật khẩu' $appPassword;Click (Element $h 'Đăng nhập' ([System.Windows.Automation.ControlType]::Button));$main=Window $p 'CoffeeManager';[void](Wait-Value {Element $main 'Danh mục nền' ([System.Windows.Automation.ControlType]::Button)} 'Authenticated Main not ready');Capture $main 'runtime-authenticated-main';Close-App $p $main;Record 'T07-K01.WindowsRuntimeExe' 'Actual runtime Windows exe logs in and opens ADMIN shell, then exits without financial write' 'Owner/runtime identity verified; authenticated Main/catalog navigation visible; exit0'
 $status='PASS'
}catch{
 $status='BLOCKED';Record 'WindowsRound' 'Complete all native/principal cases' ('Stage='+$stage+'; exception='+$_.Exception.GetType().Name+'; command='+$_.InvocationInfo.MyCommand.Name+'; line='+$_.InvocationInfo.ScriptLineNumber+'; errorId='+$_.FullyQualifiedErrorId) 'BLOCKED'
}finally{
 foreach($p in $processes){try{if(-not $p.HasExited){$p.Kill();$p.WaitForExit()};$p.Dispose()}catch{}}
 if($owner){try{foreach($identity in $logins){$q=$identity.Replace(']',']]');Run-TestSql $owner "IF USER_ID(N'$identity') IS NOT NULL DROP USER [$q]"};$owner.ChangeDatabase('master');foreach($identity in $logins){$q=$identity.Replace(']',']]');Run-TestSql $owner "IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name=N'$identity') DROP LOGIN [$q]"};$cleanup.Add([ordered]@{Kind='SqlPrincipals';Status='PASS';Count=$logins.Count})}catch{$status='BLOCKED';$cleanup.Add([ordered]@{Kind='SqlPrincipals';Status='FAIL';ErrorType=$_.Exception.GetType().Name})}finally{$owner.Dispose()}}
 foreach($account in $created){try{$u=Get-LocalUser -Name $account.Name -ErrorAction SilentlyContinue;if($u){Need ($u.SID.Value -eq $account.Sid) 'Refusing account with changed SID';Remove-LocalUser -SID $u.SID};$cleanup.Add([ordered]@{Kind='WindowsAccount';Name=$account.Name;Sid=$account.Sid;Status='PASS'})}catch{$status='BLOCKED';$cleanup.Add([ordered]@{Kind='WindowsAccount';Name=$account.Name;Status='FAIL';ErrorType=$_.Exception.GetType().Name})}}
 $result=[ordered]@{At=(Get-Date).ToString('o');Status=$status;Database=$db;Stage=$stage;TemporaryAccounts=$created;Records=$records;Cleanup=$cleanup;Passwords='Generated in RAM only; not emitted';Deployment=$work}
 [IO.File]::WriteAllText((Join-Path $Output 'results.json'),($result|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
 $setupPassword=$runtimePassword=$appPassword=$hashBefore=$password=$null
}
if($status -ne 'PASS'){exit 1}
