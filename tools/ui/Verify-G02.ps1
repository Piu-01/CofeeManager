#requires -Version 5.1
[CmdletBinding()]param(
 [Parameter(Mandatory=$true)][int]$ProcessId,
 [ValidateSet(1280,1366)][int]$Width=1280,
 [ValidateSet(720,768)][int]$Height=720,
 [ValidateSet(100,125,150)][int]$DpiPercent=125,
 [switch]$AcceptanceApproved
)
$ErrorActionPreference='Stop'
if(-not $AcceptanceApproved){throw 'Capture native UI only after approval of the G02 acceptance round.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
public static class G02NativeCapture {
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 public delegate bool EnumWindow(IntPtr h,IntPtr arg);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int he,uint flags);
 [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h,EnumWindow callback,IntPtr arg);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,StringBuilder text,int max);
 public static List<IntPtr> Children(IntPtr h) { var list=new List<IntPtr>(); EnumWindow cb=(c,a)=>{list.Add(c);return true;}; EnumChildWindows(h,cb,IntPtr.Zero); return list; }
}
'@
$p=Get-Process -Id $ProcessId
if($p.ProcessName -ne 'CafeManager.WinForms'){throw 'Explicit CoffeeManager.WinForms process required.'}
$p.Refresh();$handle=$p.MainWindowHandle
if($handle -eq [IntPtr]::Zero){throw 'No native main window.'}
# Login/setup may contain credentials. This collector captures the authenticated
# shell only and never reads control text, clipboard, environment or connection.
if($p.MainWindowTitle -ne 'CoffeeManager'){throw 'Open the authenticated MainForm before capture; no login/setup capture.'}
$dpi=[G02NativeCapture]::GetDpiForWindow($handle)
if($dpi -ne (96*$DpiPercent/100)){throw "Actual DPI=$dpi does not match requested $DpiPercent%; change display scaling locally first."}
[void][G02NativeCapture]::SetWindowPos($handle,[IntPtr]::Zero,0,0,$Width,$Height,0x0044)
$r=[G02NativeCapture+Rect]::new();[void][G02NativeCapture]::GetWindowRect($handle,[ref]$r)
$directory=Join-Path $root ('docs\tasks\g02-truy-cap-ung-dung-cau-hinh-nen\runs\UI_'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8))
[void][IO.Directory]::CreateDirectory($directory)
$image=[Drawing.Bitmap]::new(($r.Right-$r.Left),($r.Bottom-$r.Top));$graphics=[Drawing.Graphics]::FromImage($image)
try{$graphics.CopyFromScreen($r.Left,$r.Top,0,0,$image.Size);$image.Save((Join-Path $directory 'native.png'),[Drawing.Imaging.ImageFormat]::Png)}finally{$graphics.Dispose();$image.Dispose()}
$children=@([G02NativeCapture]::Children($handle)|ForEach-Object{
 $bounds=[G02NativeCapture+Rect]::new();[void][G02NativeCapture]::GetWindowRect($_,[ref]$bounds)
 $class=[Text.StringBuilder]::new(256);[void][G02NativeCapture]::GetClassName($_,$class,256)
 [ordered]@{Handle=$_.ToInt64();Class=$class.ToString();Visible=[G02NativeCapture]::IsWindowVisible($_);Bounds=@($bounds.Left,$bounds.Top,$bounds.Right,$bounds.Bottom)}
})
$manifest=@(Get-ChildItem (Join-Path $root 'src') -Recurse -File|Where-Object{$_.FullName -notmatch '\\(bin|obj)\\'}|ForEach-Object{[ordered]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash $_.FullName).Hash}})
$evidence=[ordered]@{ProcessId=$ProcessId;Dpi=$dpi;RequestedSize=@($Width,$Height);ActualBounds=@($r.Left,$r.Top,$r.Right,$r.Bottom);ChildWindows=$children;ProcessHandles=$p.HandleCount;SourceManifest=$manifest;Status='EVIDENCE ONLY: visual, keyboard, event/instance/SQL counts require separate actual observations'}
[IO.File]::WriteAllText((Join-Path $directory 'native.json'),($evidence|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
Write-Output "Native evidence: $directory"
