<#
Idempotent PowerShell script to create a Postgres database and least-privileged user.
Usage: run interactively in PowerShell on a trusted admin machine that can reach 12.10.83.6.
This script only prepares and executes SQL via psql; it does NOT store credentials.
#>
param(
    [string]$PgHost = '12.10.83.6',
    [int]$Port = 5432,
    [string]$AdminUser = 'postgres',
    [string]$NewDb = 'diet_and_exercise',
    [string]$NewUser = 'diet_and_exercise_user'
)

Write-Host "This script will create database '$NewDb' and role '$NewUser' on $($PgHost):$Port (idempotent)."
$adminSecure = Read-Host -Prompt "Postgres admin password for $AdminUser@$PgHost" -AsSecureString
$newUserSecure = Read-Host -Prompt "Password to set for new DB user '$NewUser' (will be stored only in memory)" -AsSecureString

$ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminSecure)
$adminPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)

$ptr2 = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($newUserSecure)
$newUserPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr2)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr2)

try {
    $env:PGPASSWORD = $adminPlain

    $sql = @"
SELECT
  'CREATE ROLE $NewUser WITH LOGIN PASSWORD ' || quote_literal('$newUserPlain') || ' NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT;'
WHERE NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = '$NewUser');
\gexec

SELECT
  'CREATE DATABASE $NewDb OWNER $NewUser;'
WHERE NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = '$NewDb');
\gexec

\connect $NewDb

CREATE SCHEMA IF NOT EXISTS app AUTHORIZATION $NewUser;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA public TO $NewUser;
ALTER DEFAULT PRIVILEGES FOR ROLE $NewUser IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $NewUser;
"@

    Write-Host "Running psql against $PgHost (you will be prompted if psql requires password)."
    $psqlArgs = @(
        "-h", $PgHost,
        "-p", $Port.ToString(),
        "-U", $AdminUser,
        "-d", "postgres",
        "--set", "ON_ERROR_STOP=on",
        "-q",
        "-f", "-"
    )

    $proc = Start-Process -FilePath "psql" -ArgumentList $psqlArgs -NoNewWindow -RedirectStandardInput "Pipe" -RedirectStandardOutput "Pipe" -RedirectStandardError "Pipe" -PassThru -Wait
    if ($proc -eq $null) { throw "Failed to start psql process. Ensure psql is installed and on PATH." }

    $proc.StandardInput.WriteLine($sql)
    $proc.StandardInput.Close()

    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $exitCode = $proc.ExitCode

    if ($exitCode -ne 0) {
        Write-Error "psql exited with code $exitCode"
        if ($stdout) { Write-Host "psql stdout:`n$stdout" }
        if ($stderr) { Write-Host "psql stderr:`n$stderr" }
        throw "psql failed (exit $exitCode)."
    } else {
        Write-Host "psql succeeded. Output (truncated):"
        if ($stdout) { $stdout.Split([Environment]::NewLine) | Select-Object -First 200 | ForEach-Object { Write-Host $_ } }
        else { Write-Host "(no stdout)" }
    }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    if ($adminPlain) { $adminPlain = $null }
    if ($newUserPlain) { $newUserPlain = $null }
}
