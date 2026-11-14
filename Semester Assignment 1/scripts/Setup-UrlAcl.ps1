#requires -Version 5.1
<#
    One-time Windows setup for this project's HttpListener.

    HttpListener is built on HTTP.sys. A normal (non-elevated) process cannot
    reserve an arbitrary URL prefix, so the current user needs a URL ACL for
    the local development port. Run this script as Administrator once.
#>

[CmdletBinding()]
param(
    [int]$Port = 5080
)

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
$isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    Write-Host "This setup must run as Administrator. Restart PowerShell with 'Run as administrator' and run the script again." -ForegroundColor Yellow
    exit 1
}

$user = "$env:USERDOMAIN\$env:USERNAME"
$url = "http://localhost:$Port/"

Write-Host "Reserving $url for $user ..."
netsh http add urlacl url=$url user=$user listen=yes

if ($LASTEXITCODE -eq 183) {
    Write-Host "The URL reservation already exists. Nothing to do." -ForegroundColor Green
    Write-Host "Open http://localhost:$Port/ after starting the server."
    exit 0
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Could not reserve the URL. Check the netsh output above."
    exit $LASTEXITCODE
}

Write-Host "Done. You can now run BattlegroundsGallery.Web as a normal user." -ForegroundColor Green
Write-Host "Open http://localhost:$Port/ after starting the server."
