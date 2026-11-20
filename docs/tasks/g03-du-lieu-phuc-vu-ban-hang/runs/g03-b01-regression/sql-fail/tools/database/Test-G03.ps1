[CmdletBinding()]param([switch]$ApprovedAcceptance)
$ErrorActionPreference='Stop'
if(-not $ApprovedAcceptance){throw 'Chỉ chạy sau khi người dùng duyệt đợt test G03 riêng; truyền -ApprovedAcceptance khi đã có duyệt.'}
& (Join-Path $PSScriptRoot 'Invoke-G03Technical.ps1') -ThroughVersion 9 -Acceptance -ApprovedAcceptance

