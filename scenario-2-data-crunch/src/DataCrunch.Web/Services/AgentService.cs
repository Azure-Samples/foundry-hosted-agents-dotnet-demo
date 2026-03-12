using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCrunch.Web.Services;

public class AgentService(HttpClient httpClient, ILogger<AgentService> logger, IConfiguration config)
{
    public string CurrentMode => config["AgentMode"] ?? "Local";

    public async Task<AgentResponse> AnalyzeAsync(string csvContent)
    {
        var prompt = $"""
            Analyze this CSV data. Parse it first, then compute statistics for each numeric column, and detect any outliers.
            Provide a clear summary of findings.

            CSV Data:
            {csvContent}
            """;

        var payload = new { input = prompt };

        logger.LogInformation("Sending CSV ({Length} chars) to agent in {Mode} mode", csvContent.Length, CurrentMode);

        var response = await httpClient.PostAsJsonAsync("/responses", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        logger.LogInformation("Agent response received ({Length} chars)", json.Length);

        // The agent returns a Responses protocol response
        // Parse the output text from the response
        try
        {
            var agentResponse = JsonSerializer.Deserialize<ResponsesApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var outputText = agentResponse?.Output?
                .Where(o => o.Type == "message" && o.Role == "assistant")
                .SelectMany(o => o.Content ?? [])
                .Where(c => c.Type == "output_text")
                .Select(c => c.Text)
                .FirstOrDefault() ?? json;

            return new AgentResponse { Analysis = outputText, RawJson = json };
        }
        catch
        {
            // If parsing fails, return raw response
            return new AgentResponse { Analysis = json, RawJson = json };
        }
    }
}

public class AgentResponse
{
    public string Analysis { get; set; } = "";
    public string RawJson { get; set; } = "";
}

// Models for the Responses API format
public class ResponsesApiResponse
{
    public List<ResponseOutput>? Output { get; set; }
}

public class ResponseOutput
{
    public string Type { get; set; } = "";
    public string? Role { get; set; }
    public List<ResponseContent>? Content { get; set; }
}

public class ResponseContent
{
    public string Type { get; set; } = "";
    public string? Text { get; set; }
}
