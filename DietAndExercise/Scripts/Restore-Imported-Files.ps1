<#
Restore-Imported-Files.ps1
Safely restores markdown files from the importer backup (dae_import_backup_runner) back into
D:\Nextcloud\Notes\Cameron\Diet and Exercise based on filenames (yyyy-MM-dd.md).

Usage (recommended):
  1. Open PowerShell as Administrator.
  2. Set Execution Policy if needed: Set-ExecutionPolicy -Scope Process Bypass -Force
  3. Run: pwsh -NoProfile -ExecutionPolicy Bypass -File "Restore-Imported-Files.ps1"

This script creates year/month folders as needed, moves files, writes a CSV log, and removes the backup folder if empty.
#>

# Require admin to avoid permission issues when writing into Nextcloud
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "This script should be run as Administrator to ensure it can write into the Nextcloud tree."
    Write-Output "If you cannot run as Administrator, run the script manually with required permissions."
}

$backupRoot = Join-Path $env:TEMP 'dae_import_backup_runner'
if (-not (Test-Path $backupRoot)) { Write-Error "Backup root not found: $backupRoot"; exit 1 }

$targetRoot = 'D:\Nextcloud\Notes\Cameron\Diet and Exercise'
$logCsv = Join-Path $backupRoot 'restore_log.csv'
$logSb = [System.Text.StringBuilder]::new()
$logSb.AppendLine('SourcePath,DestinationPath,Status,Message') | Out-Null

$files = Get-ChildItem -Path $backupRoot -Recurse -File | Sort-Object FullName
$moved = 0; $failed = 0

foreach ($f in $files) {
    $name = $f.Name
    $destDir = $targetRoot

    if ($name -match '^(\d{4})-(\d{2})-(\d{2})\.md$') {
        $year = $matches[1]; $month = $matches[2]
        $destDir = Join-Path $targetRoot "$year\$month"
    }

    try {
        if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
        $dest = Join-Path $destDir $name
        # If destination exists, append timestamp to avoid overwriting
        if (Test-Path $dest) {
            $ts = Get-Date -Format yyyyMMdd_HHmmss
            $dest = Join-Path $destDir ("{0}_{1}.md" -f ([System.IO.Path]::GetFileNameWithoutExtension($name)), $ts)
            $dest = "$dest"
        }

        Move-Item -Path $f.FullName -Destination $dest -Force
        $logSb.AppendLine(('"{0}","{1}",Imported,""' -f $f.FullName, $dest)) | Out-Null
        $moved++
    }
    catch {
        $errMsg = $_.Exception.Message -replace '"','""'
        $logSb.AppendLine(('"{0}","",Failed,"{1}"' -f $f.FullName, $errMsg)) | Out-Null
        Write-Warning "Failed moving $($f.FullName): $errMsg"
        $failed++
    }
}

# Write CSV log
try {
    [System.IO.File]::WriteAllText($logCsv, $logSb.ToString())
}
catch {
    Write-Warning ("Failed writing log to {0}: {1}" -f $logCsv, $_.Exception.Message)
}

Write-Output "Moved $moved files, $failed failures. Log: $logCsv"

# Remove backup root if empty
if ((Get-ChildItem -Path $backupRoot -Recurse -File -ErrorAction SilentlyContinue).Count -eq 0) {
    Remove-Item -Path $backupRoot -Recurse -Force
    Write-Output "Backup root removed: $backupRoot"
}
else {
    Write-Output "Backup root not empty, left in place: $backupRoot"
}