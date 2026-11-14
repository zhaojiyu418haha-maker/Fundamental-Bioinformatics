#requires -Version 5.1
<#
    Creates the local PostgreSQL role/database and applies schema.sql + seed.sql.

    Usage (normal PowerShell):
      .\scripts\Setup-Database.ps1

    The script asks for the PostgreSQL "postgres" password. The application
    itself uses a separate local role: bg_app / bg_dev_password.
#>

[CmdletBinding()]
param(
    [string]$PostgresUser = "postgres",
    [string]$PostgresPassword,
    [string]$DatabaseName = "battlegrounds_gallery",
    [string]$ApplicationUser = "bg_app",
    [string]$ApplicationPassword = "bg_dev_password"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

$psqlCandidates = @(
    (Join-Path $projectRoot "..\SQL operating deck\bin\psql.exe"),
    "Z:\SQL operating deck\bin\psql.exe",
    "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe"
)

$psql = $psqlCandidates |
    ForEach-Object { [System.IO.Path]::GetFullPath($_) } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if (-not $psql) {
    throw "psql.exe was not found. Install PostgreSQL or add its bin directory to PATH."
}

if (-not $PostgresPassword) {
    $securePassword = Read-Host "PostgreSQL password for user '$PostgresUser'" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

    try {
        $PostgresPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-Psql {
    param(
        [string]$User,
        [string]$Password,
        [string]$Database,
        [string[]]$Arguments
    )

    $previousPassword = $env:PGPASSWORD
    $env:PGPASSWORD = $Password

    try {
        & $psql -h 127.0.0.1 -U $User -d $Database -v ON_ERROR_STOP=1 @Arguments

        if ($LASTEXITCODE -ne 0) {
            throw "psql failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $env:PGPASSWORD = $previousPassword
    }
}

Write-Host "Checking PostgreSQL connection ..."

$roleExists = & {
    $env:PGPASSWORD = $PostgresPassword
    & $psql -h 127.0.0.1 -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$ApplicationUser';"
}

if (($roleExists | Out-String).Trim() -ne "1") {
    Write-Host "Creating role '$ApplicationUser' ..."
    Invoke-Psql -User $PostgresUser -Password $PostgresPassword -Database postgres -Arguments @(
        "-c", "CREATE ROLE $ApplicationUser LOGIN PASSWORD '$ApplicationPassword';"
    )
}
else {
    Write-Host "Role '$ApplicationUser' already exists; updating its password ..."
    Invoke-Psql -User $PostgresUser -Password $PostgresPassword -Database postgres -Arguments @(
        "-c", "ALTER ROLE $ApplicationUser WITH LOGIN PASSWORD '$ApplicationPassword';"
    )
}

$databaseExists = & {
    $env:PGPASSWORD = $PostgresPassword
    & $psql -h 127.0.0.1 -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName';"
}

if (($databaseExists | Out-String).Trim() -ne "1") {
    Write-Host "Creating database '$DatabaseName' ..."
    Invoke-Psql -User $PostgresUser -Password $PostgresPassword -Database postgres -Arguments @(
        "-c", "CREATE DATABASE $DatabaseName OWNER $ApplicationUser;"
    )
}
else {
    Write-Host "Database '$DatabaseName' already exists."
}

Write-Host "Applying schema.sql ..."
Invoke-Psql -User $ApplicationUser -Password $ApplicationPassword -Database $DatabaseName -Arguments @(
    "-f", (Join-Path $projectRoot "db\schema.sql")
)

Write-Host "Applying seed.sql ..."
Invoke-Psql -User $ApplicationUser -Password $ApplicationPassword -Database $DatabaseName -Arguments @(
    "-f", (Join-Path $projectRoot "db\seed.sql")
)

Write-Host ""
Write-Host "Done. Database is ready." -ForegroundColor Green
Write-Host "Connection: $DatabaseName on localhost:5432"
Write-Host "Application user: $ApplicationUser"
