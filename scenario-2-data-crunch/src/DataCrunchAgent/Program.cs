// Data Crunch Agent — A hosted agent that analyzes CSV data with statistical tools.
// Uses Microsoft Agent Framework with Microsoft Foundry hosting adapter.

using System.ComponentModel;
using Azure.AI.AgentServer.AgentFramework.Extensions;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Structured logging for the agent process
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("DataCrunchAgent");

// Configuration from User Secrets (local dev) + environment variables (deployed container)
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set. Run setup.ps1 or: dotnet user-secrets set AZURE_OPENAI_ENDPOINT <your-endpoint>");
var deploymentName = config["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-5-mini";

// Credential: AzureCliCredential for local dev (respects az login --tenant),
// DefaultAzureCredential in containers (uses managed identity).
TokenCredential credential;
try
{
    credential = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
        ? new DefaultAzureCredential()
        : new AzureCliCredential();
}
catch (AuthenticationFailedException ex)
{
    logger.LogCritical(ex, "Authentication failed. Run: az login --tenant <your-tenant-id>");
    throw;
}

logger.LogInformation("Endpoint: {Endpoint}", endpoint);
logger.LogInformation("Model: {Model}", deploymentName);
logger.LogInformation("Auth: {CredentialType}", credential.GetType().Name);

// Build the chat client pipeline
var chatClient = new AzureOpenAIClient(new Uri(endpoint), credential)
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .Build();

// Create the agent with instructions and tools
var agent = new ChatClientAgent(chatClient,
    name: "DataCrunchAgent",
    instructions: """
        You are a data analysis assistant. When given CSV data, use your tools to:
        1. First parse the data to understand its structure using ParseData
        2. Compute statistics for each numeric column using ComputeStatistics
        3. Detect outliers in numeric columns using DetectOutliers
        Present results clearly. Highlight any anomalies or interesting patterns.
        Format numbers to 2 decimal places in your narrative.
        """,
    tools: [
        AIFunctionFactory.Create(DataParser.ParseData),
        AIFunctionFactory.Create(StatisticsCalculator.ComputeStatistics),
        AIFunctionFactory.Create(OutlierDetector.DetectOutliers)
    ])
    .AsBuilder()
    .Build();

// Start the hosting adapter — serves the agent as an HTTP endpoint on port 8088
logger.LogInformation("{AgentName} running on {Url}", "DataCrunchAgent", "http://localhost:8088");
await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
