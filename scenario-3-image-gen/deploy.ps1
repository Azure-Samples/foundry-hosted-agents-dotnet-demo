#!/usr/bin/env pwsh
# deploy.ps1 — Deploy the Image Generator Agent container to Azure
# Requires setup.ps1 to have been run first.
# Uses "az cognitiveservices agent create --source" to build, push, and deploy.
# Idempotent: safe to run multiple times (redeploys the latest code).

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$agentName = "image-generator-agent"
$sourceDir = "$PSScriptRoot/src/HostedAgent"

Write-Host ""
Write-Host "=== Image Generator Agent — Deploy ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# 1. Check that setup has been run
# ============================================================
if (-not (Test-Path ".azure")) {
    Write-Host "❌ .azure/ folder not found. Run setup.ps1 first." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ .azure/ folder found" -ForegroundColor Green

if (-not (Test-Path "$sourceDir/Dockerfile")) {
    Write-Host "❌ $sourceDir/Dockerfile not found. The agent code must be created first." -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ Dockerfile found" -ForegroundColor Green
Write-Host ""

# ============================================================
# 2. Read azd environment values
# ============================================================
Write-Host "Reading azd environment..." -ForegroundColor Yellow

$envValues = azd env get-values 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Could not read azd environment. Run setup.ps1 first." -ForegroundColor Red
    exit 1
}

$acctName = $null
$projName = $null
$acrEndpoint = $null
$projectEndpoint = $null

foreach ($line in $envValues) {
    if ($line -match '^\s*AZURE_AI_ACCOUNT_NAME\s*=\s*"?([^"]*)"?\s*$') { $acctName = $Matches[1] }
    if ($line -match '^\s*AZURE_AI_PROJECT_NAME\s*=\s*"?([^"]*)"?\s*$') { $projName = $Matches[1] }
    if ($line -match '^\s*AZURE_CONTAINER_REGISTRY_ENDPOINT\s*=\s*"?([^"]*)"?\s*$') { $acrEndpoint = $Matches[1] }
    if ($line -match '^\s*AZURE_AI_FOUNDRY_PROJECT_ENDPOINT\s*=\s*"?([^"]*)"?\s*$') { $projectEndpoint = $Matches[1] }
}

if (-not $acctName -or -not $projName -or -not $acrEndpoint) {
    Write-Host "❌ Missing required environment values (AZURE_AI_ACCOUNT_NAME, AZURE_AI_PROJECT_NAME, AZURE_CONTAINER_REGISTRY_ENDPOINT)." -ForegroundColor Red
    Write-Host "   Run setup.ps1 first to provision resources." -ForegroundColor Red
    exit 1
}

Write-Host "  Account:  $acctName" -ForegroundColor DarkGray
Write-Host "  Project:  $projName" -ForegroundColor DarkGray
Write-Host "  Registry: $acrEndpoint" -ForegroundColor DarkGray
Write-Host "  ✅ Environment values loaded" -ForegroundColor Green
Write-Host ""

# ============================================================
# 3. Ensure az cognitiveservices extension is installed
# ============================================================
Write-Host "Ensuring az cognitiveservices CLI extension..." -ForegroundColor Yellow
az extension add --name cognitiveservices --upgrade --yes 2>$null
Write-Host "  ✅ cognitiveservices extension ready" -ForegroundColor Green
Write-Host ""

# ============================================================
# 4. Deploy the agent using az cognitiveservices agent create
# ============================================================
Write-Host "Deploying agent '$agentName' to Azure..." -ForegroundColor Yellow
Write-Host "This builds the container from source, pushes to ACR, and creates the hosted agent." -ForegroundColor DarkGray
Write-Host ""

# Delete existing agent if present (for idempotent redeployment)
az cognitiveservices agent show `
    --account-name $acctName `
    --project-name $projName `
    --name $agentName 2>$null >$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Removing existing agent deployment for redeployment..." -ForegroundColor Yellow
    az cognitiveservices agent delete `
        --account-name $acctName `
        --project-name $projName `
        --name $agentName `
        --yes 2>$null
}

az cognitiveservices agent create `
    --account-name $acctName `
    --project-name $projName `
    --name $agentName `
    --source $sourceDir `
    --registry $acrEndpoint `
    --cpu 4 `
    --memory 8Gi `
    --show-logs
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Agent deployment failed." -ForegroundColor Red
    exit 1
}
Write-Host ""
Write-Host "  ✅ Agent deployed to Azure" -ForegroundColor Green
Write-Host ""

# ============================================================
# 5. Print endpoint and playground URLs
# ============================================================
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
if ($projName) {
    Write-Host "  2. Navigate to project: $projName" -ForegroundColor White
} else {
    Write-Host "  2. Navigate to your project" -ForegroundColor White
}
Write-Host "  3. Open the Agents section" -ForegroundColor White
Write-Host "  4. Launch the agent in the Playground" -ForegroundColor White
Write-Host "  5. Try: 'Generate an image of a sunset over mountains'" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
