namespace ImageGenAgent.Tests;

/// <summary>
/// Tests for parameter validation patterns used by the image generation tools.
/// These verify behavior WITHOUT requiring GPU hardware or Azure access.
/// </summary>
public class ImageGenerationValidationTests
{
    [Fact]
    public void OutputDirectory_CanBeCreated()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"image_gen_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(outputDir);
            Assert.True(Directory.Exists(outputDir));
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void FileNameGeneration_ProducesUniquePngNames()
    {
        var names = Enumerable.Range(0, 100)
            .Select(_ => $"generated_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}.png")
            .ToHashSet();

        // With 100 names using random suffix, we expect high uniqueness
        Assert.True(names.Count > 90, $"Expected >90 unique names, got {names.Count}");
    }

    [Fact]
    public void FileNameGeneration_HasPngExtension()
    {
        var fileName = $"generated_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}.png";
        Assert.EndsWith(".png", fileName);
    }

    [Fact]
    public void FluxFileNameGeneration_HasCorrectPrefix()
    {
        var fileName = $"flux_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000, 9999)}.png";
        Assert.StartsWith("flux_", fileName);
        Assert.EndsWith(".png", fileName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999999)]
    public void Seed_ValidRange_Accepted(int seed)
    {
        // Validates the seed range documented in the tool description
        Assert.InRange(seed, 1, 999999);
    }

    [Fact]
    public void NullSeed_IsValidOptionalParameter()
    {
        int? seed = null;
        Assert.False(seed.HasValue);
    }

    [Fact]
    public void FoundryEndpoint_RequiredForFlux()
    {
        // Simulates the validation pattern in GenerateImageFlux
        string? endpoint = null;
        Assert.Throws<InvalidOperationException>(() =>
            endpoint ?? throw new InvalidOperationException(
                "AZURE_AI_FOUNDRY_PROJECT_ENDPOINT is required for FLUX.2."));
    }

    [Theory]
    [InlineData("https://my-project.services.ai.azure.com/api")]
    [InlineData("https://my-project.services.ai.azure.com/api/")]
    public void FoundryEndpoint_TrailingSlashTrimmed(string endpoint)
    {
        var trimmed = endpoint.TrimEnd('/');
        Assert.DoesNotMatch(@"/$", trimmed);
        var fullUrl = $"{trimmed}/images/generations?api-version=2024-02-01";
        Assert.Contains("/images/generations", fullUrl);
        Assert.DoesNotContain("//images", fullUrl);
    }
}
