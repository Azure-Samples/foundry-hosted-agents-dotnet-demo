// Image Generator Agent — A hosted agent that generates images from text descriptions.
// Uses ElBruno.Text2Image for local GPU (Stable Diffusion) and cloud (FLUX.2) generation.
// Orchestrated by Microsoft Agent Framework with Microsoft Foundry hosting adapter.
//
// This demonstrates GPU-powered hosted agents:
//   - The LLM (gpt-5-mini) orchestrates, but function tools run on GPU hardware
//   - GPU inference is 100-200x faster than CPU for diffusion models
//   - Same hosted agent pattern as scenario-1, but with GPU workload profile
//   - See: https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents

using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.AI.AgentServer.AgentFramework.Extensions;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

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
TokenCredential credential = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
    ? new DefaultAzureCredential()
    : new AzureCliCredential();

Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Model: {deploymentName}");
Console.WriteLine($"Auth: {credential.GetType().Name}");

// FUNCTION TOOLS — Three tools demonstrating GPU-backed server-side computation.
// The model decides which tool to call based on user intent.

[Description("Generates an image from a text description using Stable Diffusion 1.5 on GPU. Returns the file path of the generated image.")]
static async Task<string> GenerateImage(
    [Description("Text description of the image to generate")] string prompt,
    [Description("Optional seed (1-999999) for reproducible results")] int? seed = null)
{
    using var generator = new StableDiffusion15();
    var options = new ElBruno.Text2Image.ImageGenerationOptions();
    if (seed.HasValue) options.Seed = seed.Value;
    var result = await generator.GenerateAsync(prompt, options);

    var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
    Directory.CreateDirectory(outputDir);
    var fileName = $"generated_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}.png";
    var filePath = Path.Combine(outputDir, fileName);
    await result.SaveAsync(filePath);

    return $"Image generated: {fileName} ({result.Width}x{result.Height}, seed: {result.Seed}, time: {result.InferenceTimeMs}ms)";
}

[Description("Generates a high-quality image using FLUX.2 via Microsoft Foundry cloud endpoint")]
async Task<string> GenerateImageFlux(
    [Description("Text description of the image to generate")] string prompt)
{
    var foundryEndpoint = config["AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"]
        ?? throw new InvalidOperationException("AZURE_AI_FOUNDRY_PROJECT_ENDPOINT is required for FLUX.2. Run setup.ps1 or: dotnet user-secrets set AZURE_AI_FOUNDRY_PROJECT_ENDPOINT <your-endpoint>");

    // Call the Microsoft Foundry image generation endpoint
    var token = await credential.GetTokenAsync(
        new Azure.Core.TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), default);

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

    var requestBody = new { prompt, size = "1024x1024", n = 1 };
    var response = await httpClient.PostAsJsonAsync(
        $"{foundryEndpoint.TrimEnd('/')}/images/generations?api-version=2024-02-01", requestBody);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    var imageUrl = json.GetProperty("data")[0].GetProperty("url").GetString()!;

    // Download and save the generated image
    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
    var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
    Directory.CreateDirectory(outputDir);
    var fileName = $"flux_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}.png";
    var filePath = Path.Combine(outputDir, fileName);
    await File.WriteAllBytesAsync(filePath, imageBytes);

    return $"FLUX.2 image generated: {fileName} (1024x1024). Higher quality with better text rendering.";
}

[Description("Lists available image generation models and their capabilities")]
static string ListModels()
{
    return """
        Available models:
        1. Stable Diffusion 1.5 (Local/GPU) — Fast, 512x512, good for general images. Uses ONNX Runtime on GPU.
        2. FLUX.2 (Cloud/Microsoft Foundry) — High quality, great text rendering, photorealistic. Requires AZURE_AI_FOUNDRY_PROJECT_ENDPOINT.
        
        Use GenerateImage for local GPU generation (faster, no cloud dependency).
        Use GenerateImageFlux for cloud generation (higher quality, requires Foundry endpoint).
        """;
}

// Build the chat client pipeline
var chatClient = new AzureOpenAIClient(new Uri(endpoint), credential)
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .Build();

// Create the agent with instructions and tools
var agent = new ChatClientAgent(chatClient,
    name: "ImageGeneratorAgent",
    instructions: """
        You are a creative assistant that generates images from text descriptions.
        You can create images using local GPU-accelerated Stable Diffusion or cloud-based FLUX.2.
        When asked to generate an image, use GenerateImage for quick local generation or GenerateImageFlux for higher quality cloud generation.
        Always describe what was generated in the response. Be creative and helpful.
        If the user asks about available models, use ListModels.
        """,
    tools: [
        AIFunctionFactory.Create(GenerateImage),
        AIFunctionFactory.Create(GenerateImageFlux),
        AIFunctionFactory.Create(ListModels)])
    .AsBuilder()
    .Build();

// HOSTING ADAPTER — Same pattern as scenario-1, serves the agent on port 8088.
// When deployed to Azure, this runs in a GPU workload profile container.
Console.WriteLine("Image Generator Agent running on http://localhost:8088");
await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
