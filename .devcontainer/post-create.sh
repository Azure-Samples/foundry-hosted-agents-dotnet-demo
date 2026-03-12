#!/bin/bash
set -e

echo "📦 Installing .NET Aspire workload..."
dotnet workload install aspire

echo "📦 Restoring NuGet packages..."
dotnet restore foundry-hosted-agents-dotnet-demo.slnx

echo "✅ Dev container ready!"
echo ""
echo "Quick start:"
echo "  Scenario 1: cd scenario-1-intro/src/HostedAgent && dotnet run"
echo "  Scenario 2: cd scenario-2-data-crunch && dotnet run --project src/DataCrunch.AppHost"
