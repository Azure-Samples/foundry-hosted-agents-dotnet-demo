// Code Metrics Agent — A hosted agent that analyzes source code complexity, line counts, and maintainability.
// Uses Microsoft Agent Framework with Microsoft Foundry hosting adapter.

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
var logger = loggerFactory.CreateLogger("CodeMetricsAgent");

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
    name: "CodeMetricsAgent",
    instructions: """
        You are a code analysis assistant. When given source code, use your tools to analyze complexity,
        count lines, and calculate maintainability. Present results in a clear, organized format with
        actionable recommendations.
        """,
    tools: [
        AIFunctionFactory.Create(ComplexityAnalyzer.AnalyzeComplexity),
        AIFunctionFactory.Create(LineCounter.CountLines),
        AIFunctionFactory.Create(MaintainabilityCalculator.CalculateMaintainability)
    ])
    .AsBuilder()
    .Build();

// Start the hosting adapter — serves the agent as an HTTP endpoint on port 8088
logger.LogInformation("{AgentName} running on {Url}", "CodeMetricsAgent", "http://localhost:8088");
await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
