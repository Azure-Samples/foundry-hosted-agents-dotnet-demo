namespace DataCrunchAgent.Tests;

public class OutlierDetectorTests
{
    [Fact]
    public void KnownOutlier_DetectedCorrectly()
    {
        // Dataset: 1,2,3,4,5,100 — 100 is a clear outlier
        var result = OutlierDetector.DetectOutliers("1,2,3,4,5,100", "TestCol");

        Assert.Contains("Outliers:    1 found", result);
        Assert.Contains("100.00", result);
        Assert.Contains("above", result);
    }

    [Fact]
    public void NoOutliers_ReportsZero()
    {
        // Tight dataset with no outliers
        var result = OutlierDetector.DetectOutliers("10,11,12,13,14,15", "Tight");

        Assert.Contains("Outliers:    0 found", result);
    }

    [Fact]
    public void TooFewDataPoints_ReturnsNotEnoughMessage()
    {
        var result = OutlierDetector.DetectOutliers("1,2,3", "Small");

        Assert.Contains("Not enough data points (3)", result);
        Assert.Contains("Need at least 4", result);
    }

    [Fact]
    public void ExactlyFourPoints_Works()
    {
        var result = OutlierDetector.DetectOutliers("1,2,3,4", "Four");

        Assert.Contains("Outlier Analysis for 'Four':", result);
        Assert.Contains("Q1:", result);
        Assert.Contains("Q3:", result);
    }

    [Fact]
    public void LowOutlier_DetectedCorrectly()
    {
        // Dataset: -100,10,11,12,13,14 — -100 is a low outlier
        var result = OutlierDetector.DetectOutliers("-100,10,11,12,13,14", "LowTest");

        Assert.Contains("Outliers:    1 found", result);
        Assert.Contains("-100.00", result);
        Assert.Contains("below", result);
    }

    [Fact]
    public void BothHighAndLowOutliers_Detected()
    {
        // Dataset with outliers on both ends
        var result = OutlierDetector.DetectOutliers("-50,1,2,3,4,5,50", "Both");

        Assert.Contains("Outliers:    2 found", result);
    }

    [Fact]
    public void AllSameValues_NoOutliers()
    {
        var result = OutlierDetector.DetectOutliers("5,5,5,5,5", "Same");

        Assert.Contains("Outliers:    0 found", result);
    }

    [Fact]
    public void ColumnName_AppearsInOutput()
    {
        var result = OutlierDetector.DetectOutliers("1,2,3,4,5", "Revenue");

        Assert.Contains("Outlier Analysis for 'Revenue':", result);
    }

    [Fact]
    public void IqrFences_ComputedCorrectly()
    {
        // 1,2,3,4,5: Q1=2, Q3=4, IQR=2, lower=-1, upper=7
        var result = OutlierDetector.DetectOutliers("1,2,3,4,5", "Fences");

        Assert.Contains("IQR:", result);
        Assert.Contains("Lower Fence:", result);
        Assert.Contains("Upper Fence:", result);
    }

    [Fact]
    public void EmptyInput_ReturnsNotEnoughMessage()
    {
        var result = OutlierDetector.DetectOutliers("", "Empty");

        Assert.Contains("Not enough data points (0)", result);
    }

    [Fact]
    public void NonNumericInput_ReturnsNotEnoughMessage()
    {
        var result = OutlierDetector.DetectOutliers("abc,def,ghi", "Bad");

        Assert.Contains("Not enough data points (0)", result);
    }

    [Fact]
    public void ValueAtExactFence_NotAnOutlier()
    {
        // With 1,2,3,4,5: Q1=2, Q3=4, IQR=2, fences=[-1, 7]
        // Adding value 7 (at exact upper fence) — should NOT be an outlier
        var result = OutlierDetector.DetectOutliers("1,2,3,4,5,7", "AtFence");

        Assert.Contains("Outliers:    0 found", result);
    }
}
