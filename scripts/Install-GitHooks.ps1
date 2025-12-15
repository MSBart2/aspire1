#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Installs Git hooks to enforce branching strategy
.DESCRIPTION
    Copies pre-commit and pre-push hooks from scripts/hooks/ to .git/hooks/
    These hooks prevent direct commits and pushes to main/master branches
.EXAMPLE
    .\scripts\Install-GitHooks.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Ensure we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Error "This script must be run from the root of a Git repository"
    exit 1
}

$hooksDir = ".git\hooks"
$sourceHooksDir = "scripts\hooks"

# Verify source hooks directory exists
if (-not (Test-Path $sourceHooksDir)) {
    Write-Error "Source hooks directory not found: $sourceHooksDir"
    exit 1
}

# Ensure .git/hooks directory exists
if (-not (Test-Path $hooksDir)) {
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
}

Write-Host ""
Write-Host "Installing Git hooks..." -ForegroundColor Cyan
Write-Host ""

# Copy all hook files
$hookFiles = Get-ChildItem -Path $sourceHooksDir -File
$installedCount = 0

foreach ($hookFile in $hookFiles) {
    $destination = Join-Path $hooksDir $hookFile.Name

    # Backup existing hook if it exists and is different
    if (Test-Path $destination) {
        $existingContent = Get-Content $destination -Raw -ErrorAction SilentlyContinue
        $newContent = Get-Content $hookFile.FullName -Raw

        if ($existingContent -ne $newContent) {
            $backupPath = "$destination.backup"
            Copy-Item $destination $backupPath -Force
            Write-Host "  ⚠️  Backed up existing hook: $($hookFile.Name).backup" -ForegroundColor Yellow
        }
    }

    # Copy the hook
    Copy-Item $hookFile.FullName $destination -Force
    Write-Host "  ✅ Installed: $($hookFile.Name)" -ForegroundColor Green
    $installedCount++
}

Write-Host ""
Write-Host "Successfully installed $installedCount Git hook(s)!" -ForegroundColor Green
Write-Host ""
Write-Host "The following protections are now active:" -ForegroundColor Cyan
Write-Host "  • Direct commits to main/master branches are blocked" -ForegroundColor White
Write-Host "  • Direct pushes to main/master branches are blocked" -ForegroundColor White
Write-Host ""
Write-Host "Use feature branches for all development work." -ForegroundColor Yellow
Write-Host ""

exit 0
