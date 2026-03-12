using System.ComponentModel;

/// <summary>
/// Computes descriptive statistics for a numeric column.
/// </summary>
static class StatisticsCalculator
{
    [Description("Computes descriptive statistics for a numeric column: count, min, max, mean, median, standard deviation, P25, P75")]
    public static string ComputeStatistics(
        [Description("Comma-separated numeric values")] string values,
        [Description("Name of the column being analyzed")] string columnName)
    {
        var nums = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => double.TryParse(v, out var d) ? (double?)d : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (nums.Count == 0)
            return $"Error: No valid numeric values found for column '{columnName}'.";

        nums.Sort();

        var count = nums.Count;
        var min = nums[0];
        var max = nums[^1];
        var mean = nums.Average();
        var median = Percentile(nums, 50);
        var p25 = Percentile(nums, 25);
        var p75 = Percentile(nums, 75);

        var variance = nums.Sum(v => (v - mean) * (v - mean)) / count;
        var stdev = Math.Sqrt(variance);

        return $"""
            Statistics for '{columnName}':
              Count:    {count}
              Min:      {min:F2}
              Max:      {max:F2}
              Mean:     {mean:F2}
              Median:   {median:F2}
              Std Dev:  {stdev:F2}
              P25:      {p25:F2}
              P75:      {p75:F2}
            """;
    }

    internal static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 1) return sorted[0];
        var rank = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(rank);
        var upper = Math.Min(lower + 1, sorted.Count - 1);
        var weight = rank - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
}
