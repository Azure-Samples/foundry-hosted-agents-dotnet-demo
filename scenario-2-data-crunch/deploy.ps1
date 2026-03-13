#!/usr/bin/env pwsh
# deploy.ps1 — Deploy the Data Crunch Agent container to Azure
# Requires setup.ps1 to have been run first.
# Idempotent: safe to run multiple times (redeploys the latest code).

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Data Crunch Agent — Deploy ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# 1. Check that setup has been run
# ============================================================
if (-not (Test-Path ".azure")) {
    Write-Host "❌ .azure/ folder not found. Run setup.ps1 first." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ .azure/ folder found" -ForegroundColor Green

if (-not (Test-Path "$PSScriptRoot/azure.yaml")) {
    Write-Host "❌ azure.yaml not found. Run setup.ps1 first." -ForegroundColor Red
    exit 1
}
$yamlContent = Get-Content "$PSScriptRoot/azure.yaml" -Raw
if ($yamlContent -notmatch 'services:') {
    Write-Host "❌ azure.yaml has no 'services' section. Run setup.ps1 again to register the agent." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ azure.yaml found with services definition" -ForegroundColor Green

if (-not (Test-Path "$PSScriptRoot/src/DataCrunchAgent/Dockerfile")) {
    Write-Host "❌ src/DataCrunchAgent/Dockerfile not found. The agent code must be created first." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ Dockerfile found" -ForegroundColor Green
Write-Host ""

# ============================================================
# 2. Check Docker Desktop is running
# ============================================================
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker info > $null 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Docker is not running. Start Docker Desktop and try again." -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not available. Install Docker Desktop: https://docs.docker.com/get-docker/" -ForegroundColor Red
    exit 1
}
Write-Host ""

# ============================================================
# 3. Deploy with azd
# ============================================================
Write-Host "Deploying agent to Azure with 'azd deploy'..." -ForegroundColor Yellow
Write-Host "This builds the container, pushes to ACR, and creates the hosted agent deployment." -ForegroundColor DarkGray
Write-Host ""

azd deploy
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 'azd deploy' failed." -ForegroundColor Red
    exit 1
}
Write-Host ""
Write-Host "  ✅ Agent deployed to Azure" -ForegroundColor Green
Write-Host ""

# ============================================================
# 4. Print endpoint and playground URLs
# ============================================================
Write-Host "Retrieving deployment information..." -ForegroundColor Yellow

$envValues = azd env get-values 2>$null
$projectEndpoint = $null
$projectName = $null
$resourceGroup = $null

foreach ($line in $envValues) {
    if ($line -match '^\s*AZURE_AI_FOUNDRY_PROJECT_ENDPOINT\s*=\s*"?([^"]*)"?\s*$') {
        $projectEndpoint = $Matches[1]
    }
    if ($line -match '^\s*AZURE_AI_PROJECT_NAME\s*=\s*"?([^"]*)"?\s*$') {
        $projectName = $Matches[1]
    }
    if ($line -match '^\s*AZURE_RESOURCE_GROUP\s*=\s*"?([^"]*)"?\s*$') {
        $resourceGroup = $Matches[1]
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deployment Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($projectEndpoint) {
    Write-Host "  Project endpoint:" -ForegroundColor White
    Write-Host "  $projectEndpoint" -ForegroundColor Green
}

Write-Host ""
Write-Host "  Test your agent:" -ForegroundColor White
Write-Host "  1. Open https://ai.azure.com" -ForegroundColor White
if ($projectName) {
    Write-Host "  2. Navigate to project: $projectName" -ForegroundColor White
} else {
    Write-Host "  2. Navigate to your project" -ForegroundColor White
}
Write-Host "  3. Open the Agents section" -ForegroundColor White
Write-Host "  4. Launch the agent in the Playground" -ForegroundColor White
Write-Host "  5. Upload a CSV file and ask the agent to analyze it" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
