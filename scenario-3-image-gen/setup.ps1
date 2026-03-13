#!/usr/bin/env pwsh
# setup.ps1 — Automated setup for Image Generator Agent
# Provisions Azure infrastructure via azd and configures .NET User Secrets.
# Idempotent: safe to run multiple times.
# Note: GPU workload profile is required for optimal Stable Diffusion performance.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Image Generator Agent — Setup ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# 1. Check prerequisites
# ============================================================
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

$missing = @()
foreach ($tool in @("azd", "dotnet", "az", "docker")) {
    if (Get-Command $tool -ErrorAction SilentlyContinue) {
        Write-Host "  ✅ $tool" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $tool — not found" -ForegroundColor Red
        $missing += $tool
    }
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing prerequisites: $($missing -join ', ')" -ForegroundColor Red
    Write-Host "Install them before running this script:" -ForegroundColor Red
    if ($missing -contains "azd")    { Write-Host "  azd    → https://aka.ms/install-azd" }
    if ($missing -contains "dotnet") { Write-Host "  dotnet → https://dotnet.microsoft.com/download" }
    if ($missing -contains "az")     { Write-Host "  az     → https://learn.microsoft.com/cli/azure/install-azure-cli" }
    if ($missing -contains "docker") { Write-Host "  docker → https://docs.docker.com/get-docker/" }
    exit 1
}

Write-Host ""

# ============================================================
# 2. Ensure azd ai agent extension is installed
# ============================================================
Write-Host "Ensuring azd AI agent extension is installed..." -ForegroundColor Yellow
azd extension install azure.ai.agents 2>$null
Write-Host "  ✅ azure.ai.agents extension ready" -ForegroundColor Green
Write-Host ""

# ============================================================
# 3. Initialize azd project (if not already done)
# ============================================================
if (-not (Test-Path ".azure")) {
    Write-Host "Initializing azd project with Foundry starter template..." -ForegroundColor Yellow
    Write-Host "You will be prompted to select an environment name, subscription, and location." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ⚠️  azd will warn that the directory is not empty." -ForegroundColor Yellow
    Write-Host "     Select YES to continue — the template files merge with the existing repo." -ForegroundColor Yellow
    Write-Host ""

    azd init -t Azure-Samples/azd-ai-starter-basic
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 'azd init' failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✅ azd project initialized" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "ℹ️  .azure/ folder exists — skipping azd init (already initialized)." -ForegroundColor DarkGray
    Write-Host ""
}

# ============================================================
# 4. Register the agent definition
# ============================================================
$agentYaml = "$PSScriptRoot/src/HostedAgent/agent.yaml"
if (Test-Path $agentYaml) {
    Write-Host "Registering agent definition from $agentYaml..." -ForegroundColor Yellow
    azd ai agent init -m $agentYaml
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  'azd ai agent init' returned non-zero. The agent may already be registered." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ Agent definition registered" -ForegroundColor Green
    }
    Write-Host ""
} else {
    Write-Host "⚠️  $agentYaml not found — skipping agent registration." -ForegroundColor Yellow
    Write-Host "   Run this script again after the agent code is created." -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================
# 5. Provision Azure resources
# ============================================================
Write-Host "Provisioning Azure resources with 'azd provision'..." -ForegroundColor Yellow
Write-Host "This creates the Foundry project, model deployment, ACR, and supporting services." -ForegroundColor DarkGray
Write-Host "Note: GPU workload profile will be configured during deployment." -ForegroundColor DarkGray
Write-Host ""

azd provision
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 'azd provision' failed." -ForegroundColor Red
    exit 1
}
Write-Host ""
Write-Host "  ✅ Azure resources provisioned" -ForegroundColor Green
Write-Host ""

# ============================================================
# 6. Set .NET User Secrets from azd environment values
# ============================================================
Write-Host "Configuring .NET User Secrets from azd environment..." -ForegroundColor Yellow

$csprojPath = "$PSScriptRoot/src/HostedAgent/HostedAgent.csproj"
$envValues = azd env get-values 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Could not read azd environment values. Skipping User Secrets configuration." -ForegroundColor Yellow
} else {
    # Parse azd env output (KEY="VALUE" format)
    $envMap = @{}

    foreach ($line in $envValues) {
        if ($line -match '^\s*([A-Z_][A-Z0-9_]*)\s*=\s*"?([^"]*)"?\s*$') {
            $key = $Matches[1]
            $val = $Matches[2]
            $envMap[$key] = $val
        }
    }

    # Set user secrets for the variables the agent needs
    $keysToWrite = @(
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_DEPLOYMENT_NAME",
        "AZURE_AI_FOUNDRY_PROJECT_ENDPOINT",
        "AZURE_SUBSCRIPTION_ID",
        "AZURE_RESOURCE_GROUP",
        "AZURE_LOCATION"
    )

    $secretsSet = 0
    foreach ($key in $keysToWrite) {
        if ($envMap.ContainsKey($key) -and $envMap[$key]) {
            dotnet user-secrets set --project $csprojPath $key $envMap[$key] > $null 2>&1
            if ($LASTEXITCODE -eq 0) { $secretsSet++ }
        }
    }

    # Also include any other AZURE_ variables not already covered
    foreach ($key in ($envMap.Keys | Sort-Object)) {
        if ($key -like "AZURE_*" -and $key -notin $keysToWrite -and $envMap[$key]) {
            dotnet user-secrets set --project $csprojPath $key $envMap[$key] > $null 2>&1
            if ($LASTEXITCODE -eq 0) { $secretsSet++ }
        }
    }

    if ($secretsSet -gt 0) {
        Write-Host "  ✅ User Secrets configured ($secretsSet values set)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  No environment variables found to set." -ForegroundColor Yellow
    }
}
Write-Host ""

# ============================================================
# 7. Print tenant login command
# ============================================================
$subId = $envMap["AZURE_SUBSCRIPTION_ID"]
if ($subId) {
    try {
        $tenantId = az account show --subscription $subId --query tenantId -o tsv 2>$null
        if ($tenantId) {
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "  Before running the agent, log in to" -ForegroundColor Cyan
            Write-Host "  the correct Azure tenant:" -ForegroundColor Cyan
            Write-Host "" -ForegroundColor Cyan
            Write-Host "  az login --tenant $tenantId" -ForegroundColor White
            Write-Host "========================================" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "⚠️  Could not detect tenant ID. Run 'az login' manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Could not detect subscription. Run 'az login' manually." -ForegroundColor Yellow
}

# ============================================================
# Done
# ============================================================
Write-Host ""
Write-Host "✅ Setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run the az login command shown above" -ForegroundColor White
Write-Host "  2. cd scenario-3-image-gen/src/HostedAgent && dotnet run" -ForegroundColor White
Write-Host "  3. Test with: POST http://localhost:8088/responses" -ForegroundColor White
Write-Host "  4. GPU is required for optimal Stable Diffusion performance" -ForegroundColor DarkGray
Write-Host ""
