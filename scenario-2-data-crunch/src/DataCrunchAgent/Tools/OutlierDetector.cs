using System.ComponentModel;

/// <summary>
/// Detects statistical outliers using the IQR method.
/// </summary>
static class OutlierDetector
{
    [Description("Detects statistical outliers using the IQR method and returns values outside 1.5x the interquartile range")]
    public static string DetectOutliers(
        [Description("Comma-separated numeric values")] string values,
        [Description("Name of the column being analyzed")] string columnName)
    {
        var nums = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => double.TryParse(v, out var d) ? (double?)d : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (nums.Count < 4)
            return $"Not enough data points ({nums.Count}) to detect outliers for column '{columnName}'. Need at least 4.";

        var sorted = nums.OrderBy(v => v).ToList();
        var q1 = StatisticsCalculator.Percentile(sorted, 25);
        var q3 = StatisticsCalculator.Percentile(sorted, 75);
        var iqr = q3 - q1;
        var lowerFence = q1 - 1.5 * iqr;
        var upperFence = q3 + 1.5 * iqr;

        var outliers = nums
            .Select((v, i) => new { Value = v, Index = i })
            .Where(x => x.Value < lowerFence || x.Value > upperFence)
            .ToList();

        var result = $"""
            Outlier Analysis for '{columnName}':
              Q1:          {q1:F2}
              Q3:          {q3:F2}
              IQR:         {iqr:F2}
              Lower Fence: {lowerFence:F2}
              Upper Fence: {upperFence:F2}
              Outliers:    {outliers.Count} found
            """;

        if (outliers.Count > 0)
        {
            var details = string.Join("\n", outliers.Select(o =>
                $"    - Index {o.Index}: {o.Value:F2} ({(o.Value < lowerFence ? "below" : "above")} fence)"));
            result += "\n" + details;
        }

        return result;
    }
}
