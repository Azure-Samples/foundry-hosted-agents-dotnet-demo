#!/usr/bin/env pwsh
# setup.ps1 — Automated setup for Data Crunch Agent
# Provisions Azure infrastructure via azd and configures .NET User Secrets.
# Idempotent: safe to run multiple times.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Data Crunch Agent — Setup ===" -ForegroundColor Cyan
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

# ============================================================
# 1b. Check Aspire workload
# ============================================================
Write-Host ""
Write-Host "Checking .NET Aspire workload..." -ForegroundColor Yellow

$workloadOutput = dotnet workload list 2>&1 | Out-String
if ($workloadOutput -match "aspire") {
    Write-Host "  ✅ Aspire workload installed" -ForegroundColor Green
} else {
    Write-Host "  ❌ Aspire workload not found" -ForegroundColor Red
    Write-Host "  Install it with: dotnet workload install aspire" -ForegroundColor Yellow
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
# 4. Provision Azure resources
# ============================================================
Write-Host "Provisioning Azure resources with 'azd provision'..." -ForegroundColor Yellow
Write-Host "This creates the Foundry project, model deployment, ACR, and supporting services." -ForegroundColor DarkGray
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
# 4b. Deploy the gpt-5-mini model
# ============================================================
Write-Host "Deploying gpt-5-mini model to Foundry..." -ForegroundColor Yellow

$provisionEnv = azd env get-values 2>$null
$acctName = $null
$rgName = $null
foreach ($line in $provisionEnv) {
    if ($line -match '^\s*AZURE_AI_ACCOUNT_NAME\s*=\s*"?([^"]*)"?\s*$') { $acctName = $Matches[1] }
    if ($line -match '^\s*AZURE_RESOURCE_GROUP\s*=\s*"?([^"]*)"?\s*$') { $rgName = $Matches[1] }
}

if ($acctName -and $rgName) {
    az cognitiveservices account deployment create `
        --name $acctName `
        --resource-group $rgName `
        --deployment-name gpt-5-mini `
        --model-name gpt-5-mini `
        --model-version 2025-08-07 `
        --model-format OpenAI `
        --sku-capacity 10 `
        --sku-name GlobalStandard
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  Model deployment returned non-zero. The model may already be deployed." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ gpt-5-mini model deployed" -ForegroundColor Green
    }
} else {
    Write-Host "⚠️  Could not determine account name or resource group. Skipping model deployment." -ForegroundColor Yellow
    Write-Host "   Deploy the model manually via Azure Portal or CLI." -ForegroundColor Yellow
}
Write-Host ""

# ============================================================
# 5. Register the agent definition and ensure azure.yaml has services
# ============================================================
$agentYaml = "$PSScriptRoot/src/DataCrunchAgent/agent.yaml"
if (Test-Path $agentYaml) {
    Write-Host "Registering agent definition from $agentYaml..." -ForegroundColor Yellow
    azd ai agent init -m $agentYaml --no-prompt 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  'azd ai agent init' could not run non-interactively." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ Agent definition registered via azd" -ForegroundColor Green
    }

    # Ensure azure.yaml has a services section (required for azd deploy)
    $azureYaml = "$PSScriptRoot/azure.yaml"
    if (Test-Path $azureYaml) {
        $yamlContent = Get-Content $azureYaml -Raw
        if ($yamlContent -notmatch 'services:') {
            Write-Host "Adding services section to azure.yaml..." -ForegroundColor Yellow
            $servicesBlock = @"

services:
  data-crunch-agent:
    project: ./src/DataCrunchAgent
    host: ai.agent
    language: dotnet
    docker:
      path: ./Dockerfile
      context: .
"@
            Add-Content -Path $azureYaml -Value $servicesBlock
            Write-Host "  ✅ Services section added to azure.yaml" -ForegroundColor Green
        } else {
            Write-Host "  ✅ azure.yaml already has services section" -ForegroundColor Green
        }
    }
    # Restore our custom agent.yaml in case azd overwrote it
    Push-Location $PSScriptRoot
    git checkout -- "src/DataCrunchAgent/agent.yaml" 2>$null
    Pop-Location
    Write-Host ""
} else {
    Write-Host "⚠️  $agentYaml not found — skipping agent registration." -ForegroundColor Yellow
    Write-Host "   Run this script again after the agent code is created." -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================
# 6. Set .NET User Secrets from azd environment values
# ============================================================
Write-Host "Configuring .NET User Secrets from azd environment..." -ForegroundColor Yellow

$csprojPath = "$PSScriptRoot/src/DataCrunchAgent/DataCrunchAgent.csproj"
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
Write-Host "  2. dotnet run --project src/DataCrunch.AppHost" -ForegroundColor White
Write-Host "  3. Open the Blazor app at http://localhost:5000" -ForegroundColor White
Write-Host "  4. Upload a CSV and click Analyze" -ForegroundColor White
Write-Host ""
