param([Parameter(Mandatory=$true)][int]$ProcessId,[string]$User='thungan',[string]$Pass='Cash@123',[string]$OutFile)
. 'D:\Workspace\Nam_3\HK1\CoffeeManager\tools\database\G04Ui.ps1'
$win=Get-AppWindow $ProcessId
[void][G04UiNative]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle)
$edits=@(Get-Descendants $win 'Edit' | Sort-Object { $_.Current.BoundingRectangle.Y })
if($edits.Count -lt 2){throw 'Login edits not found'}
Set-ElementValue $edits[0] $User
Set-ElementValue $edits[1] $Pass
$buttons=@(Get-Descendants $win 'Button' | Where-Object { $_.Current.BoundingRectangle.Width -ge 200 })
if($buttons.Count -ne 1){throw "Login button candidates: $($buttons.Count)"}
[void](Invoke-Element $buttons[0])
Start-Sleep -Seconds 3
Write-Output 'LOGIN_CLICKED'
$dumpArgs=@('-NoProfile','-ExecutionPolicy','Bypass','-File','D:\Workspace\Nam_3\HK1\CoffeeManager\tools\database\Dump-G04Ui.ps1','-ProcessId',$ProcessId,'-MaxDepth','4')
if($OutFile){ & powershell @dumpArgs *> $OutFile; Write-Output "DUMP=$OutFile" }
