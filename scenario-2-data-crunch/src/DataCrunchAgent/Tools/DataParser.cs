using System.ComponentModel;

/// <summary>
/// Parses raw CSV data into a structured summary.
/// </summary>
static class DataParser
{
    [Description("Parses CSV data and returns a structured summary with column names, row count, and data preview")]
    public static string ParseData(
        [Description("Raw CSV data with headers in first row")] string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData))
            return "Error: No data provided.";

        var lines = rawData
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();

        if (lines.Length == 0)
            return "Error: No data provided.";

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        var dataRows = lines.Skip(1).ToArray();

        var preview = dataRows.Take(5).Select(row =>
        {
            var cells = row.Split(',');
            var pairs = headers.Select((h, i) => $"    \"{h}\": \"{(i < cells.Length ? cells[i].Trim() : "")}\"");
            return "  { " + string.Join(", ", pairs) + " }";
        });

        return $"""
            Parsed CSV Summary:
            - Columns ({headers.Length}): {string.Join(", ", headers)}
            - Row count: {dataRows.Length}
            - Preview (first {Math.Min(5, dataRows.Length)} rows):
            [
            {string.Join(",\n", preview)}
            ]
            """;
    }
}
