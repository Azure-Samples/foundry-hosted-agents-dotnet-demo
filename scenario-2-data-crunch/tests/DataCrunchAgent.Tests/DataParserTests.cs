namespace DataCrunchAgent.Tests;

public class DataParserTests
{
    [Fact]
    public void EmptyInput_ReturnsError()
    {
        var result = DataParser.ParseData("");
        Assert.Equal("Error: No data provided.", result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsError()
    {
        var result = DataParser.ParseData("   \t  ");
        Assert.Equal("Error: No data provided.", result);
    }

    [Fact]
    public void NullInput_ReturnsError()
    {
        var result = DataParser.ParseData(null!);
        Assert.Equal("Error: No data provided.", result);
    }

    [Fact]
    public void HeaderOnly_ReturnsZeroRows()
    {
        var result = DataParser.ParseData("Name,Age,City");

        Assert.Contains("Columns (3): Name, Age, City", result);
        Assert.Contains("Row count: 0", result);
    }

    [Fact]
    public void SingleRow_ParsesCorrectly()
    {
        var csv = "Name,Age\nAlice,30";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Columns (2): Name, Age", result);
        Assert.Contains("Row count: 1", result);
        Assert.Contains("\"Name\": \"Alice\"", result);
        Assert.Contains("\"Age\": \"30\"", result);
    }

    [Fact]
    public void MultipleRows_ParsesCorrectly()
    {
        var csv = "Name,Age,City\nAlice,30,NYC\nBob,25,LA\nCharlie,35,Chicago";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Columns (3): Name, Age, City", result);
        Assert.Contains("Row count: 3", result);
        Assert.Contains("Preview (first 3 rows)", result);
    }

    [Fact]
    public void MoreThanFiveRows_PreviewsOnlyFive()
    {
        var csv = "Val\n1\n2\n3\n4\n5\n6\n7";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Row count: 7", result);
        Assert.Contains("Preview (first 5 rows)", result);
        // Row "6" and "7" should not appear in the preview section
        Assert.DoesNotContain("\"Val\": \"6\"", result);
        Assert.DoesNotContain("\"Val\": \"7\"", result);
    }

    [Fact]
    public void MissingCells_HandledGracefully()
    {
        var csv = "A,B,C\n1,2\n3";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Columns (3): A, B, C", result);
        Assert.Contains("Row count: 2", result);
        // Missing cells should produce empty strings
        Assert.Contains("\"C\": \"\"", result);
    }

    [Fact]
    public void WindowsLineEndings_ParsedCorrectly()
    {
        var csv = "Name,Age\r\nAlice,30\r\nBob,25";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Row count: 2", result);
        Assert.Contains("\"Name\": \"Alice\"", result);
    }

    [Fact]
    public void WhitespaceInHeaders_Trimmed()
    {
        var csv = " Name , Age \nAlice,30";
        var result = DataParser.ParseData(csv);

        Assert.Contains("Columns (2): Name, Age", result);
    }
}
