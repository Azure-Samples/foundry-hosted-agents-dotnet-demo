// Time Zone Agent — A hosted agent that tells the current time in any timezone.
// This demonstrates the core hosted agent pattern using Microsoft Agent Framework.
//
// Key concepts from https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents:
//   1. Function Tool    — GetCurrentDateTime() is a real C# method the model can call server-side
//   2. Hosting Adapter  — RunAIAgentAsync() exposes this agent as an HTTP endpoint on port 8088
//   3. Protocol Translation — The adapter converts Foundry Responses API ↔ Agent Framework automatically
//   4. Agent Identity   — DefaultAzureCredential handles auth locally; managed identity in production

using System.ComponentModel;
using Azure.AI.AgentServer.AgentFramework.Extensions;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Configuration from environment variables
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5-mini";
Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Model: {deploymentName}");

// FUNCTION TOOL — This is the core value of a hosted agent. The model decides WHEN to call
// this method, but the method runs real C# code server-side. No hallucinated results.
[Description("Gets the current date and time for a given IANA timezone")]
static string GetCurrentDateTime(
    [Description("IANA timezone identifier (e.g. America/New_York, Asia/Tokyo, Europe/London)")] string ianaTimezone)
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezone);
    var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
    return $"Current time in {ianaTimezone}: {now:dddd, MMMM dd, yyyy 'at' hh:mm tt zzz}";
}

// Build the chat client pipeline
var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .Build();

// Create the agent with instructions and tools
var agent = new ChatClientAgent(chatClient,
    name: "TimeZoneAgent",
    instructions: """
        You are a helpful assistant that can tell the current date and time in any timezone.
        When asked about the time, use the GetCurrentDateTime tool with the appropriate IANA timezone identifier.
        Be concise and friendly in your responses.
        """,
    tools: [AIFunctionFactory.Create(GetCurrentDateTime)])
    .AsBuilder()
    .Build();

// HOSTING ADAPTER — This single line does the heavy lifting:
// - Starts an HTTP server on port 8088
// - Translates Foundry Responses Protocol ↔ Microsoft Agent Framework
// - Integrates OpenTelemetry for observability (traces, metrics, logs)
// - Makes your local agent compatible with Foundry's managed infrastructure
Console.WriteLine("Time Zone Agent running on http://localhost:8088");
await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
