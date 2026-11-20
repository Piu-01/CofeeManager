$ErrorActionPreference='Stop'
$u=Get-LocalUser -Name CafeManagerApp -ErrorAction SilentlyContinue
if($u) {
 if($u.SID.Value -ne 'S-1-5-21-995916740-4115604996-2892334199-1007') {throw 'Unexpected SID'}
 Remove-LocalUser -SID $u.SID
}
if(Get-LocalUser -Name CafeManagerApp -ErrorAction SilentlyContinue) {throw 'User remains'}
'Windows user CafeManagerApp removed and verified.' | Set-Content (Join-Path $PSScriptRoot 'cleanup-windows-result.txt')
