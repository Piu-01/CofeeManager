$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
. 'D:\Workspace\Nam_3\HK1\CoffeeManager\tools\database\G04Ui.ps1'
function Get-Win([int]$processId){ $w=Get-AppWindow $processId; [void](Focus-Window $w); return $w }
function Find-Named($root,[string]$type,[string]$pattern='*'){
  return @(Get-Descendants $root $type | Where-Object { $_.Current.Name -like $pattern })
}
function Click-Nav($win,[string]$pattern){
  $b=Find-Named $win 'Button' $pattern | Select-Object -First 1
  if($null -eq $b){ throw "Nav not found: $pattern" }
  [void](Invoke-Element $b)
  Start-Sleep -Milliseconds 1200
  return $b.Current.Name
}
function Get-Grid($win){
  $g=Find-Named $win 'DataGrid'
  if($g.Count -lt 1){ throw 'No grid' }
  return $g[0]
}
function Get-Cells($win){
  return @(Get-Descendants $win 'DataItem')
}
function Get-CenteredCell($win,[int]$row,[int]$col){
  # Cell automation names are "Row N"; order is by rect (top, then left). Return Nth cell of row.
  $cells=@(Get-Cells $win | Where-Object { $_.Current.Name -eq "Row $row" -and $_.Current.BoundingRectangle.Width -gt 0 } | Sort-Object { $_.Current.BoundingRectangle.X })
  if($cells.Count -le $col){ throw "Row $row has $($cells.Count) cells, need $col" }
  return $cells[$col]
}
function Click-Cell($win,[int]$row,[int]$col){
  $c=Get-CenteredCell $win $row $col
  $r=$c.Current.BoundingRectangle
  Click-At ([int]($r.X+$r.Width/2)) ([int]($r.Y+$r.Height/2))
  Start-Sleep -Milliseconds 600
  return $c
}
function Dump-GridInfo($win){
  foreach($g in @(Find-Named $win 'DataGrid')){
    Write-Output ("GRID|" + $g.Current.Name + "|" + $g.Current.BoundingRectangle.ToString())
    foreach($c in @(Get-Descendants $g 'DataItem')){
      $r=$c.Current.BoundingRectangle
      if([double]::IsInfinity($r.X) -or $r.Width -le 0){ continue }
      Write-Output ("CELL|" + $c.Current.Name + "|" + [int]$r.X + "," + [int]$r.Y + "|" + [int]$r.Width + "x" + [int]$r.Height)
    }
  }
  Write-Output '---EDITS---'
  foreach($e in @(Get-Descendants $win 'Edit')){
    $r=$e.Current.BoundingRectangle
    $v=''; $p=$null
    if($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$p)){$v=$p.Current.Value}
    Write-Output ("EDIT|" + $e.Current.Name + "|" + $v + "|" + [int]$r.X + "," + [int]$r.Y + "|" + [int]$r.Width + "x" + [int]$r.Height)
  }
  Write-Output '---TEXTS---'
  foreach($t in @(Get-Descendants $win 'Text')){
    $r=$t.Current.BoundingRectangle
    if($t.Current.Name -and $r.Y -gt 700){ Write-Output ("TEXT|" + $t.Current.Name + "|" + [int]$r.Y) }
  }
  Write-Output '---BUTTONS---'
  foreach($b in @(Get-Descendants $win 'Button')){
    $r=$b.Current.BoundingRectangle
    if($r.Y -gt 300 -and $r.Y -lt 800 -and $r.X -gt 356){ Write-Output ("BTN|" + $b.Current.Name + "|" + [int]$r.X + "," + [int]$r.Y + "|" + [int]$r.Width) }
  }
}
function Get-Row($win,[int]$row){
  $r=Find-Named $win 'Custom' ("Row " + $row) | Where-Object { $_.Current.BoundingRectangle.Width -gt 300 } | Select-Object -First 1
  if($null -eq $r){ throw "Row $row not found" }
  return $r
}
function Get-RowCells($win,[int]$row){
  $r=Get-Row $win $row
  $walker=[System.Windows.Automation.TreeWalker]::ControlViewWalker
  $items=New-Object System.Collections.ArrayList
  $c=$walker.GetFirstChild($r)
  while($null -ne $c){
    if($c.Current.ControlType.ProgrammaticName -eq 'ControlType.DataItem'){[void]$items.Add($c)}
    $c=$walker.GetNextSibling($c)
  }
  $arr=@($items | Sort-Object { $_.Current.BoundingRectangle.X })
  return $arr
}
function Click-RowCell($win,[int]$row,[int]$col){
  $cells=Get-RowCells $win $row
  if($cells.Count -le $col){ throw "Row ${row}: $($cells.Count) cells, need index $col" }
  $c=$cells[$col]; $r=$c.Current.BoundingRectangle
  Click-At ([int]($r.X+$r.Width/2)) ([int]($r.Y+$r.Height/2))
  Start-Sleep -Milliseconds 700
  return $c.Current.Name
}
function Get-OverlayEdits($win){
  $out=@()
  foreach($e in @(Get-Descendants $win 'Edit')){
    $r=$e.Current.BoundingRectangle
    if($r.Y -gt 380 -and $r.Width -gt 60 -and -not [double]::IsInfinity($r.X)){
      $v=''; $p=$null
      if($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$p)){$v=$p.Current.Value}
      $out+=[pscustomobject]@{Element=$e;Value=$v;Rect=$r}
    }
  }
  return $out
}
function Set-OverlayAndEnter([object]$overlay,[string]$text){
  Set-Focus $overlay.Element
  Set-ElementValue $overlay.Element $text
  Start-Sleep -Milliseconds 100
  [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
  Start-Sleep -Milliseconds 1500
}
function Esc-Key(){ [System.Windows.Forms.SendKeys]::SendWait('{ESC}'); Start-Sleep -Milliseconds 500 }
function Get-StatusTexts($win){
  $texts=@(Get-Descendants $win 'Text' | ForEach-Object { $_.Current.Name } | Where-Object { $_ })
  return $texts
}
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class InlineNative {
 [StructLayout(LayoutKind.Sequential)] public struct CURSORINFO { public int cbSize; public int flags; public IntPtr hCursor; public int x; public int y; }
 [DllImport("user32.dll")] public static extern bool GetCursorInfo(ref CURSORINFO pci);
 [DllImport("user32.dll")] public static extern IntPtr LoadCursor(IntPtr h, int name);
}
'@
function Get-CursorHandle {
  $ci=New-Object InlineNative+CURSORINFO
  $ci.cbSize=[System.Runtime.InteropServices.Marshal]::SizeOf($ci)
  [void][InlineNative]::GetCursorInfo([ref]$ci)
  return $ci.hCursor
}
function Get-CursorKind {
  $h=Get-CursorHandle
  if($h -eq [InlineNative]::LoadCursor([IntPtr]::Zero,32513)){return 'IBeam'}
  if($h -eq [InlineNative]::LoadCursor([IntPtr]::Zero,32512)){return 'Arrow'}
  if($h -eq [InlineNative]::LoadCursor([IntPtr]::Zero,32649)){return 'Hand'}
  return ('Other:' + $h)
}
Add-Type @'
using System;using System.Text;using System.Collections.Generic;using System.Runtime.InteropServices;
public static class InlineWins {
 public delegate bool EnumProc(IntPtr h, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
 public static List<IntPtr> Visible(uint pid){
  var list=new List<IntPtr>();
  EnumWindows((h,l)=>{ uint p; GetWindowThreadProcessId(h,out p); if(p==pid && IsWindowVisible(h)){ var t=new StringBuilder(256); GetWindowText(h,t,256); if(t.Length>0) list.Add(h); } return true; }, IntPtr.Zero);
  return list;
 }
}
'@
function Get-AppDialogs([int]$processId){
  $out=@()
  foreach($h in [InlineWins]::Visible([uint32]$processId)){
    $el=[System.Windows.Automation.AutomationElement]::FromHandle($h)
    if($el.Current.Name -and $el.Current.Name -ne 'CoffeeManager' -and $el.Current.Name -notlike 'CoffeeManager*'){ $out+=$el }
  }
  return $out
}
function Wait-Dialog([int]$processId,[int]$timeoutSec=10){
  $deadline=(Get-Date).AddSeconds($timeoutSec)
  while((Get-Date) -lt $deadline){
    $d=@(Get-AppDialogs $processId)
    if($d.Count -gt 0){ return $d[0] }
    Start-Sleep -Milliseconds 300
  }
  throw 'Dialog not found'
}
function Get-DialogButtons($dialog){ return @(Get-Descendants $dialog 'Button' | Where-Object { $_.Current.Name }) }
function Click-Element($el){ [void](Invoke-Element $el); Start-Sleep -Milliseconds 800 }
function Move-To-Cell($win,[int]$row,[int]$col){
  $cells=Get-RowCells $win $row
  $r=$cells[$col].Current.BoundingRectangle
  [void][G04UiNative]::SetCursorPos([int]($r.X+$r.Width/2),[int]($r.Y+$r.Height/2))
  Start-Sleep -Milliseconds 400
  return $cells[$col]
}
