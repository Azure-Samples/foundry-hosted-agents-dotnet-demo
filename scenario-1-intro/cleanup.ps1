#!/usr/bin/env pwsh
# cleanup.ps1 — Remove all Azure resources and local configuration
# Safe to run multiple times. If resources don't exist, steps are skipped.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Hosted Agents Demo — Cleanup ===" -ForegroundColor Cyan
Write-Host ""

$cleanedUp = @()

# --- Delete Azure resources ---
if (Test-Path ".azure") {
    Write-Host "⏳ Deleting Azure resources with 'azd down --purge --force'..." -ForegroundColor Yellow
    try {
        azd down --purge --force
        if ($LASTEXITCODE -eq 0) {
            $cleanedUp += "✅ Azure resources deleted"
            Write-Host "Azure resources deleted." -ForegroundColor Green
        } else {
            Write-Host "⚠️  'azd down' returned a non-zero exit code. Some resources may remain." -ForegroundColor Yellow
            $cleanedUp += "⚠️  Azure resources may not be fully deleted"
        }
    } catch {
        Write-Host "⚠️  Could not run 'azd down'. You may need to delete resources manually in the Azure portal." -ForegroundColor Yellow
        $cleanedUp += "⚠️  Azure resource deletion skipped (azd not available)"
    }
} else {
    Write-Host "ℹ️  No .azure/ folder found — skipping Azure resource deletion." -ForegroundColor DarkGray
    $cleanedUp += "ℹ️  No Azure resources to delete (.azure/ not found)"
}

# --- Delete local .azure folder ---
if (Test-Path ".azure") {
    Write-Host "🗑️  Removing .azure/ folder..." -ForegroundColor Yellow
    Remove-Item -Path ".azure" -Recurse -Force
    $cleanedUp += "✅ .azure/ folder removed"
    Write-Host ".azure/ folder removed." -ForegroundColor Green
}

# --- Clear .NET User Secrets ---
$csprojPath = "$PSScriptRoot/src/time-zone-agent/HostedAgent.csproj"
if (Test-Path $csprojPath) {
    Write-Host "🗑️  Clearing .NET User Secrets..." -ForegroundColor Yellow
    dotnet user-secrets clear --project $csprojPath > $null 2>&1
    $cleanedUp += "✅ .NET User Secrets cleared"
    Write-Host "User Secrets cleared." -ForegroundColor Green
} else {
    Write-Host "ℹ️  Project file not found — skipping User Secrets cleanup." -ForegroundColor DarkGray
}

# --- Print cleanup summary ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cleanup Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
foreach ($item in $cleanedUp) {
    Write-Host "  $item"
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host ""
