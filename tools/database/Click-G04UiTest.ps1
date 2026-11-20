param([Parameter(Mandatory=$true)][int]$ProcessId,[string]$Name,[string]$Pattern,[string]$OutFile,[int]$Index=0)
. 'D:\Workspace\Nam_3\HK1\CoffeeManager\tools\database\G04Ui.ps1'
$win=Get-AppWindow $ProcessId
[void][G04UiNative]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle)
if($Pattern){
 $buttons=@(Get-Descendants $win 'Button' | Where-Object { $_.Current.Name -match $Pattern })
}else{
 if(-not $Name){throw 'Need -Name or -Pattern'}
 $buttons=@(Get-Descendants $win 'Button' | Where-Object { $_.Current.Name -eq $Name })
}
if($buttons.Count -le $Index){throw "Button not found: $Name $Pattern (found $($buttons.Count))"}
[void](Invoke-Element $buttons[$Index])
Start-Sleep -Seconds 2
Write-Output ("CLICKED {0}" -f $buttons[$Index].Current.Name)
if($OutFile){ & powershell -NoProfile -ExecutionPolicy Bypass -File 'D:\Workspace\Nam_3\HK1\CoffeeManager\tools\database\Dump-G04Ui.ps1' -ProcessId $ProcessId -MaxDepth 8 *> $OutFile; Write-Output "DUMP=$OutFile" }
