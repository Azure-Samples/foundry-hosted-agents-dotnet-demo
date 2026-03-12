#!/usr/bin/env pwsh
# cleanup.ps1 — Remove all Azure resources and local configuration
# Safe to run multiple times. If resources don't exist, steps are skipped.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Image Generator Agent — Cleanup ===" -ForegroundColor Cyan
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

# --- Delete .env file ---
if (Test-Path ".env") {
    Write-Host "🗑️  Removing .env file..." -ForegroundColor Yellow
    Remove-Item -Path ".env" -Force
    $cleanedUp += "✅ .env file removed"
    Write-Host ".env file removed." -ForegroundColor Green
} else {
    Write-Host "ℹ️  No .env file found — nothing to remove." -ForegroundColor DarkGray
}

# --- Delete generated images ---
$outputDir = "scenario-3-image-gen/src/HostedAgent/output"
if (Test-Path $outputDir) {
    Write-Host "🗑️  Removing generated images from $outputDir..." -ForegroundColor Yellow
    Remove-Item -Path $outputDir -Recurse -Force
    $cleanedUp += "✅ Generated images removed"
    Write-Host "Generated images removed." -ForegroundColor Green
} else {
    Write-Host "ℹ️  No output/ folder found — no generated images to remove." -ForegroundColor DarkGray
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
