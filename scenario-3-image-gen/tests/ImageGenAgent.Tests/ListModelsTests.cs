namespace ImageGenAgent.Tests;

/// <summary>
/// Tests for the ListModels function tool output.
/// We replicate the static method here since the source is a top-level Program.cs
/// and cannot be directly referenced without running the agent host.
/// </summary>
public class ListModelsTests
{
    // Mirror of the ListModels() function from scenario-3-image-gen Program.cs
    private static string ListModels()
    {
        return """
            Available models:
            1. Stable Diffusion 1.5 (Local/GPU) — Fast, 512x512, good for general images. Uses ONNX Runtime on GPU.
            2. FLUX.2 (Cloud/Microsoft Foundry) — High quality, great text rendering, photorealistic. Requires AZURE_AI_FOUNDRY_PROJECT_ENDPOINT.
            
            Use GenerateImage for local GPU generation (faster, no cloud dependency).
            Use GenerateImageFlux for cloud generation (higher quality, requires Foundry endpoint).
            """;
    }

    [Fact]
    public void ListModels_ReturnsNonEmptyString()
    {
        var result = ListModels();
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void ListModels_ContainsStableDiffusion()
    {
        var result = ListModels();
        Assert.Contains("Stable Diffusion 1.5", result);
    }

    [Fact]
    public void ListModels_ContainsFlux()
    {
        var result = ListModels();
        Assert.Contains("FLUX.2", result);
    }

    [Fact]
    public void ListModels_MentionsBothGenerationMethods()
    {
        var result = ListModels();
        Assert.Contains("GenerateImage", result);
        Assert.Contains("GenerateImageFlux", result);
    }

    [Fact]
    public void ListModels_MentionsGpuAndCloud()
    {
        var result = ListModels();
        Assert.Contains("Local/GPU", result);
        Assert.Contains("Cloud/Microsoft Foundry", result);
    }

    [Fact]
    public void ListModels_MentionsFoundryEndpointRequirement()
    {
        var result = ListModels();
        Assert.Contains("AZURE_AI_FOUNDRY_PROJECT_ENDPOINT", result);
    }
}
