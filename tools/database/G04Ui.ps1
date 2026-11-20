# G04 UI test helper library (ASCII only). Dot-source from step scripts.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class G04UiNative {
 [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")]public static extern bool BringWindowToTop(IntPtr h);
 [DllImport("user32.dll")]public static extern bool ShowWindow(IntPtr h,int cmd);
 [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
 [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")]public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]public static extern void mouse_event(uint f,uint dx,uint dy,uint d,UIntPtr e);
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
}
'@
[void][G04UiNative]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
$script:AE=[System.Windows.Automation.AutomationElement]
function Get-AppWindow([int]$procId,[int]$timeoutSec=30) {
 $deadline=(Get-Date).AddSeconds($timeoutSec)
 while((Get-Date) -lt $deadline) {
  $cond=New-Object System.Windows.Automation.PropertyCondition($script:AE::ProcessIdProperty,$procId)
  $wins=$script:AE::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$cond)
  $win=$null
  foreach($cand in $wins){
   if($cand.Current.ClassName -like 'WindowsForms10*'){ $win=$cand; break }
   if($null -eq $win -and -not [string]::IsNullOrWhiteSpace($cand.Current.Name)){ $win=$cand }
  }
  if($null -ne $win){return $win}
  Start-Sleep -Milliseconds 300
 }
 throw "Window not found for process $procId"
}
function Get-Descendants($root,[string]$typeName=$null) {
 $walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker
 $list=New-Object System.Collections.ArrayList
 $stack=New-Object System.Collections.Stack
 $child=$walker.GetFirstChild($root)
 while($null -ne $child){$stack.Push($child); $child=$walker.GetNextSibling($child)}
 while($stack.Count -gt 0){
  $el=$stack.Pop()
  if($null -eq $typeName -or $el.Current.ControlType.ProgrammaticName -eq "ControlType.$typeName"){[void]$list.Add($el)}
  $c=$walker.GetFirstChild($el)
  while($null -ne $c){$stack.Push($c); $c=$walker.GetNextSibling($c)}
 }
 return $list
}
function Invoke-Element($el) {
 $pattern=$null
 if($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$pattern)){$pattern.Invoke(); return 'invoke'}
 if($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){$pattern.Select(); return 'select'}
 if($el.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern,[ref]$pattern)){$pattern.Expand(); return 'expand'}
 $r=$el.Current.BoundingRectangle
 Click-At ([int]($r.X+$r.Width/2)) ([int]($r.Y+$r.Height/2))
 return 'click'
}
function Click-At([int]$x,[int]$y) {
 [void][G04UiNative]::SetCursorPos($x,$y)
 [G04UiNative]::mouse_event(0x0002,0,0,0,[UIntPtr]::Zero)
 Start-Sleep -Milliseconds 60
 [G04UiNative]::mouse_event(0x0004,0,0,0,[UIntPtr]::Zero)
}
function Post-Click($el) {
 $h=[IntPtr]$el.Current.NativeWindowHandle
 if($h -eq [IntPtr]::Zero){throw 'Element has no window handle'}
 $r=$el.Current.BoundingRectangle
 $x=[int]($r.Width/2); $y=[int]($r.Height/2)
 $lp=[IntPtr](($y -shl 16) -bor ($x -band 0xFFFF))
 [void][G04UiNative]::PostMessage($h,0x0201,[IntPtr]1,$lp)
 Start-Sleep -Milliseconds 40
 [void][G04UiNative]::PostMessage($h,0x0202,[IntPtr]::Zero,$lp)
}
function Set-ElementValue($el,[string]$text) {
 $pattern=$null
 if(-not $el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pattern)){throw 'No ValuePattern'}
 $pattern.SetValue($text)
}
function Set-Focus($el) {$el.SetFocus(); Start-Sleep -Milliseconds 150}
function Focus-Window($win) {
 $h=[IntPtr]$win.Current.NativeWindowHandle
 [void][G04UiNative]::ShowWindow($h,9)
 [void][G04UiNative]::BringWindowToTop($h)
 [void][G04UiNative]::SetForegroundWindow($h)
 # HWND_TOPMOST = -1, SWP_NOMOVE|SWP_NOSIZE|SWP_SHOWWINDOW = 0x3
 [void][G04UiNative]::SetWindowPos($h,[IntPtr]::new(-1),0,0,0,0,0x3)
 Start-Sleep -Milliseconds 300
 return ([G04UiNative]::GetForegroundWindow() -eq $h)
}
function Find-Button($root,[string]$namePart) {
 return @(Get-Descendants $root 'Button' | Where-Object { $_.Current.Name -like "*$namePart*" })
}
function Find-Edit($root,[int]$index=0) {
 $edits=@(Get-Descendants $root 'Edit')
 return $edits[$index]
}
function Wait-Until([scriptblock]$probe,[int]$timeoutSec=20,[string]$label='condition') {
 $deadline=(Get-Date).AddSeconds($timeoutSec)
 while((Get-Date) -lt $deadline){ if(& $probe){return $true}; Start-Sleep -Milliseconds 400 }
 throw "Timed out waiting for $label"
}
