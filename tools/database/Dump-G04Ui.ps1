param([Parameter(Mandatory=$true)][int]$ProcessId,[int]$MaxDepth=6)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class UiNative {
 [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")]public static extern bool ShowWindow(IntPtr h,int cmd);
}
'@
function Get-Window($procId) {
 $root=[System.Windows.Automation.AutomationElement]::RootElement
 $cond=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$procId)
 $wins=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$cond)
 $pick=$null
 foreach($cand in $wins){
  if($cand.Current.ClassName -like 'WindowsForms10*'){ $pick=$cand; break }
  if($null -eq $pick -and -not [string]::IsNullOrWhiteSpace($cand.Current.Name)){ $pick=$cand }
 }
 return $pick
}
function Dump($el,[int]$depth) {
 $pad=' ' * ($depth*2)
 $ct=$el.Current.ControlType.ProgrammaticName.Replace('ControlType.','')
 $r=$el.Current.BoundingRectangle
 $line="{0}{1} name='{2}' id='{3}' enabled={4} offscreen={5} rect={6},{7} {8}x{9}" -f $pad,$ct,$el.Current.Name,$el.Current.AutomationId,$el.Current.IsEnabled,$el.Current.IsOffscreen,[int]$r.X,[int]$r.Y,[int]$r.Width,[int]$r.Height
 Write-Output $line
 if($depth -ge $MaxDepth){return}
 $walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker
 $child=$walker.GetFirstChild($el)
 while($null -ne $child){ Dump $child ($depth+1); $child=$walker.GetNextSibling($child) }
}
$win=Get-Window $ProcessId
if($null -eq $win){throw 'Window not found for process'}
Write-Output ("TITLE='{0}'" -f $win.Current.Name)
Dump $win 0
