namespace DataCrunchAgent.Tests;

public class StatisticsCalculatorTests
{
    [Fact]
    public void KnownValues_ComputesCorrectStatistics()
    {
        // 1, 2, 3, 4, 5 — well-known stats
        var result = StatisticsCalculator.ComputeStatistics("1,2,3,4,5", "TestCol");

        Assert.Contains("Count:    5", result);
        Assert.Contains("Min:      1.00", result);
        Assert.Contains("Max:      5.00", result);
        Assert.Contains("Mean:     3.00", result);
        Assert.Contains("Median:   3.00", result);
        Assert.Contains("P25:      2.00", result);
        Assert.Contains("P75:      4.00", result);
    }

    [Fact]
    public void KnownValues_StdDevIsCorrect()
    {
        // Population std dev of 1,2,3,4,5 = sqrt(2) ≈ 1.41
        var result = StatisticsCalculator.ComputeStatistics("1,2,3,4,5", "TestCol");

        Assert.Contains("Std Dev:  1.41", result);
    }

    [Fact]
    public void SingleValue_AllStatsEqualThatValue()
    {
        var result = StatisticsCalculator.ComputeStatistics("42", "Single");

        Assert.Contains("Count:    1", result);
        Assert.Contains("Min:      42.00", result);
        Assert.Contains("Max:      42.00", result);
        Assert.Contains("Mean:     42.00", result);
        Assert.Contains("Median:   42.00", result);
        Assert.Contains("Std Dev:  0.00", result);
    }

    [Fact]
    public void AllSameValues_StdDevIsZero()
    {
        var result = StatisticsCalculator.ComputeStatistics("10,10,10,10", "Same");

        Assert.Contains("Mean:     10.00", result);
        Assert.Contains("Std Dev:  0.00", result);
        Assert.Contains("P25:      10.00", result);
        Assert.Contains("P75:      10.00", result);
    }

    [Fact]
    public void EmptyInput_ReturnsError()
    {
        var result = StatisticsCalculator.ComputeStatistics("", "Empty");

        Assert.Contains("Error: No valid numeric values", result);
    }

    [Fact]
    public void NonNumericValues_ReturnsError()
    {
        var result = StatisticsCalculator.ComputeStatistics("abc,def,ghi", "Bad");

        Assert.Contains("Error: No valid numeric values", result);
    }

    [Fact]
    public void MixedValidAndInvalid_IgnoresInvalidValues()
    {
        var result = StatisticsCalculator.ComputeStatistics("1,abc,3,def,5", "Mixed");

        Assert.Contains("Count:    3", result);
        Assert.Contains("Mean:     3.00", result);
    }

    [Fact]
    public void NegativeValues_HandledCorrectly()
    {
        var result = StatisticsCalculator.ComputeStatistics("-10,-5,0,5,10", "Neg");

        Assert.Contains("Min:      -10.00", result);
        Assert.Contains("Max:      10.00", result);
        Assert.Contains("Mean:     0.00", result);
    }

    [Fact]
    public void DecimalValues_HandledCorrectly()
    {
        var result = StatisticsCalculator.ComputeStatistics("1.5,2.5,3.5", "Dec");

        Assert.Contains("Mean:     2.50", result);
        Assert.Contains("Median:   2.50", result);
    }

    [Fact]
    public void ColumnName_AppearsInOutput()
    {
        var result = StatisticsCalculator.ComputeStatistics("1,2,3", "Revenue");

        Assert.Contains("Statistics for 'Revenue':", result);
    }

    [Fact]
    public void EvenNumberOfValues_MedianIsInterpolated()
    {
        // Median of 1,2,3,4 = 2.5
        var result = StatisticsCalculator.ComputeStatistics("1,2,3,4", "Even");

        Assert.Contains("Median:   2.50", result);
    }

    // Direct tests for the Percentile helper
    [Fact]
    public void Percentile_SingleElement_ReturnsThatElement()
    {
        var sorted = new List<double> { 7.0 };
        Assert.Equal(7.0, StatisticsCalculator.Percentile(sorted, 50));
    }

    [Fact]
    public void Percentile_P0_ReturnsMin()
    {
        var sorted = new List<double> { 1, 2, 3, 4, 5 };
        Assert.Equal(1.0, StatisticsCalculator.Percentile(sorted, 0));
    }

    [Fact]
    public void Percentile_P100_ReturnsMax()
    {
        var sorted = new List<double> { 1, 2, 3, 4, 5 };
        Assert.Equal(5.0, StatisticsCalculator.Percentile(sorted, 100));
    }

    [Fact]
    public void Percentile_P50_ReturnsMedian()
    {
        var sorted = new List<double> { 1, 2, 3, 4, 5 };
        Assert.Equal(3.0, StatisticsCalculator.Percentile(sorted, 50));
    }
}
